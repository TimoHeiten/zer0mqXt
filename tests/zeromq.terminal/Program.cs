using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using heitech.zer0mqXt.core.infrastructure;
using heitech.zer0mqXt.core.patterns;
using zeromq.terminal.RqRepTests;

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
                ["reqrep"] = RqRepScenarios.RunReqRep,
                ["cancel"] = RqRepScenarios.CancellationTokenOnRunningTask,
                ["bus-all"] = RqRepScenarios.UseBusInterface,
                ["contest"] = RqRepScenarios.Contest,
                ["async-server"] = RqRepScenarios.AsyncServer,
                ["bus-all"] = RqRepScenarios.UseBusInterface
            };
        }

        static async Task Main(string[] args)
        {
            var version = args.FirstOrDefault();
            var configuration = SocketConfiguration.InprocConfig($"this-inproc-sir-{Guid.NewGuid()}");
            configuration.TimeOut = TimeSpan.FromSeconds(2);
            
            string key = "contest";
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

        private class Message { public string Text { get; set; } }
    }
}
