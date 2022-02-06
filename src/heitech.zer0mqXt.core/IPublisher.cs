using System.Threading.Tasks;
using heitech.zer0mqXt.core.infrastructure;

namespace heitech.zer0mqXt.core
{
    public interface IPublisher
    {
        Task<XtResult<TMessage>> SendAsync<TMessage>(TMessage message, string topic = null)
            where TMessage : class, new();
    }
}
