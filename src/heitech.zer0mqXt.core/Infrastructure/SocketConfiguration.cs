using System;
using System.Text;
using heitech.zer0mqXt.core.Adapters;
using NetMQ;
using NetMQ.Sockets;

namespace heitech.zer0mqXt.core.infrastructure
{
    internal abstract class SocketConfiguration : IEquatable<SocketConfiguration>
    {
        ///<summary>
        /// Only one Poller is allowed for the same configuration (same port and all). So each Thread does only access it with the same Poller.
        ///</summary>
        public NetMQPoller SubscriberPollerInstance { get; }

        ///<summary>
        /// Uses Newtonsoft by default. But you can also use Utf8Json or supply your own serialization
        /// Uses System.Text by default. But you can also use Newtonsoft or supply your own serialization adapter
        ///</summary>
        public ISerializerAdapter Serializer
        {
            get => _serializer ??= new InternalAdapter();
            internal set => _serializer = value;
        }
        private ISerializerAdapter _serializer;

        public bool DeveloperMode { get; internal set; } = false;
        internal abstract string Address(NetMQSocket netMQSocket = null);
        ///<summary>
        /// Uses the BasicLogger implementation by default which only Logs to the console
        ///</summary>
        public ILogger Logger 
        {
            get => _logger ??= new BasicLogger();
            set => _logger = value;
        }
        private ILogger _logger;

        ///<summary>
        /// By default it is UTF8
        ///</summary>
        public Encoding Encoding => Serializer.Encoding;

        ///<summary>
        /// Default Timeout is 5 seconds
        ///</summary>
        public TimeSpan Timeout { get; internal set; }

        public uint? RetryCount { get; internal set; }
        public bool RetryIsActive => RetryCount.HasValue && RetryCount.Value > 0;

        private protected SocketConfiguration()
        {
            RetryCount = 1;
            SubscriberPollerInstance = new();
            Timeout = TimeSpan.FromSeconds(5);
        }

        public static SocketConfiguration InprocConfig(string name) => new Inproc(name);
        public static SocketConfiguration TcpConfig(string port) => new Tcp(port);
        public static SocketConfiguration TcpConfig(string port, string host) => new Tcp(port, host);

        public override int GetHashCode()
            => Address().GetHashCode();

        public bool Equals(SocketConfiguration other)
            => other == null || other.Address() == this.Address();

        public override bool Equals(object obj)
            => Equals(obj as SocketConfiguration);
        
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