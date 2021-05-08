using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using heitech.zer0mqXt.core;
using heitech.zer0mqXt.core.infrastructure;
using heitech.zer0mqXt.core.Main;

namespace zeromq.terminal.PubSubTests
{
    public class PubSubScenarios
    {
        // socket came late, for now no refactoring of all tests desired
        private static ISocket ToSocket(SocketConfiguration configuration)
        {
            if (configuration is SocketConfiguration.Tcp tcp)
            {
                string port = tcp.Address().Split(":").Last();
                return Zer0Mq.Go().BuildWithTcp("localhost", port);
            }
            else
            {
                return Zer0Mq.Go().BuildWithInProc(configuration.Address().Split("//").Last());
            }
        }

        internal static async Task SimplePubSub(SocketConfiguration configuration)
        {
            ISocket socket = ToSocket(configuration);
            socket.RegisterSubscriber<PubSubMessage>(msg => System.Console.WriteLine("msg received: " + msg.Message));
            await socket.PublishAsync(new PubSubMessage { Message = "Published simply"});

            Console.ReadLine();

            socket.Dispose();
        }

        internal static async Task PubSubWithCancellation(SocketConfiguration configuration)
        {
            ISocket socket = ToSocket(configuration);
            using var cts = new CancellationTokenSource();
            var token = cts.Token;
            socket.RegisterSubscriber<PubSubMessage>(msg => System.Console.WriteLine("msg received: " + msg.Message), token);
            // cts.Cancel();
            System.Console.WriteLine("try multiple Publishes");
            foreach (var item in Enumerable.Range(0, 3))
            {
                //todo Does not yet work without the console readline. A Poller is probably in order with receive ready function notifying of new Published messages
                await socket.PublishAsync(new PubSubMessage { Message = "Published with cancellation"});
                Console.ReadLine();
            }
            Console.ReadLine();
            socket.Dispose();
        }

        public class PubSubMessage
        {
            public string Message { get; set; }
        }
    }
}