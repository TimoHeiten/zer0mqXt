using System;
using System.Diagnostics;

namespace zeromq.endurance.LoadScenarios
{
    internal class Profiler : IDisposable
    {
        private Stopwatch _watch;
        private int _messages;
        private string _at;
        internal Profiler(string at, int @for) 
        {
            _at = at;
            _messages = @for;

            _watch = new();
            _watch.Start();
        }

        public void Dispose()
        {
            _watch.Stop();
            System.Console.WriteLine($"{_at} load scenario took: {_watch.ElapsedMilliseconds} ms for '{_messages}' messages");
        }
    }
}
