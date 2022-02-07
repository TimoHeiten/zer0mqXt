using System;
using heitech.zer0mqXt.core.infrastructure;
using heitech.zer0mqXt.core.PubSub;
using heitech.zer0mqXt.core.RqRp;
using heitech.zer0mqXt.core.SendReceive;

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
            => PubSubFactory.CreatePublisher(_configuration);
        public ISubscriber CreateSubscriber()
            => PubSubFactory.CreateSubscriber(_configuration);

        public IReceiver CreateReceiver()
        {
            throw new NotImplementedException();
        }
        public ISender CreateSender()
        {
            throw new NotImplementedException();
        }

        public IClient CreateClient()
            => RequestReplyFactory.CreateClient(_configuration);

        public IResponder CreateResponder()
            => RequestReplyFactory.CreateResponder(_configuration);

      

        
    }
}
