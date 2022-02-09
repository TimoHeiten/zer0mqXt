using System;
using System.Threading.Tasks;
using heitech.zer0mqXt.core.infrastructure;

namespace heitech.zer0mqXt.core.transport
{
    ///<summary>
    /// Encapsulates the retry policy grasped from the configuration
    ///</summary>
    internal class Retry
    {
        private readonly SocketConfiguration _configuration;
        internal Retry(SocketConfiguration configuration) => _configuration = configuration;

        internal XtResult<T> RunWithRetry<T>(Func<XtResult<T>> retryableAction, string operationName = "setup")
            where T : class
        {
            return RunWithRetryAsync<T>
            (
                () =>
                {
                    var result = retryableAction();
                    return Task.FromResult(result);
                },
                operationName
            )
            .GetAwaiter()
            .GetResult();
        }

        ///<summary>
        /// Register your retryable callback here
        ///</summary>
        internal async Task<XtResult<T>> RunWithRetryAsync<T>(Func<Task<XtResult<T>>> retryableAction, string operationName = "request", Action tryToReconnect = null)
            where T : class
        {
            XtResult<T> result = await TryExecuteAsync(retryableAction, operationName);
            if (!_configuration.RetryIsActive || result.IsSuccess)
                return result;

            uint cntr = _configuration.RetryCount.Value;
            while (cntr > 0)
            {
                if (result.IsSuccess)
                    break;

                await Task.Delay(_configuration.Timeout).ConfigureAwait(false);
                result = await TryExecuteAsync(retryableAction, operationName).ConfigureAwait(false);

                _configuration.Logger.Log(new ErrorLogMsg($"Request failed after Retry - [{result?.Exception?.Message}] - retries left [{cntr}]"));

                cntr--;
            }
            return result;
        }

        private async Task<XtResult<T>> TryExecuteAsync<T>(Func<Task<XtResult<T>>> retryableAction, string operationName = "request")
            where T : class
        {
            try
            {
                return await retryableAction().ConfigureAwait(false);
            }
            catch (System.Exception ex)
            {
                _configuration.Logger.Log(new ErrorLogMsg("operation failed: " + ex.Message));
                return XtResult<T>.Failed(ex);
            }
        }

        private XtResult<T> TryExecute<T>(Func<XtResult<T>> retryableAction, string operationName = "request")
            where T : class
        {
            try
            {
                return retryableAction();
            }
            catch (System.Exception ex)
            {
                _configuration.Logger.Log(new ErrorLogMsg("operation failed: " + ex.Message));
                return XtResult<T>.Failed(ex);
            }
        }
    }
}