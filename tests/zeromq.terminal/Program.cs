using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using heitech.zer0mqXt.core.infrastructure;
using heitech.zer0mqXt.core.patterns;
using zeromq.terminal.RqRepTests;
using zeromq.terminal.PubSubTests;

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
                ["reqrep"] = RqRepScenarios.RunReqRep,
                ["cancel"] = RqRepScenarios.CancellationTokenOnRunningTask,
                ["contest"] = RqRepScenarios.Contest,
                ["async-server"] = RqRepScenarios.AsyncServer,
                ["bus-all"] = RqRepScenarios.UseBusInterface,

                ["pubsub"] = PubSubScenarios.SimplePubSub,
                ["subcancel"] = PubSubScenarios.PubSubWithCancellation,
            };
        }

        static async Task Main(string[] args)
        {
            string key = "subcancel";//args.First();
            var actions = args.Where(x => _terminalActions.ContainsKey(x)).Select((x, index) => 
            {
                var configuration = BuildConfig(args, index);
                return ExecuteScenario(x, configuration);
            }
            ).ToList();
            if (actions.Any())
            {
                foreach (var task in actions)
                {
                    await task;
                }
            }
            else
            {
                if (_terminalActions.ContainsKey(key))
                    await ExecuteScenario(key, BuildConfig(args, 5));
                else
                {
                    System.Console.WriteLine($"[{key}] not found. Try any or multiple of the following");
                    System.Console.WriteLine(" - " + string.Join(Environment.NewLine + " - ", _terminalActions.Select(x => x.Key)));
                }
            }
        }

        private static SocketConfiguration BuildConfig(string[] args, int index)
        {
            var version = args.FirstOrDefault();
            var protocol = args.LastOrDefault();
            SocketConfiguration configuration = SocketConfiguration.InprocConfig($"this-inproc-sir-{Guid.NewGuid()}");
            if (protocol != null && protocol != version)
                configuration = SocketConfiguration.TcpConfig($"555{index}");
            configuration.TimeOut = TimeSpan.FromSeconds(2);
            return configuration;
        }

        private static async Task ExecuteScenario(string key, SocketConfiguration configuration)
        {
            System.Console.WriteLine($"Running scenario [{key}] with config [{configuration.GetType().Name}] at address [{configuration.Address()}]");
            await _terminalActions[key](configuration);
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
