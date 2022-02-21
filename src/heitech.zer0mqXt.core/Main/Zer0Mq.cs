using System;
using heitech.zer0mqXt.core.Adapters;
using heitech.zer0mqXt.core.infrastructure;

namespace heitech.zer0mqXt.core.Main
{
    ///<inheritdoc cref="IZer0MqBuilder"/>
    public sealed class Zer0Mq : IZer0MqBuilder
    {
        private bool _isSilent;
        private ILogger _logger;
        private TimeSpan _timeOut;
        private uint? _retryCount;
        private bool _developerMode;
        private ISerializerAdapter _serializer;

        private Zer0Mq()
        {
            _retryCount = 1;
            _logger = new BasicLogger();
            _timeOut = TimeSpan.FromSeconds(5);
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

        public IZer0MqBuilder SetRetryCount(uint count)
        {
            _retryCount = count;
            return this;
        }

        public IZer0MqBuilder DisableRetry()
        {
            _retryCount = null;
            return this;
        }

        public IZer0MqBuilder EnableDeveloperMode()
        {
            _developerMode = true;
            return this;
        }

        ///<summary>
        /// Entry point for building a new ISocket instance with the desired configuration
        ///</summary>
        public static IZer0MqBuilder Go() => new Zer0Mq();

        public IPatternFactory BuildWithInProc(string pipeName)
            => Build(new SocketConfiguration.Inproc(pipeName));

        public IPatternFactory BuildWithTcp(string host, string port)
            => Build(new SocketConfiguration.Tcp(port:port, host:host));

        private IPatternFactory Build(SocketConfiguration configuration)
        {
            configuration.Logger = _logger;
            configuration.Timeout = _timeOut;
            configuration.Serializer = _serializer;
            configuration.RetryCount = _retryCount;
            configuration.DeveloperMode = _developerMode;

            if (_isSilent)
                _logger.SetSilent();

            return new PatternFactory(configuration);
        }

        // for test purposes only
        internal static IPatternFactory From(SocketConfiguration configuration)
        {
            var mq = new Zer0Mq();
            return mq.Build(configuration);
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