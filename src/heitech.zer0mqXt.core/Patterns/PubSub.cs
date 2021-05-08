using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using heitech.zer0mqXt.core.infrastructure;
using heitech.zer0mqXt.core.transport;
using NetMQ;
using NetMQ.Sockets;

namespace heitech.zer0mqXt.core.patterns
{
    // currently only works correctly for inproc 
    internal class PubSub : IDisposable
    {
        private readonly SocketConfiguration _configuration;
        private readonly object _concurrencyToken = new object();
        private readonly List<IDisposable> _handlers = new List<IDisposable>();
        public PubSub(SocketConfiguration configuration)
        {
            this._configuration = configuration;
        }

        #region Publishing
        public async Task<XtResult<TMessage>> PublishAsync<TMessage>(TMessage message)
            where TMessage : class, new()
        {
            PublisherSocket publisherSocket = null;
            try
            {
                publisherSocket = new PublisherSocket();
                publisherSocket.Connect(_configuration.Address());
                return await Task.Run(() => 
                {
                    try
                    {
                        var msg = new PubSubMessage<TMessage>(_configuration, message);
                        publisherSocket.SendMultipartMessage(msg);
                    }
                    catch (System.Exception ex)
                    {
                        return XtResult<TMessage>.Failed(ex, "publish");
                    }

                    return XtResult<TMessage>.Success(message, "publish");
                });
                
            }
            catch (System.Exception ex)
            {
                return XtResult<TMessage>.Failed(ex, "publish");
            }
            finally
            {
                publisherSocket?.Dispose();
            }
        }
        #endregion

        #region Subscribing
        private ManualResetEvent eventHandle;
        ///<summary>
        /// Register a Subscriber for the type TMessage
        /// <para>The unsubscribe callback is used to stop the subscriber</para>
        ///</summary>
        public XtResult SubscribeHandler<TMessage>(Action<TMessage> callback, CancellationToken token)
            where TMessage : class, new()
        {
            return RegisterHandler<TMessage>(syncCallback: callback, token: token);
        }

        public XtResult SubscribeHandlerAsync<TMessage>(Func<TMessage, Task> asyncCallback, CancellationToken token)
            where TMessage : class, new()
        {
            return RegisterHandler<TMessage>(asyncCallback: asyncCallback, token: token);
        }

        private XtResult RegisterHandler<TMessage>(Action<TMessage> syncCallback = null, Func<TMessage, Task> asyncCallback = null, CancellationToken token = default)
            where TMessage : class, new()
        {
            Exception exception = null;
            // handle notifies when the server is set up
            eventHandle = new ManualResetEvent(false);

            var task = Task.Run(() => 
            {
                var next = new SubscriberHandler<TMessage>
                (
                    new SubscriberSocket(), 
                    _configuration, 
                    new NetMQ.NetMQPoller(), 
                    token, 
                    syncCallback: syncCallback, 
                    asyncCallback: asyncCallback
                );

                var (success, exception) = next.Setup();
                // open resetevent after using setup on the handler and after the poller has started asynchronously
                // dispose handler when an exception was registered during setup
                if (exception is not null) 
                {
                    next.Dispose(); 
                    return;
                }

                // add subscriber to dispsables to get rid of them later
                lock (_concurrencyToken)
                {
                    _handlers.Add(next);
                }
                eventHandle.Set();
            });

            eventHandle.WaitOne();
            if (exception is not null)
                return XtResult.Failed(exception, "register-subscriber");

            return XtResult.Success("register-subscriber");
        }

        private class SubscriberHandler<TMessage> : IDisposable
             where TMessage : class, new()
        {
            private readonly Action<TMessage> _syncCallback;
            private readonly Func<TMessage, Task> _asyncCallback;
            private readonly NetMQPoller _poller;
            private readonly CancellationToken token;
            private readonly SubscriberSocket _socket;
            private readonly SocketConfiguration _configuration;
            private bool _disposedValue;
            private EventHandler<NetMQSocketEventArgs> _socketDelegate;

            internal SubscriberHandler(SubscriberSocket subscriberSocket,
                                       SocketConfiguration configuration,
                                       NetMQPoller poller,
                                       CancellationToken token,
                                       Action<TMessage> syncCallback = null,
                                       Func<TMessage, Task> asyncCallback = null)
            {
                this.token = token;
                this._poller = poller;
                this._socket = subscriberSocket;
                this._configuration = configuration;
                this._syncCallback = syncCallback;
                this._asyncCallback = asyncCallback;
            }

            public (bool success, Exception ex) Setup()
            {
                Exception exception = null;
                try
                {
                    _socketDelegate = async (s, arg) => await HandleAsync();
                    _socket.ReceiveReady += _socketDelegate;
                    
                    string catchAllTopic = "";
                    _socket.Bind(_configuration.Address());
                    // todo use actual topics instead of catchall
                    _socket.Subscribe(catchAllTopic);
                    _configuration.Logger.Log(new DebugLogMsg($"subscribed to [{typeof(TMessage)}]"));
                    _poller.Add(_socket);
                    _poller.RunAsync();

                    return (true, null);
                }
                catch (NetMQ.EndpointNotFoundException ntfnd)
                {
                    _configuration.Logger.Log(new ErrorLogMsg(ntfnd.Message));
                    exception = ntfnd;
                    return (false, exception);
                }
            }

            public async Task HandleAsync()
            {
                if (this.token.IsCancellationRequested)
                {
                    Dispose();
                    _configuration.Logger.Log(new InfoLogMsg("SubscriberHandler was cancelled by cancellationRequest"));
                    return;
                }

                try
                {
                    NetMQMessage received = _socket.ReceiveMultipartMessage();
                    _configuration.Logger.Log(new DebugLogMsg($"handling message for [Subscriber:{typeof(TMessage)}]"));
                    var actualMessage = received.ParsePubSubMessage<TMessage>(_configuration);

                    var msg = actualMessage.IsSuccess ? actualMessage.GetResult() : new TMessage();
                    if (this._asyncCallback is null)
                        _syncCallback(msg);
                    else
                        await _asyncCallback(msg);
                }
                catch (NetMQ.TerminatingException trmnt)
                {
                    _configuration.Logger.Log(new ErrorLogMsg($"Subscriber handle for [Message:{typeof(TMessage)}] did fail: " + trmnt.Message));
                }
                catch (System.Exception ex)
                {
                    _configuration.Logger.Log(new ErrorLogMsg($"Subscriber handle for [Message:{typeof(TMessage)}] did fail: " + ex.Message));
                }
            }

            protected virtual void Dispose(bool disposing)
            {
                if (!_disposedValue)
                {
                    if (disposing)
                    {
                        if (_poller != null && _poller.IsRunning)
                            _poller.Stop();
                        else 
                            _socket.Dispose();

                        if (_socket != null && _socketDelegate != null)
                            _socket.ReceiveReady -= _socketDelegate;

                    }
                    _disposedValue = true;
                }
            }

            public void Dispose()
            {
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }
        }
        #endregion

        #region Dispose
        private bool disposedValue;

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                disposedValue = true;
                _handlers.ForEach(x => x.Dispose());
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}