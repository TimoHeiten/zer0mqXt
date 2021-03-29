using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using heitech.zer0mqXt.core;
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
        static void Main(string[] args)
        {
            var version = args.FirstOrDefault();
            var configuration = SocketConfiguration.InprocConfig("this-inproc-sir-" + Guid.NewGuid());
            
            if (version != null)
                RunPubSub(configuration);
            else
                RunReqRep(configuration);

            NetMQConfig.Cleanup();
        }

        private static void RunPubSub(SocketConfiguration configuration)
        {
            // only works with InProc
            var pubSub = new PubSub(configuration);
            Task.Run(() => 
            {
                System.Console.WriteLine("setting up the subscriber");
                pubSub.SubscribeHandler<Message>(
                    (m) => System.Console.WriteLine("message came in: " + m.Text)
                );
            });
            
            Thread.Sleep(1000);
            
            System.Console.WriteLine("now publishes");
            var input = "";
            while (input != "quit")
            {
                System.Console.WriteLine("publish next one");
                var xt = pubSub.PublishAsync(new Message() {Text = "publsihed!"}).Result;
                System.Console.WriteLine(xt);

                input = Console.ReadLine();
            }
        }

        private static void RunReqRep(SocketConfiguration configuration)
        {
            var socket = new Socket(configuration);

            socket.Respond<Request, Response>((rq) => 
            {
                System.Console.WriteLine();
                System.Console.WriteLine("now calling the factory");
                System.Console.WriteLine();
                var rsp = new Response();
                rsp.InsideResponse += " " + rq.FromRequest;

                return rsp;
            });

            System.Console.WriteLine("try request");
            XtResult<Response> result = socket.RequestAsync<Request, Response>(new Request()).Result;
            System.Console.WriteLine(result);
            if (result.IsSuccess)
                System.Console.WriteLine("SUCCEESS!! " + result.GetResult().InsideResponse);
            else
                System.Console.WriteLine("FAILURE!! " + result.Exception);
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
