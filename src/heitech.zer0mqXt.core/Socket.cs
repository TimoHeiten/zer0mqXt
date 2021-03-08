using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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

        // todo generic msg type that wraps the result, so we always get an answer fpr REQ/REP
        // __> like zer0mqXt.MsgWrapper
        // todo injection
        // todo serializer auswählen ermöglichen
        public async Task<XtResult<TResult>> RequestAsync<T, TResult>(T request)
            where T : class
            where TResult : class
        {
            try
            {
                var rqType = typeof(T).FullName;
                var rsType = typeof(TResult).FullName;

                using var rqSocket = new RequestSocket();
                rqSocket.Connect(_configuration.Address());
                byte[] frame1 = _configuration.Encoding.GetBytes(typeof(T).FullName);
                byte[] frame2 = _configuration.Encoding.GetBytes(typeof(TResult).FullName);
                string jsonFormatted = JsonConvert.SerializeObject(request);

                return await Task.Run(() => 
                {
                    rqSocket.SendMoreFrame(frame1)
                            .SendMoreFrame(frame2)
                            .SendFrame(_configuration.Encoding.GetBytes(jsonFormatted));

                    // todo this as private msg
                    var frame1Rcv = rqSocket.ReceiveFrameBytes();
                    var rcvdRqType = _configuration.Encoding.GetString(frame1Rcv);
                    if (rqType == rcvdRqType)
                        XtResult<TResult>.Failed(ZeroMqXtSocketException.Frame1RqTypeDoesNotMatch<T>(rcvdRqType));

                    var frame2Rcv = rqSocket.ReceiveFrameBytes();
                    var rcvdRsType = _configuration.Encoding.GetString(frame2Rcv);
                    if (rsType == rcvdRsType)
                        XtResult<TResult>.Failed(ZeroMqXtSocketException.Frame2RsTypeDoesNotMatch<T>(rcvdRqType));

                    var body = rqSocket.ReceiveFrameBytes();
                    var result = JsonConvert.DeserializeObject<TResult>(_configuration.Encoding.GetString(body));

                    return XtResult<TResult>.Success(result);
                });

                // now wait for reply
            }
            catch (System.Exception ex)
            {
                return XtResult<TResult>.Failed(ex);
            }
        }

        ///<summary>
        /// currently blocks 
        ///</summary>
        // todo remove blocking with thread per response and eventhanlder or such
        public void RespondTo<TResult, T>(Func<T, TResult> factory)
            where T : class
            where TResult : class
        {
            var rqType = typeof(T).FullName;
            var rsType = typeof(TResult).FullName;

            using var rsSocket = new ResponseSocket();
            rsSocket.Bind(_configuration.Address());
            while (true)
            {
                List<byte[]> bytes = rsSocket.ReceiveMultipartBytes();
                var rqTypeFrame = bytes[0];
                var rsTypeFrame = bytes[1];
                var bodyFrame = bytes[2];

                // todo check if rqtype fits
                // wrap message

                var request = JsonConvert.DeserializeObject<T>(_configuration.Encoding.GetString(bodyFrame));
                var body = JsonConvert.SerializeObject(factory(request));

                rsSocket.SendMoreFrame(_configuration.Encoding.GetBytes(rqType))
                        .SendMoreFrame(_configuration.Encoding.GetBytes(rsType))
                        .SendFrame(body);
            }
        }
    }
}