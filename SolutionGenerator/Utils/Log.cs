using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SolutionGen.Utils
{
    public static class Log
    {
        public class ScopedIndent : IDisposable
        {
            private readonly bool emptyLineAfterPop;

            public ScopedIndent(bool emptyLineAfterPop = false)
            {
                this.emptyLineAfterPop = emptyLineAfterPop;
                PushIndentLevel();
            }
            
            public virtual void Dispose()
            {
                PopIndentLevel();
                if (emptyLineAfterPop)
                {
                    WriteLine("");
                }
            }
        }
        
        private static int indentLevel = 0;

        public static void PushIndentLevel() => ++indentLevel;
        public static void PopIndentLevel() => indentLevel = Math.Max(0, --indentLevel);

        public static void WriteIndentedCollection<T>(IEnumerable<T> collection, Func<T, string> logger,
            bool emptyLineAfterPop = false)
        {
            using (new ScopedIndent(emptyLineAfterPop))
            {
                int i = -1;
                foreach (T value in collection)
                {
                    WriteLine("[{0}] {1}", ++i, logger(value));
                }
            }
        }
        
        public static void WriteLine(string format, params object[] args)
        {
            string msg = $"{"".PadRight(indentLevel, '\t')}{string.Format(format, args)}";
            Debug.WriteLine(msg);
            Console.WriteLine(msg);
        }

        public static void WriteLineWarning(string format, params object[] args)
        {
            WriteLine("[WARNING] " + format, args);
        }
    }
}