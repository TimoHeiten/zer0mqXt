using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using heitech.zer0mqXt.core.infrastructure;
using heitech.zer0mqXt.core.patterns;

namespace heitech.zer0mqXt.core.Main
{
    ///<inheritdoc cref="ISocket"/>
    internal class Socket : ISocket
    {
        private readonly RqRep _rqRep;
        private readonly PubSub _pubSub;
        private readonly SendReceive _sendReceive;

        ///<inheritdoc/>
        internal Socket(SocketConfiguration config) 
        {
            _rqRep = new RqRep(config);
            _pubSub = new PubSub(config);
            _sendReceive = new SendReceive(config);
        }

        public void Dispose()
        {
            _rqRep.Dispose();
            _pubSub.Dispose();
            _sendReceive.Dispose();
        }

        public async Task PublishAsync<TMessage>(TMessage message) 
            where TMessage : class, new()
        {
            var xtResult = await _pubSub.PublishAsync(message);

            ThrowOnNonSuccess(xtResult);
        }

        public void RegisterAsyncSubscriber<TMessage>(Func<TMessage, Task> asyncCallback, CancellationToken cancellationToken = default)
            where TMessage : class, new()
        {
            var result = _pubSub.SubscribeHandlerAsync(asyncCallback, cancellationToken);
            ThrowOnNonSuccess(result);
        }

        public void RegisterSubscriber<TMessage>(Action<TMessage> callback, CancellationToken cancellationToken = default)
            where TMessage : class, new()
        {
            var result = _pubSub.SubscribeHandler(callback, cancellationToken);
            ThrowOnNonSuccess(result);
        }

        public async Task<TResult> RequestAsync<TRequest, TResult>(TRequest request)
            where TRequest : class, new()
            where TResult : class, new()
        {
            var xtResult = await _rqRep.RequestAsync<TRequest, TResult>(request);
            ThrowOnNonSuccess(xtResult);

            return xtResult.GetResult();
        }

        public void Respond<TRequest, TResult>(Func<TRequest, TResult> callback, CancellationToken cancellationToken = default)
            where TRequest : class, new()
            where TResult : class, new()
        {
            var xtResult = _rqRep.Respond<TRequest, TResult>(callback, cancellationToken);
            ThrowOnNonSuccess(xtResult);
        }

        public void RespondAsync<TRequest, TResult>(Func<TRequest, Task<TResult>> callback, CancellationToken cancellationToken = default)
            where TRequest : class, new()
            where TResult : class, new()
        {
            var xtResult = _rqRep.RespondAsync<TRequest, TResult>(callback, cancellationToken);
            ThrowOnNonSuccess(xtResult);
        }

        public async Task SendAsync<TMessage>(TMessage message) 
            where TMessage : class, new()
        {
            var xtResult = await _sendReceive.SendAsync(message);

            ThrowOnNonSuccess(xtResult);
        }

        public void Receiver<TMessage>(Action<TMessage> callback, CancellationToken token = default) 
            where TMessage : class, new()
        {
            var xtResult = _sendReceive.SetupReceiver(callback, token);

            ThrowOnNonSuccess(xtResult);
        }

        public void ReceiverAsync<TMessage>(Func<TMessage, Task> asyncCallack, CancellationToken token = default) 
            where TMessage : class, new()
        {
            var xtResult = _sendReceive.SetupReceiverAsync(asyncCallack, token);

            ThrowOnNonSuccess(xtResult);
        }

        private void ThrowOnNonSuccess(XtResultBase xtResult)
        {
            if (xtResult.IsSuccess == false)
                throw ZeroMqXtSocketException.FromException(xtResult.Exception);
        }

         public async Task<bool> TryRequestAsync<TRequest, TResult>(TRequest request, Func<TResult, Task> successCallback, Func<Task> failureCallback)
            where TRequest : class, new()
            where TResult : class, new()
        {
            var xtResult = await _rqRep.RequestAsync<TRequest, TResult>(request);

            if (xtResult.IsSuccess)
                await successCallback(xtResult.GetResult());
            else
                await failureCallback();
            
            return xtResult.IsSuccess;
        }

        public bool TryRespond<TRequest, TResult>(Func<TRequest, TResult> callback, CancellationToken cancellationToken = default)
            where TRequest : class, new()
            where TResult : class, new()
        {
            var xtResult = _rqRep.Respond<TRequest, TResult>(callback, cancellationToken);

            return xtResult.IsSuccess;

        }

        public bool TryRespondAsync<TRequest, TResult>(Func<TRequest, Task<TResult>> callback, CancellationToken cancellationToken = default)
            where TRequest : class, new()
            where TResult : class, new()
        {
            var xtResult = _rqRep.RespondAsync<TRequest, TResult>(callback, cancellationToken);

            return xtResult.IsSuccess;
        }

        public async Task<bool> TrySendAsync<TMessage>(TMessage message) 
            where TMessage : class, new()
        {
            var xtResult = await _sendReceive.SendAsync(message);
            return xtResult.IsSuccess;
        }

        public bool TryReceive<TMessage>(Action<TMessage> callback, CancellationToken token = default) where TMessage : class, new()
        {
            var xtResult = _sendReceive.SetupReceiver(callback, token);
            return xtResult.IsSuccess;
        }

        public bool TryReceiveasync<TMessage>(Func<TMessage, Task> asyncCallack, CancellationToken token = default) where TMessage : class, new()
        {
            var xtResult = _sendReceive.SetupReceiverAsync(asyncCallack, token);
            return xtResult.IsSuccess;
        }
    }
}