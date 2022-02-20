using System;
using System.Threading;
using System.Threading.Tasks;
using heitech.zer0mqXt.core.infrastructure;
using heitech.zer0mqXt.core.Main;

namespace zeromq.endurance.LoadScenarios
{
    internal sealed class PubSubLoad : ILoadTest
    {
        private SocketConfiguration _configuration;
        private static int port = 4403;
        private readonly object _token = new object();

        public void Dispose()
        {
            // NetMQConfig.Cleanup();
        }

        public async Task SimpleMessages(int amountOfMessages)
        {
            var waitHandle = new CountdownEvent(amountOfMessages);
            lock (_token)
            {
                port += 1;
            }
            using var profile = new Profiler(nameof(PubSubLoad), amountOfMessages);

            _configuration =  SocketConfiguration.TcpConfig($"{port}");
            _configuration.Logger.SetSilent();
            var patterns = Zer0Mq.From(_configuration);
            using var publisher = patterns.CreatePublisher();
            using var subscriber = patterns.CreateSubscriber();
            subscriber.RegisterSubscriber<Message>(x => waitHandle.Signal(), onError: null, topic: null);

            var temp = amountOfMessages;
            while (amountOfMessages > 0)
            {
                await publisher.SendAsync(new Message(), topic: null);
                amountOfMessages--;
            }

            bool signaled = waitHandle.Wait(2500);
            if (!signaled)
                System.Console.WriteLine($"For {temp} there are {waitHandle.CurrentCount} messages left");
        }

        public Task SimpleMessagesRaw(int amountOfMessages)
        {
            // todo
            return Task.CompletedTask;
        }

        public class Message { public string TextProperty { get; set; } }
    }
}
