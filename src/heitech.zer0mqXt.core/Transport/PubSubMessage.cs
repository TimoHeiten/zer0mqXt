using heitech.zer0mqXt.core.infrastructure;
using NetMQ;

namespace heitech.zer0mqXt.core.transport
{
    public class PubSubMessage<TMessage> : Message<TMessage>
        where TMessage : class
    {
        public PubSubMessage(SocketConfiguration configuration, TMessage message) 
            : base(configuration, message)
        { }

        protected internal override NetMQMessage ToNetMqMessage()
        {
            var msg = new NetMQMessage();

            msg.Append(TypeFrame);
            msg.Append(Payload);

            return msg;
        }
    }
}