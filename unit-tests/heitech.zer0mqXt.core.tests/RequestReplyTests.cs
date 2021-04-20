using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using heitech.zer0mqXt.core.infrastructure;
using heitech.zer0mqXt.core.patterns;
using Xunit;

namespace heitech.zer0mqXt.core.tests
{
    public class RequestReplyTests : IDisposable
    {

        [Fact]
        public async Task SimpleRequestAndReply_InProc()
        {
            // Arrange
            var sut = new RqRep(new ConfigurationTestData().GetSocketConfigInProc);
            sut.Respond<Request, Response>(rq => new Response { ResponseNumber = rq.RequestNumber });

            // Act
            var xtResult = await sut.RequestAsync<Request, Response>(new Request { RequestNumber = 42 });

            // Assert
            Assert.True(xtResult.IsSuccess);
            Assert.Equal(42, xtResult.GetResult().ResponseNumber);
            sut.Dispose();
        }

        [Theory]
        [ClassData(typeof(ConfigurationTestData))]
        public async Task SimpleRequestAndReply_Fails_when_factory_throws_Exception_But_still_gets_an_answer(object configuration)
        {
            // Arrange
            var sut = new RqRep((SocketConfiguration)configuration);
            sut.Respond<Request, Response>(rq => throw new TimeoutException());

            // Act
            var xtResult = await sut.RequestAsync<Request, Response>(new Request { RequestNumber = 42 });

            // Assert
            Assert.False(xtResult.IsSuccess);
            Assert.NotNull(xtResult.Exception);
            sut.Dispose();
        }

        [Theory]
        [ClassData(typeof(ConfigurationTestData))]
        public async Task Multiple_Threads_Send_To_One_Responder_Works(object configuration)
        {
            // Arrange
            var sut = new RqRep((SocketConfiguration)configuration);
            sut.Respond<Request, Response>(rq => new Response { ResponseNumber = rq.RequestNumber });
            var input_output_Tuples = new List<(int, int)>();
            var taskList = new List<Task>()
             {
                 DoMultipleRequestAsync(sut, 1, input_output_Tuples),
                 DoMultipleRequestAsync(sut, 2, input_output_Tuples),
                 DoMultipleRequestAsync(sut, 3, input_output_Tuples),
             };

            //   Act
            await Task.WhenAll(taskList);

            //   Assert
            foreach (var (_in, _out) in input_output_Tuples)
                Assert.Equal(_in, _out);

            sut.Dispose();
        }

        private async Task DoMultipleRequestAsync(RqRep sut, int input, List<(int, int)> input_output_Tuples)
        {
            var result = await sut.RequestAsync<Request, Response>(new Request { RequestNumber = input });
            Assert.True(result.IsSuccess);
            input_output_Tuples.Add((input, result.GetResult().ResponseNumber));
        }

        [Theory]
        [ClassData(typeof(ConfigurationTestData))]
        public async Task Requests_Without_a_Server_returns_Endpoint_not_found_Exception(object configuration)
        {
            // Arrange
            var config = (SocketConfiguration)configuration;
            config.TimeOut = TimeSpan.FromMilliseconds(50);
            var sut = new RqRep(config);
            // no server this time around

            // Act
            var xtResult = await sut.RequestAsync<Request, Response>(new Request());

            // Assert
            Assert.False(xtResult.IsSuccess);
            sut.Dispose();
        }

        [Theory]
        [ClassData(typeof(ConfigurationTestData))]
        public async Task Requests_With_Server_TimeOut_return_no_success(object configuration)
        {
            // Arrange
            var config = (SocketConfiguration)configuration;
            config.TimeOut = TimeSpan.FromSeconds(1);
            var sut = new RqRep(config);
            sut.Respond<Request, Response>(rq =>
            {
                // is a timeout
                Thread.Sleep(1500);
                return new Response() { ResponseNumber = 88 };
            });

            // Act
            var xtResult = await sut.RequestAsync<Request, Response>(new Request());

            // Assert
            Assert.False(xtResult.IsSuccess);
            sut.Dispose();
        }


        [Fact]
        public async Task AsyncRqRep()
        {
            // Arrange
            var ipc = new ConfigurationTestData().GetSocketConfigInProc;
            var sut = new RqRep(ipc);
            sut.RespondAsync<Request, Response>(r =>
            {
                return Task.FromResult(new Response { ResponseNumber = (int)Math.Pow(r.RequestNumber, r.RequestNumber) });
            });

            // Act
            var result = await sut.RequestAsync<Request, Response>(new Request { RequestNumber = 2 });

            // Assert
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task Exception_propagation_when_server_response_Throws_to_Requester()
        {
             var ipc = new ConfigurationTestData().GetSocketConfigInProc;
            var sut = new RqRep(ipc);
            sut.Respond<Request, Response>(r =>
            {
                throw new ArgumentException("this is a unit test proving the exception propagation works");
            });

            // Act
            var result = await sut.RequestAsync<Request, Response>(new Request { RequestNumber = 2 });

            // Assert
            Assert.False(result.IsSuccess);
            Assert.NotNull(result.Exception);
            Assert.Contains("ArgumentException", result.Exception.Message);
            Assert.StartsWith("Server failed with" + Environment.NewLine + "ArgumentException", result.Exception.Message);
        }

        public void Dispose()
        {
            SocketConfiguration.CleanUp();
        }

        // todo cancel server, stops response
        // todo multiple servers for same type (Contestion?)

        private class Request { public int RequestNumber { get; set; } }
        private class Response { public int ResponseNumber { get; set; } }
    }
}