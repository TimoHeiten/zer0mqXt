using heitech.zer0mqXt.core.infrastructure;
using NetMQ;

namespace heitech.zer0mqXt.core.transport
{
    internal class PubSubMessage<TMessage> : Message<TMessage>
        where TMessage : class
    {
        private readonly string _topicFrame;
        public PubSubMessage(SocketConfiguration configuration, TMessage message, string topicFrame) 
            : base(configuration, message)
        { }

        protected internal override NetMQMessage ToNetMqMessage()
        {
            var msg = new NetMQMessage();

            msg.Append(_configuration.Serializer.Serialize(_topicFrame));
            msg.Append(Payload);

            return msg;
        }
    }
}