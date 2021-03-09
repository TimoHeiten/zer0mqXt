using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace heitech.zer0mqXt.core
{
    public class Socket : IDisposable
    {
        private readonly SocketConfiguration _configuration;
        public Socket(SocketConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<XtResult<TResult>> RequestAsync<T, TResult>(T request)
            where T : class, new()
            where TResult : class
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
            _configuration.Logger.Log(new DebugLogMsg($"Send Request<{typeof(T)}, {typeof(TResult)}> to Address - {_configuration.Address()}"));
            using var rqSocket = new RequestSocket();
            rqSocket.Connect(_configuration.Address());

            var message = new Message<T>(_configuration, request);

            return await Task.Run(() => 
            {
                // todo try send here (response server could be blocked)
                rqSocket.SendMoreFrame(message.RequestTypeFrame)
                        .SendMoreFrame(message.Success)
                        .SendFrame(message.Payload);

                var payload = new List<byte[]>();
                bool noTimeOut = rqSocket.TryReceiveMultipartBytes(_configuration.TimeOut, ref payload, expectedFrameCount: 3);

                if (!noTimeOut)
                    return XtResult<TResult>.Failed(new TimeoutException($"Request<{typeof(T)}, {typeof(TResult)}> timed out"));

                var xtResult = Message<TResult>.ParseMessage(_configuration, payload);

                return xtResult.IsSuccess
                        ? XtResult<TResult>.Success(xtResult.GetResult().Content)
                        : XtResult<TResult>.Failed(xtResult.Exception);
            });
        }


        private ManualResetEvent eventHandle;
        private bool killSwitch = false;


        ///<summary>
        /// Register Callback on the Respond Action at the server, can also register a callback to control how long the server will be running in the background
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
                    Message<TResult> message = null;
                    try
                    {
                        List<byte[]> bytes = rsSocket.ReceiveMultipartBytes(3);
                        _configuration.Logger.Log(new DebugLogMsg($"handling response for [Request:{typeof(T)}] and [Response:{typeof(TResult)}]"));
                        var xtResult = Message<T>.ParseMessage(_configuration, bytes);

                        TResult content = factory(xtResult.IsSuccess ? xtResult.GetResult().Content : new T());
                        message = new Message<TResult>(_configuration, content, success: xtResult.IsSuccess);
                    }
                    catch (System.Exception ex)
                    {
                        _configuration.Logger.Log(new ErrorLogMsg($"Responding to [Request:{typeof(T)}] with [Response:{typeof(TResult)}] did fail: " + ex.Message));
                        // failure to parse or any other exception leads to a non successful response, which will be handled on the request side
                        message = new Message<TResult>(_configuration, default(TResult), success: false);
                    }
                    // todo use try send here, else get back to loop
                    rsSocket.SendMoreFrame(message.RequestTypeFrame)
                            .SendMoreFrame(message.Success)
                            .SendFrame(message.Payload);
                }
            });
            // wait for the Set inside the background thread
            eventHandle.WaitOne();
        }

        #region Dispose
        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
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