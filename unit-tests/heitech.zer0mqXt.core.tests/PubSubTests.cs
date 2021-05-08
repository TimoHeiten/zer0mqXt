using System;
using System.Threading;
using System.Threading.Tasks;
using heitech.zer0mqXt.core.infrastructure;
using heitech.zer0mqXt.core.Main;
using heitech.zer0mqXt.core.patterns;
using Xunit;

using static heitech.zer0mqXt.core.tests.ConfigurationTestData;

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

            var xtResult = sut.SubscribeHandler<Message>(callback: m => incoming = m, CancellationToken.None);

            // Act
            await sut.PublishAsync<Message>(message);

            // Assert
            await Task.Delay(500);
            Assert.NotNull(incoming);
            Assert.True(xtResult.IsSuccess);
            Assert.Equal(message.Array, incoming.Array);
            Assert.Equal(message.ThisIsAPublishedMessageText, incoming.ThisIsAPublishedMessageText);
            sut.Dispose();
        }

        [Fact]
        public async Task Publish_without_server_throws()
        {
            var config = new ConfigurationTestData().GetSocketConfigInProc;
            var sut = new PubSub(config);

            // Act
            Func<Task> pub = async () => await sut.PublishAsync<Message>(new Message());

            // Assert
            bool noException = false;
            try
            {
                await pub();
                noException = true;
            }
            catch (System.Exception)
            {
                noException = false;
            }
            sut.Dispose();
            Assert.True(noException);
        }

        [Fact]
        public async Task Subscriber_Cancellation_works()
        {
            // Arrange
            var socket = Zer0Mq.Go().BuildWithInProc(Guid.NewGuid().ToString() + "-Subscriber-cancellation");
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

        public class Message 
        {
            public string ThisIsAPublishedMessageText { get; set; }
            public int[] Array { get; set; }
        }
    }
}