using System;
using System.Diagnostics;

namespace SolutionGen.Utils
{
    public static class Log
    {
        public static void WriteLine(string format, params object[] args)
        {
            Debug.WriteLine(format, args);
            Console.WriteLine(format, args);
        }
    }
}