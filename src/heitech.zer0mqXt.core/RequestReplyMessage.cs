using NetMQ;

namespace heitech.zer0mqXt.core
{
    public class RequestReplyMessage<TMessage> : Message<TMessage>
        where TMessage : class
    {
        private readonly bool _isSuccess;
        private byte[] Success => _serializer.Serialize<string>(_isSuccess.ToString());

        internal RequestReplyMessage(SocketConfiguration configuration, TMessage message, bool success = true)
            : base(configuration, message)
        {
            _isSuccess = success;
        }

        protected internal override NetMQMessage ToNetMqMessage()
        {
            var msg = new NetMQMessage();
            msg.Append(TypeFrame);
            msg.Append(Success);
            msg.Append(Payload);

            return msg;
        }
    }
}