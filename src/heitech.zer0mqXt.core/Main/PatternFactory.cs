using heitech.zer0mqXt.core.infrastructure;
using heitech.zer0mqXt.core.patterns.RqRp;
using heitech.zer0mqXt.core.PubSub;
using heitech.zer0mqXt.core.RqRp;
using heitech.zer0mqXt.core.SendReceive;
using heitech.zer0mqXt.core.transport;

namespace heitech.zer0mqXt.core.Main
{
    internal class PatternFactory : IPatternFactory
    {
        private readonly SocketConfiguration _configuration;
        internal PatternFactory(SocketConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IPublisher CreatePublisher()
        {
            var result = Publisher.TryInitialize(_configuration);
            if (result.IsSuccess)
                return result.GetResult();

            _configuration.Logger.Log(new ErrorLogMsg($"Failed to create Requester at address : [{_configuration.Address()}]"));
            throw ZeroMqXtSocketException.FromException(result.Exception);
        }

        public ISubscriber CreateSubscriber()
            => new Subscriber(_configuration); // we can have as many Subscribers as we want but one publisher

        public IReceiver CreateReceiver()
            => new Receiver(_configuration, this.CreateResponder());
        public ISender CreateSender()
            => new Sender(_configuration);

        public IClient CreateClient()
        {
            var retry = new Retry(_configuration);
            // try to create a client by connecting to a socket.
            // for inproc it can fail if there is no responder already that binds to the socket
            // therefore we use the retry here
            var clientResult = retry.RunWithRetry<IClient>
            (
                () => 
                {
                    var result = Client.TryInitialize(_configuration);
                    if (result.IsSuccess)
                    {
                        return result;
                    }
                    return result;
                }
            );

            if (clientResult.IsSuccess)
                return clientResult.GetResult();

            // if neither the retry nor the cache succeeds, we log and throw
            _configuration.Logger.Log(new ErrorLogMsg($"Failed to create Requester at address : [{_configuration.Address()}]"));
            throw ZeroMqXtSocketException.FromException(clientResult.Exception);
        }

        public IResponder CreateResponder()
            => new Responder(_configuration);
    }
}
