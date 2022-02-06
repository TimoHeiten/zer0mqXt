using System;
using System.Threading;
using System.Threading.Tasks;
using heitech.zer0mqXt.core.infrastructure;
using heitech.zer0mqXt.core.Main;
using heitech.zer0mqXt.core.patterns;
using Xunit;

namespace heitech.zer0mqXt.core.tests
{
    public class PubSubTests
    {
        [Fact]
        public async Task SimplePubSub_InProc()
        {
            // Arrange
            Message incoming = null;
            var message = new Message { ThisIsAPublishedMessageText = "published a message", Array = new[] { 1, 2, 3, 4 } };

            SocketConfiguration config = new ConfigurationTestData().GetSocketConfigInProc;
            config.Logger.SetSilent();
            using var subscriber = new Subscriber(config);
            using var sut = new Publisher(config);
            sut.SetupPublisher();

            var waitHandle = new ManualResetEvent(false);
            var xtResult = subscriber.RegisterSubscriber<Message>(
                callback: m => 
                {
                    incoming = m; 
                    waitHandle.Set(); 
                }, topic: null, CancellationToken.None
            );
            Assert.True(xtResult.IsSuccess);

            // Act
            await sut.SendAsync<Message>(message);

            // Assert
            bool wasSignaled = waitHandle.WaitOne(5000);
            Assert.True(wasSignaled);
            Assert.NotNull(incoming);
            Assert.True(xtResult.IsSuccess);
            Assert.Equal(message.Array, incoming.Array);
            Assert.Equal(message.ThisIsAPublishedMessageText, incoming.ThisIsAPublishedMessageText);
        }

        [Fact]
        public void Publish_without_bind_from_publisher_fails()
        {
            var config = new ConfigurationTestData().GetSocketConfigInProc;
            using var subscriber = new Subscriber(config);

            // Act
            XtResult result = subscriber.RegisterSubscriber<Message>(async m => await Task.CompletedTask, topic: null, CancellationToken.None);

            // Assert
            // no assert as no exception is expected otherwiese the test would fail at the call to the function pub()
            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task Subscriber_Cancellation_works()
        {
            // Arrange
            var socket = Zer0Mq.Go().SilenceLogger().BuildWithInProc(Guid.NewGuid().ToString() + "-Subscriber-cancellation");
            var publisher = socket.GetPublisher();
            bool wasReceived = false;
            var tokenSource = new CancellationTokenSource();
            var token = tokenSource.Token;
            // cancel before it is registered
            tokenSource.Cancel();
            socket.RegisterSubscriber<Message>(msg => wasReceived = true, token);

            // Act
            await publisher.SendAsync<Message>(new Message());

            // Assert
            Assert.False(wasReceived);
        }

        [Fact]
        public async Task PubSub_with_retry_works()
        {
            // Arrange
            Message capturedResponse = null;
            using var socket = ConfigurationTestData.BuildInProcSocketInstanceForTest("retry-socket-pub-sub", timeoutInMs: 500, usePblshr: true);
            var publisher = socket.GetPublisher();
            var waitHandle = new ManualResetEvent(false);
            // setup subscriber to handle messages in a background thread
            socket.RegisterSubscriber<Message>(msg => { capturedResponse = msg; waitHandle.Set(); });

            // Act
            // setup server and wait for retry to work
            var result = await publisher.SendAsync(new Message { ThisIsAPublishedMessageText = "published-message" });

            // Assert
            Assert.True(result.IsSuccess);
            bool wasSignaled = waitHandle.WaitOne(1000);
            Assert.True(wasSignaled);
            Assert.NotNull(capturedResponse);
            Assert.Equal("published-message", capturedResponse.ThisIsAPublishedMessageText);
        }

        #region TCP Tests do not work in automated testing for some reason or another...

        [Fact]
        public async Task SimplePubSub_Tcp()
        {
            // Arrange
            Message incoming = null;
            var resetEvent = new ManualResetEvent(false);
            var message = new Message { ThisIsAPublishedMessageText = "published a message", Array = new[] { 1, 2, 3, 4 } };

            var config = new ConfigurationTestData().GetSocketConfigInProc;
            using var sut = Zer0Mq.Go().BuildWithTcp("localhost", "4880");
            var publisher = sut.GetPublisher();

            Action a = () => sut.RegisterSubscriber<Message>(callback: m => { incoming = m; resetEvent.Set(); });
            // sanityCheck
            var ex = Record.Exception(a);
            Assert.Null(ex);

            // Act
            await publisher.SendAsync(message);

            // Assert
            bool wasSignaled = resetEvent.WaitOne(1500);
            Assert.True(wasSignaled);
            Assert.NotNull(incoming);
            Assert.Equal(message.Array, incoming.Array);
            Assert.Equal(message.ThisIsAPublishedMessageText, incoming.ThisIsAPublishedMessageText);
        }

        [Fact]
        public async Task PubSub_with_two_Subscribers_on_different_types_works_with_correct_topic_typeframe()
        {
            // Arrange
            Message incoming = null;
            OtherMessage otherIncoming = null;
            var waitHandle1 = new ManualResetEvent(false);
            var waitHandle2 = new ManualResetEvent(false);
            using var sut = Zer0Mq.Go().BuildWithInProc($"{Guid.NewGuid()}");
            var publisher = sut.GetPublisher();

            Action a = () => sut.RegisterSubscriber<Message>(callback: m => { incoming = m; waitHandle1.Set(); });
            // sanityCheck
            var ex = Record.Exception(a);
            Assert.Null(ex);
            Action a2 = () => sut.RegisterSubscriber<OtherMessage>(callback: m => { otherIncoming = m; waitHandle2.Set(); });
            // sanityCheck
            var ex2 = Record.Exception(a2);
            Assert.Null(ex2);

            // Act
            await publisher.SendAsync<Message>(new Message());
            await publisher.SendAsync<OtherMessage>(new OtherMessage { Content = "abcaffeschnee" });

            // Assert
            bool wasSignaled1 = waitHandle1.WaitOne(1000);
            bool wasSignaled2 = waitHandle2.WaitOne(1000);
            Assert.True(wasSignaled1);
            Assert.True(wasSignaled2);

            Assert.NotNull(incoming);
            Assert.NotNull(otherIncoming);
            Assert.Equal("abcaffeschnee", otherIncoming.Content);
        }

        [Fact()]
        public async Task TwoSubscribersWithOneSocket_Both_Get_A_Message_On_Same_Topic()
        {
            // Arrange
            Message incoming = null;
            Message otherIncoming = null;
            var cntdwn = new CountdownEvent(2);
            var sut = Zer0Mq.Go().BuildWithInProc($"{Guid.NewGuid()}");
            var publisher = sut.GetPublisher();

            Action a = () => sut.RegisterSubscriber<Message>(
                callback: m =>
                {
                    incoming = m; 
                    cntdwn.Signal();
                }
            );
            // sanityCheck
            var ex = Record.Exception(a);
            Assert.Null(ex);
            Action a2 = () => sut.RegisterSubscriber<Message>(callback: m =>
            {
                otherIncoming = m;
                cntdwn.Signal();
            });
            // sanityCheck
            var ex2 = Record.Exception(a2);
            Assert.Null(ex2);

            // Act
            await publisher.SendAsync<Message>(new Message { ThisIsAPublishedMessageText = "abcaffeschnee" });

            // Assert
            bool wasSignaled = cntdwn.Wait(TimeSpan.FromMilliseconds(2500));
            Assert.True(wasSignaled);
            Assert.NotNull(incoming);
            Assert.NotNull(otherIncoming);
            Assert.Equal("abcaffeschnee", incoming.ThisIsAPublishedMessageText);
            Assert.Equal("abcaffeschnee", otherIncoming.ThisIsAPublishedMessageText);
            sut.Dispose();
        }

        private class OtherMessage
        {
            public string Content { get; set; }
        }
        #endregion

        // [Fact]
        // public async Task Multiple_Subscriber_on_Single_Publisher()
        // {
        //     // Arrange
        //     // 3 subs
        //     var socket = Zer0Mq.Go().BuildWithInProc("multiple-subscribers-one-publisher");
        //     var publisher = socket.GetPublisher();
        //     void subAction(ManualResetEvent handle, Message m) { handle.Set(); }
        //     var waitHandle1 = new ManualResetEvent(false);
        //     var waitHandle2 = new ManualResetEvent(false);
        //     var waitHandle3 = new ManualResetEvent(false);
        //     socket.RegisterSubscriber<Message>(m => subAction(waitHandle1, m));
        //     socket.RegisterSubscriber<Message>(m => subAction(waitHandle2, m));
        //     socket.RegisterSubscriber<Message>(m => subAction(waitHandle3, m));

        //     // Act
        //     await publisher.SendAsync(new Message());

        //     // Assert
        //     bool signaled1 = waitHandle1.WaitOne(1500);
        //     bool signaled2 = waitHandle2.WaitOne(1500);
        //     bool signaled3 = waitHandle3.WaitOne(1500);

        //     Assert.All(new [] { signaled1, signaled2, signaled3 }, x => Assert.True(x));
        // }

        public class Message
        {
            public string ThisIsAPublishedMessageText { get; set; }
            public int[] Array { get; set; } = System.Array.Empty<int>();
        }
    }
}