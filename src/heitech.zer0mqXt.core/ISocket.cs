using System;
using System.Threading;
using System.Threading.Tasks;

namespace heitech.zer0mqXt.core
{
    ///<summary>
    /// Access to all underlying patterns of the zeroMQ Library
    ///</summary>
    public interface ISocket : IDisposable
    { 
        ///<summary>
        /// Request a given TResult for the configuration and type of TRequest 
        ///</summary>
        Task<TResult> RequestAsync<TRequest, TResult>(TRequest request)
            where TRequest : class, new()
            where TResult : class, new();

        ///<summary>
        /// Setup a non blocking Responder to a Request of type of TRequest and return a TResult
        ///</summary>
        void Respond<TRequest, TResult>(Func<TRequest, TResult> callback, CancellationToken cancellationToken = default)
            where TRequest : class, new()
            where TResult : class, new();

        ///<summary>
        /// Setup an asynchronous and non blocking Responder for a Request of TRequest and return a TResult
        ///</summary>
        void RespondAsync<TRequest, TResult>(Func<TRequest, Task<TResult>> callback, CancellationToken cancellationToken = default)
            where TRequest : class, new()
            where TResult : class, new();

        ///<summary>
        /// Publish a Message to all listening subscribers
        ///</summary>
        Task PublishAsync<TMessage>(TMessage message)
            where TMessage : class, new();

        ///<summary>
        /// Register a non blocking Subscriber to a TMessage
        ///</summary>
        void RegisterSubscriber<TMessage>(Action<TMessage> callback, CancellationToken cancellationToken = default)
            where TMessage : class, new();

        ///<summary>
        /// Register a non blocking and asynchronous Subscriber to a TMessage
        ///</summary>
        void RegisterAsyncSubscriber<TMessage>(Func<TMessage, Task> asyncCallback, CancellationToken cancellationToken = default)
            where TMessage : class, new();

        ///<summary>
        /// Send a Message of type TMessage to the current configuration
        ///</summary>
        Task SendAsync<TMessage>(TMessage message)
            where TMessage : class, new();

        ///<summary>
        /// Setup a non blocking Receiver of type TMessage to the current configuration
        ///</summary>
        void Receiver<TMessage>(Action<TMessage> callback, CancellationToken token = default)
            where TMessage : class, new();

        ///<summary>
        /// Setup an asynchronous and non blocking Receiver of type TMessage to the current configuration
        ///</summary>
        void ReceiverAsync<TMessage>(Func<TMessage, Task> asyncCallack, CancellationToken token = default)
            where TMessage : class, new();
    }
}