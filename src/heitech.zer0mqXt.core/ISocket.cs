using System;
using System.Threading;
using System.Threading.Tasks;

namespace heitech.zer0mqXt.core
{
    public interface ISocket : IDisposable
    { 
        Task<TResult> RequestAsync<TRequest, TResult>(TRequest request)
            where TRequest : class, new()
            where TResult : class, new();
        void Respond<TRequest, TResult>(Func<TRequest, TResult> callback, CancellationToken cancellationToken = default)
            where TRequest : class, new()
            where TResult : class, new();
        void RespondAsync<TRequest, TResult>(Func<TRequest, Task<TResult>> callback, CancellationToken cancellationToken = default)
            where TRequest : class, new()
            where TResult : class, new();

        Task PublishAsync<TMessage>(TMessage message)
            where TMessage : class, new();

        void RegisterSubscriber<TMessage>(Action<TMessage> callback, CancellationToken cancellationToken = default)
            where TMessage : class, new();
        void RegisterAsyncSubscriber<TMessage>(Func<TMessage, Task> asyncCallback, CancellationToken cancellationToken = default)
            where TMessage : class, new();
    }
}