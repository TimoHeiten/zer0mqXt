using System;
using NetMQ;

namespace heitech.zer0mqXt.core.utils
{
    public static class PollerUtils
    {
        public static void DisposeOf(this NetMQPoller poller, NetMQSocket anySocket, EventHandler<NetMQSocketEventArgs> receiveDelegate)
        {
            anySocket.ReceiveReady -= receiveDelegate;
            poller.StopAsync();
            poller.RemoveAndDispose(anySocket);
            poller.Dispose();
        }
    }
}
