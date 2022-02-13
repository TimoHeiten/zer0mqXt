using System;

namespace zeromq.terminal.Utils
{
    public static class TestHelper
    {
        public static bool IsActive = true;

        public static void Print(string msg)
        {
            if (IsActive)
                System.Console.WriteLine(msg);
        }

        public static void SignalSuccess(bool success = true)
        {
            System.Console.WriteLine("".PadRight(50, '-'));
            System.Console.WriteLine(success ? "Success!" : "Failed!");
            System.Console.WriteLine("".PadRight(50, '-'));
        }
    }
}
