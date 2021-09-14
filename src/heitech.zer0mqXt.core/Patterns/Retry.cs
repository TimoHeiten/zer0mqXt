using System;
using System.Threading.Tasks;
using heitech.zer0mqXt.core.infrastructure;

namespace heitech.zer0mqXt.core.patterns
{
    ///<summary>
    /// Encapsulates the retry policy grasped from the configuration
    ///</summary>
    internal class Retry
    {
        private readonly SocketConfiguration _configuration;

        internal Retry(SocketConfiguration configuration)
        {
            this._configuration = configuration;
        }

        ///<summary>
        /// Register your retryable callback here
        ///</summary>
        internal async Task<XtResult<T>> RunAsyncWithRetry<T>(Func<Task<XtResult<T>>> retryableAction, string operationName = "request")
            where T : class
        {
            try
            {
                return await retryableAction().ConfigureAwait(false);
            }
            catch (NetMQ.EndpointNotFoundException ntfnd)
            {
                _configuration.Logger.Log(new ErrorLogMsg($"NetMQ.Endpoint could not be found at {_configuration.Address()}: " + ntfnd.Message));
                await Task.Delay((int)_configuration.TimeOut.TotalMilliseconds).ConfigureAwait(false);
                try
                {
                    return await retryableAction().ConfigureAwait(false);
                }
                catch (System.Exception inner)
                {
                    _configuration.Logger.Log(new ErrorLogMsg("Request failed after Retry: " + inner.Message));
                    return XtResult<T>.Failed(inner, operationName);
                }
            }
            catch (System.Exception ex)
            {
                _configuration.Logger.Log(new ErrorLogMsg("operation failed: " + ex.Message));
                return XtResult<T>.Failed(ex, operationName);
            }
        }
    }
}