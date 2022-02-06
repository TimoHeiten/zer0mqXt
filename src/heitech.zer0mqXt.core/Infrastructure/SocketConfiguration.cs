using System;
using System.Text;
using heitech.zer0mqXt.core.Adapters;
using NetMQ;
using NetMQ.Sockets;

namespace heitech.zer0mqXt.core.infrastructure
{
    internal abstract class SocketConfiguration
    {
        ///<summary>
        /// Uses Newtonsoft by default. But you can also use Utf8Json or supply your own serialization
        ///</summary>
        public ISerializerAdapter Serializer { get; set; }
        internal abstract string Address(NetMQSocket netMQSocket = null);
        ///<summary>
        /// Uses the BasicLogger implementation by default which only Logs to the console
        ///</summary>
        public ILogger Logger { get; set; }

        private protected SocketConfiguration()
        {
            Logger = new BasicLogger();

            var adapter = new InternalAdapter();
            Encoding = adapter.Encoding;
            Serializer = adapter;

            RetryIsActive = true;
            Timeout = TimeSpan.FromSeconds(5);
        }

        ///<summary>
        /// By default it is UTF8
        ///</summary>
        public Encoding Encoding { get; set; } 

        ///<summary>
        /// Default Timeout is 5 seconds
        ///</summary>
        public TimeSpan Timeout { get; set; }

        public bool RetryIsActive { get; set; }

        public static SocketConfiguration InprocConfig(string name) => new Inproc(name);
        public static SocketConfiguration TcpConfig(string port) => new Tcp(port);
        public static SocketConfiguration TcpConfig(string port, string host) => new Tcp(port, host);

        public static void CleanUp() {  }

        public sealed class Tcp : SocketConfiguration
        {
            private readonly string _address;
            private readonly string _port;
            private readonly string _host;
            internal Tcp(string port, string host = "localhost")
            {
                _host = host; _port = port;
                _address = $"tcp://{host}:{port}";
            }

            internal override string Address(NetMQSocket socket = null)
            {
                if (socket != null && socket.GetType() == typeof(PublisherSocket))
                {
                    return $"tcp://*:{_port}";
                }
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

            internal override string Address(NetMQSocket socket = null)
            {
                return _address;
            }
        }
    }
}