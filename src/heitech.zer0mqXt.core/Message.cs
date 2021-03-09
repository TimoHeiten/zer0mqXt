using System;
using System.Collections.Generic;
using System.Linq;

namespace heitech.zer0mqXt.core
{
    public class Message<TMessage>
        where TMessage : class
    {
        private readonly Serializer _serializer;
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

        internal Message(Serializer serializer, TMessage message, bool success = true)
        {
            _isSuccess = success;
            _serializer = serializer;
            Content = message;
        }

        ///<summary>
        /// Construct a Message from ReceivedFrames
        ///</summary>
        internal static XtResult<Message<TMessage>> ParseMessage(Serializer serializer, List<byte[]> bytes)
        {
            if (bytes.Count != 3)
                return XtResult<Message<TMessage>>.Failed(ZeroMqXtSocketException.MissedExpectedFrameCount(bytes.Count));

            string msgType = typeof(TMessage).FullName;

            byte[] frame = bytes.First();
            string typeFromFrame = serializer.Deserialize<string>(frame);

            if (typeFromFrame != msgType)
                return XtResult<Message<TMessage>>.Failed(ZeroMqXtSocketException.Frame1RqTypeDoesNotMatch<TMessage>(typeFromFrame));

            bool isSuccess = Convert.ToBoolean(serializer.Deserialize<string>(bytes[1]));
            if (!isSuccess)
                return XtResult<Message<TMessage>>.Failed(new InvalidCastException("could not cast " + serializer.Encoding.GetString(bytes[1])+ " to boolean!"));

            TMessage result = serializer.Deserialize<TMessage>(bytes.Last());

            return XtResult<Message<TMessage>>.Success(new Message<TMessage>(serializer, result));
        }
    }
}