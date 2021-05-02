using System;
using System.Collections;
using System.Collections.Generic;
using heitech.zer0mqXt.core.infrastructure;

namespace heitech.zer0mqXt.core.tests
{
    public class ConfigurationTestData : IEnumerable<object[]>
    {
        internal SocketConfiguration GetSocketConfigInProc
        {
            get 
            {
                var c = SocketConfiguration.InprocConfig("test-pipe" + Guid.NewGuid());
                c.Logger.SetSilent();
                return c;
            }

        }

        internal SocketConfiguration GetSocketConfigTcp
        {
            get 
            {
                var c = SocketConfiguration.TcpConfig(port: "5566", host: "localhost");
                c.Logger.SetSilent();
                return c;
            }
        }

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
    }
}