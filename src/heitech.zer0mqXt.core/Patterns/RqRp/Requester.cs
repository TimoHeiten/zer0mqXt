using System;
using System.Threading.Tasks;
using heitech.zer0mqXt.core.infrastructure;
using heitech.zer0mqXt.core.RqRp;
using heitech.zer0mqXt.core.transport;
using NetMQ;
using NetMQ.Sockets;

namespace heitech.zer0mqXt.core.patterns.RqRp
{
    internal class Client : IClient
    {
        private readonly Retry _retry;
        private readonly RequestSocket _rqSocket;
        private readonly SocketConfiguration _configuration;

        private Client(SocketConfiguration configuration, RequestSocket socket)
        {
            _rqSocket = socket;
            _configuration = configuration;
            _retry = new Retry(configuration);
        }

        private const string operation = "request";
        private object _concurrencyToken = new();
        internal static XtResult<IClient> TryInitialize(SocketConfiguration configuration)
        {
            try
            {
                var socket = new RequestSocket();
                configuration.Logger.Log(new DebugLogMsg($"Setup Requester at [{configuration.Address()}]"));
                socket.Connect(configuration.Address());
                return XtResult<IClient>.Success(new Client(configuration, socket));
            }
            catch (System.Exception ex)
            {
                return XtResult<IClient>.Failed(ZeroMqXtSocketException.FromException(ex));
            }
        }

        public async Task<XtResult<TResponse>> RequestAsync<TRequest, TResponse>(TRequest request)
            where TRequest : class, new()
            where TResponse : class, new()
        {
            return await _retry.RunAsyncWithRetry(async () => 
            {
                _configuration.Logger.Log(new DebugLogMsg($"Send Request<{typeof(TRequest)}, {typeof(TResponse)}> to Address - {_configuration.Address()}"));
                var message = new RequestReplyMessage<TRequest>(_configuration, request);

                return await Task.Run(() =>
                {
                    // the request to be send with timeout
                    bool rqDidNotTimeOut = _rqSocket.TrySendMultipartMessage(_configuration.Timeout, message);
                    if (!rqDidNotTimeOut)
                        return XtResult<TResponse>.Failed(new TimeoutException($"Request<{typeof(TRequest)}, {typeof(TResponse)}> timed out"), operation);

                    _configuration.Logger.Log(new DebugLogMsg($"successfully sent [Request:{typeof(TRequest)}] and waiting for response [Response:{typeof(TResponse)}]"));
                    // wait for the response with timeout
                    var response = new NetMQMessage();
                    bool noTimeOut = _rqSocket.TryReceiveMultipartMessage(_configuration.Timeout, ref response, expectedFrameCount: 3);
                    if (!noTimeOut)
                        return XtResult<TResponse>.Failed(new TimeoutException($"Request<{typeof(TRequest)}, {typeof(TResponse)}> timed out"), operation);

                    _configuration.Logger.Log(new DebugLogMsg($"received Response [Response:{typeof(TResponse)}]"));

                    // parse the response and return the result
                    var xtResult = response.ParseRqRepMessage<TResponse>(_configuration);

                    return xtResult.IsSuccess
                            ? XtResult<TResponse>.Success(xtResult.GetResult(), operation)
                            : XtResult<TResponse>.Failed(xtResult.Exception, operation);
                }).ConfigureAwait(false);
            },tryToReconnect: () => _rqSocket.Connect(_configuration.Address())
            );

        }

        public void Dispose()
        {
            _rqSocket.Disconnect(_configuration.Address());
            _rqSocket?.Dispose();
        }
    }
}
