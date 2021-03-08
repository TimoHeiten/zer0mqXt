using System;
using System.Threading.Tasks;
using heitech.zer0mqXt.core;

namespace zeromq.terminal
{
    class Program
    {
        static void Main(string[] args)
        {
            var configuration = SocketConfiguration.InprocConfig("this-inproc-mama");
            var socket = new Socket(configuration);

            Task.Run(() => 
            {
                socket.RespondTo<Response, Request>((rq) => 
                {
                    System.Console.WriteLine();
                    System.Console.WriteLine("now calling the factory");
                    System.Console.WriteLine();
                    var rsp = new Response();
                    rsp.InsideResponse += " " + rq.FromRequest;

                    return rsp;
                });
            });

            XtResult<Response> result = socket.RequestAsync<Request, Response>(new Request()).Result;
            System.Console.WriteLine(result);
            if (result.IsSuccess)
                System.Console.WriteLine(result.GetResult().InsideResponse);
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
