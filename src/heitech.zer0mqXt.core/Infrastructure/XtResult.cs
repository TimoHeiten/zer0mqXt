using System;

namespace heitech.zer0mqXt.core.infrastructure
{
    ///<summary>
    /// Represents the Result of a zer0mqXt Message Interaction
    ///</summary>
    public class XtResultBase
    {
        ///<summary>
        /// Was the Operation successful?
        ///</summary>
        public bool IsSuccess => (Exception is null);
        ///<summary>
        /// Exception is not null if XtResult.IsSuccess is false
        ///</summary>
        public Exception Exception { get; protected set; }

        ///<summary>
        /// For debugger purposes the Operation is documented
        ///</summary>
        public string Operation { get; protected set; }
    }

    public sealed class XtResult<T> : XtResultBase
        where T : class
    {
        private T _result;
        public T GetResult()
        {
            if (Exception is null)
                return _result;

            throw Exception;
        }

        internal static XtResult<T> Failed(Exception exception, string operation = "request")
        {
            return new XtResult<T> { Exception = exception, Operation = operation };
        }

        internal static XtResult<T> Success(T result, string operation = "request")
        {
            if (result == null)
                return XtResult<T>.Failed(new NullReferenceException($"result of type {typeof(T)} was null"));
            return new XtResult<T> { _result = result, Exception = null, Operation = operation };
        }

        public override string ToString()
        {
            var success = IsSuccess ? "succeeded " : "failed";
            var firstPart = $"XtResult of [{Operation}] - has {success}";
            var second = IsSuccess ? _result?.GetType() : Exception?.GetType();

            return $"{firstPart} with Type [{second}]";
        }
    }

    public sealed class XtResult : XtResultBase 
    {
        internal static XtResult Failed(Exception exception, string operation = "request")
        {
            return new XtResult { Exception = exception };
        }

        internal static XtResult Success(string operation = "request")
        {
            return new XtResult { Exception = null, Operation = operation };
        }

        public override string ToString()
        {
            var success = IsSuccess ? " succeeded " : " failed";
            var firstPart = $"XtResult of [{Operation}] - has {success}";
            var second = " with: " + Exception?.GetType();

            return $"{firstPart} {second}";
        }
    }
}