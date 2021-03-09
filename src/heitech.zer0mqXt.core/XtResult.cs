using System;

namespace heitech.zer0mqXt.core
{
    ///<summary>
    /// Represents the Result of a zer0mqXt Message Interaction
    ///</summary>
    public sealed class XtResult<T>
        where T : class
    {
        ///<summary>
        /// Was the Operation successful?
        ///</summary>
        public bool IsSuccess => !(_result is null);
        ///<summary>
        /// Exception is not null if XtResult.IsSuccess is false
        ///</summary>
        public Exception Exception { get; private set; }

        ///<summary>
        /// For debugger purposes the Operation is documented
        ///</summary>
        public string Operation { get; private set; }


        private T _result;
        public T GetResult()
        {
            if (Exception is null)
                return _result;

            throw Exception;
        }

        internal static XtResult<T> Failed(Exception exception, string operation = "request")
        {
            return new XtResult<T> { Exception = exception };
        }

        internal static XtResult<T> Success(T result, string operation = "request")
        {
            if (result == null)
                return XtResult<T>.Failed(new NullReferenceException($"result of type {typeof(T)} was null"));
            return new XtResult<T> { _result = result, Exception = null, Operation = operation };
        }

        public override string ToString()
        {
            var success = IsSuccess ? " succeeded " : " failed";
            var firstPart = $"Result of [{Operation}] - has {success}";
            var second = IsSuccess ? _result?.GetType() : Exception?.GetType();

            return $"{firstPart} with Type [{second}]";
        }
    }
}