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

        #region Request / Client
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

                _configuration.Logger.Log(new DebugLogMsg($"successfully sent [Request:{typeof(T)}] and waiting for response [Response:{typeof(TResult)}]"));
                // wait for the response with timeout
                var response = new NetMQMessage();
                bool noTimeOut = rqSocket.TryReceiveMultipartMessage(_configuration.TimeOut, ref response, expectedFrameCount: 3);
                if (!noTimeOut)
                    return XtResult<TResult>.Failed(new TimeoutException($"Request<{typeof(T)}, {typeof(TResult)}> timed out"), operation);
                
                _configuration.Logger.Log(new DebugLogMsg($"received Response [Response:{typeof(TResult)}]"));

                // parse the response and return the result
                var xtResult = response.ParseRqRepMessage<TResult>(_configuration);

                return xtResult.IsSuccess
                        ? XtResult<TResult>.Success(xtResult.GetResult(), operation)
                        : XtResult<TResult>.Failed(xtResult.Exception, operation);
            });
        }
        #endregion


        #region Response / Server
        private ManualResetEvent eventHandle;
        private ResponseSocket responseSocket;
        private NetMQ.NetMQPoller poller;
        private EventHandler<NetMQSocketEventArgs> receiveHandler;
        private readonly object concurrencyToken = new object();
        private bool responderIsSetup = false;
        private bool respondingIsActive = false;

        ///<summary>
        /// Register async Callback on the Respond Action at the server
        ///</summary>
        public void RespondAsync<T, TResult>(Func<T, Task<TResult>> factory, CancellationToken cancellationToken = default)
            where T : class, new()
            where TResult : class
        {
            var responseHandler = ResponseHandler<T, TResult>.ASync(factory);
            SetupResponder(responseHandler, cancellationToken);
        }

        ///<summary>
        /// Register sync Callback on the Respond Action at the server
        ///</summary>
        public void Respond<T, TResult>(Func<T, TResult> factory, CancellationToken cancellationToken = default)
            where T : class, new()
            where TResult : class
        {
            var responseHandler = ResponseHandler<T, TResult>.Sync(factory);
            SetupResponder(responseHandler, cancellationToken);
        }

        ///<summary>
        /// necessary indirection for the responsehandler to be used in sync or async fashion
        ///</summary>
        private void SetupResponder<T, TResult>(ResponseHandler<T, TResult> handler, CancellationToken token)
            where T : class, new()
            where TResult : class
        {
            lock (concurrencyToken)
            {
                if (responderIsSetup)
                {
                    throw new ZeroMqXtSocketException("Responder for this instance of Socket exists. Use a new instance for each server");
                }
                responderIsSetup = true;
                respondingIsActive = true;

            }

            poller = new NetMQ.NetMQPoller();
            responseSocket = new ResponseSocket();
            // handle notifies when the server is set up
            eventHandle = new ManualResetEvent(false);
            // create a new background thread with the response callback
            
            Task.Run(() => 
            {
                try
                {
                    responseSocket.Bind(_configuration.Address());

                    // add to poller and register handler
                    poller.Add(responseSocket);
                    receiveHandler = async (s, e) => await ResponseHandlerCallback(responseSocket, handler, token);
                    responseSocket.ReceiveReady += receiveHandler;

                    // poller blocks, so it has to be started after the eventhandle is set
                    poller.RunAsync();
                    
                    // open resetevent after binding to the socket and when the poller is started
                    eventHandle.Set();
                }
                catch (Exception exception)
                {
                    _configuration.Logger.Log(new ErrorLogMsg(exception.Message));
                    Dispose();
                }
            }, token);

            // wait for the Set inside the background thread so we can know at the calling client that the server is set up properly
            eventHandle.WaitOne();
        }

        ///<summary>
        /// indirection for Response handling either asynchronous or synchronous
        ///</summary>
        private class ResponseHandler<T, TResult>
            where T : class, new()
            where TResult : class
        {
            private readonly Func<T, TResult> syncCallback;
            private readonly Func<T, Task<TResult>> asyncCallback;

            private ResponseHandler(Func<T, TResult> syncCallback=null, Func<T, Task<TResult>> asyncCallback=null)
            {
                this.syncCallback = syncCallback;
                this.asyncCallback = asyncCallback;
            }

            internal static ResponseHandler<T, TResult> Sync(Func<T, TResult> syncCallback) 
                => new ResponseHandler<T, TResult>(syncCallback: syncCallback);

            internal static ResponseHandler<T, TResult> ASync(Func<T, Task<TResult>> asyncCallback) 
                => new ResponseHandler<T, TResult>(asyncCallback: asyncCallback);

            public async Task<TResult> HandleAsync(XtResult<T> incomingRequest)
            {
                T request = incomingRequest.IsSuccess ? incomingRequest.GetResult() : new T();
                if (asyncCallback is null)
                    return syncCallback(request);
                else
                    return await asyncCallback(request);
            }
        }

        ///<summary>
        /// actual response handling including the response handler wrapper for sync or async callback
        ///</summary>
        private async Task ResponseHandlerCallback<T, TResult>(ResponseSocket socket, ResponseHandler<T, TResult> handler, CancellationToken token)
            where T : class, new()
            where TResult : class
        {
            if (!respondingIsActive || token.IsCancellationRequested)
            {
                Dispose();
                _configuration.Logger.Log(new InfoLogMsg("Task was cancelled by cancellationRequest"));
                return;
            }

            try
            {
                Message<TResult> response = null;
                try
                {
                    // block on this thread for incoming requests of the type T (Request)
                    NetMQMessage incomingRequest = socket.ReceiveMultipartMessage();
                    _configuration.Logger.Log(new DebugLogMsg($"handling response for [Request:{typeof(T)}] and [Response:{typeof(TResult)}]"));

                    var actualRequestResult = incomingRequest.ParseRqRepMessage<T>(_configuration);
                    TResult result = await handler.HandleAsync(actualRequestResult);

                    response = new RequestReplyMessage<TResult>(_configuration, result, actualRequestResult.IsSuccess);
                    _configuration.Logger.Log(new DebugLogMsg($"sending response for [Request:{typeof(T)}] and [Response:{typeof(TResult)}]"));
                }
                catch (System.Exception ex)
                {
                    // failure to parse or any other exception leads to a non successful response, which then in turn can be handled on the request side
                    _configuration.Logger.Log(new ErrorLogMsg($"Responding to [Request:{typeof(T)}] with [Response:{typeof(TResult)}] did fail: " + ex.Message));
                    response = new RequestReplyMessage<TResult>(_configuration, default(TResult), success: false);
                }

                // try send response with timeout
                bool noTimeout = socket.TrySendMultipartMessage(_configuration.TimeOut, response);
                if (!noTimeout)
                    _configuration.Logger.Log(new ErrorLogMsg($"Responding to [Request:{typeof(T)}] with [Response:{typeof(TResult)}] timed-out after {_configuration.TimeOut}"));
            }
            catch (NetMQ.TerminatingException terminating)
            {
                _configuration.Logger.Log(new ErrorLogMsg($"repsonseHandler failed with terminating exception: [{terminating.Message}]"));
                Dispose();
            }
        }
        #endregion

        #region Dispose
        private bool disposedValue;

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    respondingIsActive = false;
                    poller?.Stop();
                    if (responseSocket != null && receiveHandler != null)
                        responseSocket.ReceiveReady -= receiveHandler;
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