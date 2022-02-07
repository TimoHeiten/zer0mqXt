using System;
using heitech.zer0mqXt.core.PubSub;

namespace heitech.zer0mqXt.core.tests
{
    public class SUT<T1, T2> : IDisposable
        where T1 : IDisposable
        where T2 : IDisposable
    {
        public T1 Server;
        public T2 Client;
        protected SUT(Func<T1> serverFactory, Func<T2> clientFactory)
        {
            Server = serverFactory();
            Client = clientFactory();
        }

        public static SUT<IPublisher, ISubscriber> PubSub(IPatternFactory pattern) 
            => new SUT<IPublisher, ISubscriber>(pattern.CreatePublisher, pattern.CreateSubscriber);

        public void Dispose()
        {
            Server.Dispose();
            Client.Dispose();
        }
    }
}