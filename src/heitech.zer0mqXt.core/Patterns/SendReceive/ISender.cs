using System;
using System.Threading.Tasks;
using heitech.zer0mqXt.core.infrastructure;

namespace heitech.zer0mqXt.core.SendReceive
{
    ///<summary>
    /// Point to point send (abstraction over RqRep without a visible Response)
    ///</summary>
    public interface ISender : IDisposable
    {
        Task<XtResult> SendAsync<TMessage>(TMessage message)
            where TMessage : class, new();
    }
}
