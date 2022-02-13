using System;
using System.Threading.Tasks;
using heitech.zer0mqXt.core.infrastructure;

namespace heitech.zer0mqXt.core.RqRp
{
    public interface IClient : IDisposable
    {
        Task<XtResult<TResponse>> RequestAsync<TRequest, TResponse>(TRequest rq)
            where TRequest : class,new()
            where TResponse : class,new();
    }
}
