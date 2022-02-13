using System;
using System.Threading;
using System.Threading.Tasks;
using heitech.zer0mqXt.core.infrastructure;

namespace heitech.zer0mqXt.core.PubSub
{
    public interface ISubscriber : IDisposable
    {
        ///<summary>
        /// Register a non blocking Subscriber to a TMessage. Make sure the Publisher already exists and is setup first
        ///</summary>
        XtResult RegisterSubscriber<TMessage>(Action<TMessage> callback, Action onError = null, string topic = null, CancellationToken cancellationToken = default)
            where TMessage : class, new();

        ///<summary>
        /// Register a non blocking and asynchronous Subscriber to a TMessage.  Make sure the Publisher already exists and is setup first
        ///</summary>
        XtResult RegisterAsyncSubscriber<TMessage>(Func<TMessage, Task> asyncCallback, Func<Task> onError = null, string topic = null, CancellationToken cancellationToken = default)
            where TMessage : class, new();
    }
}
