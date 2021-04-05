using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using heitech.zer0mqXt.core.infrastructure;
using heitech.zer0mqXt.core.patterns;
using NetMQ;

namespace zeromq.terminal
{
    ///<summary>
    /// Test all the happy pathes for all socket types in the library but with inproc only
    ///</summary>
    class Program
    {
        static async Task Main(string[] args)
        {
            var version = args.FirstOrDefault();
            var configuration = SocketConfiguration.InprocConfig("this-inproc-sir-" + Guid.NewGuid());
            
            if (version != null)
                await RunPubSub(configuration);
            else
                await RunReqRep(configuration);

            NetMQConfig.Cleanup();
        }

        private static async Task RunPubSub(SocketConfiguration configuration)
        {
            // it works only for InProc right now
            var pubSub = new PubSub(configuration);
            await Task.Run(() => 
            {
                System.Console.WriteLine("setting up the subscriber");
                pubSub.SubscribeHandler<Message>(
                    (m) => System.Console.WriteLine("message came in: " + m.Text)
                );
            });
            
            System.Console.WriteLine("now publishes");
            var input = "";
            while (input != "quit")
            {
                System.Console.WriteLine("publish next one");
                var xt = await pubSub.PublishAsync(new Message() {Text = "publsihed!"});
                System.Console.WriteLine(xt);

                input = Console.ReadLine();
            }
        }

        private static async Task RunReqRep(SocketConfiguration configuration)
        {
            var socket = new Socket(configuration);

            var cts = new CancellationTokenSource();
            var token = cts.Token;
            socket.Respond<Request, Response>((rq) => 
            {
                System.Console.WriteLine();
                System.Console.WriteLine("now calling the factory");
                System.Console.WriteLine();
                var rsp = new Response();
                rsp.InsideResponse += " " + rq.FromRequest;
                
                return rsp;
            }, respondOnce: true);

            System.Console.WriteLine("try request");
            XtResult<Response> result = await socket.RequestAsync<Request, Response>(new Request());
            if (result.IsSuccess)
                System.Console.WriteLine("SUCCEESS!! " + result.GetResult().InsideResponse);
            else
                System.Console.WriteLine("FAILURE!! " + result.Exception);

            socket.Dispose();
        }

        private class Request 
        {
            public string FromRequest { get; set; } = "Message from Request";
        }

        private class Response
        {
            public string InsideResponse { get; set; } = "Message inside Response";
        }

        private class Message { public string Text { get; set; } }
    }
}
