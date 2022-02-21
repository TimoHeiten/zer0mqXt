using Xunit;
using System.Threading.Tasks;
using static heitech.zer0mqXt.core.tests.PubSubInProcTests;
using System.Threading;
using heitech.zer0mqXt.core.Main;
using FluentAssertions;

namespace heitech.zer0mqXt.core.tests
{
    public class PubSubTcpTests
    {
        private const int SIG_WAIT = 2500;
        [Fact]
        public async Task SimplePubSub_Tcp()
        {
            // Arrange
            Message incoming = null;
            var resetEvent = new ManualResetEvent(false);
            var message = new Message { ThisIsAPublishedMessageText = "published a message", Array = new[] { 1, 2, 3, 4 } };

            var config = new ConfigurationTestData().GetSocketConfigInProc;
            var pattern = Zer0Mq.Go().SilenceLogger().BuildWithTcp("localhost", "4880");
            using var publisher = pattern.CreatePublisher();
            using var subscriber = pattern.CreateSubscriber();
            var xt = subscriber.RegisterSubscriber<Message>(callback: m => { incoming = m; resetEvent.Set(); });
            // sanityCheck
            Assert.True(xt.IsSuccess);
            await Task.Delay(250); // wait for the subscriber to be setup

            // Act
            await publisher.SendAsync(message);

            // Assert
            bool wasSignaled = resetEvent.WaitOne(SIG_WAIT);
            wasSignaled.Should().BeTrue();
            incoming.Should().NotBeNull();
            incoming.Array.Should().BeEquivalentTo(message.Array);
            incoming.ThisIsAPublishedMessageText.Should().Be(message.ThisIsAPublishedMessageText);
        }
    }
}