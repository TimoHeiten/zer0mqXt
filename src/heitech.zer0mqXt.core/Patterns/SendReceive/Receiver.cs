using System;
using System.Threading;
using System.Threading.Tasks;
using heitech.zer0mqXt.core.infrastructure;
using heitech.zer0mqXt.core.RqRp;

namespace heitech.zer0mqXt.core.SendReceive
{
    internal class Receiver : IReceiver
    {
        private readonly IResponder _responder;
        private readonly SocketConfiguration _configuration;
        internal Receiver(SocketConfiguration configuration)
        {
            _configuration = configuration;
            _responder = RequestReplyFactory.CreateResponder(configuration);
        }

        public XtResult SetupReceiver<TMessage>(Action<TMessage> callback, CancellationToken token = default)
            where TMessage : class, new()
        {
            var setupResult = _responder.Respond<TMessage, EmptyResponse>((msg) => { callback(msg); return new EmptyResponse(); }, () => new EmptyResponse(), token);
            return setupResult.IsSuccess
                   ? XtResult.Success("receive")
                   : XtResult.Failed(setupResult.Exception, "receive");
        }

        public void Dispose()
            => _responder.Dispose();

        public XtResult SetupReceiverAsync<TMessage>(Func<TMessage, Task> callback, CancellationToken token = default) where TMessage : class, new()
        {
            var setupResult = _responder.RespondAsync<TMessage, EmptyResponse>(async (msg) => { await callback(msg); return new EmptyResponse(); }, () => Task.FromResult(new EmptyResponse()), token);
            return setupResult.IsSuccess
                   ? XtResult.Success("receive")
                   : XtResult.Failed(setupResult.Exception, "receive");
        }
    }
}
