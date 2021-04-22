using System;

namespace heitech.zer0mqXt.core.infrastructure
{
    public class ZeroMqXtSocketException : Exception
    {
        internal ZeroMqXtSocketException(string message) : base(message)
        { }

        internal static ZeroMqXtSocketException FromException(Exception exception)
        {
            var msgParts = new [] { exception.GetType().Name, exception.Message, exception.StackTrace };
            return new ZeroMqXtSocketException(string.Join(Environment.NewLine, msgParts));
        }

        internal static ZeroMqXtSocketException Frame1TypeDoesNotMatch<T>(string otherType, string operation = "request") => new ZeroMqXtSocketException($"For operation [{operation}] the type {typeof(T)} was expected, but got [{otherType}]");
        internal static ZeroMqXtSocketException Frame2TypeDoesNotMatch<TResult>(string otherType, string operation = "request") => new ZeroMqXtSocketException($"For operation [{operation}] the type [{typeof(TResult)}] was expected, but got [{otherType}]");
        internal static ZeroMqXtSocketException SerializationFailed(string fromException) => new ZeroMqXtSocketException(fromException);
        internal static ZeroMqXtSocketException MissedExpectedFrameCount(int actualCount, int expectedCount = 3)
        {
            return new ZeroMqXtSocketException($"Frames count was {actualCount} but M U S T be exactly {expectedCount}");
        }
    }
}