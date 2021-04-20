using System;
using System.Threading;
using System.Threading.Tasks;
using heitech.zer0mqXt.core.infrastructure;
using heitech.zer0mqXt.core.patterns;

namespace heitech.zer0mqXt.core.Main
{
    internal class Socket : ISocket
    {
        private readonly RqRep _rqRep;
        private readonly PubSub _pubSub;
        public Socket(SocketConfiguration config) 
        {
            _rqRep = new RqRep(config);
            _pubSub = new PubSub(config);
        }

        public void Dispose()
        {
            _rqRep.Dispose();
            _pubSub.Dispose();
        }

        public async Task PublishAsync<TMessage>(TMessage message) 
            where TMessage : class, new()
        {
            var result = await _pubSub.PublishAsync(message);

            if (result.IsSuccess == false)
                throw ZeroMqXtSocketException.FromException(result.Exception);
        }

        public void RegisterAsyncSubscriber<TMessage>(Func<TMessage, Task> asyncCallback, CancellationToken cancellationToken = default)
            where TMessage : class, new()
        {
            // todo should not return a task...also does not use token yet
            _pubSub.SubscribeAsyncHandler(asyncCallback).Wait();
        }

        public void RegisterSubscriber<TMessage>(Action<TMessage> callback, CancellationToken cancellationToken = default)
            where TMessage : class, new()
        {
            _pubSub.SubscribeHandler(callback, unsubscribeWhen: () => cancellationToken.IsCancellationRequested);
        }

        public async Task<TResult> RequestAsync<TRequest, TResult>(TRequest request)
            where TRequest : class, new()
            where TResult : class, new()
        {
            var xtResult = await _rqRep.RequestAsync<TRequest, TResult>(request);
            if (xtResult.IsSuccess == false)
                throw ZeroMqXtSocketException.FromException(xtResult.Exception);

            return xtResult.GetResult();
        }

        public void Respond<TRequest, TResult>(Func<TRequest, TResult> callback, CancellationToken cancellationToken = default)
            where TRequest : class, new()
            where TResult : class, new()
        {
            _rqRep.Respond<TRequest, TResult>(callback, cancellationToken);
        }

        public void RespondAsync<TRequest, TResult>(Func<TRequest, Task<TResult>> callback, CancellationToken cancellationToken = default)
            where TRequest : class, new()
            where TResult : class, new()
        {
            // todo
        }
    }
}