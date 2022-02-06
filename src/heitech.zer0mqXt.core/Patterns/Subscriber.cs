using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using heitech.zer0mqXt.core.infrastructure;
using heitech.zer0mqXt.core.transport;
using NetMQ;
using NetMQ.Sockets;

namespace heitech.zer0mqXt.core.patterns
{
    public class Subscriber : IDisposable
    {
        private readonly List<SubscriberRegistration> _registrations = new();
        private readonly SocketConfiguration _configuration;
        private readonly NetMQPoller _poller = new(); // one poller to rule them all
        private bool pollerStarted = false;

        internal Subscriber(SocketConfiguration configuration)
            => _configuration = configuration;

        public XtResult RegisterSubscriber<T>(Action<T> callback, string topic = null, CancellationToken token = default)
            where T : class, new()
        {
            var next = new SubscriberRegistration(_poller, _configuration);
            _registrations.Add(next);
            TryStartPoller();

            return next.Register<T>(topic, syncCallback: callback, asyncCallback: null, token);
        }
        private readonly object _token = new object();
        private void TryStartPoller()
        {
            lock (_token)
            {
                if (!pollerStarted)
                {
                    _poller.RunAsync();
                    pollerStarted = true;
                }
            }
        }

        public XtResult RegisterAsyncSubscriber<T>(Func<T, Task> asyncCallback, string topic = null, CancellationToken token = default)
            where T : class, new()
        {
            var next = new SubscriberRegistration(_poller, _configuration);
            _registrations.Add(next);
            TryStartPoller();

            return next.Register<T>(topic, syncCallback: null, asyncCallback: asyncCallback, token);
        }

        public void Dispose()
        {
            if (_poller != null)
            {
                _poller.Stop();
            }

            _registrations.ForEach(x => x.Dispose());
        }

    }
}
