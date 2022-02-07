using System;
using System.Collections.Generic;
using heitech.zer0mqXt.core.infrastructure;
using heitech.zer0mqXt.core.utils;

namespace heitech.zer0mqXt.core.PubSub
{
    internal static class PubSubFactory
    {
        private static readonly object _concurrencyToken = new();
        private static readonly Dictionary<SocketConfiguration, ISubscriber> _subscriberCache = new();
        private static readonly Dictionary<SocketConfiguration, IPublisher> _publisherCache = new();
        internal static IPublisher CreatePublisher(SocketConfiguration configuration)
        {
             var result = Publisher.TryInitialize(configuration);
            if (result.IsSuccess)
                return configuration.Create<IPublisher>(_concurrencyToken, _publisherCache, (c) => result.GetResult());

            configuration.Logger.Log(new ErrorLogMsg($"Failed to create Requester at address : [{configuration.Address()}]"));
            throw result.Exception;
        }

        internal static ISubscriber CreateSubscriber(SocketConfiguration socketConfiguration)
            => new Subscriber(socketConfiguration); // we can have as many Subscribers as we want but one publisher

        internal static void KillPubslisher(this SocketConfiguration key)
            => key.Kill<IPublisher>(_concurrencyToken, _publisherCache);
    }
}
