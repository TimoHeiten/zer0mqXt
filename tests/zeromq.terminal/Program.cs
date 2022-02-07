using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using heitech.zer0mqXt.core.infrastructure;
using heitech.zer0mqXt.core.Main;
using heitech.zer0mqXt.core.RqRp;
// using zeromq.terminal.PubSubTests;
// using zeromq.terminal.RqRepTests;

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
                // ["reqrep"] = RqRepScenarios.RunReqRep,
                // ["cancel"] = RqRepScenarios.CancellationTokenOnRunningTask,
                // ["contest"] = RqRepScenarios.Contest,
                // ["async-server"] = RqRepScenarios.AsyncServer,
                // ["bus-all"] = RqRepScenarios.UseBusInterface,

                // ["pubsub"] = PubSubScenarios.SimplePubSub,
                // ["subcancel"] = PubSubScenarios.PubSubWithCancellation,

                [HELP] = ShowHelp
            };
        }

        const string HELP = "help";
        static Task ShowHelp(SocketConfiguration _)
        {
            System.Console.WriteLine("Use one of the following keys as cmd param to test different scenarios:");
            string keys = string.Join(Environment.NewLine, _terminalActions.Keys.Where(x => x != HELP).Select((name, index) => $"{++index}\t{name}"));
            System.Console.WriteLine(keys);

            return Task.CompletedTask;
        }

        private class Msg { public string Content { get; set; } }
        private class MsgV2 { public string Content { get; set; } }

        static async Task Main(string[] args)
        {
            var pattern = heitech.zer0mqXt.core.Main.Zer0Mq.Go().BuildWithTcp("localhost", "4580");
            System.Console.WriteLine(string.Join(" - ", args));
            if (args.FirstOrDefault() == "p")
            {
                // var requester = RequestReplyFactory.CreateClient(SocketConfiguration.TcpConfig("4577"));
                // while (true)
                // {
                //     var result = await requester.RequestAsync<Msg, MsgV2>(new Msg { Content = "Msgv1 - " });
                //     System.Console.WriteLine(result.GetResult().Content);
                // }
                var publisher = pattern.CreatePublisher();
                while (true)
                {
                    await publisher.SendAsync(new Msg { Content = "publisher greets u" });
                    Console.ReadLine();
                }
            }
            else
            {
                var subscriber = pattern.CreateSubscriber();
                subscriber.RegisterSubscriber<Msg>(m => System.Console.WriteLine(m.Content + " in callback-1!"));
                // needs a snd subscriber
                var sndSubscriber = pattern.CreateSubscriber();
                sndSubscriber.RegisterSubscriber<Msg>(m => System.Console.WriteLine(m.Content + " in callback-2!"));

                // var responder = RequestReplyFactory.CreateResponder(SocketConfiguration.TcpConfig("4577"));
                // var setupResult = responder.Respond<Msg, MsgV2>(msg => new MsgV2 { Content = msg.Content + "extended by responder" });
                // var result = responder.Respond<Msg, MsgV2>((s) => new MsgV2());
                // System.Console.WriteLine($"'{result.IsSuccess}' - Expected was 'False'");
                Console.ReadLine();
            }

            return;

            string key = args.FirstOrDefault() ?? HELP;
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
            var configuration = SocketConfiguration.InprocConfig($"this-inproc-sir-{Guid.NewGuid()}");

            if (protocol != null && protocol != version)
                configuration = SocketConfiguration.TcpConfig($"555{index}");

            configuration.Timeout = TimeSpan.FromSeconds(2);
            return configuration;
        }

        private static async Task ExecuteScenario(string key, SocketConfiguration configuration)
        {
            System.Console.WriteLine($"Running scenario [{key}] with config [{configuration.GetType().Name}] at address [{configuration.Address()}]");
            await _terminalActions[key](configuration);
        }
    }
}
