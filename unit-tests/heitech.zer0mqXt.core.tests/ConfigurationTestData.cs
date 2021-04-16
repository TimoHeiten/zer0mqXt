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
            => SocketConfiguration.InprocConfig("test-pipe" + Guid.NewGuid());

        public SocketConfiguration GetSocketConfigTcp
            => SocketConfiguration.TcpConfig(port: "5566", host: "localhost");

        public IEnumerator<object[]> GetEnumerator()
        {
            var inProc = GetSocketConfigInProc;
            inProc.Logger.SetSilent();

            var tcp = GetSocketConfigTcp;
            tcp.TimeOut = TimeSpan.FromSeconds(10);
            tcp.Logger.SetSilent();
            
            yield return new object[] { inProc };
            yield return new object[] { tcp };
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