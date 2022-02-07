// using System;
// using System.Linq;
// using System.Threading;
// using System.Threading.Tasks;
// using heitech.zer0mqXt.core;
// using heitech.zer0mqXt.core.infrastructure;
// using heitech.zer0mqXt.core.Main;

// namespace zeromq.terminal.PubSubTests
// {
//     public class PubSubScenarios
//     {
//         // socket came late, for now no refactoring of all tests desired
//         private static ISocket ToSocket(SocketConfiguration configuration)
//         {
//             if (configuration is SocketConfiguration.Tcp tcp)
//             {
//                 string port = tcp.Address().Split(":").Last();
//                 return Zer0Mq.Go().BuildWithTcp("localhost", port);
//             }
//             else
//             {
//                 return Zer0Mq.Go().BuildWithInProc(configuration.Address().Split("//").Last());
//             }
//         }

//         internal static async Task SimplePubSub(SocketConfiguration configuration)
//         {
//             ISocket socket = ToSocket(configuration);
//             var pub = socket.GetPublisher();
//             socket.RegisterSubscriber<PubSubMessage>(msg => System.Console.WriteLine("msg received: " + msg.Message));
//             await pub.SendAsync(new PubSubMessage { Message = "Published simply"});

//             System.Console.WriteLine("press enter to exit");
//             Console.ReadLine();

//             socket.Dispose();
//         }

//         internal static async Task PubSubWithCancellation(SocketConfiguration configuration)
//         {
//             ISocket socket = ToSocket(configuration);
//             var pub = socket.GetPublisher();
//             using var cts = new CancellationTokenSource();
//             var token = cts.Token;
//             socket.RegisterSubscriber<PubSubMessage>(msg => System.Console.WriteLine("msg received: " + msg.Message), null, token);
//             cts.Cancel();
//             System.Console.WriteLine("try multiple Publishes");
//             foreach (var item in Enumerable.Range(0, 3))
//             {
//                 await pub.SendAsync(new PubSubMessage { Message = "Published with cancellation"});
//             }
//             System.Console.WriteLine("press enter to exit");
//             Console.ReadLine();
//             socket.Dispose();
//         }

//         public class PubSubMessage
//         {
//             public string Message { get; set; }
//         }
//     }
// }