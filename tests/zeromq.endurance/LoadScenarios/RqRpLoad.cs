using System;
using System.Diagnostics;
using System.Threading.Tasks;
using heitech.zer0mqXt.core.infrastructure;
using heitech.zer0mqXt.core.Main;

namespace zeromq.endurance.LoadScenarios
{
    internal sealed class RqRpLoad
    {
        private readonly SocketConfiguration _configuration;
        internal RqRpLoad(SocketConfiguration configuration)
        {
            this._configuration = configuration;
        }

        public async Task RunLoadTest(int amountOfMessages)
        {
            var watch = new Stopwatch();
            watch.Start();
            var patterns = Zer0Mq.From(_configuration);
            using var receiver = patterns.CreateResponder();
            receiver.Respond<Request, Response>(x => new Response());

            using var client = patterns.CreateClient();
            
            while (amountOfMessages > 0)
            {
                await client.RequestAsync<Request, Response>(new Request());
                amountOfMessages--;
            }


            watch.Stop();
            System.Console.WriteLine($"took rqrep load scenario: {watch.ElapsedMilliseconds} ms");
        }

        private class Request { public string ReqText { get; set; } }
        private class Response { public string RepText { get; set; } }
    }
}
