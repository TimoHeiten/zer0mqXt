using heitech.zer0mqXt.core.infrastructure;
using heitech.zer0mqXt.core.transport;
using NetMQ;
using NetMQ.Sockets;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace heitech.zer0mqXt.core.patterns
{
    public sealed class Socket : IDisposable
    {
        private readonly SocketConfiguration _configuration;
        public Socket(SocketConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<XtResult<TResult>> RequestAsync<T, TResult>(T request)
            where T : class, new()
            where TResult : class, new()
        {
            try
            {
                return await DoRequestAsync<T, TResult>(request);
            }
            catch (NetMQ.EndpointNotFoundException ntfnd)
            {
                _configuration.Logger.Log(new ErrorLogMsg($"NetMQ.Endpoint could not be found at {_configuration.Address()}: " + ntfnd.Message));
                await Task.Delay((int)_configuration.TimeOut.TotalMilliseconds);
                try
                {
                    return await DoRequestAsync<T, TResult>(request);
                }
                catch (System.Exception inner)
                {
                    _configuration.Logger.Log(new ErrorLogMsg("Request failed after Retry: " + inner.Message));
                    return XtResult<TResult>.Failed(inner);
                }
            }
            catch (System.Exception ex)
            {
                _configuration.Logger.Log(new ErrorLogMsg("Request failed: " + ex.Message));
                return XtResult<TResult>.Failed(ex);
            }
        }

        private async Task<XtResult<TResult>> DoRequestAsync<T, TResult>(T request)
            where T : class, new()
            where TResult : class
        {
            const string operation = "request";

            _configuration.Logger.Log(new DebugLogMsg($"Send Request<{typeof(T)}, {typeof(TResult)}> to Address - {_configuration.Address()}"));
            using var rqSocket = new RequestSocket();
            rqSocket.Connect(_configuration.Address());

            var message = new RequestReplyMessage<T>(_configuration, request);

            return await Task.Run(() => 
            {
                // the request to be send with timeout
                bool rqDidNotTimeOut = rqSocket.TrySendMultipartMessage(_configuration.TimeOut, message);
                if (!rqDidNotTimeOut)
                    return XtResult<TResult>.Failed(new TimeoutException($"Request<{typeof(T)}, {typeof(TResult)}> timed out"), operation);

                // wait for the response with timeout
                var response = new NetMQMessage();
                bool noTimeOut = rqSocket.TryReceiveMultipartMessage(_configuration.TimeOut, ref response, expectedFrameCount: 3);
                if (!noTimeOut)
                    return XtResult<TResult>.Failed(new TimeoutException($"Request<{typeof(T)}, {typeof(TResult)}> timed out"), operation);

                // parse the response and return the result
                var xtResult = response.ParseRqRepMessage<TResult>(_configuration);

                return xtResult.IsSuccess
                        ? XtResult<TResult>.Success(xtResult.GetResult(), operation)
                        : XtResult<TResult>.Failed(xtResult.Exception, operation);
            });
        }

        private ManualResetEvent eventHandle;
        private bool killSwitch = false;

        ///<summary>
        /// Register Callback on the Respond Action at the server, can also register a callback to control how long the server will be running in the background
        /// <para>Each type and each method call register a single thread for this type according to the configuration</para>
        ///</summary>
        public void Respond<T, TResult>(Func<T, TResult> factory, Func<bool> stillBlocking = null)
            where T : class, new()
            where TResult : class
        {
            // handle notifies when the server is set up
            eventHandle = new ManualResetEvent(false);
            // create a new background thread with the response callback
            Task.Run(() => 
            {
                using var rsSocket = new ResponseSocket();
                rsSocket.Bind(_configuration.Address());

                // open resetevent after binding to the socket, and block after that
                eventHandle.Set();

                while (stillBlocking == null ? !killSwitch : stillBlocking())
                {
                    Message<TResult> response = null;
                    try
                    {
                        // block on this thread for incoming requests of the Type
                        NetMQMessage incomingRequest = rsSocket.ReceiveMultipartMessage();
                        _configuration.Logger.Log(new DebugLogMsg($"handling response for [Request:{typeof(T)}] and [Response:{typeof(TResult)}]"));

                        var actualRequestResult = incomingRequest.ParseRqRepMessage<T>(_configuration);
                        TResult result = factory(actualRequestResult.IsSuccess ? actualRequestResult.GetResult() : new T());

                        response = new RequestReplyMessage<TResult>(_configuration, result, actualRequestResult.IsSuccess);
                    }
                    catch (System.Exception ex)
                    {
                        // failure to parse or any other exception leads to a non successful response, which then in turn can be handled on the request side
                        // todo exception propagation?
                        _configuration.Logger.Log(new ErrorLogMsg($"Responding to [Request:{typeof(T)}] with [Response:{typeof(TResult)}] did fail: " + ex.Message));
                        response = new RequestReplyMessage<TResult>(_configuration, default(TResult), success: false);
                    }
                    // try send response with timeout
                    bool noTimeout = rsSocket.TrySendMultipartMessage(_configuration.TimeOut, response);
                    if (!noTimeout)
                        _configuration.Logger.Log(new ErrorLogMsg($"Responding to [Request:{typeof(T)}] with [Response:{typeof(TResult)}] timed-out after {_configuration.TimeOut}"));
                }
            });
            // wait for the Set inside the background thread
            eventHandle.WaitOne();
        }

        #region Dispose
        private bool disposedValue;

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    killSwitch = true;
                    NetMQConfig.Cleanup();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}