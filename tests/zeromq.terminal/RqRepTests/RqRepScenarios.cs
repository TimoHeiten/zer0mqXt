using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using heitech.zer0mqXt.core.infrastructure;
using heitech.zer0mqXt.core.Main;
using heitech.zer0mqXt.core.patterns;

namespace zeromq.terminal.RqRepTests
{
    public static class RqRepScenarios
    {
        internal static async Task RunReqRep(SocketConfiguration configuration)
        {
            using var rqRep = new RqRep(configuration);

            SetupResponder(rqRep);

            System.Console.WriteLine("try request");
            await RequestAndWriteResultAsync(rqRep);
        }

        internal static async Task CancellationTokenOnRunningTask(SocketConfiguration configuration)
        {
            var socket = new RqRep(configuration);

            using var cts = new CancellationTokenSource();
            var token = cts.Token;
            SetupResponder(socket, token: token);
            cts.Cancel();

            // the first one will always come through, no matter what
            System.Console.WriteLine("try multiple requests");
            foreach (var item in Enumerable.Range(0, 3))
            {
                await RequestAndWriteResultAsync(socket);
            } 
        }

        internal static async Task AsyncServer(SocketConfiguration configuration)
        {
            using var socket = new RqRep(configuration);

            socket.RespondAsync<Request, Response>(async r => 
                {
                    await Task.Delay(100);
                    return new Response { InsideResponse = "waited asynchronously for 100ms" };
                }
            );

            await RequestAndWriteResultAsync(socket);
        }

        internal static void SetupResponder(RqRep rqRep, CancellationToken token = default)
        {
            rqRep.Respond<Request, Response>((rq) => 
            {
                System.Console.WriteLine();
                System.Console.WriteLine("now calling the factory");
                System.Console.WriteLine();
                var rsp = new Response();
                rsp.InsideResponse += " " + rq.FromRequest;

                return rsp;
            }, cancellationToken: token);
        }

        internal static async Task RequestAndWriteResultAsync(RqRep rqRep)
        {
            XtResult<Response> result = await rqRep.RequestAsync<Request, Response>(new Request());
            System.Console.WriteLine(result);

            if (result.IsSuccess)
                System.Console.WriteLine("SUCCESS!! " + result.GetResult().InsideResponse);
            else
                System.Console.WriteLine("FAILURE!! " + result.Exception);
        }

        internal static async Task UseBusInterface(SocketConfiguration _)
        {
            using var socket = Zer0Mq.Go().BuildWithInProc("bus-interface");

            socket.Respond<Request, Response>((r) => new Response());

            var response = await socket.RequestAsync<Request, Response>(new Request());
            System.Console.WriteLine(response.InsideResponse + " works");
        }

        internal static Task Contest(SocketConfiguration _)
        {
            using var socket = Zer0Mq.Go().BuildWithInProc("contestion");
            socket.Respond<Request, Response>(rq => new Response { InsideResponse = "first-server"});
            try
            {
                var socket2 = Zer0Mq.Go().BuildWithInProc("contestion");
                socket2.Respond<Request, Response>(rq => new Response { InsideResponse = "second-server"});
            }
            catch (ZeroMqXtSocketException socketException)
            {
                System.Console.WriteLine("expected exception was thrown: " + socketException.ToString());
                return Task.CompletedTask;
            }

            throw new InvalidOperationException("Socketexception (AdressAlready In Use) expected");
        }


        public class Request 
        {
            public string FromRequest { get; set; } = "Message from Request";
        }

        public class Response
        {
            public string InsideResponse { get; set; } = "Message inside Response";
        }
    }
}