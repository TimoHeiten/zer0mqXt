using System;
using heitech.zer0mqXt.core.infrastructure;
using heitech.zer0mqXt.core.transport;
using NetMQ;
using NetMQ.Sockets;

namespace heitech.zer0mqXt.core.patterns
{
    public class Publisher : IDisposable
    {
        private readonly SocketConfiguration _configuration;
        private readonly object _concurrencyToken = new();
        private PublisherSocket _publisherSocket;
        internal Publisher(SocketConfiguration configuration)
        {
            _configuration = configuration;
        }

        private bool _isSetup => _publisherSocket != null;
        // todo retry and xtresult, also async
        public void Send<TMessage>(TMessage message, string topic = null)
            where TMessage : class, new()
        {
            lock (_concurrencyToken)
            {
                if (!_isSetup)
                {
                    string address = _configuration.Address(_publisherSocket);
                    Print(new DebugLogMsg($"Setup publisher at [{address}]"));
                    _publisherSocket = new PublisherSocket();
                    _publisherSocket.Bind(address);
                }
            }

            string topicFrame = topic ?? _configuration.Serializer.Encoding.GetString(Message<TMessage>.CreateTypeFrame(_configuration.Serializer));
            System.Console.WriteLine($"send topic: {topicFrame}");
            NetMQMessage pubSubMessage = new PubSubMessage<TMessage>(_configuration, message, topicFrame);
            
            _publisherSocket.SendMoreFrame(topicFrame).SendFrame(pubSubMessage.Last.ToByteArray());
        }

        private void Print(LogMessage msg)
        {
            _configuration.Logger.Log(msg);
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
