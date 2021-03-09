using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NetMQ;
using NetMQ.Sockets;

namespace heitech.zer0mqXt.core
{
    public class PubSub
    {
        private readonly SocketConfiguration configuration;

        public PubSub(SocketConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public async Task<XtResult<TMessage>> PublishAsync<TMessage>(TMessage message)
            where TMessage : class
        {
            using var pubSocket = new PublisherSocket();
            pubSocket.Bind("tcp://*:12345"); // configuration.Address()

            return await Task.Run(() => 
            {
                try
                {
                    var msg = new Message<TMessage>(configuration, message);
                    pubSocket.SendMoreFrame(msg.RequestTypeFrame) // this time around it is the Topic
                            .SendFrame(msg.Payload);
                }
                catch (System.Exception ex)
                {
                    return XtResult<TMessage>.Failed(ex);
                    throw;
                }

                return XtResult<TMessage>.Success(message);
            });
        }

        public Task SubscribeHandler<TMessage>(Action<TMessage> callback, Func<bool> unsubscribeWhen = null)
            where TMessage : class
        {
            using var subSocket = new SubscriberSocket();
            subSocket.Connect("tcp://localhost:12345"); //configuration.Address());
            subSocket.Subscribe(typeof(TMessage).Name);

            while (unsubscribeWhen == null ? true : unsubscribeWhen())
            {
                configuration.Logger.Log(new DebugLogMsg("now blocks in subscriber"));
                
                List<byte[]> payload = subSocket.ReceiveMultipartBytes(2);
                var successBytes = configuration.Encoding.GetBytes(true.ToString());
                payload = new [] { payload[0], successBytes, payload[1] }.ToList();
                var xtResult = Message<TMessage>.ParseMessage(configuration, payload);

                if (xtResult.IsSuccess)
                    configuration.Logger.Log(new DebugLogMsg("done waiting"));
                else
                    configuration.Logger.Log(new ErrorLogMsg("failed the subscriber " + xtResult.Exception.Message));
            }
            return Task.CompletedTask;
        }

        public async Task SubscribeAsyncHandler<TMessage>(Func<TMessage, Task> asyncCallback)
        {
            await Task.CompletedTask;
        }

    }
}