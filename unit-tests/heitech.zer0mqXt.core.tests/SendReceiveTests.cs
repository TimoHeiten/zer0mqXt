using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using heitech.zer0mqXt.core.infrastructure;
using heitech.zer0mqXt.core.Main;
using heitech.zer0mqXt.core.patterns.RqRp;
using heitech.zer0mqXt.core.SendReceive;
using Xunit;

namespace heitech.zer0mqXt.core.tests
{
    public class SendReceiveTests : IDisposable
    {
        private readonly IPatternFactory _factory;
        private readonly IReceiver _receiver;
        // for inproc to work, the BIND socket (Server) needs to be there first.
        // we need to create the sender from the factory only after BIND happened
        // therefore we only create it in the actual test and not the ctor
        private ISender Sender
        {
            get
            {
                if (_sender == null)
                {
                    _sender = _factory.CreateSender();
                }
                return _sender;
            }
        }

        private ISender _sender;
        public SendReceiveTests()
        {
            _factory = Zer0Mq.Go().SilenceLogger().BuildWithInProc($"{Guid.NewGuid()}");
            _receiver = _factory.CreateReceiver();
        }

        public void Dispose()
        {
            _sender?.Dispose();
            _receiver.Dispose();
        }

        [Fact]
        public async Task SendReceive_works()
        {
            // Arrange
            int result = -1;
            _receiver.SetupReceiver<Message>(m => result = m.Number);

            // Act
            await Sender.SendAsync(new Message { Number = 42 });

            // Assert
            result.Should().Be(42);
        }

        [Theory]
        [ClassData(typeof(ConfigurationTestData))]
        public void Sender_Without_a_Server_returns_Endpoint_not_found_Exception(object configuration)
        {
            // Arrange
            var config = (SocketConfiguration)configuration;
            config.Logger.SetSilent();
            bool isTcp = config.Address().Contains("tcp");
            config.Timeout = TimeSpan.FromMilliseconds(50);
            // no server this time around

            // Act
            var ex = Record.Exception(() => new Sender(config));

            // Assert
            if (isTcp)
                ex.Should().BeNull();
            else
                ex.Should().BeOfType<ZeroMqXtSocketException>();
        }

        [Theory]
        [ClassData(typeof(ConfigurationTestData))]
        public async Task Sends_With_Server_Timeout_return_no_success(object configuration)
        {
            // Arrange
            var config = (SocketConfiguration)configuration;
            config.Logger.SetSilent();
            config.Timeout = TimeSpan.FromSeconds(1);
            using var rec = new Receiver(config, new Responder(config));
            // is a Timeout
            rec.SetupReceiver<Message>(m => Thread.Sleep(1500));
            using var sender = new Sender(config);

            // Act
            var xtResult = await sender.SendAsync(new Message());

            // Assert
            xtResult.IsSuccess.Should().BeFalse();
        }


        [Fact]
        public async Task AsyncSendAndReceive()
        {
            // Arrange
            var result = -1;
            _receiver.SetupReceiverAsync<Message>(r =>
            {
                result = r.Number;
                return Task.CompletedTask;
            });

            // Act
            var xtResult = await Sender.SendAsync(new Message { Number = 2 });

            // Assert
            xtResult.IsSuccess.Should().BeTrue();
            result.Should().Be(2);
        }

        [Fact]
        public async Task Exception_propagation_when_server_response_Throws_to_Requester()
        {
            // Arrange
            // Arrange
            var p2 = Zer0Mq.Go().SilenceLogger().EnableDeveloperMode().BuildWithInProc($"{Guid.NewGuid()}");
            using var receiver2 = p2.CreateReceiver();
            receiver2.SetupReceiver<Message>(r =>
            {
                throw new ArgumentException("this is a unit test proving the exception propagation works");
            });
            using var sender2 = p2.CreateSender();

            // Act
            var result = await sender2.SendAsync<Message>(new Message());

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Exception.Should().NotBeNull();
            result.Exception.Message.Should().Contain("ArgumentException");
            result.Exception.Message.Should().StartWith(
                "Server failed with" + Environment.NewLine + "ArgumentException", result.Exception.Message
            );
        }

        [Fact]
        public void Single_instance_of_SendReceive_trying_to_setup_another_receiver_on_same_instance_returns_no_success()
        {
            // Arrange
            _receiver.SetupReceiver<Message>((r) => { });
            _ = Sender;
            // Act
            var result = _receiver.SetupReceiver<Message>((r) => { });

            // Assert
            result.IsSuccess.Should().BeFalse();
        }

        private class Message { public int Number { get; set; } = 12; }
    }
}