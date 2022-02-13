using heitech.zer0mqXt.core.infrastructure;
using heitech.zer0mqXt.core.utils;

namespace heitech.zer0mqXt.core.PubSub
{
    internal static class PubSubFactory
    {
        private static readonly object _concurrencyToken = new();
        internal static IPublisher CreatePublisher(SocketConfiguration configuration)
        {
            var result = Publisher.TryInitialize(configuration);
            if (result.IsSuccess)
                return result.GetResult();

            configuration.Logger.Log(new ErrorLogMsg($"Failed to create Requester at address : [{configuration.Address()}]"));
            throw ZeroMqXtSocketException.FromException(result.Exception);
        }

        internal static ISubscriber CreateSubscriber(SocketConfiguration socketConfiguration)
            => new Subscriber(socketConfiguration); // we can have as many Subscribers as we want but one publisher
    }
}
