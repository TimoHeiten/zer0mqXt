// todo
/*
    this aint core no more,
    when we want to have managed bus functionality, this will be the heitech.zer0mqXt.managed namespace
*/

// using System;
// using System.Collections.Generic;
// using System.Threading;
// using System.Threading.Tasks;
// using heitech.zer0mqXt.core.infrastructure;
// using heitech.zer0mqXt.core.patterns;
// using NetMQ;

// namespace heitech.zer0mqXt.core.Main
// {
//     // todo! introduce an instance per pattern, so the bus does not dispose something like the subscriber on accident
//     ///<inheritdoc cref="ISocket"/>
//     internal class Socket : ISocket
//     {
//         private readonly object _token = new();
//         private readonly RqRep _rqRep;
//         private Publisher _publisher;
//         private SubscriberV2Container _subscriber;
//         private readonly SendReceive _sendReceive;
//         private readonly Func<Publisher> _factory;

//         ///<inheritdoc/>
//         internal Socket(SocketConfiguration config)
//         {
//             _rqRep = new RqRep(config);
//             _sendReceive = new SendReceive(config);
//             _factory = () => new Publisher(config);
//             _subscriber = new SubscriberV2Container(config);
//         }

//         public void Dispose()
//         {
//             _rqRep.Dispose();
//             _sendReceive.Dispose();
//             _publisher?.Dispose();
//             _subscriber?.Dispose();
//         }

//         public async Task PublishAsync<TMessage>(TMessage message)
//             where TMessage : class, new()
//         {
//             lock (_token)
//             {
//                 if (_publisher == null)
//                 {
//                     _publisher = _factory();
//                 }
//             }

//             var xtResult = _publisher.Send(message);
//             ThrowOnNonSuccess(xtResult);
//             await Task.CompletedTask;
//         }

//         public void RegisterAsyncSubscriber<TMessage>(Func<TMessage, Task> asyncCallback, CancellationToken cancellationToken = default)
//             where TMessage : class, new()
//         {
//             throw new NotImplementedException();
//             // ThrowOnNonSuccess(result);
//         }

//         public void RegisterSubscriber<TMessage>(Action<TMessage> callback, string topic = null, CancellationToken cancellationToken = default)
//             where TMessage : class, new()
//         {
//             XtResult result = _subscriber.Register<TMessage>(callback, topic, cancellationToken);
//             ThrowOnNonSuccess(result);
//         }

//         public async Task<TResult> RequestAsync<TRequest, TResult>(TRequest request)
//             where TRequest : class, new()
//             where TResult : class, new()
//         {
//             var xtResult = await _rqRep.RequestAsync<TRequest, TResult>(request).ConfigureAwait(false);
//             ThrowOnNonSuccess(xtResult);

//             return xtResult.GetResult();
//         }

//         public void Respond<TRequest, TResult>(Func<TRequest, TResult> callback, CancellationToken cancellationToken = default)
//             where TRequest : class, new()
//             where TResult : class, new()
//         {
//             var xtResult = _rqRep.Respond<TRequest, TResult>(callback, cancellationToken);
//             ThrowOnNonSuccess(xtResult);
//         }

//         public void RespondAsync<TRequest, TResult>(Func<TRequest, Task<TResult>> callback, CancellationToken cancellationToken = default)
//             where TRequest : class, new()
//             where TResult : class, new()
//         {
//             var xtResult = _rqRep.RespondAsync<TRequest, TResult>(callback, cancellationToken);
//             ThrowOnNonSuccess(xtResult);
//         }

//         public async Task SendAsync<TMessage>(TMessage message)
//             where TMessage : class, new()
//         {
//             var xtResult = await _sendReceive.SendAsync(message).ConfigureAwait(false);

//             ThrowOnNonSuccess(xtResult);
//         }

//         public void Receiver<TMessage>(Action<TMessage> callback, CancellationToken token = default)
//             where TMessage : class, new()
//         {
//             var xtResult = _sendReceive.SetupReceiver(callback, token);

//             ThrowOnNonSuccess(xtResult);
//         }

//         public void ReceiverAsync<TMessage>(Func<TMessage, Task> asyncCallack, CancellationToken token = default)
//             where TMessage : class, new()
//         {
//             var xtResult = _sendReceive.SetupReceiverAsync(asyncCallack, token);

//             ThrowOnNonSuccess(xtResult);
//         }

//         private void ThrowOnNonSuccess(XtResultBase xtResult)
//         {
//             if (xtResult.IsSuccess == false)
//                 throw ZeroMqXtSocketException.FromException(xtResult.Exception);
//         }

//         public async Task<bool> TryRequestAsync<TRequest, TResult>(TRequest request, Func<TResult, Task> successCallback, Func<Task> failureCallback)
//            where TRequest : class, new()
//            where TResult : class, new()
//         {
//             var xtResult = await _rqRep.RequestAsync<TRequest, TResult>(request).ConfigureAwait(false);

//             if (xtResult.IsSuccess)
//                 await successCallback(xtResult.GetResult()).ConfigureAwait(false);
//             else
//                 await failureCallback().ConfigureAwait(false);

//             return xtResult.IsSuccess;
//         }

//         public bool TryRespond<TRequest, TResult>(Func<TRequest, TResult> callback, CancellationToken cancellationToken = default)
//             where TRequest : class, new()
//             where TResult : class, new()
//         {
//             var xtResult = _rqRep.Respond<TRequest, TResult>(callback, cancellationToken);

//             return xtResult.IsSuccess;

//         }

//         public bool TryRespondAsync<TRequest, TResult>(Func<TRequest, Task<TResult>> callback, CancellationToken cancellationToken = default)
//             where TRequest : class, new()
//             where TResult : class, new()
//         {
//             var xtResult = _rqRep.RespondAsync<TRequest, TResult>(callback, cancellationToken);

//             return xtResult.IsSuccess;
//         }

//         public async Task<bool> TrySendAsync<TMessage>(TMessage message)
//             where TMessage : class, new()
//         {
//             var xtResult = await _sendReceive.SendAsync(message).ConfigureAwait(false);
//             return xtResult.IsSuccess;
//         }

//         public bool TryReceive<TMessage>(Action<TMessage> callback, CancellationToken token = default) where TMessage : class, new()
//         {
//             var xtResult = _sendReceive.SetupReceiver(callback, token);
//             return xtResult.IsSuccess;
//         }

//         public bool TryReceiveasync<TMessage>(Func<TMessage, Task> asyncCallack, CancellationToken token = default) where TMessage : class, new()
//         {
//             var xtResult = _sendReceive.SetupReceiverAsync(asyncCallack, token);
//             return xtResult.IsSuccess;
//         }

//         public IPublisher GetPublisher()
//         {
//             lock (_token)
//             {
//                 if (_publisher == null)
//                 {
//                     _publisher = _factory();
//                     _publisher.SetupPublisher();
//                 }
//             }
//             return _publisher;
//         }

//         public Subscriber GetSub()
//         {
//             throw new NotImplementedException();
//         }
//     }
// }