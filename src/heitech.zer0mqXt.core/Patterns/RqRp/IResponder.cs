using System;
using System.Threading;
using System.Threading.Tasks;
using heitech.zer0mqXt.core.infrastructure;

namespace heitech.zer0mqXt.core.RqRp
{
    public interface IResponder : IDisposable
    {
        XtResult RespondAsync<TRequest, TResponse>(Func<TRequest, Task<TResponse>> asyncHandler, Func<Task<TResponse>> onError = null, CancellationToken token = default)
            where TRequest : class, new()
            where TResponse : class, new();
        XtResult Respond<TRequest, TResponse>(Func<TRequest, TResponse> handler, Func<TResponse> onError = null, CancellationToken token = default)
            where TRequest : class, new()
            where TResponse : class, new();
    }
}
