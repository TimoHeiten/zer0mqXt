using System;
using System.Threading.Tasks;
using heitech.zer0mqXt.core.infrastructure;
using heitech.zer0mqXt.core.transport;
using NetMQ;
using NetMQ.Sockets;

namespace heitech.zer0mqXt.core.patterns
{
    public class Publisher : IDisposable, IPublisher
    {
        private PublisherSocket _publisherSocket;
        private readonly object _concurrencyToken = new();
        private readonly SocketConfiguration _configuration;
        private readonly Retry _retry;

        internal Publisher(SocketConfiguration configuration)
        {
            _retry = new Retry(configuration);
            _configuration = configuration;
        }

        private bool _isSetup => _publisherSocket != null;

        internal void SetupPublisher()
        {
            lock (_concurrencyToken)
            {
                if (!_isSetup)
                {
                    string address = _configuration.Address(_publisherSocket);
                    _configuration.Logger.Log(new DebugLogMsg($"Setup publisher at [{address}]"));
                    _publisherSocket = new PublisherSocket();

                    try
                    {
                        _publisherSocket.Bind(address);
                    }
                    catch (System.Exception ex)
                    {
                        _configuration.Logger.Log(new ErrorLogMsg($"Binding Publisher at [{address}] failed:{Environment.NewLine}" + ex));
                        throw;
                    }
                }
            }
        }
        public async Task<XtResult<TMessage>> SendAsync<TMessage>(TMessage message, string topic = null)
            where TMessage : class, new()
        {
            SetupPublisher();
            return await _retry.RunAsyncWithRetry(async () => await PublishAsync(message, topic));
        }

        private Task<XtResult<TMessage>> PublishAsync<TMessage>(TMessage message, string topic)
            where TMessage : class, new()
        {
            return Task.Run<XtResult<TMessage>>(() =>
            {
                try
                {
                    string topicFrame = _configuration.GetTopicFrame<TMessage>(topic);
                    byte[] pubSubMessage = _configuration.PubSubMessage(message);
                    _configuration.Logger.Log(new DebugLogMsg($"published to {topicFrame} for message {typeof(TMessage)}"));
                    _publisherSocket.SendMoreFrame(topicFrame).SendFrame(pubSubMessage);

                    return XtResult<TMessage>.Success(message);
                }
                catch (System.Exception ex)
                {
                    return XtResult<TMessage>.Failed(ex, "publish");
                }
            });
        }

        public void Dispose()
        {
            _publisherSocket?.Dispose();
        }
    }
}
