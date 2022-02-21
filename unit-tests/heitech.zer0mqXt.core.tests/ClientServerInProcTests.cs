using System;
using System.Threading.Tasks;
using FluentAssertions;
using heitech.zer0mqXt.core.infrastructure;
using heitech.zer0mqXt.core.Main;
using heitech.zer0mqXt.core.RqRp;
using Xunit;
using Xunit.Abstractions;

namespace heitech.zer0mqXt.core.tests
{
    public class ClientServerInProcTests : IDisposable
    {
        private IClient _client; // need to create in each test after setting up the responder, else it will crash
        private IResponder _responder;
        private readonly IPatternFactory _patterns;
        private readonly ITestOutputHelper _h;
        public ClientServerInProcTests(ITestOutputHelper h)
        {
            _h = h;
            _patterns = Zer0Mq.Go().SilenceLogger().BuildWithInProc($"{Guid.NewGuid()}");
            _responder = _patterns.CreateResponder();
        }

        private IClient Client
        {
            get
            {
                if (_client == null)
                {
                    _client = _patterns.CreateClient();
                }
                return _client;
            }
        }

        [Fact]
        public async Task SimpleRequestAndReply_InProc()
        {
            // Arrange
            _responder.Respond<Request, Response>(rq => new Response { ResponseNumber = rq.RequestNumber });

            // Act
            var xtResult = await Client.RequestAsync<Request, Response>(new Request { RequestNumber = 42 });

            // Assert
            xtResult.IsSuccess.Should().BeTrue();
            xtResult.GetResult().ResponseNumber.Should().Be(42);
        }

        [Fact]
        public async Task SimpleRequestAndReply_Fails_when_factory_throws_Exception_But_still_gets_an_answer()
        {
            // Arrange
            _responder.Respond<Request, Response>(rq => throw new TimeoutException());

            // Act
            var xtResult = await Client.RequestAsync<Request, Response>(new Request { RequestNumber = 42 });

            // Assert
            xtResult.IsSuccess.Should().BeFalse();
            xtResult.Exception.Should().NotBeNull();
        }

        [Fact]
        public void Requests_Without_a_Server_returns_Endpoint_not_found_Exception()
        {
            // Arrange
            var config = (SocketConfiguration)new ConfigurationTestData().GetSocketConfigInProc;
            var pattern = Zer0Mq.From(config);
            config.Timeout = TimeSpan.FromMilliseconds(50);
            // no server this time around

            // Act
            var ex = Record.Exception(() => pattern.CreateClient());

            // Assert
            ex.Should().BeOfType<ZeroMqXtSocketException>();
        }

        [Fact]
        public async Task AsyncRqRep()
        {
            // Arrange
            _responder.RespondAsync<Request, Response>(r =>
            {
                return Task.FromResult(new Response { ResponseNumber = (int)Math.Pow(r.RequestNumber, r.RequestNumber) });
            });
            _client = _patterns.CreateClient();

            // Act
            var result = await Client.RequestAsync<Request, Response>(new Request { RequestNumber = 2 });

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task Exception_propagation_when_server_response_Throws_to_Requester()
        {
            // Arrange
            var p2 = Zer0Mq.Go().EnableDeveloperMode().BuildWithInProc($"{Guid.NewGuid()}");
            using var responder2 = p2.CreateResponder();
            responder2.Respond<Request, Response>(r =>
            {
                throw new ArgumentException("this is a unit test proving the exception propagation works");
            });
            using var client2 = p2.CreateClient();

            // Act
            var result = await client2.RequestAsync<Request, Response>(new Request { RequestNumber = 2 });

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Exception.Should().NotBeNull();
            result.Exception.Message.Should().Contain("ArgumentException");
            result.Exception.Message.Should().StartWith($"Server failed with{Environment.NewLine}ArgumentException");
        }

        [Fact]
        public void Single_instance_of_Responder_trying_to_setup_another_responseHandler_returns_setupResult_false()
        {
            // Arrange
            _responder.Respond<Request, Response>((r) => new Response());

            // Act
            var result = _responder.Respond<Request, Response>((r) => new Response());

            // Assert
            result.IsSuccess.Should().BeFalse();
        }

        [Theory]
        [InlineData(0, false)]
        [InlineData(1, true)]
        [InlineData(3, true)]
        public async Task RetryWorksForTheSpecifiedRetryCount(uint retryCount, bool expectedSuccess)
        {
            IClient client = null;
            try
            {
                // Arrange
                var socket = Zer0Mq.Go().SetLogger(new LoggerAdapter { H = _h }).SilenceLogger().SetTimeOut(300).SetRetryCount(retryCount).BuildWithInProc($"{Guid.NewGuid()}");
                using var responder = socket.CreateResponder();
                var setup = Task.Run(async () =>
                {
                    await Task.Delay(150);
                    responder.Respond<Request, Response>((r) => new Response { ResponseNumber = 2 * r.RequestNumber });
                });
                // Act uses retry here
                var ex = Record.Exception(() => { client = socket.CreateClient(); });
                // Act
                // and uses retry also here
                if (!expectedSuccess)
                {
                    Assert.IsType<ZeroMqXtSocketException>(ex);
                    return;
                }

                var result = await client?.RequestAsync<Request, Response>(new Request { RequestNumber = 21 });
                await setup;

                // Assert
                ex.Should().BeNull();
            }
            finally
            {
                client?.Dispose();
            }
        }

        [Fact]
        public async Task Request_ReplyError_Does_Not_Propagate_remote_Stacktrace()
        {
            // Arrange
            _responder.Respond<Request, Response>(x => throw new InvalidOperationException("Message is propagated"));
            _client = _patterns.CreateClient();

            // Act
            var result = await _client.RequestAsync<Request, Response>(new Request());

            // Assert
            Assert.False(result.IsSuccess);
            Assert.DoesNotContain("propagated", result.Exception.Message);
            var ex = ZeroMqXtSocketException.ResponseFailed<Response>();
            Assert.Equal(ex.Message, result.Exception.Message);
        }

        [Fact]
        public async Task RequestAndReply_OnlySendStacktraceInDeveloperMode()
        {
            // Arrange
            var patternsv2 = Zer0Mq.Go().EnableDeveloperMode().BuildWithInProc($"{Guid.NewGuid()}");
            using var respond = patternsv2.CreateResponder();
            respond.Respond<Request, Response>(x => throw new InvalidOperationException("Message is propagated"));
            using var client = patternsv2.CreateClient();

            // Act
            var result = await client.RequestAsync<Request, Response>(new Request());

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("propagated", result.Exception.Message);
        }

        private class LoggerAdapter : ILogger
        {
            public ITestOutputHelper H;
            public void Log(LogMessage message)
            {
                H.WriteLine(message.Msg);
            }

            public void SetLogLevel(int level)
            {
                //
            }

            public void SetSilent()
            {
                //
            }
        }

        // todo interface for try was removed
        // [Fact]
        // public async Task TryRequest_returns_false_and_invokes_the_failure_callback_when_no_server_exists()
        // {
        //     // Arrange
        //     var socket = ConfigurationTestData.BuildInProcSocketInstanceForTest("try-request-pipe");
        //     bool successCalled = false;
        //     bool failureCalled = false;

        //     // Act
        //     bool result = await socket.TryRequestAsync<Request, Response>(new Request(),
        //         successCallback: r => { successCalled = true; return Task.CompletedTask; },
        //         failureCallback: () => { failureCalled = true; return Task.CompletedTask; }
        //     );

        //     // Assert
        //     Assert.False(successCalled);
        //     Assert.True(failureCalled);
        //     Assert.False(result);
        // }

        // [Fact]
        // public void TryRespond_returns_false_when_a_server_already_exists()
        // {
        //     // Arrange
        //     using var socket = ConfigurationTestData.BuildInProcSocketInstanceForTest("inproc-try-methods-pipe-respond");
        //     using var anotherSocket = ConfigurationTestData.BuildInProcSocketInstanceForTest("inproc-try-methods-pipe-respond");
        //     socket.Respond<Request, Response>(r => new Response());

        //     // Act
        //     bool success = anotherSocket.TryRespond<Request, Response>((r) => new Response());

        //     // Assert
        //     Assert.False(success);
        // }

        // [Fact]
        // public void TryRespond_on_same_socket_instance_still_throws()
        // {
        //     // Arrange
        //     using var socket = ConfigurationTestData.BuildInProcSocketInstanceForTest("inproc-try-methods-pipe-respond-same-socket");
        //     socket.Respond<Request, Response>(r => new Response());

        //     // Act
        //     Action a = () => socket.TryRespond<Request, Response>((r) => new Response());

        //     // Assert
        //     Assert.Throws<ZeroMqXtSocketException>(a);
        // }

        [Fact]
        public async Task Multiple_Threads_Send_To_One_Responder_Works()
        {
            // Arrange
            var pattern = Zer0Mq.Go().BuildWithInProc($"{Guid.NewGuid()}");

            using var responder = pattern.CreateResponder();
            responder.Respond<Request, Response>(rq => new Response { ResponseNumber = rq.RequestNumber });

            using var client = pattern.CreateClient();

            var input_output_Tuples = new System.Collections.Generic.List<(int, int)>();
            var taskList = new System.Collections.Generic.List<Task>()
             {
                 DoMultipleRequestAsync(client, 1, input_output_Tuples),
                 DoMultipleRequestAsync(client, 2, input_output_Tuples),
                //  DoMultipleRequestAsync(client, 3, input_output_Tuples),
             };

            //   Act
            await Task.WhenAll(taskList);

            //   Assert
            foreach (var (_in, _out) in input_output_Tuples)
                Assert.Equal(_in, _out);
        }

        private async Task DoMultipleRequestAsync(IClient sut, int input, System.Collections.Generic.List<(int, int)> input_output_Tuples)
        {
            var result = await sut.RequestAsync<Request, Response>(new Request { RequestNumber = input });
            result.IsSuccess.Should().BeTrue();
            input_output_Tuples.Add((input, result.GetResult().ResponseNumber));
        }

        internal class Request { public int RequestNumber { get; set; } }
        private class Response { public int ResponseNumber { get; set; } }


        public void Dispose()
        {
            _responder.Dispose();
            _client?.Dispose();
        }
    }
}