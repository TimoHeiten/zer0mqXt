using System;

namespace heitech.zer0mqXt.core
{
    public class XtResult<T>
        where T : class
    {
        public bool IsSuccess => !(_result is null);
        public Exception Exception { get; private set; }

        public string Operation { get; private set; }


        private T _result;
        public T GetResult()
        {
            if (Exception is null)
                return _result;

            throw Exception;
        }

        public static XtResult<T> Failed(Exception exception, string operation = "request")
        {
            return new XtResult<T> { Exception = exception };
        }

        public static XtResult<T> Success(T result, string operation = "request")
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