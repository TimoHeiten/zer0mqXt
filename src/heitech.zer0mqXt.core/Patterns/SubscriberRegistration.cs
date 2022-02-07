// using System;
// using System.ComponentModel;
// using System.Threading;
// using System.Threading.Tasks;
// using heitech.zer0mqXt.core.infrastructure;
// using heitech.zer0mqXt.core.transport;
// using NetMQ;
// using NetMQ.Sockets;

// namespace heitech.zer0mqXt.core.patterns
// {
//     internal class SubscriberRegistration
//     {
//         private bool isDisposed;
//         private readonly NetMQPoller _poller;
//         private DoWorkEventHandler _workHandler;
//         private readonly SubscriberSocket _socket;
//         private readonly BackgroundWorker _worker;
//         private readonly SocketConfiguration _configuration;
//         private EventHandler<NetMQSocketEventArgs> _socketDelegate;

//         internal SubscriberRegistration(NetMQPoller poller, SocketConfiguration configuration)
//         {
//             _poller = poller;
//             _configuration = configuration;

//             // one instance of a socket per registration. Else the NetMQ engine wont work (you need a Poller for multiple sockets on a single Queue)
//             _socket = new SubscriberSocket();
//             _worker = new BackgroundWorker();
//             _worker.WorkerSupportsCancellation = true;
//         }

//         internal XtResult Register<T>(string topic, Action<T> syncCallback, Func<T, Task> asyncCallback, CancellationToken token)
//             where T : class, new()
//         {
//             var resetHandle = new ManualResetEvent(false);
//             _workHandler = (s, e) =>
//             {
//                 try
//                 {
//                     SetupSocketAndPoller(topic, syncCallback, asyncCallback, token);
//                     resetHandle.Set();
//                 }
//                 catch (Exception ex)
//                 {
//                     _configuration.Logger.Log(new ErrorLogMsg($"{ex}"));
//                 }
//             };

//             _worker.DoWork += _workHandler;
//             _worker.RunWorkerAsync();

//             bool wasSignaled = resetHandle.WaitOne(_configuration.Timeout);

//             return wasSignaled
//                    ? XtResult.Success("register-subscriber")
//                    : XtResult.Failed(ZeroMqXtSocketException.FromException(new InvalidOperationException("could not setup socket")));
//         }

//         private void SetupSocketAndPoller<T>(string topic, Action<T> syncCallback, Func<T, Task> asyncCallback, CancellationToken token)
//             where T : class, new()
//         {
//             string address = _configuration.Address();
//             string topicFrame = _configuration.GetTopicFrame<T>(topic);
//             _configuration.Logger.Log(new DebugLogMsg($"setup a Subscriber for [{typeof(T)}]| at [{address}]"));

//             try
//             {
//                 _socketDelegate = async (s, e) => await ReceiveSocketFramesAsync(syncCallback, asyncCallback, topic, token);
//                 _socket.ReceiveReady += _socketDelegate;

//                 _socket.Connect(address);
//                 _socket.Subscribe(topicFrame);
//             }
//             catch (System.Exception ex)
//             {
//                 _configuration.Logger.Log(new ErrorLogMsg($"{ex}"));
//                 throw;
//             }
//             _poller.Add(_socket);
//         }

//         private async Task ReceiveSocketFramesAsync<T>(Action<T> syncCallback, Func<T, Task> asyncCallback, string topic, CancellationToken token)
//             where T : class, new()
//         {
//             while (!token.IsCancellationRequested || isDisposed)
//             {
//                 try
//                 {
//                     var receivedtopic = _socket.ReceiveFrameString();
//                     var receivedMsg = _socket.ReceiveFrameBytes();

//                     var actualMessage = _configuration.Serializer.Deserialize<T>(receivedMsg);
//                     _configuration.Logger.Log(new DebugLogMsg($"received message at topic [{receivedtopic}] for [{typeof(T)}]"));

//                     if (syncCallback != null)
//                         syncCallback(actualMessage);
//                     else
//                         await asyncCallback(actualMessage);
//                 }
//                 catch (System.Exception ex)
//                 {
//                     _configuration.Logger.Log(new ErrorLogMsg($"Subscriber with Topic [{topic}] failed due to: [{ex}]"));
//                     throw;
//                 }
//             }
//         }

//         public void Dispose()
//         {
//             if (!isDisposed)
//             {
//                 isDisposed = true;
//                 _workHandler = null;
//                 _worker.CancelAsync();
//                 _worker.Dispose();
//                 if (_socket != null)
//                 {
//                     _socket.ReceiveReady -= _socketDelegate;
//                     _socketDelegate = null;
//                 }
//             }
//         }
//     }
// }