// using System;
// using System.Collections.Generic;
// using System.Threading;
// using heitech.zer0mqXt.core.infrastructure;
// using heitech.zer0mqXt.core.transport;
// using NetMQ;
// using NetMQ.Sockets;

// namespace heitech.zer0mqXt.core.patterns
// {
//     public class SubscriberV2Container : IDisposable
//     {
//         private NetMQPoller polly;
//         private NetMQPoller _poller
//         {
//             get
//             {
//                 if (polly == null)
//                 {
//                     polly = new();
//                 }
//                 return polly;
//             }
//         }
//         private readonly SocketConfiguration _configuration;
//         private readonly List<(SubscriberSocket, EventHandler<NetMQSocketEventArgs>)> _sockets = new();
//         private bool disposedValue;

//         internal SubscriberV2Container(SocketConfiguration configuration)
//         {
//             _configuration = configuration;
//         }

//         public XtResult Register<TMessage>(Action<TMessage> callback, string topic = null, CancellationToken token = default)
//             where TMessage : class, new()
//         {
//             try
//             {
//                 SubscriberSocket socket = new();
//                 EventHandler<NetMQSocketEventArgs> handler = (s, a) =>
//                 {
//                     while (!token.IsCancellationRequested && a.IsReadyToReceive)
//                     {
//                         var topic = a.Socket.ReceiveFrameString();
//                         System.Console.WriteLine("incoming topic " + topic);
//                         var payload = a.Socket.ReceiveFrameBytes();

//                         var actualMsg = _configuration.Serializer.Deserialize<TMessage>(payload);
//                         callback(actualMsg);
//                     }
//                 };

//                 string address = _configuration.Address();
//                 string topicFrame = _configuration.GetTopicFrame<TMessage>(topic);

//                 socket.Connect(_configuration.Address());
//                 socket.Subscribe(topicFrame);
//                 socket.ReceiveReady += handler;

//                 _poller.Add(socket);
//                 _sockets.Add((socket, handler));
//                 _poller.RunAsync();

//                 _configuration.Logger.Log(new DebugLogMsg($"Setup a Subscriber for [{typeof(TMessage)}]| at [{address}] done"));

//                 return XtResult.Success("register-subscriber");
//             }
//             catch (System.Exception ex)
//             {
//                 return XtResult.Failed(ex);
//             }
//         }

//         protected virtual void Dispose(bool disposing)
//         {
//             if (!disposedValue)
//             {
//                 if (disposing)
//                 {
//                     foreach (var item in _sockets)
//                     {
//                         var (socket, handler) = item;
//                         socket.ReceiveReady -= handler;
//                         _poller.RemoveAndDispose(socket);
//                     }
//                 }
//                 disposedValue = true;
//             }
//         }

//         public void Dispose()
//         {
//             Dispose(disposing: true);
//             GC.SuppressFinalize(this);
//         }
//     }
// }
