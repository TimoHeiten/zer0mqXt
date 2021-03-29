using System;
using System.Collections;
using System.Collections.Generic;
using heitech.zer0mqXt.core.infrastructure;
using heitech.zer0mqXt.core.patterns;

namespace heitech.zer0mqXt.core.tests
{
    public class ConfigurationTestData : IEnumerable<object[]>
    {
        public SocketConfiguration GetSocketConfigInProc
            => SocketConfiguration.InprocConfig("test-pipe");

        public SocketConfiguration GetSocketConfigTcp
            => SocketConfiguration.TcpConfig(port: "5566", host: "localhost");

        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[] { GetSocketConfigInProc };
            yield return new object[] { GetSocketConfigTcp };
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public static Wrapper<PubSub> CreatePubSub(SocketConfiguration configuration)
            => new Wrapper<PubSub>(new PubSub(configuration));

        public static Wrapper<Socket> CreateSocket(SocketConfiguration configuration)
            => new Wrapper<Socket>(new Socket(configuration));

        public class Wrapper<T> : IDisposable
            where T : IDisposable
        {
            public T Socket;
            public Wrapper(T socket)
            {
                this.Socket = socket;
            }

            public void Dispose()
            {
                Socket.Dispose();
            }
        }
    }
}