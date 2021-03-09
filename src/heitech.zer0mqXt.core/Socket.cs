using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace heitech.zer0mqXt.core
{
    public class Socket : IDisposable
    {
        private readonly SocketConfiguration _configuration;
        public Socket(SocketConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<XtResult<TResult>> RequestAsync<T, TResult>(T request)
            where T : class, new()
            where TResult : class
        {
            try
            {
                return await DoRequestAsync<T, TResult>(request);
            }
            catch (NetMQ.EndpointNotFoundException)
            {
                await Task.Delay((int)_configuration.TimeOut.TotalMilliseconds);
                try
                {
                    return await DoRequestAsync<T, TResult>(request);
                }
                catch (System.Exception inner)
                {
                    return XtResult<TResult>.Failed(inner);
                }
            }
            catch (System.Exception ex)
            {
                return XtResult<TResult>.Failed(ex);
            }
        }

        private async Task<XtResult<TResult>> DoRequestAsync<T, TResult>(T request)
            where T : class, new()
            where TResult : class
        {
            using var rqSocket = new RequestSocket();
            rqSocket.Connect(_configuration.Address());

            var message = new Message<T>(_configuration.Serializer, request);

            return await Task.Run(() => 
            {
                rqSocket.SendMoreFrame(message.RequestTypeFrame)
                        .SendMoreFrame(message.Success)
                        .SendFrame(message.Payload);

                var payload = new List<byte[]>();
                bool noTimeOut = rqSocket.TryReceiveMultipartBytes(_configuration.TimeOut, ref payload, expectedFrameCount: 3);

                if (!noTimeOut)
                    return XtResult<TResult>.Failed(new TimeoutException($"Request<{typeof(T)}, {typeof(TResult)}> timed out"));

                var xtResult = Message<TResult>.ParseMessage(_configuration.Serializer, payload);

                return xtResult.IsSuccess
                        ? XtResult<TResult>.Success(xtResult.GetResult().Content)
                        : XtResult<TResult>.Failed(xtResult.Exception);
            });
        }


        private ManualResetEvent eventHandle;
        private bool killSwitch = false;


        ///<summary>
        /// currently blocks 
        ///</summary>
        public void Respond<T, TResult>(Func<T, TResult> factory, Func<bool> stillBlocking = null)
            where T : class, new()
            where TResult : class
        {
            eventHandle = new ManualResetEvent(false);
            Task.Run(() => 
            {
                using var rsSocket = new ResponseSocket();
                rsSocket.Bind(_configuration.Address());

                eventHandle.Set();

                while (stillBlocking == null ? !killSwitch : stillBlocking())
                {
                    Message<TResult> message = null;
                    try
                    {
                        List<byte[]> bytes = rsSocket.ReceiveMultipartBytes(3);
                        var xtResult = Message<T>.ParseMessage(_configuration.Serializer, bytes);

                        TResult content = factory(xtResult.IsSuccess ? xtResult.GetResult().Content : new T());
                        message = new Message<TResult>(_configuration.Serializer, content, success: xtResult.IsSuccess);
                    }
                    catch (System.Exception)
                    {
                        // todo Logger logs here
                        message = new Message<TResult>(_configuration.Serializer, default(TResult), success: false);
                    }
                    rsSocket.SendMoreFrame(message.RequestTypeFrame)
                            .SendMoreFrame(message.Success)
                            .SendFrame(message.Payload);
                }
            });

            eventHandle.WaitOne();
        }

        #region Dispose
        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    killSwitch = true;
                    NetMQConfig.Cleanup();
                }
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