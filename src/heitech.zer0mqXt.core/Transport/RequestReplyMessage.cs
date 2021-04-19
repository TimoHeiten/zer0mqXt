using heitech.zer0mqXt.core.infrastructure;
using NetMQ;

namespace heitech.zer0mqXt.core.transport
{
    internal class RequestReplyMessage<TMessage> : Message<TMessage>
        where TMessage : class
    {
        private readonly bool _isSuccess;
        private byte[] Success => _serializer.Serialize<string>(_isSuccess.ToString());

        internal RequestReplyMessage(SocketConfiguration configuration, TMessage message, bool success = true)
            : base(configuration, message)
        {
            _isSuccess = success;
        }

        private RequestReplyMessage(SocketConfiguration configuration, string message)
            : base(configuration, errorMsg: message)
        {
            _isSuccess = false;
        }

        internal static RequestReplyMessage<TMessage> FromError(SocketConfiguration configuration, string error)
        {
            return new RequestReplyMessage<TMessage>(configuration, error);
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