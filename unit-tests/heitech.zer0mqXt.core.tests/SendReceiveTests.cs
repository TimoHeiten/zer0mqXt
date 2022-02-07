using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using heitech.zer0mqXt.core.infrastructure;
using heitech.zer0mqXt.core.Main;
using heitech.zer0mqXt.core.patterns;
using heitech.zer0mqXt.core.RqRp;
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
            _sender = _factory.CreateSender();

            // Act
            await _sender.SendAsync(new Message { Number = 42 });

            // Assert
            Assert.Equal(42, result);
        }

        [Theory]
        [ClassData(typeof(ConfigurationTestData))]
        public void Sender_Without_a_Server_returns_Endpoint_not_found_Exception(object configuration)
        {
            // Arrange
            var config = (SocketConfiguration)configuration;
            bool isTcp = config.Address().Contains("tcp");
            config.Timeout = TimeSpan.FromMilliseconds(50);
            // no server this time around

            // Act
            var ex = Record.Exception(() => new Sender(config));

            // Assert
            if (isTcp)
                Assert.Null(ex);
            else
                Assert.IsType<ZeroMqXtSocketException>(ex);
        }

        [Theory]
        [ClassData(typeof(ConfigurationTestData))]
        public async Task Sends_With_Server_Timeout_return_no_success(object configuration)
        {
            // Arrange
            var config = (SocketConfiguration)configuration;
            config.Timeout = TimeSpan.FromSeconds(1);
            using var rec = new Receiver(config);
            // is a Timeout
            rec.SetupReceiver<Message>(m => Thread.Sleep(1500));
            using var sender = new Sender(config);

            // Act
            var xtResult = await sender.SendAsync(new Message());

            // Assert
            Assert.False(xtResult.IsSuccess);
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
            _sender = _factory.CreateSender();

            // Act
            var xtResult = await _sender.SendAsync(new Message { Number = 2 });

            // Assert
            Assert.True(xtResult.IsSuccess);
            Assert.Equal(2, result);
        }

        [Fact]
        public async Task Exception_propagation_when_server_response_Throws_to_Requester()
        {
            // Arrange
            _receiver.SetupReceiver<Message>(r =>
            {
                throw new ArgumentException("this is a unit test proving the exception propagation works");
            });
            _sender = _factory.CreateSender();

            // Act
            var result = await _sender.SendAsync<Message>(new Message());

            // Assert
            Assert.False(result.IsSuccess);
            Assert.NotNull(result.Exception);
            Assert.Contains("ArgumentException", result.Exception.Message);
            Assert.StartsWith("Server failed with" + Environment.NewLine + "ArgumentException", result.Exception.Message);
        }

        [Fact]
        public void Single_instance_of_SendReceive_trying_to_setup_another_responder_or_receiver_on_same_instance_returns_no_success()
        {
            // Arrange
            _receiver.SetupReceiver<Message>((r) => { });
            _sender = _factory.CreateSender();
            // Act
            var result = _receiver.SetupReceiver<Message>((r) => {});
            var isSameSender = _factory.CreateSender();
            
            // Assert
            //! not a really nice way, but was done after huge refactoring session so
            //! it is fine for now 
            Func<ISender, IClient> getRq = s => (IClient)s.GetType()
                                                          .GetField("_client", BindingFlags.NonPublic | BindingFlags.Instance)
                                                          .GetValue(s); 
            Assert.False(result.IsSuccess);
            // compare references of the underlying REP client
            Assert.True(getRq(_sender) == getRq(isSameSender));
        }

        private class Message { public int Number { get; set; } = 12; }
    }
}