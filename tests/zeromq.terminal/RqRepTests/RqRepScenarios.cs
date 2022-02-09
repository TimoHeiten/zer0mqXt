using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using heitech.zer0mqXt.core.infrastructure;
using heitech.zer0mqXt.core.Main;
using heitech.zer0mqXt.core.RqRp;
using zeromq.terminal.Utils;

namespace zeromq.terminal.RqRepTests
{
    public static class RqRepScenarios
    {
        internal static async Task RunReqRep(SocketConfiguration configuration)
        {
            var pattern = Zer0Mq.From(configuration);
            using var responder = pattern.CreateResponder();

            SetupResponder(responder);

            TestHelper.Print("try request");
            using var client = pattern.CreateClient();
            await RequestAndWriteResultAsync(client);
        }

        internal static async Task CancellationTokenOnRunningTask(SocketConfiguration configuration)
        {
            var pattern = Zer0Mq.From(configuration);
            using var responder = pattern.CreateResponder();

            using var cts = new CancellationTokenSource();
            var token = cts.Token;
            SetupResponder(responder, token: token);
            cts.Cancel();

            TestHelper.Print("".PadRight(50, '-'));
            TestHelper.Print("try 3 requests - expect 3 failures since there is no server left by now");
            TestHelper.Print("".PadRight(50, '-'));

            using var client = pattern.CreateClient();
            int countFailures = 0;
            foreach (var item in Enumerable.Range(0, 3))
            {
                var result = await RequestAndWriteResultAsync(client);
                if (!result)
                    countFailures++;
            }

            TestHelper.Print("");
            TestHelper.Print($"expected 3 failures - got '{countFailures}'");
        }

        internal static async Task AsyncServer(SocketConfiguration configuration)
        {
            var pattern = Zer0Mq.From(configuration);
            using var responder = pattern.CreateResponder();

            responder.RespondAsync<Request, Response>(async r =>
                {
                    await Task.Delay(100);
                    return new Response { InsideResponse = "waited asynchronously for 100ms" };
                }
            );

            using var client = pattern.CreateClient();
            await RequestAndWriteResultAsync(client);
        }

        internal static void SetupResponder(IResponder responder, CancellationToken token = default)
        {
            responder.Respond<Request, Response>((rq) =>
            {
                TestHelper.Print("");
                TestHelper.Print("now inside the responsehandler");
                TestHelper.Print("");
                var rsp = new Response();
                rsp.InsideResponse += " " + rq.FromRequest;

                return rsp;
            }, token: token);
        }

        internal static async Task<bool> RequestAndWriteResultAsync(IClient client)
        {
            XtResult<Response> result = await client.RequestAsync<Request, Response>(new Request());
            TestHelper.Print($"{result}");

            if (result.IsSuccess)
                TestHelper.Print("SUCCESS!! " + result.GetResult().InsideResponse);
            else
                TestHelper.Print("FAILURE!! " + result.Exception);

            return result.IsSuccess;
        }

        internal static async Task UseBusInterface(SocketConfiguration _)
        {
            var pattern = Zer0Mq.Go().BuildWithInProc("bus-interface");
            using var responder = pattern.CreateResponder();
            responder.Respond<Request, Response>((r) => new Response());

            using var client = pattern.CreateClient();
            var response = await client.RequestAsync<Request, Response>(new Request());
            TestHelper.Print(response.GetResult().InsideResponse + " works");
        }

        // !FIXME
        internal static async Task Contest(SocketConfiguration _)
        {
            var pattern = Zer0Mq.Go().BuildWithTcp("localhost","4200");
            using var responder = pattern.CreateResponder();
            responder.Respond<Request, Response>(rq => new Response { InsideResponse = "first-server" });
            using var client = pattern.CreateClient();

            try
            {
                var pattern2 = Zer0Mq.Go().BuildWithTcp("localhost","4200");
                using var responder2 = pattern2.CreateResponder();
                var result = responder2.Respond<Request, Response>(rq => new Response { InsideResponse = "second-server" });
                System.Console.WriteLine(result.IsSuccess);
                if (!result.IsSuccess)
                    throw new ZeroMqXtSocketException("Adress already in use");
            }
            catch (ZeroMqXtSocketException socketException)
            {
                TestHelper.Print("!!!EXPECTED!!! exception was thrown: " + socketException.ToString());
            }
            Thread.Sleep(500);
            await client.RequestAsync<Request, Response>(new Request());
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