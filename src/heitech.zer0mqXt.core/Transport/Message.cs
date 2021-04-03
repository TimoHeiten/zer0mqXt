using System;
using heitech.zer0mqXt.core.infrastructure;
using NetMQ;

namespace heitech.zer0mqXt.core.transport
{
    public abstract class Message<TMessage>
        where TMessage : class
    {
        protected Serializer _serializer;
        protected SocketConfiguration _configuration;
        protected byte[] TypeFrame => _serializer.Serialize(typeof(TMessage).TypeFrameName());
        protected byte[] Payload => _serializer.Serialize<TMessage>(Content);
        public TMessage Content { get; private set; }

        private protected Message(SocketConfiguration configuration, TMessage message)
        {
            Content = message;
            _configuration = configuration;
            _serializer = configuration.Serializer;
        }

        protected internal abstract NetMQMessage ToNetMqMessage();
        public static implicit operator NetMQMessage(Message<TMessage> self)
        {
            return self.ToNetMqMessage();
        }
    }

    public static class MessageParser
    {
        internal static XtResult<TMessage> ParsePubSubMessage<TMessage>(this NetMQMessage msg, SocketConfiguration configuration)
            where TMessage : class
        {
            const string operation = "parse-pub-sub-msg";
            if (msg.FrameCount != 2)
            {
                var exc = ZeroMqXtSocketException.MissedExpectedFrameCount(msg.FrameCount);
                configuration.Logger.Log(new DebugLogMsg(exc.Message));
                return XtResult<TMessage>.Failed(ZeroMqXtSocketException.MissedExpectedFrameCount(msg.FrameCount, 2));
            }
            
            var receivedType = configuration.Serializer.Deserialize<string>(msg.First.ToByteArray());
            if (receivedType != typeof(TMessage).FullName)
                return XtResult<TMessage>.Failed(ZeroMqXtSocketException.Frame1TypeDoesNotMatch<TMessage>(receivedType));

            try
            {
                var instance = configuration.Serializer.Deserialize<TMessage>(msg.Last.ToByteArray());
                return XtResult<TMessage>.Success(instance, operation: operation);
            }
            catch (System.Exception ex)
            {
                return XtResult<TMessage>.Failed(ex, operation: operation);
            }

        }

        ///<summary>
        /// Construct a Message from ReceivedFrames - according to 
        /// <para>Frame 1 - RequestTypeName</para>
        /// <para>Frame 2 - Successful parsing and use of Incoming Instance of RequestType / on Response it is the successful deserialization</para>
        /// <para>Frame 3 - Actual Payload and instance of the RequestType</para>
        ///</summary>
         internal static XtResult<TMessage> ParseRqRepMessage<TMessage>(this NetMQMessage msg, SocketConfiguration configuration)
            where TMessage : class
        {
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
                var excptn = new InvalidOperationException("Frame2 holds " + configuration.Serializer.Encoding.GetString(successFrame) + " therefore the Message cannot be savely parsed and is disregarded instead");
                configuration.Logger.Log(new DebugLogMsg(excptn.Message));
                return XtResult<TMessage>.Failed(excptn, operation);
            }
            #endregion

            try
            {
                // actual message body deserialization
                TMessage result = configuration.Serializer.Deserialize<TMessage>(payloadFrame);
                if (result == default(TMessage))
                    return XtResult<TMessage>.Failed(new ArgumentNullException($"{typeof(TMessage)} yielded the default value on deserializing! Proceeding is not safe."), operation);

                return XtResult<TMessage>.Success(result, operation);
            }
            catch (System.Exception ex)
            {
                return XtResult<TMessage>.Failed(ZeroMqXtSocketException.SerializationFailed(ex.Message), operation);
            }
        }

        public static string TypeFrameName(this Type type) => type.FullName; // todo rather use fullyqualifed name (assembly)
    }
}