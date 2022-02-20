using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
            int amountOfMessages = 1000;
            var first = args.FirstOrDefault();
            _ = first != null && int.TryParse(first, out amountOfMessages);

            var loadTests = new ILoadTest[]
            {
                new PubSubLoad(), // having these in parallel does not work for now (accessing a single socket from multiple threads ain´t allowed...)
                new RqRpLoad()
            };

            for (int i = 0; i < loadTests.Length; i++)
            {
                await RunNext(loadTests[i], amountOfMessages);
            }

            System.Console.WriteLine("All Tests are done");
        }


        private static async Task RunNext(ILoadTest tester, int no)
        {
            int chunkMessages(int denominator)
            {
                var r = ((double)no)/denominator;
                return (int)System.Math.Floor(r);
            }

            var tasks = new List<Task> {
                tester.SimpleMessages(chunkMessages(2)),
                tester.SimpleMessages(chunkMessages(4)),
                tester.SimpleMessages(chunkMessages(8)),
                tester.SimpleMessages(chunkMessages(16)),
                tester.SimpleMessages(chunkMessages(16))
            };

            await Task.WhenAll(tasks);

            var tasks2 = new List<Task> {
                tester.SimpleMessagesRaw(chunkMessages(2)),
                tester.SimpleMessagesRaw(chunkMessages(4)),
                tester.SimpleMessagesRaw(chunkMessages(8)),
                tester.SimpleMessagesRaw(chunkMessages(16)),
                tester.SimpleMessagesRaw(chunkMessages(16))
            };

            System.Console.WriteLine($"press button - load test for '{tester.Name}' is done!");
            System.Console.ReadLine();
            System.Console.WriteLine("done");
            tester.Dispose();
        }
    }
}