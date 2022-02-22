using System;
using System.Threading;
using System.Threading.Tasks;
using heitech.zer0mqXt.core.infrastructure;
using heitech.zer0mqXt.core.transport;
using heitech.zer0mqXt.core.utils;
using NetMQ;
using NetMQ.Sockets;

namespace heitech.zer0mqXt.core.PubSub
{
    internal class Subscriber : ISubscriber
    {
        private readonly NetMQPoller _poller;
        private readonly SubscriberSocket _socket;
        private readonly SocketConfiguration _configuration;

        private EventHandler<NetMQSocketEventArgs> _receiveHandler;
        private bool disposedValue;
        private bool _isSetup = false;
        private object _concurrencyToken = new();

        internal Subscriber(SocketConfiguration configuration)
        {
            _poller = configuration.SubscriberPollerInstance;
            _socket = new();
            _socket.Options.ReceiveHighWatermark = 0;
            _configuration = configuration;
        }

        public XtResult RegisterSubscriber<TMessage>(Action<TMessage> callback, Action onError = null, string topic = null, CancellationToken cancellationToken = default)
            where TMessage : class, new()
        {
            lock (_concurrencyToken)
            {
                if (!_isSetup)
                {
                    var handler = MessageHandler<TMessage>.Sync(_configuration, callback);
                    _isSetup = true;
                    return SetupMessageHandler<TMessage>(
                        handler, 
                        topic, 
                        () => { onError?.Invoke(); return Task.CompletedTask; }, 
                        cancellationToken
                    );
                }
                 return XtResult.Failed(new ZeroMqXtSocketException("already setup this subscriber! Create a New One instead"), "setup-subscriber");
            }
        }

        public XtResult RegisterAsyncSubscriber<TMessage>(Func<TMessage, Task> asyncCallback, Func<Task> onError = null, string topic = null, CancellationToken cancellationToken = default) where TMessage : class, new()
        {
            lock (_concurrencyToken)
            {
                if (!_isSetup)
                {
                    var handler = MessageHandler<TMessage>.ASync(_configuration, asyncCallback);
                    _isSetup = true;
                    return SetupMessageHandler<TMessage>(handler, topic, onError, cancellationToken);
                }
                return XtResult.Failed(new ZeroMqXtSocketException("already setup this subscriber! Create a New One instead"), "setup-subscriber");
            }
        }


        private XtResult SetupMessageHandler<TMessage>(MessageHandler<TMessage> handler, string topic, Func<Task> onError, CancellationToken token)
            where TMessage : class, new()
        {
            var waitHandle = new ManualResetEvent(false);
            try
            {
                // connect and setup and subscrube to the ready event for non blocking interactions
                string topicFrame = _configuration.GetTopicFrame<TMessage>(topic);
                _configuration.Logger.Log(new DebugLogMsg($"subscribing to '{topicFrame}' for message {typeof(TMessage)} at {_configuration.Address()}"));
                _socket.Connect(_configuration.Address());

                _socket.Subscribe(topicFrame);
                
                _receiveHandler = async (s, e)
                => await handler.OnReceive(e.Socket, onError, handler, Dispose, token).ConfigureAwait(false);

                _socket.ReceiveReady += _receiveHandler;

                // add to poller and register handler
                _poller.Add(_socket);
                if (!_poller.IsRunning)
                    _poller.RunAsync();
            }
            catch (Exception exception)
            {
                _configuration.Logger.Log(new ErrorLogMsg(exception?.GetType().Name + "-" + exception.Message));
                Dispose();
            }
            finally
            {
                // open resetevent after binding to the socket and when the poller is started
                waitHandle.Set();
            }

            bool wasSignaled = waitHandle.WaitOne(500);
            return wasSignaled
                   ? XtResult.Success("setup-subscriber")
                   : XtResult.Failed(new ZeroMqXtSocketException("setup-susbcriber-failed"));
        }


        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _poller.DisposeOf(_socket, _receiveHandler);
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
