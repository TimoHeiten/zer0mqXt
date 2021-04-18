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
            _serializer = new InternalAdapters();
        }

        public static IZer0MqBuilder Go() => new Zer0Mq();

        public IEntry BuildWithInProc(string pipeName)
            => Build(new SocketConfiguration.Inproc(pipeName));

        public IEntry BuildWithTcp(string host, string port)
            => Build(new SocketConfiguration.Tcp(port:port, host:host));

        private IEntry Build(SocketConfiguration configuration)
        {
            configuration.Serializer = _serializer;
            configuration.Logger = _logger;

            return new Bus(configuration);
        }
    }
}