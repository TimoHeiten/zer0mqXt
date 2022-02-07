using System;
using heitech.zer0mqXt.core.infrastructure;

namespace heitech.zer0mqXt.core.transport
{
    internal static class PubSubMessageHelper
    {
        public static string GetTopicFrame<TMessage>(this SocketConfiguration configuration, string topic)
            => topic ?? typeof(TMessage).TypeFrameName();

        public static byte[] PubSubMessage<TMessage>(this SocketConfiguration configuration, TMessage message)
            where TMessage : class, new()
            => configuration.Serializer.Serialize(message);

        public static XtResult<TMessage> ParseIncomingFrame<TMessage>(this SocketConfiguration configuration, byte[] messagePayload)
            where TMessage : class, new()
        {
            // type frame is the topic name, if it does not match it will not be here.
            const string operation = "parse-pub-sub-msg";
            try
            {
                var instance = configuration.Serializer.Deserialize<TMessage>(messagePayload);
                return XtResult<TMessage>.Success(instance, operation: operation);
            }
            catch (Exception ex)
            {
                return XtResult<TMessage>.Failed(ex, operation: operation);
            }
        }
    }
}