using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using heitech.zer0mqXt.core.infrastructure;
using heitech.zer0mqXt.core.transport;
using NetMQ;
using NetMQ.Sockets;

namespace heitech.zer0mqXt.core.patterns
{
    public class Subscriber : IDisposable
    {
        private DoWorkEventHandler _workHandler;
        private readonly SubscriberSocket _socket;
        private readonly SocketConfiguration _configuration;
        private readonly List<BackgroundWorker> _workers = new();

        internal Subscriber(SocketConfiguration configuration)
        {
            _configuration = configuration;
            _socket = new SubscriberSocket();
        }

        public void Dispose()
        {
            _workers.ForEach(x => { x.CancelAsync(); x.Dispose(); });
            _socket.Dispose();
            _workHandler = null;
        }

        // todo error handling etc.
        // also add async handler
        public void RegisterSubscriber<T>(Action<T> callback, string topic = null, CancellationToken token = default)
            where T : class, new()
        {
            string topicFrame = topic ?? _configuration.Serializer.Encoding.GetString(Message<T>.CreateTypeFrame(_configuration.Serializer));
            string address = _configuration.Address();

            _socket.Connect(address);
            _socket.Subscribe(topicFrame);
            System.Console.WriteLine($"subscribed with {address} and topic: {topicFrame}");
            var next = new BackgroundWorker();
            _workHandler = (s, e) =>
            {
                while (!token.IsCancellationRequested)
                {
                    var receivedtopic = _socket.ReceiveFrameString();
                    var receivedMsg = _socket.ReceiveFrameBytes();

                    var actualMessage = _configuration.Serializer.Deserialize<T>(receivedMsg);
                    callback(actualMessage);
                }
            };
            next.DoWork += _workHandler;
            _workers.Add(next);

            next.RunWorkerAsync();
        }
    }
}
