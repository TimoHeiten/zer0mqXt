using System;

namespace heitech.zer0mqXt.core
{
    public class ZeroMqXtSocketException : Exception
    {
        public ZeroMqXtSocketException(string message) : base(message)
        { }

        internal static ZeroMqXtSocketException Frame1RqTypeDoesNotMatch<T>(string otherType) => new ZeroMqXtSocketException($"{typeof(T)} of request type does not match the request type sent by the the response: {otherType}");

        internal static ZeroMqXtSocketException Frame2RsTypeDoesNotMatch<TResult>(string otherType) => new ZeroMqXtSocketException($"Response type of {typeof(TResult)} for a request does not match the response type of {otherType}");

        
        internal static ZeroMqXtSocketException SerializationFailed(string fromException) => new ZeroMqXtSocketException(fromException);

        internal static ZeroMqXtSocketException MissedExpectedFrameCount(int count)
        {
            return new ZeroMqXtSocketException($"Frames count was {count} but M U S T be exactly 3");
        }
    }
}