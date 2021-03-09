using System;
using System.Threading;
using heitech.zer0mqXt.core;

namespace zeromq.terminal
{
    ///<summary>
    /// Test all the happy pathes for all socket types in the library but with inproc only
    ///</summary>
    class Program
    {

        private static ManualResetEvent handle;
        static void Main(string[] args)
        {
            handle = new ManualResetEvent(false);
            var configuration = SocketConfiguration.InprocConfig("this-inproc-sir-" + Guid.NewGuid());
            var socket = new Socket(configuration);

            socket.Respond<Request, Response>((rq) => 
            {
                System.Console.WriteLine();
                System.Console.WriteLine("now calling the factory");
                System.Console.WriteLine();
                var rsp = new Response();
                rsp.InsideResponse += " " + rq.FromRequest;

                return rsp;
            });

            System.Console.WriteLine("try request");
            XtResult<Response> result = socket.RequestAsync<Request, Response>(new Request()).Result;
            System.Console.WriteLine(result);
            if (result.IsSuccess)
                System.Console.WriteLine("SUCCEESS!! " + result.GetResult().InsideResponse);
            else
                System.Console.WriteLine("FAILURE!! " + result.Exception);
        }

        private class Request 
        {
            public string FromRequest { get; set; } = "Message from Request";
        }

        private class Response
        {
            public string InsideResponse { get; set; } = "Message inside Response";
        }
    }
}
