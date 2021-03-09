using System;
using System.Collections.Generic;
using System.Linq;

namespace heitech.zer0mqXt.core
{
    public class Message<TMessage>
        where TMessage : class
    {
        private readonly Serializer _serializer;
        private readonly SocketConfiguration _configuration;
        private readonly List<byte[]> _payload = new List<byte[]>(3);

        ///<summary>
        /// Frame 1 - Type Name
        ///</summary>
        public byte[] RequestTypeFrame => _serializer.Serialize(typeof(TMessage).FullName);
        ///<summary>
        /// Frame 2 - Is Parsing etc Successful?
        ///</summary>
        public byte[] Success => _serializer.Serialize<string>(_isSuccess.ToString());
        ///<summary>
        /// Frame 3 - actual Message Instance
        ///</summary>
        public byte[] Payload => _serializer.Serialize<TMessage>(Content);
        private readonly bool _isSuccess;
        public TMessage Content { get; private set; }

        internal Message(SocketConfiguration configuration, TMessage message, bool success = true)
        {
            Content = message;
            _isSuccess = success;
            _configuration = configuration;
            _serializer = configuration.Serializer;
        }

        ///<summary>
        /// Construct a Message from ReceivedFrames - according to 
        /// <para>Frame 1 - RequestType</para>
        /// <para>Frame 2 - Successful parsing and use of Incoming Instance of RequestType</para>
        /// <para>Frame 3 - Actual Payload and instance of the RequestType</para>
        ///</summary>
        internal static XtResult<Message<TMessage>> ParseMessage(SocketConfiguration configuration, List<byte[]> bytes)
        {
            #region precondition checks
            if (bytes.Count != 3)
            {
                var exc = ZeroMqXtSocketException.MissedExpectedFrameCount(bytes.Count);
                configuration.Logger.Log(new DebugLogMsg(exc.Message));
                return XtResult<Message<TMessage>>.Failed(exc);
            }

            string msgType = typeof(TMessage).FullName;

            byte[] frame = bytes.First();
            string typeFromFrame = configuration.Serializer.Deserialize<string>(frame);

            if (typeFromFrame != msgType)
            {
                var exception = ZeroMqXtSocketException.Frame1RqTypeDoesNotMatch<TMessage>(typeFromFrame);
                configuration.Logger.Log(new DebugLogMsg(exception.Message));
                return XtResult<Message<TMessage>>.Failed(exception);
            }

            bool isSuccess = Convert.ToBoolean(configuration.Serializer.Deserialize<string>(bytes[1]));
            if (!isSuccess)
            {
                var excptn = new InvalidOperationException("Frame2 holds " + configuration.Serializer.Encoding.GetString(bytes[1]) + " therefore the Message cannot be savely parsed and is thrown out instead");
                configuration.Logger.Log(new DebugLogMsg(excptn.Message));
                return XtResult<Message<TMessage>>.Failed(excptn);
            }
            #endregion

            // actual message body deserialization
            TMessage result = configuration.Serializer.Deserialize<TMessage>(bytes.Last());
            return XtResult<Message<TMessage>>.Success(new Message<TMessage>(configuration, result));
        }
    }
}