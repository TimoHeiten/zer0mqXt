using System.Threading.Tasks;
using heitech.zer0mqXt.core.infrastructure;
using heitech.zer0mqXt.core.RqRp;

namespace heitech.zer0mqXt.core.SendReceive
{
    internal class Sender : ISender
    {
        private readonly IClient _client;
        internal Sender(SocketConfiguration configuration)
            => _client = RequestReplyFactory.CreateClient(configuration);

        public async Task<XtResult> SendAsync<TMessage>(TMessage message)
            where TMessage : class, new()
        {
            var xtResult = await _client.RequestAsync<TMessage, EmptyResponse>(message).ConfigureAwait(false);

            return xtResult.IsSuccess
                   ? XtResult.Success("send")
                   : XtResult.Failed(xtResult.Exception, "send");
        }

        public void Dispose()
            => _client.Dispose();
    }
}
