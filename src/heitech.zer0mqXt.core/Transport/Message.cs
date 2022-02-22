using System;
using System.Linq;
using System.Reflection;
using heitech.zer0mqXt.core.Adapters;
using heitech.zer0mqXt.core.infrastructure;
using heitech.zer0mqXt.core.Transport;
using NetMQ;

namespace heitech.zer0mqXt.core.transport
{
    internal abstract class Message<TMessage>
        where TMessage : class
    {
        protected ISerializerAdapter _serializer;
        protected SocketConfiguration _configuration;
        protected byte[] TypeFrame => _serializer.Serialize(typeof(TMessage).TypeFrameName());
        protected byte[] Payload { get; private set; }
        public TMessage Content { get; private set; }

        private protected Message(SocketConfiguration configuration, TMessage message)
        {
            Content = message;
            _configuration = configuration;
            _serializer = configuration.Serializer;
            Payload = _serializer.Serialize<TMessage>(Content);
        }

        private protected Message(SocketConfiguration configuration, string errorMsg)
        {
            _configuration = configuration;
            _serializer = configuration.Serializer;
            Payload = _serializer.Serialize<string>(errorMsg);
        }

        protected internal abstract NetMQMessage ToNetMqMessage();
        public static implicit operator NetMQMessage(Message<TMessage> self)
        {
            return self.ToNetMqMessage();
        }
    }

    public static class MessageParser
    {
        ///<summary>
        /// Construct a Message from ReceivedFrames - according to 
        /// <para>Frame 1 - RequestTypeName</para>
        /// <para>Frame 2 - Successful parsing and use of Incoming Instance of RequestType / on Response it is the successful deserialization</para>
        /// <para>Frame 3 - Actual Payload and instance of the RequestType</para>
        ///</summary>
        internal static XtResult<TMessage> ParseRqRepMessage<TMessage>(this NetMQMessage msg, SocketConfiguration configuration)
           where TMessage : class
        {
            configuration.Logger.Log(new DebugLogMsg($"parsing [{typeof(TMessage)}]"));
            const string operation = "parse-rq-rep-msg";
            #region precondition checks
            if (msg.FrameCount != 3)
            {
                var exc = ZeroMqXtSocketException.MissedExpectedFrameCount(msg.FrameCount);
                configuration.Logger.Log(new DebugLogMsg(exc.Message));
                return XtResult<TMessage>.Failed(exc, operation);
            }

            byte[] typeFrame = msg.Pop().ToByteArray();
            byte[] successFrame = msg.Pop().ToByteArray();
            byte[] payloadFrame = msg.Pop().ToByteArray();

            string msgType = typeof(TMessage).TypeFrameName();
            string typeFromFrame = configuration.Serializer.Deserialize<string>(typeFrame);
            if (typeFromFrame != msgType)
            {
                var exception = ZeroMqXtSocketException.Frame1TypeDoesNotMatch<TMessage>(typeFromFrame);
                configuration.Logger.Log(new DebugLogMsg(exception.Message));
                return XtResult<TMessage>.Failed(exception, operation);
            }

            bool isSuccess = Convert.ToBoolean(configuration.Serializer.Deserialize<string>(successFrame));
            if (!isSuccess)
            {
                Exception ex =  ZeroMqXtSocketException.ResponseFailed<TMessage>();
                if (configuration.DeveloperMode)
                {
                    var exceptionText = configuration.Serializer.Deserialize<string>(payloadFrame);
                    ex = new ZeroMqXtSocketException("Server failed with" + exceptionText);
                }

                configuration.Logger.Log(new ErrorLogMsg(ex.Message));
                return XtResult<TMessage>.Failed(ex, operation);
            }
            #endregion

            try
            {
                // actual message body deserialization
                TMessage result = configuration.Serializer.Deserialize<TMessage>(payloadFrame);
                if (result == default(TMessage))
                    return XtResult<TMessage>.Failed(new ArgumentNullException($"{typeof(TMessage)} yielded the default value on deserializing! Proceeding is not safe."), operation);

                configuration.Logger.Log(new DebugLogMsg($"parsing was successful for [{typeof(TMessage)}]"));
                return XtResult<TMessage>.Success(result, operation);
            }
            catch (System.Exception ex)
            {
                return XtResult<TMessage>.Failed(ZeroMqXtSocketException.SerializationFailed(ex.Message), operation);
            }
        }

        ///<summary>
        /// Get the TypeFrame by Convention (Type-Name) or by declarative MessageAttribute
        ///</summary>
        public static string TypeFrameName(this Type type) 
        {
            // todo optimize with static cache or others?
            var attribute = type.GetCustomAttributes()
                                .SingleOrDefault(x => x.GetType() == typeof(Zer0mqMessageAttribute));
            
            return attribute is not null && attribute is Zer0mqMessageAttribute zer0
                   ? zer0.MessageId
                   : type.Name; 
        } 
    }
}