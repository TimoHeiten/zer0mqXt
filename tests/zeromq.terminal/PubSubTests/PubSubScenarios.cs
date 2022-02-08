using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using heitech.zer0mqXt.core;
using heitech.zer0mqXt.core.infrastructure;
using heitech.zer0mqXt.core.Main;
using zeromq.terminal.Utils;

namespace zeromq.terminal.PubSubTests
{
    public class PubSubScenarios
    {
        // socket came late, for now no refactoring of all tests desired
        private static IPatternFactory ToSocket(SocketConfiguration configuration)
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
            var socket = ToSocket(configuration);
            using var pub = socket.CreatePublisher();
            using var sub = socket.CreateSubscriber();

            sub.RegisterSubscriber<PubSubMessage>(msg => System.Console.WriteLine("msg received: " + msg.Message));

            await pub.SendAsync(new PubSubMessage { Message = "Published simply" });
            TestHelper.Print("press enter to exit");
            Console.ReadLine();
        }

        internal static async Task PubSubWithCancellation(SocketConfiguration configuration)
        {
            var socket = ToSocket(configuration);
            using var pub = socket.CreatePublisher();
            using var sub = socket.CreateSubscriber();
            using var cts = new CancellationTokenSource();

            var token = cts.Token;
            sub.RegisterSubscriber<PubSubMessage>(msg => System.Console.WriteLine("msg received: " + msg.Message), cancellationToken: token);
            cts.Cancel();

            TestHelper.Print("try multiple Publishes");
            foreach (var item in Enumerable.Range(0, 3))
            {
                await pub.SendAsync(new PubSubMessage { Message = "Published with cancellation" });
            }
            TestHelper.Print("press enter to exit");
            Console.ReadLine();
        }

        internal static async Task MultipleSubscribersOverTcp(SocketConfiguration configuration)
        {
            var socket = ToSocket(SocketConfiguration.TcpConfig(port: "4333"));
            using var pub = socket.CreatePublisher();
            using var sub1 = socket.CreateSubscriber();
            using var sub2 = socket.CreateSubscriber();
            using var sub3 = socket.CreateSubscriber();

            sub1.RegisterSubscriber<PubSubMessage>(msg => System.Console.WriteLine("msg received in 1: " + msg.Message));
            sub2.RegisterSubscriber<PubSubMessage>(msg => System.Console.WriteLine("msg received in 2: " + msg.Message));
            sub3.RegisterSubscriber<PubSubMessage>(msg => System.Console.WriteLine("msg received in 3: " + msg.Message));

            await pub.SendAsync(new PubSubMessage { Message = "Published simply" });

            TestHelper.Print("press enter to exit");
            Console.ReadLine();
        }

        internal static async Task SpecificTopicWorks(SocketConfiguration configuration)
        {
            var socket = ToSocket(configuration);
            using var pub = socket.CreatePublisher();
            using var sub1 = socket.CreateSubscriber();
            var cntdwn = new CountdownEvent(3);
            var total = 0;

            sub1.RegisterSubscriber<PubSubMessage>(_ => { cntdwn.Signal(); total++; }, null, "topic");
            await pub.SendAsync(new PubSubMessage(), "topic/subtopic");
            await pub.SendAsync(new PubSubMessage(), "topic/");
            await pub.SendAsync(new PubSubMessage(), "topical");
            await pub.SendAsync(new PubSubMessage(), "topic"); // should not be received

            bool signlaed = cntdwn.Wait(1500);
            TestHelper.SignalSuccess(signlaed && total == 3);
        }

        public class PubSubMessage
        {
            public string Message { get; set; }
        }
    }
}