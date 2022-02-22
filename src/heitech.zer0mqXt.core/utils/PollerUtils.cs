using System;
using System.Threading.Tasks;
using NetMQ;

namespace heitech.zer0mqXt.core.utils
{
    public static class PollerUtils
    {
        public static void DisposeOf(this NetMQPoller poller, NetMQSocket anySocket, EventHandler<NetMQSocketEventArgs> receiveDelegate)
        {
            anySocket.ReceiveReady -= receiveDelegate;
            if (poller != null && poller.IsRunning)
            {
                // needs to be done from the Poller´s thread or else a race condition is encountered.
                new Task(() => { poller.Remove(anySocket); anySocket.Dispose(); })
                .Start(poller);
            }
        }
    }
}
