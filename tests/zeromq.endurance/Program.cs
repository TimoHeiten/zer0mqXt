using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using heitech.zer0mqXt.core;
using heitech.zer0mqXt.core.infrastructure;
using heitech.zer0mqXt.core.Main;
using zeromq.endurance.LoadScenarios;

namespace zeromq.endurance
{
    ///<summary>
    /// Test different patterns in a load scenario, test different sockets on same configuration etc.
    ///</summary>
    class Program
    {
        private class Message { public string ReqText { get; set; } }
        private class Messagev2 { public string ReqText { get; set; } }
        static async Task Main(string[] args)
        {
            var config = SocketConfiguration.TcpConfig("4403");
            config.Logger.SetSilent();
            var patterns = Zer0Mq.From(config);

            IPatternFactory patternsv2 = Zer0Mq.Go().BuildWithTcp("host2", "3");
            if (args.SingleOrDefault() == "r")
            {
                var client = patterns.CreatePublisher();
                while (true)
                {
                    XtResult<Message> r = await client.SendAsync<Message>(new Message { ReqText = "hallo server?" }, topic: "abc/affe/schnee");
                    if (!r.IsSuccess)
                        System.Console.WriteLine(r.Exception.Message);
                    else
                    {
                        System.Console.WriteLine("succeesssd");
                    }
                    System.Console.ReadLine();
                }
            }
            else
            {
                var server = patterns.CreateSubscriber();
                server.RegisterSubscriber<Message>(
                    rq =>
                    {
                        System.Console.WriteLine(rq.ReqText + " im ersten server");
                    },
                    topic:"abc"
                );

                System.Console.WriteLine("nächste bitte");
                System.Console.ReadLine();
                var server2 = patterns.CreateSubscriber();
                server2.RegisterSubscriber<Messagev2>(
                    rq =>
                    {
                        System.Console.WriteLine(rq.ReqText + " im zweiten sub");
                    },
                    topic:"abc/affe"
                );
                System.Console.ReadLine();
            }

            return;

            var rqrpLoad = new RqRpLoad(config);
            int no = int.Parse(args.Single());

            var tasks = new List<Task> {
                rqrpLoad.RunLoadTest(no/2),
                rqrpLoad.RunLoadTest(no/4),
                rqrpLoad.RunLoadTest(no/4)
            };

            await Task.WhenAll(tasks);

            System.Console.WriteLine("press button - test is done");
            System.Console.ReadLine();
            System.Console.WriteLine("done");
        }
    }
}