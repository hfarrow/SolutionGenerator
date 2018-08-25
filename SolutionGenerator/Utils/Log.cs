using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SolutionGen.Utils
{
    public static class Log
    {
        public enum Level
        {
            Debug,
            Info,
            Heading,
            Warn,
            Error
        }
        
        public class ScopedIndent : IDisposable
        {
            public ScopedIndent()
            {
                PushIndentLevel();
            }
            
            public void Dispose()
            {
                PopIndentLevel();
            }
        }
        
        public class ScopedTimer : IDisposable
        {
            private readonly Level level;
            private readonly string description;
            private readonly Stopwatch timer = Stopwatch.StartNew();
            
            public ScopedTimer(Level level, string description)
            {
                this.level = level;
                this.description = description;
            }
            
            public void Dispose()
            {
                timer.Stop();
                Timer(level, description, timer);
            }
        }
        
        private static int indentLevel;
        public static int IndentSize = 2;

        public static void PushIndentLevel() => ++indentLevel;
        public static void PopIndentLevel() => indentLevel = Math.Max(0, --indentLevel);

        public static Level LogLevel { get; set; } = Level.Warn;

        private static void ProcessIndentedCollection<T>(
            IEnumerable<T> collection,
            Func<T, string> formatter,
            Action<string, object[]> logger)
        {
            using (new ScopedIndent())
            {
                int i = -1;
                foreach (T value in collection)
                {
                    logger("[{0}] {1}", new object[]{++i, formatter(value)});
                }
            }
        }
        
        private static void ProcessIndentedCollection(
            IEnumerable collection,
            Func<object, string> formatter,
            Action<string, object[]> logger)
        {
            using (new ScopedIndent())
            {
                int i = -1;
                foreach (object value in collection)
                {
                    logger("[{0}] {1}", new object[]{++i, formatter(value)});
                }
            }
        }

        public static void IndentedCollection<T>(
            IEnumerable<T> collection,
            Func<T, string> formatter,
            Action<string, object[]> logger)
        {
            ProcessIndentedCollection(collection, formatter, logger);
        }

        public static string GetIndentedCollection<T>(
            IEnumerable<T> collection,
            Func<T, string> formatter)
        {
            var builder = new StringBuilder();
            ProcessIndentedCollection(
                collection,
                formatter,
                (f, a) => builder.AppendFormat("\n" + GetIndent() + f, a));
            return builder.ToString();
        }
        
        public static string GetIndentedCollection(IEnumerable collection, Func<object, string> formatter)
        {
            var builder = new StringBuilder();
            ProcessIndentedCollection(
                collection,
                formatter,
                (f, a) => builder.AppendFormat("\n" + GetIndent() + f, a));
            return builder.ToString();
        }

        public static void IndentedCollection<T>(IEnumerable<T> collection, Action<string, object[]> logger)
            => IndentedCollection(collection, x => x.ToString(), logger);
        
        public static string GetIndentedCollection<T>(IEnumerable<T> collection)
            => GetIndentedCollection(collection, x => x.ToString());

        public static void Heading(string format, params object[] args)
        {
            if (LogLevel <= Level.Heading)
            {
                WriteLine(ConsoleColor.White, ConsoleColor.Black, format, args);
            }
        }
        
        public static void Info(string format, params object[] args)
        {
            if (LogLevel <= Level.Info)
            {
                WriteLine(ConsoleColor.Gray, ConsoleColor.Black, format, args);
            }
        }
        
        public static void Debug(string format, params object[] args)
        {
            if (LogLevel <= Level.Debug)
            {
                WriteLine(ConsoleColor.DarkGray, ConsoleColor.Black, format, args);
            }
        }

        public static void Warn(string format, params object[] args)
        {
            if (LogLevel <= Level.Warn)
            {
                WriteLine(ConsoleColor.DarkYellow, ConsoleColor.Black, "[WARNING] " + format, args);
            }
        }
        
        public static void Error(string format, params object[] args)
        {
            if (LogLevel <= Level.Error)
            {
                WriteLine(ConsoleColor.Red, ConsoleColor.Black, "[ERROR] " + format, args);
            }
        }

        public static void Timer(Level level, string description, Stopwatch timer)
        {
            if (LogLevel <= level)
            {
                WriteLine(ConsoleColor.Blue, ConsoleColor.Black,
                    "Finished timer \"{0}\" with duration '{1:g}'",
                    description, timer.Elapsed.TotalSeconds);
            }
        }
        
        public static void WriteLine(ConsoleColor foreground, ConsoleColor background, string format, params object[] args)
        {
            string msg = GetLine(format, args);
            System.Diagnostics.Debug.WriteLine(msg);

            Console.ForegroundColor = foreground;
            Console.BackgroundColor = background;
            Console.WriteLine(msg);
            Console.ResetColor();
        }

        public static string GetLine(string format, params object[] args)
        {
            return $"{GetIndent()}{string.Format(format, args ?? new object[0])}";
        }

        public static string GetIndent()
        {
            return "".PadRight(indentLevel * IndentSize, ' ');
        }
    }
}