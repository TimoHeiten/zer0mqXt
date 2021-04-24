using System;
using System.Threading;
using System.Threading.Tasks;
using heitech.zer0mqXt.core.infrastructure;
using heitech.zer0mqXt.core.Main;
using heitech.zer0mqXt.core.patterns;
using Xunit;

namespace heitech.zer0mqXt.core.tests
{
    public class SendReceiveTests
    {
        [Theory]
        [ClassData(typeof(ConfigurationTestData))]
        public async Task SendReceive_works(object configuration)
        {
            // Arrange
            var config = (SocketConfiguration)configuration;
            using var sut = new SendReceive(config);
            int result = -1;
            sut.SetupReceiver<Message>(m => result = m.Number);

            // Act
            await sut.SendAsync(new Message { Number = 42 });

            // Assert
            Assert.Equal(42, result);
        }

        [Theory]
        [ClassData(typeof(ConfigurationTestData))]
        public async Task Sender_Without_a_Server_returns_Endpoint_not_found_Exception(object configuration)
        {
            // Arrange
            var config = (SocketConfiguration)configuration;
            config.TimeOut = TimeSpan.FromMilliseconds(50);
            using var sut = new SendReceive(config);
            // no server this time around

            // Act
            var xtResult = await sut.SendAsync<Message>(new Message());

            // Assert
            Assert.False(xtResult.IsSuccess);
        }

        [Theory]
        [ClassData(typeof(ConfigurationTestData))]
        public async Task Sends_With_Server_TimeOut_return_no_success(object configuration)
        {
            // Arrange
            var config = (SocketConfiguration)configuration;
            config.TimeOut = TimeSpan.FromSeconds(1);
            using var sut = new SendReceive(config);
            // is a timeout
            sut.SetupReceiver<Message>(m => Thread.Sleep(1500));

            // Act
            var xtResult = await sut.SendAsync(new Message());

            // Assert
            Assert.False(xtResult.IsSuccess);
            sut.Dispose();
        }


        [Fact]
        public async Task AsyncSendAndReceive()
        {
            // Arrange
            var ipc = new ConfigurationTestData().GetSocketConfigInProc;
            using var sut = new SendReceive(ipc);
            var result = -1;
            sut.SetupReceiverAsync<Message>(r =>
            {
                result = r.Number;
                return Task.CompletedTask;
            });

            // Act
            var xtResult = await sut.SendAsync(new Message { Number = 2 });

            // Assert
            Assert.True(xtResult.IsSuccess);
            Assert.Equal(2, result);
        }

        [Fact]
        public async Task Exception_propagation_when_server_response_Throws_to_Requester()
        {
            var ipc = new ConfigurationTestData().GetSocketConfigInProc;
            using var sut = new SendReceive(ipc);
            sut.SetupReceiver<Message>(r =>
            {
                throw new ArgumentException("this is a unit test proving the exception propagation works");
            });

            // Act
            var result = await sut.SendAsync<Message>(new Message());

            // Assert
            Assert.False(result.IsSuccess);
            Assert.NotNull(result.Exception);
            Assert.Contains("ArgumentException", result.Exception.Message);
            Assert.StartsWith("Server failed with" + Environment.NewLine + "ArgumentException", result.Exception.Message);
        }

        [Fact]
        public void Single_instance_of_SendReceive_trying_to_setup_another_responder_or_receiver_on_same_instance_throws()
        {
            // Arrange
            using var socket = Zer0Mq.Go().SilentLogger().BuildWithInProc("send-pipe-throw");
            socket.Receiver<Message>((r) => { });

            // Act
            Action setupReceiverThatThrows = () => socket.Receiver<Message>((r) => {});
            Action setupResponderActionThatThrows = () => socket.Respond<Message, Message>((r) => r);
            
            // Assert
            Assert.Throws<ZeroMqXtSocketException>(setupReceiverThatThrows);
            Assert.Throws<ZeroMqXtSocketException>(setupResponderActionThatThrows);
        }

        [Fact]
        public void Multiple_Socket_instances_and_multiple_responders_on_same_configuration_and_address_throws()
        {
            const string inpipeThrows = "send-pipe-throws";
            Func<IZer0MqBuilder> builderFactory = () => Zer0Mq.Go().SilentLogger();
            // Arrange
            using var socket = builderFactory().BuildWithInProc(inpipeThrows);
            socket.Receiver<Message>((r) => { });

            // Act
            using var socket2 = builderFactory().BuildWithInProc(inpipeThrows);
            Action throwingAction = () => socket2.Respond<Message, Message>((r) => r);
            using var socket3 = builderFactory().BuildWithInProc(inpipeThrows);
            Action throwingAction2 = () => socket2.Receiver<Message>((r) => { });

            // Assert
            Assert.Throws<ZeroMqXtSocketException>(throwingAction);
            Assert.Throws<ZeroMqXtSocketException>(throwingAction2);
        }



        private class Message { public int Number { get; set; } = 12; }
    }
}