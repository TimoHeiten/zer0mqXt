using System;
using System.Collections.Generic;
using System.Linq;
using heitech.zer0mqXt.core.infrastructure;

namespace heitech.zer0mqXt.core.utils
{
    internal static class FactoryHelpers
    {
        internal static T Create<T>(this SocketConfiguration configuration, object concurrencytoken, Dictionary<SocketConfiguration, T> cache, Func<SocketConfiguration, T> factory)
        {
            lock (concurrencytoken)
            {
                if (cache.TryGetValue(configuration, out T item))
                    return item;

                item = factory(configuration);
                cache.Add(configuration, item);

                return item;
            }
        }

        internal static void KillAll<T1, T2>(this Dictionary<SocketConfiguration, T1> first, Dictionary<SocketConfiguration, T2> snd)
            where T1 : IDisposable
            where T2 : IDisposable
        {
            first.Values.Cast<IDisposable>()
                         .Concat(snd.Values.Cast<IDisposable>())
                         .ToList()
                         .ForEach(x => x.Dispose());
        }

        internal static void Kill<T>(this SocketConfiguration key, object concurrencytoken, Dictionary<SocketConfiguration, T> cache)
           where T : IDisposable
        {
            lock (concurrencytoken)
            {
                if (cache.ContainsKey(key))
                    cache.Remove(key);
            }
        }
    }
}
