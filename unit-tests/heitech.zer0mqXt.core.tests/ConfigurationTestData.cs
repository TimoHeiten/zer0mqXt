using System;
using System.Collections;
using System.Collections.Generic;
using heitech.zer0mqXt.core.infrastructure;
using heitech.zer0mqXt.core.Main;

namespace heitech.zer0mqXt.core.tests
{
    public class ConfigurationTestData : IEnumerable<object[]>
    {
        internal SocketConfiguration GetSocketConfigInProc
            => SocketConfiguration.InprocConfig("test-pipe" + Guid.NewGuid());

        internal SocketConfiguration GetSocketConfigTcp
            => SocketConfiguration.TcpConfig(port: "5566", host: "localhost");

        public IEnumerator<object[]> GetEnumerator()
        {
            var inProc = GetSocketConfigInProc;
            inProc.Logger.SetSilent();

            var tcp = GetSocketConfigTcp;
            tcp.TimeOut = TimeSpan.FromSeconds(15);
            tcp.Logger.SetSilent();
            
            yield return new object[] { inProc };
            yield return new object[] { tcp };
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();


        public static ISocket BuildInprocSocketInstanceForTest(string pipeName)
        {
            return Zer0Mq.Go().SilenceLogger().BuildWithInProc(pipeName);
        }
    }
}