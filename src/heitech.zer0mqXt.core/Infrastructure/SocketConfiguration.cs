using System;
using System.Text;
using NetMQ;

namespace heitech.zer0mqXt.core.infrastructure
{
    public abstract class SocketConfiguration
    {
        ///<summary>
        /// Uses Newtonsoft by default. But you can also use Utf8Json or supply your own serialization
        ///</summary>
        public Serializer Serializer { get; set; }
        internal abstract string Address();
        ///<summary>
        /// Uses the BasicLogger implementation by default which only Logs to the console
        ///</summary>
        public ILogger Logger { get; set; }

        private protected SocketConfiguration()
        {
            Logger = new BasicLogger();
            Encoding = Encoding.UTF8;
            TimeOut = TimeSpan.FromSeconds(5);
            Serializer = Serializer.UseNewtonsoft(Encoding);
        }

        ///<summary>
        /// By default it is UTF8
        ///</summary>
        public Encoding Encoding { get; set; } 

        ///<summary>
        /// Default Timeout is 15 seconds
        ///</summary>
        public TimeSpan TimeOut { get; set; }

        public static Inproc InprocConfig(string name) => new Inproc(name);
        public static Tcp TcpConfig(string port) => new Tcp(port);
        public static Tcp TcpConfig(string port, string host) => new Tcp(port, host);

        public static void CleanUp() => NetMQConfig.Cleanup();

        public sealed class Tcp : SocketConfiguration
        {
            private readonly string _address;
            internal Tcp(string port, string host = "localhost")
            {
                _address = $"tcp://{host}:{port}";
            }

            internal override string Address()
            {
                return _address;
            }
        }

        public sealed class Inproc : SocketConfiguration
        {
            private readonly string _address;
            internal Inproc(string name)
            {
                _address = $"inproc://{name}";
            }

            internal override string Address()
            {
                return _address;
            }
        }
    }
}