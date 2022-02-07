using System;
using System.Collections.Generic;
using heitech.zer0mqXt.core.infrastructure;
using heitech.zer0mqXt.core.patterns.RqRp;
using heitech.zer0mqXt.core.utils;

namespace heitech.zer0mqXt.core.RqRp
{
    internal static class RequestReplyFactory
    {
        private static object _concurrencyToken = new();
        private static Dictionary<SocketConfiguration, IClient> _requestCache = new();
        private static Dictionary<SocketConfiguration, IResponder> _responderCache = new();

        public static IClient CreateClient(SocketConfiguration configuration)
        {
            var result = Client.TryInitialize(configuration);
            if (result.IsSuccess)
                return configuration.Create<IClient>(_concurrencyToken, _requestCache, (c) => result.GetResult());
            else if (_requestCache.TryGetValue(configuration, out IClient client))
                return client;

            configuration.Logger.Log(new ErrorLogMsg($"Failed to create Requester at address : [{configuration.Address()}]"));
            throw result.Exception;
        }

        public static IResponder CreateResponder(SocketConfiguration configuration)
            => configuration.Create<IResponder>(_concurrencyToken, _responderCache, (c) => new Responder(c));

        internal static void KillRequester(SocketConfiguration configuration)
            => configuration.Kill<IClient>(_concurrencyToken, _requestCache);

        internal static void KillResponder(SocketConfiguration configuration)
        => configuration.Kill<IResponder>(_concurrencyToken, _responderCache);
    }
}
