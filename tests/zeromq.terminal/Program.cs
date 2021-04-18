using System;
using System.Collections.Generic;
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
        static Dictionary<string, Func<SocketConfiguration, Task>> _terminalActions;

        static Program()
        {
            _terminalActions = new Dictionary<string, Func<SocketConfiguration, Task>>
            {
                ["pubsub"] = RunPubSub,
                ["reqrep"] = RunReqRep,
                ["cancel"] = CancellationTokenOnRunningTask,
                ["async-server"] = RunRequestWithAsyncServer
            };
        }
        static async Task Main(string[] args)
        {
            var version = args.FirstOrDefault();
            var configuration = SocketConfiguration.InprocConfig($"this-inproc-sir-{Guid.NewGuid()}");
            configuration.TimeOut = TimeSpan.FromSeconds(2);
            
            string key = "reqrep";
            var actions = args.Where(x => _terminalActions.ContainsKey(x)).Select(x => _terminalActions[x]).ToList();
            if (actions.Any())
            {
                foreach (var action in actions)
                {
                    await action(configuration);
                }
            }
            else
            {
                await _terminalActions[key](configuration);
            }
        }

        private static async Task RunPubSub(SocketConfiguration configuration)
        {
            // it works only for InProc right now
            using var pubSub = new PubSub(configuration);
            await Task.Run(() => 
            {
                System.Console.WriteLine("setting up the subscriber");
                pubSub.SubscribeHandler<Message>((m) => System.Console.WriteLine("message came in: " + m.Text));
            });
            
            System.Console.WriteLine("now publishes");
            var input = "";
            while (input != "quit")
            {
                System.Console.WriteLine("publish next one");
                var xt = await pubSub.PublishAsync(new Message() {Text = "published!"});
                System.Console.WriteLine(xt);

                input = Console.ReadLine();
            }
        }

        private static async Task RunReqRep(SocketConfiguration configuration)
        {
            using var socket = new Socket(configuration);

            SetupResponder(socket);

            System.Console.WriteLine("try request");
            await RequestAndWriteResultAsync(socket);
            socket.Dispose();
        }

        private static async Task CancellationTokenOnRunningTask(SocketConfiguration configuration)
        {
            var socket = new Socket(configuration);

            using var cts = new CancellationTokenSource();
            var token = cts.Token;
            SetupResponder(socket, token: token);
            cts.Cancel();

            // the first one will always come threw, no matter what
            System.Console.WriteLine("try multiple requests");
            foreach (var item in Enumerable.Range(0, 3))
            {
                await RequestAndWriteResultAsync(socket);
            } 
        }

        private static async Task RunRequestWithAsyncServer(SocketConfiguration configuration)
        {
            var socket = new Socket(configuration);

            socket.RespondAsync<Request, Response>(async r => 
                {
                    await Task.Delay(100);
                    return new Response { InsideResponse = "waited asynchronously for 100ms" };
                }
            );

            await RequestAndWriteResultAsync(socket);

            socket.Dispose();
        }

        private static void SetupResponder(Socket socket, CancellationToken token = default)
        {
            socket.Respond<Request, Response>((rq) => 
            {
                System.Console.WriteLine();
                System.Console.WriteLine("now calling the factory");
                System.Console.WriteLine();
                var rsp = new Response();
                rsp.InsideResponse += " " + rq.FromRequest;

                return rsp;
            }, cancellationToken: token);
        }

        private static async Task RequestAndWriteResultAsync(Socket socket)
        {
            XtResult<Response> result = await socket.RequestAsync<Request, Response>(new Request());
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
