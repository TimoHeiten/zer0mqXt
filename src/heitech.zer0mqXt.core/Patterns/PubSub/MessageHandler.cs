using System;
using System.Threading;
using System.Threading.Tasks;
using heitech.zer0mqXt.core.infrastructure;
using heitech.zer0mqXt.core.transport;
using NetMQ;

namespace heitech.zer0mqXt.core.PubSub
{
    public class MessageHandler<TMessage>
        where TMessage : class, new()
    {
        private Action<TMessage> _syncCallback;
        private Func<TMessage, Task> _asyncCallback;
        private readonly SocketConfiguration _configuration;
        private MessageHandler(SocketConfiguration configuration)
            => _configuration = configuration;

        internal static MessageHandler<TMessage> Sync(SocketConfiguration configuration, Action<TMessage> callback)
            => new(configuration) { _syncCallback = callback };
        internal static MessageHandler<TMessage> ASync(SocketConfiguration configuration, Func<TMessage, Task> callback)
            => new(configuration) { _asyncCallback = callback };

        public async Task HandleWithAppropriateCallbackAsync(XtResult<TMessage> message, Func<Task> onError)
        {
            if (message.IsSuccess)
            {
                if (onError != null)
                    await onError();
                else
                    _configuration.Logger.Log(new ErrorLogMsg($"no error handler registered: [{message.Exception}]"));
            }

            if (_syncCallback != null)
                _syncCallback(message.GetResult());
            else
                await _asyncCallback(message.GetResult());
        }

        internal async Task OnReceive(NetMQSocket socket, Func<Task> onError, MessageHandler<TMessage> handler, Action kill, CancellationToken token)
        {
            if (token.IsCancellationRequested)
            {
                kill();
                _configuration.Logger.Log(new InfoLogMsg("Task was cancelled by cancellationRequest"));
                return;
            }

            XtResult<TMessage> actualMessage = null;
            var topic = socket.ReceiveFrameString();
            _configuration.Logger.Log(new DebugLogMsg($"received topic: '{topic}' for {typeof(TMessage)}"));
            
            var payload = socket.ReceiveFrameBytes();
            actualMessage = _configuration.ParseIncomingFrame<TMessage>(payload);

            await handler.HandleWithAppropriateCallbackAsync(actualMessage, onError);
        }
    }
}
