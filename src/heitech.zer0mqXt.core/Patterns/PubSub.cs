using System;
using System.Threading;
using System.Threading.Tasks;
using heitech.zer0mqXt.core.infrastructure;
using heitech.zer0mqXt.core.transport;
using NetMQ;
using NetMQ.Sockets;

namespace heitech.zer0mqXt.core.patterns
{
    // currently only works correctly for inproc 
    public class PubSub : IDisposable
    {
        private readonly SocketConfiguration _configuration;
        public PubSub(SocketConfiguration configuration)
        {
            this._configuration = configuration;
        }

        public async Task<XtResult<TMessage>> PublishAsync<TMessage>(TMessage message)
            where TMessage : class
        {
            using var pubSocket = new PublisherSocket();
            pubSocket.Connect(_configuration.Address());

            return await Task.Run(() => 
            {
                try
                {
                    var msg = new PubSubMessage<TMessage>(_configuration, message);
                    pubSocket.SendMultipartMessage(msg);
                }
                catch (System.Exception ex)
                {
                    return XtResult<TMessage>.Failed(ex, "publish");
                }

                return XtResult<TMessage>.Success(message, "publish");
            });
        }


        private ManualResetEvent eventHandle;
        ///<summary>
        /// Register a Subscriber for the type TMessage
        /// <para>The unsubscribe callback is used to stop the subscriber</para>
        ///</summary>
        public void SubscribeHandler<TMessage>(Action<TMessage> callback, Func<bool> unsubscribeWhen = null)
            where TMessage : class, new()
        {
            // handle notifies when the server is set up
            eventHandle = new ManualResetEvent(false);
            Exception exception = null;

            Task.Run(() => 
            {
                using var subSocket = new SubscriberSocket();
                try
                {
                    string catchAllTopic = "";
                    subSocket.Bind(_configuration.Address());
                    subSocket.Subscribe(catchAllTopic);
                    _configuration.Logger.Log(new DebugLogMsg($"subscribed to [{typeof(TMessage)}]"));
                }
                catch (NetMQ.EndpointNotFoundException ntfnd)
                {
                    _configuration.Logger.Log(new ErrorLogMsg(ntfnd.Message));
                    exception = ntfnd;
                }
                // open resetevent after binding to the socket, and block after that
                eventHandle.Set();
                if (exception is not null) 
                    return;

                bool isRunning = true;
                while (isRunning)
                {
                    try
                    {
                        NetMQMessage received = subSocket.ReceiveMultipartMessage();
                        var actualMessage = received.ParsePubSubMessage<TMessage>(_configuration);
                        callback(actualMessage.IsSuccess ? actualMessage.GetResult() : new TMessage());
                    }
                    catch (NetMQ.TerminatingException trmnt)
                    {
                        isRunning = false;
                        _configuration.Logger.Log(new ErrorLogMsg($"Subscriber handle for [Message:{typeof(TMessage)}] did fail: " + trmnt.Message));
                    }
                    catch (System.Exception ex)
                    {
                        _configuration.Logger.Log(new ErrorLogMsg($"Subscriber handle for [Message:{typeof(TMessage)}] did fail: " + ex.Message));
                    }
                    isRunning = isRunning && (unsubscribeWhen?.Invoke() ?? true);
                }
            });

            eventHandle.WaitOne();
            if (exception is not null) 
                throw exception;
        }

        public async Task SubscribeAsyncHandler<TMessage>(Func<TMessage, Task> asyncCallback)
        {
            await Task.CompletedTask;
        }

        #region Dispose
        private bool disposedValue;

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                disposedValue = true;
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