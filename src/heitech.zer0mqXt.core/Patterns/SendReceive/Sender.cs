using System.Threading.Tasks;
using heitech.zer0mqXt.core.infrastructure;
using heitech.zer0mqXt.core.RqRp;

namespace heitech.zer0mqXt.core.SendReceive
{
    internal class Sender : ISender
    {
        private readonly IClient _requester;
        private readonly SocketConfiguration _configuration;
        internal Sender(SocketConfiguration configuration)
            => _requester = RequestReplyFactory.CreateClient(configuration);

        public async Task<XtResult> SendAsync<TMessage>(TMessage message)
            where TMessage : class, new()
        {
            var xtResult = await _requester.RequestAsync<TMessage, EmptyResponse>(message).ConfigureAwait(false);

            return xtResult.IsSuccess
                   ? XtResult.Success("send")
                   : XtResult.Failed(xtResult.Exception, "send");
        }

        ///<summary>
        /// Just needed to satisfy the way the underlying NetMQ RqRep works with regards to requiring to follow the protocol of Rq and subsequent Response
        ///</summary>
        public class EmptyResponse { }

        public void Dispose()
            => _requester.Dispose();
    }
}
