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
        private const string tcp_not_working_in_automated_tests = "tcp_not_working_in_automated_tests";

        [Fact]
        public async Task SimplePubSub_InProc()
        {
            // Arrange
            Message incoming = null;
            var message = new Message { ThisIsAPublishedMessageText = "published a message", Array = new[] { 1, 2, 3, 4 } };

            SocketConfiguration config = new ConfigurationTestData().GetSocketConfigInProc;
            config.Logger.SetSilent();
            var sut = new PubSub(config);
            sut.PrimePublisher();

            var waitHandle = new ManualResetEvent(false);
            var xtResult = sut.SubscribeHandler<Message>(callback: m => { incoming = m; waitHandle.Set(); }, CancellationToken.None);

            // Act
            await sut.PublishAsync<Message>(message);

            // Assert
            waitHandle.WaitOne();
            Assert.NotNull(incoming);
            Assert.True(xtResult.IsSuccess);
            Assert.Equal(message.Array, incoming.Array);
            Assert.Equal(message.ThisIsAPublishedMessageText, incoming.ThisIsAPublishedMessageText);
            sut.Dispose();
        }

        [Fact]
        public void Publish_without_bind_from_publisher_is_not_successfull()
        {
            var config = new ConfigurationTestData().GetSocketConfigInProc;
            var sut = new PubSub(config);

            // Act
            XtResult result = sut.SubscribeHandlerAsync<Message>(async m => await Task.CompletedTask, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            sut.Dispose();
            // no assert as no exception is expected otherwiese the test would fail at the call to the function pub()
        }

        [Fact]
        public async Task Subscriber_Cancellation_works()
        {
           // Arrange
           var socket = Zer0Mq.Go().SilenceLogger().UsePublisher().BuildWithInProc(Guid.NewGuid().ToString() + "-Subscriber-cancellation");
           bool wasReceived = false;
           var tokenSource = new CancellationTokenSource();
           var token = tokenSource.Token;
           tokenSource.Cancel();
           socket.RegisterSubscriber<Message>(msg => wasReceived = true, token);

           // Act
           await socket.PublishAsync<Message>(new Message());

           // Assert
           Assert.False(wasReceived);

        }

        [Fact]
        public async Task PubSub_with_retry_works()
        {
            // Arrange
            Message capturedResponse = null;
            using var socket = ConfigurationTestData.BuildInProcSocketInstanceForTest("retry-socket-pub-sub", timeoutInMs: 500, usePblshr: true);
            var waitHandle = new ManualResetEvent(false);
            // setup subscriber to handle messages in a background thread
            socket.RegisterSubscriber<Message>(msg => { capturedResponse = msg; waitHandle.Set(); });

            // Act
            // setup server and wait for retry to work
            await socket.PublishAsync(new Message { ThisIsAPublishedMessageText = "published-message" });

            // Assert
            waitHandle.WaitOne();
            Assert.NotNull(capturedResponse);
            Assert.Equal("published-message", capturedResponse.ThisIsAPublishedMessageText);
        }

        #region TCP Tests do not work in automated testing for some reason or another...

        [Fact(Skip = tcp_not_working_in_automated_tests)]
        public async Task SimplePubSub_Tcp()
        {
            // Arrange
            Message incoming = null;
            var resetEvent = new ManualResetEvent(false);
            var message = new Message { ThisIsAPublishedMessageText = "published a message", Array = new[] { 1, 2, 3, 4 } };

            var config = new ConfigurationTestData().GetSocketConfigInProc;
            using var sut = Zer0Mq.Go().UsePublisher().BuildWithTcp("localhost", "4880");

            Action a = () => sut.RegisterSubscriber<Message>(callback: m => { incoming = m; resetEvent.Set(); });
            // sanityCheck
            var ex = Record.Exception(a);
            Assert.Null(ex);
            

            // Act
            await sut.PublishAsync(message);

            // Assert
            bool wasSignaled = resetEvent.WaitOne(1500);
            Assert.True(wasSignaled);
            Assert.NotNull(incoming);
            Assert.Equal(message.Array, incoming.Array);
            Assert.Equal(message.ThisIsAPublishedMessageText, incoming.ThisIsAPublishedMessageText);
        }

        [Fact(Skip = tcp_not_working_in_automated_tests)]
        public async Task PubSub_with_two_Subscribers_on_different_types_works_with_correct_topic_typeframe()
        {
           // Arrange
           Message incoming = null;
           OtherMessage otherIncoming = null;
           var cntdwn = new CountdownEvent(2);
           using var sut = Zer0Mq.Go().UsePublisher().BuildWithTcp("localhost", "4889");

           
           Action a = () => sut.RegisterSubscriber<Message>(callback: m => { incoming = m; cntdwn.Signal(); });
           // sanityCheck
           var ex = Record.Exception(a);
           Assert.Null(ex);
           Action a2 = () => sut.RegisterSubscriber<OtherMessage>(callback: m => { otherIncoming = m; cntdwn.Signal(); });
           // sanityCheck
           var ex2 = Record.Exception(a);
           Assert.Null(ex2);
           

           // Act
           await sut.PublishAsync<Message>(new Message());
           await sut.PublishAsync<OtherMessage>(new OtherMessage { Content = "abcaffeschnee" });

           // Assert
           int timeout = 100;
           while (!cntdwn.IsSet || timeout <= 0)
           {
               await Task.Delay(10);
               timeout--;
           }
           Assert.True(cntdwn.IsSet);
           Assert.NotNull(incoming);
           Assert.NotNull(otherIncoming);
           Assert.Equal("abcaffeschnee", otherIncoming.Content);
        }

        [Fact(Skip = tcp_not_working_in_automated_tests)]
        public async Task TwoSubscribersWithOneSocket_Both_Get_A_Message_On_Same_Topic()
        {
           // Arrange
           Message incoming = null;
           Message otherIncoming = null;
           var cntdwn = new CountdownEvent(2);
           using var sut = Zer0Mq.Go().UsePublisher().BuildWithTcp("localhost", "4890");

           Action a = () => sut.RegisterSubscriber<Message>(callback: m => { incoming = m; cntdwn.Signal(); });
           // sanityCheck
           var ex = Record.Exception(a);
           Assert.Null(ex);
           Action a2 = () => sut.RegisterSubscriber<Message>(callback: m => { otherIncoming = m; cntdwn.Signal(); });
           // sanityCheck
           var ex2 = Record.Exception(a);
           Assert.Null(ex2);


            // Act
            await sut.PublishAsync<Message>(new Message { ThisIsAPublishedMessageText = "abcaffeschnee" });

           // Assert
           int timeout = 100;
           while (!cntdwn.IsSet || timeout <= 0)
           {
               await Task.Delay(10);
               timeout--;
           }
           Assert.True(cntdwn.IsSet);
           Assert.NotNull(incoming);
           Assert.NotNull(otherIncoming);
           Assert.Equal("abcaffeschnee", incoming.ThisIsAPublishedMessageText);
           Assert.Equal("abcaffeschnee", otherIncoming.ThisIsAPublishedMessageText);
        }

        private class OtherMessage
        {
            public string Content { get; set; }
        }
        #endregion

        [Fact]
        public async Task Multiple_Subscriber_on_Single_Publisher()
        {
            // Arrange
            // 3 subs
            int counter = 0;
            var socket = Zer0Mq.Go().UsePublisher().BuildWithInProc("multiple-subscribers-one-publisher");
            void subAction(ManualResetEvent handle, Message m) { counter++; handle.Set(); }
            var waitHandle1 = new ManualResetEvent(false);
            var waitHandle2 = new ManualResetEvent(false);
            var waitHandle3 = new ManualResetEvent(false);
            socket.RegisterSubscriber<Message>(m => subAction(waitHandle1, m));
            socket.RegisterSubscriber<Message>(m => subAction(waitHandle2, m));
            socket.RegisterSubscriber<Message>(m => subAction(waitHandle3, m));

            // Act
            await socket.PublishAsync(new Message());

            // Assert
            waitHandle1.WaitOne();
            waitHandle2.WaitOne();
            waitHandle3.WaitOne();
            Assert.Equal(3, counter);
        }

        public class Message
        {
            public string ThisIsAPublishedMessageText { get; set; }
            public int[] Array { get; set; } = System.Array.Empty<int>();
        }
    }
}