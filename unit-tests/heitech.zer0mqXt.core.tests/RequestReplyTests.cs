using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using heitech.zer0mqXt.core.infrastructure;
using heitech.zer0mqXt.core.Main;
using heitech.zer0mqXt.core.patterns;
using heitech.zer0mqXt.core.RqRp;
using Xunit;

namespace heitech.zer0mqXt.core.tests
{
    public class RequestReplyTests : IDisposable
    {
        [Fact]
        public async Task SimpleRequestAndReply_InProc()
        {
            // Arrange
            var pattern = Zer0Mq.Go().SilenceLogger().BuildWithInProc("test-pipe-" + Guid.NewGuid());
            using var responder = pattern.CreateResponder();
            responder.Respond<Request, Response>(rq => new Response { ResponseNumber = rq.RequestNumber });

            using var client = pattern.CreateClient();
            // Act
            var xtResult = await client.RequestAsync<Request, Response>(new Request { RequestNumber = 42 });

            // Assert
            Assert.True(xtResult.IsSuccess);
            Assert.Equal(42, xtResult.GetResult().ResponseNumber);
        }

        [Theory]
        [ClassData(typeof(ConfigurationTestData))]
        public async Task SimpleRequestAndReply_Fails_when_factory_throws_Exception_But_still_gets_an_answer(object configuration)
        {
            // Arrange
            var pattern = Zer0Mq.From((SocketConfiguration)configuration);
            using var responder = pattern.CreateResponder();
            responder.Respond<Request, Response>(rq => throw new TimeoutException());
            using var client = pattern.CreateClient();

            // Act
            var xtResult = await client.RequestAsync<Request, Response>(new Request { RequestNumber = 42 });

            // Assert
            Assert.False(xtResult.IsSuccess);
            Assert.NotNull(xtResult.Exception);
        }

        //! todo blocks forever
        // [Theory]
        // [ClassData(typeof(ConfigurationTestData))]
        // public async Task Multiple_Threads_Send_To_One_Responder_Works(object configuration)
        // {
        //     // Arrange
        //     var pattern = Zer0Mq.From((SocketConfiguration)configuration);

        //     using var responder = pattern.CreateResponder();
        //     responder.Respond<Request, Response>(rq => new Response { ResponseNumber = rq.RequestNumber });

        //     using var client = pattern.CreateClient();

        //     var input_output_Tuples = new List<(int, int)>();
        //     var taskList = new List<Task>()
        //      {
        //          DoMultipleRequestAsync(client, 1, input_output_Tuples),
        //          DoMultipleRequestAsync(client, 2, input_output_Tuples),
        //          DoMultipleRequestAsync(client, 3, input_output_Tuples),
        //      };

        //     //   Act
        //     await Task.WhenAll(taskList);

        //     //   Assert
        //     foreach (var (_in, _out) in input_output_Tuples)
        //         Assert.Equal(_in, _out);
        // }

        // private async Task DoMultipleRequestAsync(IClient sut, int input, List<(int, int)> input_output_Tuples)
        // {
        //     var result = await sut.RequestAsync<Request, Response>(new Request { RequestNumber = input });
        //     Assert.True(result.IsSuccess);
        //     input_output_Tuples.Add((input, result.GetResult().ResponseNumber));
        // }

        [Theory]
        [ClassData(typeof(ConfigurationTestData))]
        public void Requests_Without_a_Server_returns_Endpoint_not_found_Exception(object configuration)
        {
            // Arrange
            var config = (SocketConfiguration)configuration;
            bool isTcp = config.Address().Contains("tcp");
            config.Timeout = TimeSpan.FromMilliseconds(50);
            var pattern = Zer0Mq.From(config);
            // no server this time around

            // Act
            var ex = Record.Exception(() => pattern.CreateClient());

            // Assert
            if (isTcp)
                Assert.Null(ex);
            else
                Assert.IsType<ZeroMqXtSocketException>(ex);
        }

        // todo fails
        // [Theory]
        // [ClassData(typeof(ConfigurationTestData))]
        // public async Task Requests_With_Server_TimeOut_return_no_success(object configuration)
        // {
        //     // Arrange
        //     var config = (SocketConfiguration)configuration;
        //     config.Timeout = TimeSpan.FromMilliseconds(100);
        //     config.RetryIsActive = false;
        //     var pattern = Zer0Mq.From(config);
        //     using var responder = pattern.CreateResponder();
        //     responder.Respond<Request, Response>(rq =>
        //     {
        //         // is a timeout
        //         Thread.Sleep(1500);
        //         return new Response() { ResponseNumber = 88 };
        //     });
        //     using var client = pattern.CreateClient();

        //     // Act
        //     var xtResult = await client.RequestAsync<Request, Response>(new Request());

        //     // Assert
        //     Assert.False(xtResult.IsSuccess);
        // }


        [Fact]
        public async Task AsyncRqRep()
        {
            // Arrange
            var ipc = new ConfigurationTestData().GetSocketConfigInProc;
            var patterns = Zer0Mq.From(ipc);
            using var responder = patterns.CreateResponder();
            responder.RespondAsync<Request, Response>(r =>
            {
                return Task.FromResult(new Response { ResponseNumber = (int)Math.Pow(r.RequestNumber, r.RequestNumber) });
            });
            using var client = patterns.CreateClient();

            // Act
            var result = await client.RequestAsync<Request, Response>(new Request { RequestNumber = 2 });

            // Assert
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task Exception_propagation_when_server_response_Throws_to_Requester()
        {
            var ipc = new ConfigurationTestData().GetSocketConfigInProc;
            var patterns = Zer0Mq.From(ipc);
            using var responder = patterns.CreateResponder();
            responder.Respond<Request, Response>(r =>
            {
                throw new ArgumentException("this is a unit test proving the exception propagation works");
            });
            using var client = patterns.CreateClient();

            // Act
            var result = await client.RequestAsync<Request, Response>(new Request { RequestNumber = 2 });

            // Assert
            Assert.False(result.IsSuccess);
            Assert.NotNull(result.Exception);
            Assert.Contains("ArgumentException", result.Exception.Message);
            Assert.StartsWith("Server failed with" + Environment.NewLine + "ArgumentException", result.Exception.Message);
        }

        [Fact]
        public void Single_instance_of_RqRep_trying_to_setup_another_responder_on_same_instance_throws()
        {
            // Arrange
            var patterns = ConfigurationTestData.BuildInProcSocketInstanceForTest("single-instance-rq-rep-pipe");
            using var responder = patterns.CreateResponder();
            responder.Respond<Request, Response>((r) => new Response());
            using var client = patterns.CreateClient();

            // Act
            var result = responder.Respond<Request, Response>((r) => new Response());
            
            // Assert
            Assert.False(result.IsSuccess);
        }

        [Fact]
        public void Multiple_Socket_instances_and_multiple_responders_on_same_configuration_and_address_throws()
        {
            // Arrange
            var socket = Zer0Mq.Go().SilenceLogger().BuildWithInProc("pipe-throws");
            using var responder = socket.CreateResponder();
            responder.Respond<Request, Response>((r) => new Response { ResponseNumber = 1} );

            // Act
            var socket2 = Zer0Mq.Go().SilenceLogger().BuildWithInProc("pipe-throws");
            using var rsp2 = socket2.CreateResponder();
            var result = rsp2.Respond<Request, Response>((r) => new Response { ResponseNumber = 2} );

            // Assert
            Assert.False(result.IsSuccess);
            Assert.True(rsp2 == responder);
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

        // todo retry

        public void Dispose()
        {
            SocketConfiguration.CleanUp();
        }

        private class Request { public int RequestNumber { get; set; } }
        private class Response { public int ResponseNumber { get; set; } }
    }
}