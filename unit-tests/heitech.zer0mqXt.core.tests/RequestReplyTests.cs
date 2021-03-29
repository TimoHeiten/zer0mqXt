using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using heitech.zer0mqXt.core.infrastructure;
using heitech.zer0mqXt.core.patterns;
using Xunit;

using static heitech.zer0mqXt.core.tests.ConfigurationTestData;

namespace heitech.zer0mqXt.core.tests
{
    public class RequestReplyTests
    {

        [Theory]
        [ClassData(typeof(ConfigurationTestData))]
        public async Task SimpleRequestAndReply_InProc(SocketConfiguration configuration)
        {
            // Arrange
            using var wrapper = CreateSocket(configuration);
            var sut = wrapper.Socket;
            sut.Respond<Request, Response>(rq => new Response { ResponseNumber = rq.RequestNumber });

            // Act
            var xtResult = await sut.RequestAsync<Request, Response>(new Request { RequestNumber = 42 });

            // Assert
            Assert.True(xtResult.IsSuccess);
            Assert.Equal(42, xtResult.GetResult().ResponseNumber);

        }

         [Theory]
         [ClassData(typeof(ConfigurationTestData))]
         public async Task SimpleRequestAndReply_Fails_when_factory_throws_Exception_But_still_gets_an_answer(SocketConfiguration configuration)
         {
            // Arrange
            using var wrapper = CreateSocket(configuration);
            var sut = wrapper.Socket;
            sut.Respond<Request, Response>(rq => throw new TimeoutException());

            // Act
             var xtResult = await sut.RequestAsync<Request, Response>(new Request { RequestNumber = 42 });

            // Assert
             Assert.False(xtResult.IsSuccess);
             Assert.NotNull(xtResult.Exception);
         }

         [Theory]
         [ClassData(typeof(ConfigurationTestData))]
         public async Task Multiple_Threads_Send_To_One_Responder_Works(SocketConfiguration configuration)
         {
             // Arrange
             using var wrapper = CreateSocket(configuration);
             var sut = wrapper.Socket;
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
         }

         private async Task DoMultipleRequestAsync(Socket sut, int input, List<(int, int)> input_output_Tuples)
         {
             var result = await sut.RequestAsync<Request, Response>(new Request { RequestNumber = input });
             input_output_Tuples.Add((input, result.GetResult().ResponseNumber));
         }

         [Theory]
         [ClassData(typeof(ConfigurationTestData))]
         public async Task Requests_Without_a_Server_returns_Endpoint_not_found_Exception(SocketConfiguration configuration)
         {
             // Arrange
             configuration.TimeOut = TimeSpan.FromMilliseconds(50);
             using var wrapper = CreateSocket(configuration);
             var sut = wrapper.Socket;
             // no server this time around

             // Act
             var xtResult = await sut.RequestAsync<Request, Response>(new Request());

             // Assert
             Assert.False(xtResult.IsSuccess);
         }

         [Theory]
         [ClassData(typeof(ConfigurationTestData))]
         public async Task Requests_Without_Server_TimeOut(SocketConfiguration configuration)
         {
             // Arrange
             configuration.TimeOut = TimeSpan.FromSeconds(1);
             using var wrapper = CreateSocket(configuration);
             var sut = wrapper.Socket;
             sut.Respond<Request, Response>(rq => 
             {
                 // is a timeout
                 Thread.Sleep(1500);
                 return new Response() { ResponseNumber = 88 };
             });
             var diagnostic = new System.Diagnostics.Stopwatch();
             diagnostic.Start();

             // Act
             var xtResult = await sut.RequestAsync<Request, Response>(new Request());

             // Assert
             diagnostic.Stop();
             Assert.True(diagnostic.ElapsedMilliseconds > 700);
             Assert.False(xtResult.IsSuccess);
         }

        [Fact]
        public async Task AsyncRqRep()
        {
             // fails
             // todo change to be able to use task (for now it needs parameterless ctor so it can be newed, which task obv does not support)
             // Arrange
             var ipc = new ConfigurationTestData().GetSocketConfigInProc;
             using var socket = CreateSocket(ipc);
             var sut = socket.Socket;
             sut.Respond<Request, Task<Response>>(r => 
             {
                return Task.FromResult(new Response { ResponseNumber = (int)Math.Pow(r.RequestNumber, r.RequestNumber)});
             });

            // Act
            var result = await sut.RequestAsync<Request, Response>(new Request { RequestNumber = 2 });

            // Assert
            Assert.True(result.IsSuccess);
        }

        // todo cancel server, stops response
        // todo multiple servers for same type (Contestion?)

        private class Request { public int RequestNumber { get; set; } }
        private class Response { public int ResponseNumber { get; set; } }
    }
}