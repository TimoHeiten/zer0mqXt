using heitech.zer0mqXt.core.PubSub;
using heitech.zer0mqXt.core.RqRp;
using heitech.zer0mqXt.core.SendReceive;

namespace heitech.zer0mqXt.core
{
    public interface IPatternFactory
    {
        IClient CreateClient();
        IResponder CreateResponder();

        IPublisher CreatePublisher();
        ISubscriber CreateSubscriber();

        ISender CreateSender();
        IReceiver CreateReceiver();
    }
}
