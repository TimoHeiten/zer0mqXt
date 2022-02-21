using System;
using System.Threading;
using System.Threading.Tasks;
using heitech.zer0mqXt.core.infrastructure;
using heitech.zer0mqXt.core.transport;
using NetMQ;
using NetMQ.Sockets;

namespace heitech.zer0mqXt.core.patterns.RqRp
{
    ///<summary>
    /// indirection for Response handling either asynchronous or synchronous
    ///</summary>
    public class ResponseHandler<T, TResult>
        where T : class, new()
        where TResult : class
    {
        private readonly Func<T, TResult> _syncCallback;
        private readonly SocketConfiguration _configuration;
        private readonly Func<T, Task<TResult>> _asyncCallback;
        private Func<Task<TResult>> onError = () => Task.FromResult(default(TResult));


        private ResponseHandler(SocketConfiguration configuration, Func<T, TResult> syncCallback = null, Func<T, Task<TResult>> asyncCallback = null)
        {
            _syncCallback = syncCallback;
            _asyncCallback = asyncCallback;
            _configuration = configuration;
        }

        internal void SetOnError(Func<Task<TResult>> onError)
        {
            if (onError != null)
                this.onError = onError;
        }

        internal void SetOnError(Func<TResult> onError)
        {
            if (onError != null)
                this.onError = () =>  Task.FromResult(onError());
        }

        internal static ResponseHandler<T, TResult> Sync(SocketConfiguration config, Func<T, TResult> syncCallback)
            => new ResponseHandler<T, TResult>(config, syncCallback: syncCallback);

        internal static ResponseHandler<T, TResult> ASync(SocketConfiguration config, Func<T, Task<TResult>> asyncCallback)
            => new ResponseHandler<T, TResult>(config, asyncCallback: asyncCallback);

        public async Task<TResult> HandleAsync(XtResult<T> incomingRequest)
        {
            if (!incomingRequest.IsSuccess)
                return await onError();

            T request = incomingRequest.GetResult(); 
            if (_asyncCallback is null)
                return _syncCallback(request);
            else
                return await _asyncCallback(request).ConfigureAwait(false);
        }

        ///<summary>
        /// actual response handling including the response handler wrapper for sync or async callback
        ///</summary>
        internal async Task ResponseHandlerCallback(ResponseSocket socket, ResponseHandler<T, TResult> handler, Action kill, CancellationToken token)
        {
            if (token.IsCancellationRequested)
            {
                kill();
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
                    TResult result = await handler.HandleAsync(actualRequestResult).ConfigureAwait(false);

                    response = new RequestReplyMessage<TResult>(_configuration, result, actualRequestResult.IsSuccess);
                    _configuration.Logger.Log(new DebugLogMsg($"sending response for [Request:{typeof(T)}] and [Response:{typeof(TResult)}]"));
                }
                catch (System.Exception ex)
                {
                    // failure to parse or any other exception leads to a non successful response, which then in turn can be handled on the request side
                    _configuration.Logger.Log(new ErrorLogMsg($"Responding to [Request:{typeof(T)}] with [Response:{typeof(TResult)}] did fail: " + ex.Message));

                    var msgParts = new[] { ex.GetType().Name, ex.Message, ex.StackTrace };
                    var msg = Environment.NewLine + string.Join(Environment.NewLine, msgParts);

                    response = RequestReplyMessage<TResult>.FromError(_configuration, msg);
                }

                // try send response with timeout
                bool noTimeout = socket.TrySendMultipartMessage(_configuration.Timeout, response);
                if (!noTimeout)
                    _configuration.Logger.Log(new ErrorLogMsg($"Responding to [Request:{typeof(T)}] with [Response:{typeof(TResult)}] timed-out after {_configuration.Timeout}"));
            }
            catch (NetMQ.TerminatingException terminating)
            {
                _configuration.Logger.Log(new ErrorLogMsg($"repsonseHandler failed with terminating exception: [{terminating.Message}]"));
                kill();
            }
        }
    }
}
