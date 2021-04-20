using heitech.zer0mqXt.core.Adapters;
using heitech.zer0mqXt.core.infrastructure;

namespace heitech.zer0mqXt.core.Main
{
    public class Zer0Mq : IZer0MqBuilder
    {
        private ILogger _logger;
        private ISerializerAdapter _serializer;
        private Zer0Mq()
        {
            _logger = new BasicLogger();
            _serializer = new InternalAdapter();
        }

        public IZer0MqBuilder SetSerializer(ISerializerAdapter adapter)
        {
            _serializer = adapter;
            return this;
        }

        public IZer0MqBuilder SetLogger(ILogger adapter)
        {
            _logger = adapter;
            return this;
        }

        public static IZer0MqBuilder Go() => new Zer0Mq();

        public ISocket BuildWithInProc(string pipeName)
            => Build(new SocketConfiguration.Inproc(pipeName));

        public ISocket BuildWithTcp(string host, string port)
            => Build(new SocketConfiguration.Tcp(port:port, host:host));

        private ISocket Build(SocketConfiguration configuration)
        {
            configuration.Serializer = _serializer;
            configuration.Logger = _logger;

            return new Socket(configuration);
        }
    }
}