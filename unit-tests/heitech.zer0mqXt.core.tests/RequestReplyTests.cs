using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace heitech.zer0mqXt.core.tests
{
    public class RequestReplyTests
    {
        private static SocketWrapper CreateSocket(SocketConfiguration configuration)
            => new SocketWrapper(new Socket(configuration));

        private class SocketWrapper : IDisposable
        {
            public Socket Socket;
            public SocketWrapper(Socket socket)
            {
                this.Socket = socket;
            }

            public void Dispose()
            {
                Socket.Dispose();
            }
        }

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

            //   Act
             var xtResult = await sut.RequestAsync<Request, Response>(new Request { RequestNumber = 42 });

            //   Assert
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

        //todo cancel server, stops response

        private class Request { public int RequestNumber { get; set; } }
        private class Response { public int ResponseNumber { get; set; } }


        public class ConfigurationTestData : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                yield return new object[] { SocketConfiguration.InprocConfig("test-pipe") };
                yield return new object[] { SocketConfiguration.TcpConfig(port: "5566", host: "localhost") };
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}