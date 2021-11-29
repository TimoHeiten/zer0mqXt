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
    // FIXME currently only works correctly for inproc 
    internal class PubSub : IDisposable
    {
        private readonly SocketConfiguration _configuration;
        private readonly object _concurrencyToken = new object();
        private readonly List<IDisposable> _handlers = new List<IDisposable>();
        public PubSub(SocketConfiguration configuration)
        {
            _configuration = configuration;
        }

        private PublisherSocket _publisherSocket;

        internal void PrimePublisher()
        {
            try
            {
                _publisherSocket = new PublisherSocket();
                _publisherSocket.Bind(_configuration.Address());
            }
            catch (Exception ex)
            {
                _configuration.Logger.Log(new ErrorLogMsg(ex.Message));
                throw;
            }
        }


        #region Publishing
        public async Task<XtResult<TMessage>> PublishAsync<TMessage>(TMessage message)
            where TMessage : class, new()
        {
            const string operationName = "publish";
            if (_publisherSocket == null)
            {
                const string errorMsg = "publisherSocket was not primed (setup connection via NetMQ.Bind). When creating a Zer0MQ Bus, make sure to build it with UsePublisher.";
                _configuration.Logger.Log(new ErrorLogMsg(errorMsg));
                throw new NetMQException(errorMsg);
            }

            var retry = new Retry(_configuration);

            try
            {
                Func<Task<XtResult<TMessage>>> retryableAction = async () => await Task.Run(() => 
                {
                    try
                    {
                        var msg = new PubSubMessage<TMessage>(_configuration, message);
                        _publisherSocket.SendMultipartMessage(msg);
                    }
                    catch (Exception ex)
                    {
                        return XtResult<TMessage>.Failed(ex, operationName);
                    }

                    return XtResult<TMessage>.Success(message, operationName);
                }).ConfigureAwait(false);

                return await retry.RunAsyncWithRetry(retryableAction);
            }
            catch (Exception ex)
            {
                return XtResult<TMessage>.Failed(ex, operationName);
            }
            finally
            {
                _publisherSocket?.Dispose();
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
            Exception handlerException = null;
            // handle notifies when the server is set up
            eventHandle = new ManualResetEvent(false);

            _ = Task.Run(() => 
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

                var (success, resultingException) = next.Setup();
                handlerException = resultingException;
                // dispose handler when an exception was registered during setup
                if (handlerException is not null) 
                {
                    next.Dispose();
                    eventHandle.Set();
                    return;
                }

                // add subscriber to handlers to get rid of them later
                lock (_concurrencyToken)
                {
                    _handlers.Add(next);
                }
                // open resetevent after using setup on the handler and after the poller has started asynchronously
                eventHandle.Set();
            });

            eventHandle.WaitOne();
            if (handlerException is not null)
                return XtResult.Failed(handlerException, "register-subscriber");

            return XtResult.Success("register-subscriber");
        }

        private class SubscriberHandler<TMessage> : IDisposable
             where TMessage : class, new()
        {
            private readonly Action<TMessage> _syncCallback;
            private readonly Func<TMessage, Task> _asyncCallback;
            private readonly NetMQPoller _poller;
            private readonly CancellationToken _token;
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
                _token = token;
                _poller = poller;
                _socket = subscriberSocket;
                _configuration = configuration;
                _syncCallback = syncCallback;
                _asyncCallback = asyncCallback;
            }

            public (bool success, Exception ex) Setup()
            {
                try
                {
                    _socketDelegate = async (s, arg) => await HandleAsync().ConfigureAwait(false);
                    _socket.ReceiveReady += _socketDelegate;
                    
                    // todo use actual topics instead of catchall
                    string catchAllTopic = "";
                    _socket.Connect(_configuration.Address());
                    _socket.Subscribe(catchAllTopic);
                    _configuration.Logger.Log(new DebugLogMsg($"subscribed to [{typeof(TMessage)}]"));
                    _poller.Add(_socket);
                    _poller.RunAsync();

                    return (true, null);
                }
                catch (Exception ex)
                {
                    _configuration.Logger.Log(new ErrorLogMsg($"Subscribe did not work :[{ex.Message}]. Did you Prime the publisher for this socket?"));
                    return (false, ex);
                }
            }

            public async Task HandleAsync()
            {
                if (_token.IsCancellationRequested)
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
                        await _asyncCallback(msg).ConfigureAwait(false);
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
                            _socket?.Dispose();

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