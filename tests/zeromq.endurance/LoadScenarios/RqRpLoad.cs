using System;
using System.Diagnostics;
using System.Threading.Tasks;
using heitech.zer0mqXt.core.infrastructure;
using heitech.zer0mqXt.core.Main;
using NetMQ;
using NetMQ.Sockets;

namespace zeromq.endurance.LoadScenarios
{
    internal sealed class RqRpLoad : ILoadTest
    {
        private readonly SocketConfiguration _configuration;
        internal RqRpLoad()
        {
            _configuration = SocketConfiguration.TcpConfig("4403");
            _configuration.Logger.SetSilent();
        }

        public void Dispose()
        {
            // NetMQConfig.Cleanup();
        }

        public async Task SimpleMessages(int amountOfMessages)
        {
            using var profiler = new Profiler(nameof(RqRpLoad), amountOfMessages);

            var patterns = Zer0Mq.From(_configuration);
            using var receiver = patterns.CreateResponder();
            receiver.Respond<Request, Response>(x => new Response() { RepText = "World" });

            using var client = patterns.CreateClient();

            while (amountOfMessages > 0)
            {
                await client.RequestAsync<Request, Response>(new Request() { ReqText = "Hello" });
                amountOfMessages--;
            }
        }

        private class Request { public string ReqText { get; set; } }
        private class Response { public string RepText { get; set; } }

        public Task SimpleMessagesRaw(int amountOfMessages)
        {
            using var profiler = new Profiler(nameof(RqRpLoad) + " " + nameof(SimpleMessagesRaw), amountOfMessages);

            var address = _configuration.Address();
            using var responseSocket = new ResponseSocket(address);
            using var requestSocket = new RequestSocket(address);

            while (amountOfMessages > 0)
            {
                requestSocket.SendFrame("Hello");
                var message = responseSocket.ReceiveFrameString();
                responseSocket.SendFrame("World");
                message = requestSocket.ReceiveFrameString();
                amountOfMessages--;
            }
            return Task.CompletedTask;
        }

        // todo blocks for now. but is way closer to the actual functionality of the library for a meaningful comparison
        // private void Send(RequestSocket rqSocket)
        // {
        //    // the request to be send with timeout
        //     var message = new NetMQMessage();
        //     message.Append("TypeFrame");
        //     message.Append("SuccessFrame");
        //     message.Append("PayloadFrame");

        //     bool rqDidNotTimeOut = rqSocket.TrySendMultipartMessage(_configuration.Timeout, message);
        //     if (rqDidNotTimeOut)
        //         throw new TimeoutException("at sending the reqeuest (raw)");

        //     // wait for the response with timeout
        //     var response = new NetMQMessage();
        //     bool noTimeOut = rqSocket.TryReceiveMultipartMessage(_configuration.Timeout, ref response, expectedFrameCount: 3);
        //     if (!noTimeOut)
        //         throw new TimeoutException("at receiving the response (raw)");
        // }

        // private void Receive(ResponseSocket socket)
        // {
        //     // block on this thread for incoming requests of the type T (Request)
        //     NetMQMessage incomingRequest = socket.ReceiveMultipartMessage();

        //     // try send response with timeout
        //     var response = new NetMQMessage();
        //     bool noTimeout = socket.TrySendMultipartMessage(_configuration.Timeout, response);
        //     if (!noTimeout)
        //         _configuration.Logger.Log(new ErrorLogMsg($"Responding to [Request:{typeof(T)}] with [Response:{typeof(TResult)}] timed-out after {_configuration.Timeout}"));
        // }
    }
}
