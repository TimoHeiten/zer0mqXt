using System;
using System.Threading.Tasks;
using heitech.zer0mqXt.core.infrastructure;
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
            bool unsubscirbeNow = false;
            var message = new Message { ThisIsAPublishedMessageText = "published a message", Array = new [] { 1, 2, 3, 4}};

            SocketConfiguration config = new ConfigurationTestData().GetSocketConfigInProc;
            config.Logger.SetSilent();
            var sut = new PubSub(config);

            sut.SubscribeHandler<Message>(callback: m => incoming = m, unsubscribeWhen: () => unsubscirbeNow);

            // Act
            await sut.PublishAsync<Message>(message);

            // Assert
            await Task.Delay(250);
            unsubscirbeNow = true;
            Assert.NotNull(incoming);
            Assert.Equal(message.Array, incoming.Array);
            Assert.Equal(message.ThisIsAPublishedMessageText, incoming.ThisIsAPublishedMessageText);
            sut.Dispose();
        }

        [Fact]
        public async Task Publish_without_server_throws()
        {
            SocketConfiguration config = new ConfigurationTestData().GetSocketConfigInProc;
            var sut = new PubSub(config);

            // Act
            Func<Task> pub = async () =>  await sut.PublishAsync<Message>(new Message());

            // Assert
            await Assert.ThrowsAsync<NetMQ.EndpointNotFoundException>(pub);
            sut.Dispose();
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