using heitech.zer0mqXt.core.infrastructure;
using heitech.zer0mqXt.core.patterns.RqRp;
using heitech.zer0mqXt.core.transport;

namespace heitech.zer0mqXt.core.RqRp
{
    internal static class RequestReplyFactory
    {
        private static object _concurrencyToken = new();
        public static IClient CreateClient(SocketConfiguration configuration)
        {
            var retry = new Retry(configuration);
            // try to create a client by connecting to a socket.
            // for inproc it can fail if there is no responder already that binds to the socket
            // therefore we use the retry here
            var clientResult = retry.RunWithRetry<IClient>
            (
                () => 
                {
                    var result = Client.TryInitialize(configuration);
                    if (result.IsSuccess)
                    {
                        return result;
                    }
                    return result;
                }
            );

            if (clientResult.IsSuccess)
                return clientResult.GetResult();

            // if neither the retry nor the cache succeeds, we log and throw
            configuration.Logger.Log(new ErrorLogMsg($"Failed to create Requester at address : [{configuration.Address()}]"));
            throw ZeroMqXtSocketException.FromException(clientResult.Exception);
        }

        public static IResponder CreateResponder(SocketConfiguration configuration)
            => new Responder(configuration);
    }
}
