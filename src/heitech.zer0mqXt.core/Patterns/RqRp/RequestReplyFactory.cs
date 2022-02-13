using heitech.zer0mqXt.core.infrastructure;
using heitech.zer0mqXt.core.patterns.RqRp;

namespace heitech.zer0mqXt.core.RqRp
{
    internal static class RequestReplyFactory
    {
        private static object _concurrencyToken = new();
        public static IClient CreateClient(SocketConfiguration configuration)
        {
            var result = Client.TryInitialize(configuration);
            if (result.IsSuccess)
                return result.GetResult();

            configuration.Logger.Log(new ErrorLogMsg($"Failed to create Requester at address : [{configuration.Address()}]"));
            throw result.Exception;
        }

        public static IResponder CreateResponder(SocketConfiguration configuration)
            => new Responder(configuration);

    }
}
