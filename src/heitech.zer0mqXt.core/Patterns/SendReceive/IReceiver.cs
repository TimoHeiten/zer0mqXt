using System;
using System.Threading;
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
    }
}
