using System;
using heitech.zer0mqXt.core.Adapters;
using heitech.zer0mqXt.core.infrastructure;

namespace heitech.zer0mqXt.core.Main
{
    ///<inheritdoc cref="IZer0MqBuilder"/>
    public class Zer0Mq : IZer0MqBuilder
    {
        private bool _isSilent;
        private ILogger _logger;
        private TimeSpan _timeOut;
        private ISerializerAdapter _serializer;

        private Zer0Mq()
        {
            _timeOut = TimeSpan.FromSeconds(5);
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

        ///<summary>
        /// Entry point for building a new ISocket instance with the desired configuration
        ///</summary>
        public static IZer0MqBuilder Go() => new Zer0Mq();

        public ISocket BuildWithInProc(string pipeName)
            => Build(new SocketConfiguration.Inproc(pipeName));

        public ISocket BuildWithTcp(string host, string port)
            => Build(new SocketConfiguration.Tcp(port:port, host:host));

        private ISocket Build(SocketConfiguration configuration)
        {
            configuration.Logger = _logger;
            configuration.Timeout = _timeOut;
            configuration.Serializer = _serializer;

            if (_isSilent)
                _logger.SetSilent();

            var socket = new Socket(configuration);

            return socket;
        }

        public IZer0MqBuilder SilenceLogger()
        {
            _isSilent = true;
            return this;
        }

        public IZer0MqBuilder SetTimeOut(long timeOutInMs)
        {
            _timeOut = TimeSpan.FromMilliseconds(timeOutInMs);
            return this;
        }

        public IZer0MqBuilder SetTimeOut(TimeSpan timeOut)
        {
            _timeOut = timeOut;
            return this;
        }
    }
}