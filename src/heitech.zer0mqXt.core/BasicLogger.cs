using System;

namespace heitech.zer0mqXt.core
{
    ///<summary>
    /// Logs to Console only
    ///</summary>
    public class BasicLogger : ILogger
    {
        public int MaxLogLevel = 3;

        public void Log(LogMessage message)
        {
            if (message.LogLevel > MaxLogLevel)
                return;

            ConsoleColoring coloring = ConsoleColoring.Default;
            switch (message)
            {
                case ErrorLogMsg error:
                    coloring = ConsoleColoring.Error;
                    break;
                case InfoLogMsg error:
                    coloring = ConsoleColoring.Info;
                    break;
                case DebugLogMsg error:
                    coloring = ConsoleColoring.Debug;
                    break;
            }
            coloring.Apply();
            Console.Write(message + Environment.NewLine);
            coloring.Dispose();
        }

        private class ConsoleColoring : IDisposable
        {
            private static ConsoleColor _default;
            static ConsoleColoring()
            {
                _default = Console.ForegroundColor;
            }
            private readonly ConsoleColor color;

            public static ConsoleColoring Default => new ConsoleColoring(_default);
            public static ConsoleColoring Error => new ConsoleColoring(ConsoleColor.Red);
            public static ConsoleColoring Debug => new ConsoleColoring(ConsoleColor.White);
            public static ConsoleColoring Info => new ConsoleColoring(ConsoleColor.Yellow);
            private ConsoleColoring(ConsoleColor color)
            {
                this.color = color;
            }

            public void Apply() => Console.ForegroundColor = this.color;

            public void Dispose()
            {
                Console.ResetColor();
            }
        }
    }

    public class ErrorLogMsg : LogMessage
    {
        public ErrorLogMsg(string msg)
        {
            Msg = $"[Error] - {msg}";
        }

        public override int LogLevel { get; set; } = 3;
    }

    public class InfoLogMsg : LogMessage
    {
        public InfoLogMsg(string msg)
        {
            Msg = $"[Info] - {msg}";
        }

        public override int LogLevel { get; set; } = 2;
    }

    public class DebugLogMsg : LogMessage
    {
        public DebugLogMsg(string msg) 
        {
            Msg = $"[Debug] - {msg}";
        }

        public override int LogLevel { get; set; } = 1;
    }
}