using System;
using System.Threading;
using System.Threading.Tasks;
using heitech.zer0mqXt.core.infrastructure;
using heitech.zer0mqXt.core.RqRp;
using heitech.zer0mqXt.core.utils;
using NetMQ;
using NetMQ.Sockets;

namespace heitech.zer0mqXt.core.patterns.RqRp
{
    public class Responder : IResponder
    {
        private readonly NetMQPoller _poller;
        private readonly ResponseSocket _responder;
        private readonly SocketConfiguration _configuration;
        private EventHandler<NetMQSocketEventArgs> _receiveHandler;
        private bool _isSetup = false;
        private object _concurrencyToken = new();
        private bool disposedValue;

        internal Responder(SocketConfiguration configuration)
        {
            // hepls with the correctnes and thread safety 
            _poller = new();
            _responder = new();
            _configuration = configuration;
        }

        public XtResult Respond<TRequest, TResponse>(Func<TRequest, TResponse> handler, Func<TResponse> onError = null, CancellationToken token = default)
            where TRequest : class, new()
            where TResponse : class, new()
        {
            lock (_concurrencyToken)
            {
                if (!_isSetup)
                {
                    var responseHandler = ResponseHandler<TRequest, TResponse>.Sync(_configuration, handler);
                    _isSetup = true;
                    responseHandler.SetOnError(onError);
                    return SetupResponder<TRequest, TResponse>(responseHandler, token);
                }
                else return XtResult.Failed(new ZeroMqXtSocketException("already setup this responder!"), "setup-responder");
            }
        }

        public XtResult RespondAsync<TRequest, TResponse>(Func<TRequest, Task<TResponse>> asyncHandler, Func<Task<TResponse>> onError = null, CancellationToken token = default)
            where TRequest : class, new()
            where TResponse : class, new()
        {
            lock (_concurrencyToken)
            {
                if (!_isSetup)
                {
                    var responseHandler = ResponseHandler<TRequest, TResponse>.ASync(_configuration, asyncHandler);
                    responseHandler.SetOnError(onError);
                    _isSetup = true;
                    return SetupResponder<TRequest, TResponse>(responseHandler, token);
                }
                else return XtResult.Failed(new ZeroMqXtSocketException("already setup this responder!"), "setup-responder");
            }
        }

        private XtResult SetupResponder<TRequest, TResponse>(ResponseHandler<TRequest, TResponse> handler, CancellationToken token)
            where TRequest : class, new()
            where TResponse : class, new()
        {
            var waitHandle = new ManualResetEvent(false);

            try
            {
                // bind and setup and subscrube to the ready event for non blocking interactions
                _responder.Bind(_configuration.Address());
                _receiveHandler = async (s, e) => await handler.ResponseHandlerCallback(_responder, handler, Dispose, token).ConfigureAwait(false);
                _responder.ReceiveReady += _receiveHandler;

                // add to poller and register handler
                _poller.Add(_responder);
                _poller.RunAsync();
            }
            catch (Exception exception)
            {
                _configuration.Logger.Log(new ErrorLogMsg(exception.GetType().Name + "-" + exception.Message));
            }
            finally
            {
                // open resetevent after binding to the socket and when the poller is started
                waitHandle.Set();
            }

            bool wasSignaled = waitHandle.WaitOne(500);
            return wasSignaled ? XtResult.Success("setup-responder") : XtResult.Failed(new ZeroMqXtSocketException("setup-responder-failed"));
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _poller.DisposeOf(_responder, _receiveHandler);
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
