using System;
using System.Threading;
using System.Threading.Tasks;
using heitech.zer0mqXt.core.infrastructure;

namespace heitech.zer0mqXt.core.SendReceive
{
    public interface IReceiver : IDisposable
    {
        ///<summary>
        /// Responder, but for a specific Sender and without visible TResponse
        ///</summary>
        XtResult SetupReceiver<TMessage>(Action<TMessage> callback, CancellationToken token = default)
            where TMessage : class, new();
        XtResult SetupReceiverAsync<TMessage>(Func<TMessage, Task> callback, CancellationToken token = default)
            where TMessage : class, new();
    }
}
