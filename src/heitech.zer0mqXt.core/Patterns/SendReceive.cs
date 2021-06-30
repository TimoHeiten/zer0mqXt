using System;
using System.Threading;
using System.Threading.Tasks;
using heitech.zer0mqXt.core.infrastructure;

namespace heitech.zer0mqXt.core.patterns
{
    ///<summary>
    /// single Sender and single receiver, unlike Pub Sub for each sender there must be a receiver
    ///</summary>
    internal class SendReceive : IDisposable
    {
        private readonly RqRep _rqRs;

        internal SendReceive(SocketConfiguration configuration)
        {
            _rqRs = new RqRep(configuration);
        }

        public async Task<XtResult> SendAsync<TMessage>(TMessage message)
            where TMessage : class, new()
        {
            var xtResult = await _rqRs.RequestAsync<TMessage, EmptyResponse>(message).ConfigureAwait(false);
            return xtResult.IsSuccess
                   ? XtResult.Success("send")
                   : XtResult.Failed(xtResult.Exception, "send");
        }

        public XtResult SetupReceiver<TMessage>(Action<TMessage> callback, CancellationToken token = default)
            where TMessage : class, new()
        {
            var setupResult = _rqRs.Respond<TMessage, EmptyResponse>((msg) => { callback(msg); return new EmptyResponse(); }, token);
            return setupResult.IsSuccess
                   ? XtResult.Success("receive")
                   : XtResult.Failed(setupResult.Exception, "receive");
        }

        public XtResult SetupReceiverAsync<TMessage>(Func<TMessage, Task> asyncCallback, CancellationToken token = default)
            where TMessage : class, new()
        {
            var setupResult = _rqRs.RespondAsync<TMessage, EmptyResponse>(async (msg) => { await asyncCallback(msg).ConfigureAwait(false); return new EmptyResponse(); }, token);

            return setupResult.IsSuccess
                   ? XtResult.Success("receiveAsync")
                   : XtResult.Failed(setupResult.Exception, "receiveAsync");
        }

        ///<summary>
        /// Just needed to satisfy the way the underlying NetMQ RqRep works with regards to requiring to follow the protocol of Rq and subsequent Response
        ///</summary>
        public class EmptyResponse { }

        public void Dispose() => _rqRs.Dispose();
    }
}