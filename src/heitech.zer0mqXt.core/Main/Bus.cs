using System;
using System.Threading;
using System.Threading.Tasks;
using heitech.zer0mqXt.core.infrastructure;
using heitech.zer0mqXt.core.patterns;

namespace heitech.zer0mqXt.core.Main
{
    internal class Bus : IEntry
    {
        private readonly Socket _socket;
        private readonly PubSub _pubSub;
        public Bus(SocketConfiguration config) 
        {
            _socket = new Socket(config);
            _pubSub = new PubSub(config);
        }

        public void Dispose()
        {
            _socket.Dispose();
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
            var xtResult = await _socket.RequestAsync<TRequest, TResult>(request);
            if (xtResult.IsSuccess == false)
                throw ZeroMqXtSocketException.FromException(xtResult.Exception);

            return xtResult.GetResult();
        }

        public void Respond<TRequest, TResult>(Func<TRequest, TResult> callback, CancellationToken cancellationToken = default)
            where TRequest : class, new()
            where TResult : class, new()
        {
            _socket.Respond<TRequest, TResult>(callback, cancellationToken);
        }

        public void RespondAsync<TRequest, TResult>(Func<TRequest, Task<TResult>> callback, CancellationToken cancellationToken = default)
            where TRequest : class, new()
            where TResult : class, new()
        {
            // todo
        }
    }
}