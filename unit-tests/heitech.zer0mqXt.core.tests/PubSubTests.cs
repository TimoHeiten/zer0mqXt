using System;
using System.Linq;
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
            var message = new Message { ThisIsAPublishedMessageText = "published a message", Array = new [] { 1, 2, 3, 4}};

            SocketConfiguration config = new ConfigurationTestData().GetSocketConfigInProc;
            config.Logger.SetSilent();
            var sut = new PubSub(config);
            sut.PrimePublisher();

            var waitHandle = new ManualResetEvent(false);
            var xtResult = sut.SubscribeHandler<Message>(callback: m => { incoming = m; waitHandle.Set(); } , CancellationToken.None);

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
            socket.RegisterSubscriber<Message>(msg => { capturedResponse = msg; waitHandle.Set();});

            // Act
            // setup server and wait for retry to work
            await socket.PublishAsync(new Message { ThisIsAPublishedMessageText = "published-message" });

            // Assert
            waitHandle.WaitOne();
            Thread.Sleep(300);
            Assert.NotNull(capturedResponse);
            Assert.Equal("published-message", capturedResponse.ThisIsAPublishedMessageText);
        }

        [Fact]
        public Task SimplePubSub_Tcp()
        {
            return Task.CompletedTask;
            // Arrange
            // Message incoming = null;
            // bool unsubscirbeNow = false;
            // var message = new Message { ThisIsAPublishedMessageText = "published a message", Array = new [] { 1, 2, 3, 4}};

            // SocketConfiguration config = new ConfigurationTestData().GetSocketConfigInProc;
            // using var wrapper = CreatePubSub(config);
            // var sut = wrapper.Socket;

            // sut.SubscribeHandler<Message>(callback: m => incoming = m, unsubscribeWhen: () => unsubscirbeNow);

            // // Act
            // await sut.PublishAsync<Message>(message);

            // // Assert
            // await Task.Delay(250);
            // unsubscirbeNow = true;
            // Assert.NotNull(incoming);
            // Assert.Equal(message.Array, incoming.Array);
            // Assert.Equal(message.ThisIsAPublishedMessageText, incoming.ThisIsAPublishedMessageText);
        }

        [Fact]
        public async Task Multiple_Subscriber_on_Single_Publisher()
        {
            // Arrange
            // 3 subs
            int counter = 0;
            var socket = Zer0Mq.Go().UsePublisher().BuildWithInProc("multiple-subscribers-one-publisher");
            Action<Message> subAction = m => counter++;
            var waitHandle = new ManualResetEvent(false);
            socket.RegisterSubscriber(subAction);
            socket.RegisterSubscriber(subAction);
            socket.RegisterSubscriber<Message>(m => { subAction(m); waitHandle.Set(); });
            
            // Act
            await socket.PublishAsync(new Message());
            
            // Assert
            waitHandle.WaitOne();
            Assert.Equal(3, counter);
        }

        public class Message 
        {
            public string ThisIsAPublishedMessageText { get; set; }
            public int[] Array { get; set; } = System.Array.Empty<int>();
        }
    }
}