using System;
using System.Threading.Tasks;
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
            using var wrapper = CreatePubSub(config);
            var sut = wrapper.Socket;

            sut.SubscribeHandler<Message>(callback: m => incoming = m, unsubscribeWhen: () => unsubscirbeNow);

            // Act
            await sut.PublishAsync<Message>(message);

            // Assert
            await Task.Delay(250);
            unsubscirbeNow = true;
            Assert.NotNull(incoming);
            Assert.Equal(message.Array, incoming.Array);
            Assert.Equal(message.ThisIsAPublishedMessageText, incoming.ThisIsAPublishedMessageText);
        }

        [Fact]
        public async Task Publish_without_server_throws()
        {
            SocketConfiguration config = new ConfigurationTestData().GetSocketConfigInProc;
            using var wrapper = CreatePubSub(config);
            var sut = wrapper.Socket;

            // Act
            Func<Task> pub = async () =>  await sut.PublishAsync<Message>(new Message());

            // Assert
            await Assert.ThrowsAsync<NetMQ.EndpointNotFoundException>(pub);
        }

        // todo if tcp for pub sub, turn arround subscriber and publisher connect/bind and the order in which you invoke them
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