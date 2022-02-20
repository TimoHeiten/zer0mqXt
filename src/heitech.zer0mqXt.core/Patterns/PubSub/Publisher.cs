using System;
using System.Threading.Tasks;
using heitech.zer0mqXt.core.infrastructure;
using heitech.zer0mqXt.core.transport;
using NetMQ;
using NetMQ.Sockets;

namespace heitech.zer0mqXt.core.PubSub
{
    public class Publisher : IPublisher
    {
        private bool disposedValue;
        private PublisherSocket _publisherSocket;
        private readonly object _concurrencyToken = new();
        private readonly SocketConfiguration _configuration;

        private Publisher(SocketConfiguration configuration, PublisherSocket socket)
        {
            _publisherSocket = socket;
            _publisherSocket.Options.SendHighWatermark = 0;
            _configuration = configuration;
        }

        internal static XtResult<IPublisher> TryInitialize(SocketConfiguration configuration)
        {
            var socket = new PublisherSocket();
            string address = configuration.Address(socket);
            configuration.Logger.Log(new DebugLogMsg($"Setup publisher at [{address}]"));

            try
            {
                socket.Bind(address);
                return XtResult<IPublisher>.Success(new Publisher(configuration, socket));
            }
            catch (System.Exception ex)
            {
                configuration.Logger.Log(new ErrorLogMsg($"Binding Publisher at [{address}] failed:{Environment.NewLine}" + ex));
                return XtResult<IPublisher>.Failed(ZeroMqXtSocketException.FromException(ex));
            }
        }

        public async Task<XtResult<TMessage>> SendAsync<TMessage>(TMessage message, string topic = null)
            where TMessage : class, new()
        {
            return await Task.Run(() => 
            {
                try
                {
                    string topicFrame = _configuration.GetTopicFrame<TMessage>(topic);
                    byte[] payload = _configuration.PubSubMessage(message);
                    _configuration.Logger.Log(new DebugLogMsg($"published to {topicFrame} for message {typeof(TMessage)}"));

                    _publisherSocket.SendMoreFrame(topicFrame)
                                    .SendFrame(payload);

                    return XtResult<TMessage>.Success(message);
                }
                catch (System.Exception ex)
                {
                    return XtResult<TMessage>.Failed(ex, "publish");
                }
            });
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                     _publisherSocket?.Dispose();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
