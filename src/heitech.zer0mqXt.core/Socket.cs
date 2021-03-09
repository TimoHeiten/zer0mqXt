using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace heitech.zer0mqXt.core
{
    public class Socket
    {
        private readonly SocketConfiguration _configuration;
        public Socket(SocketConfiguration configuration)
        {
            _configuration = configuration;
        }

        // todo injection
        public async Task<XtResult<TResult>> RequestAsync<T, TResult>(T request)
            where T : class, new()
            where TResult : class
        {
            try
            {
                using var rqSocket = new RequestSocket();
                rqSocket.Connect(_configuration.Address());

                var message = new Message<T>(_configuration.Serializer, request);

                return await Task.Run(() => 
                {
                    rqSocket.SendMoreFrame(message.RequestTypeFrame)
                            .SendMoreFrame(message.Success)
                            .SendFrame(message.Payload);

                    var payload = rqSocket.ReceiveMultipartBytes(3);
                    var xtResult = Message<TResult>.ParseMessage(_configuration.Serializer, payload);

                    return xtResult.IsSuccess
                           ? XtResult<TResult>.Success(xtResult.GetResult().Content)
                           : XtResult<TResult>.Failed(xtResult.Exception);
                });
            }
            catch (System.Exception ex)
            {
                return XtResult<TResult>.Failed(ex);
            }
        }


        private ManualResetEvent eventHandle;
        ///<summary>
        /// currently blocks 
        ///</summary>
        // todo remove blocking with thread per response and eventhanlder or such
        public void RespondTo<T, TResult>(Func<T, TResult> factory, Func<bool> stillBlocking = null)
            where T : class, new()
            where TResult : class
        {
            eventHandle = new ManualResetEvent(false);
            Task.Run(() => 
            {
                using var rsSocket = new ResponseSocket();
                rsSocket.Bind(_configuration.Address());

                eventHandle.Set();

                while (stillBlocking == null ? true : stillBlocking())
                {
                    try
                    {
                        List<byte[]> bytes = rsSocket.ReceiveMultipartBytes(3);
                        var xtResult = Message<T>.ParseMessage(_configuration.Serializer, bytes);

                        TResult content = factory(xtResult.IsSuccess ? xtResult.GetResult().Content : new T());
                        var message = new Message<TResult>(_configuration.Serializer, content, success: xtResult.IsSuccess);

                        rsSocket.SendMoreFrame(message.RequestTypeFrame)
                                .SendMoreFrame(message.Success)
                                .SendFrame(message.Payload);
                    }
                    catch (System.Exception)
                    {
                        // todo Logger logs here
                    }
                }
            });

            eventHandle.WaitOne();
        }
    }
}