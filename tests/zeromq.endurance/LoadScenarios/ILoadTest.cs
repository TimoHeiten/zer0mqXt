using System;
using System.Threading.Tasks;

namespace zeromq.endurance.LoadScenarios
{
    public interface ILoadTest : IDisposable
    {
        string Name => this.GetType().Name;
        Task SimpleMessages(int amountOfMessages);
        Task SimpleMessagesRaw(int amountOfMessages);
    }
}
