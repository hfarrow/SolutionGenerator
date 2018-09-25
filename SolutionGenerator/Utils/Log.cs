using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace SolutionGen.Utils
{
    public class Log
    {
        private static readonly AsyncLocal<Log> instance = new AsyncLocal<Log> { Value = new Log() };
        public static Log Instance => instance.Value ?? (instance.Value = new Log());
        
        public static Log InitBufferedTaskOutput(string bufferName, int indentLevel)
        {
            instance.Value = new Log(bufferName) {IndentLevel = indentLevel};
            instance.Value.info("Start Buffered Task Output".PadRight(50, '-'));
            return instance.Value;
        }

        private static readonly object flushLock = new object();
        public static void FlushBufferedLog()
        {
            if (Instance.buffer != null)
            {
                lock (flushLock)
                {
                    Info("End Buffered Task Output".PadRight(50, '-'));
                    Instance.buffer.ForEach(msg => msg());
                    Instance.buffer.Clear();
                }
            }
        }
        
        public enum Level
        {
            Debug,
            Info,
            Heading,
            Warn,
            Error
        }

        public class BufferedTaskOutput : IDisposable
        {
            public BufferedTaskOutput(string bufferName, int indentLevel)
            {
                InitBufferedTaskOutput(bufferName, indentLevel);
            }
            
            public void Dispose()
            {
                FlushBufferedLog();
            }
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
            private readonly object data;
            private readonly Stopwatch timer = Stopwatch.StartNew();
            
            public ScopedTimer(Level level, string description, object data = null)
            {
                this.level = level;
                this.description = description;
                this.data = data;
            }
            
            public void Dispose()
            {
                timer.Stop();
                Timer(level, description, timer, data);
            }

            public struct Result
            {
                public readonly TimeSpan Duration;
                public readonly object Data;

                public Result(TimeSpan duration, object data)
                {
                    Duration = duration;
                    Data = data;
                }
            }

            private static readonly Dictionary<string, List<Result>> results = new Dictionary<string, List<Result>>();

            public static void ClearResults()
            {
                lock (results)
                {
                    results.Clear();
                }
            }

            public static void LogResults(Level level)
            {
                lock (results)
                {
                    Timer(level, "Logging all completed timers...");
                    foreach (KeyValuePair<string, List<Result>> kvp in results)
                    {
                        Timer(level, "Timer results for \"{0}\":", kvp.Key);
                        using (new ScopedIndent())
                        {
                            kvp.Value.Sort((a, b) => b.Duration.Ticks.CompareTo(a.Duration.Ticks));
                            long sum = kvp.Value.Sum(r => r.Duration.Ticks);
                            long min = kvp.Value.Min(r => r.Duration.Ticks);
                            long max = kvp.Value.Max(r => r.Duration.Ticks);
                            long ave = (long) kvp.Value.Average(r => r.Duration.Ticks);
                            Timer(level, "Sum={0:g} Min={1:g} Ave={2:g} Max={3:g}",
                                new TimeSpan(sum).TotalSeconds,
                                new TimeSpan(min).TotalSeconds,
                                new TimeSpan(ave).TotalSeconds,
                                new TimeSpan(max).TotalSeconds);

                            IndentedCollection(kvp.Value,
                                r => $"{r.Duration.TotalSeconds,-15:g}{r.Data}",
                                (fmt, args) => Timer(level, fmt, args));
                        }
                    }
                }
            }

            internal static void TrackTimerResult(string description, TimeSpan duration, object data)
            {
                lock (results)
                {
                    if (!results.TryGetValue(description, out List<Result> list))
                    {
                        list = new List<Result>();
                        results[description] = list;
                    }

                    list.Add(new Result(duration, data));
                }
            }
        }

        private readonly List<Action> buffer;
        private readonly string bufferName;
        public int IndentLevel { get; private set; }
        public const int INDENT_SIZE = 2;

        public void pushIndentLevel() => ++IndentLevel;
        public void popIndentLevel() => IndentLevel = Math.Max(0, --IndentLevel);

        public static void PushIndentLevel()
            => Instance.pushIndentLevel();

        public static void PopIndentLevel()
            => Instance.popIndentLevel();

        public static Level LogLevel { get; set; } = Level.Warn;

        public Log()
        {
        }

        public Log(string bufferName)
        {
            this.bufferName = bufferName;
            buffer = new List<Action>();
        }

        private static void ProcessIndentedCollection<T>(
            IEnumerable<T> collection,
            Func<T, string> formatter,
            Action<string, object[]> logger)
        {
            using (new ScopedIndent())
            {
                T[] arr = collection.ToArray();
                int padding = (int) Math.Floor(Math.Log10(arr.Length) + 1);
                int i = -1;
                foreach (T value in arr)
                {
                    logger("[{0}] {1}", new object[]{(++i).ToString().PadLeft(padding), formatter(value)});
                }
            }
        }
        
        private static void ProcessIndentedCollection(
            IEnumerable collection,
            Func<object, string> formatter,
            Action<string, object[]> logger)
        {
            ProcessIndentedCollection(collection.Cast<object>(), formatter, logger);
        }
        
        public static void IndentedCollection<T>(
            IEnumerable<T> collection,
            Func<T, string> formatter,
            Action<string, object[]> logger)
        {
            ProcessIndentedCollection(collection, formatter, logger);
        }
        
        public static void IndentedCollection<T>(IEnumerable<T> collection, Action<string, object[]> logger)
            => IndentedCollection(collection, x => x?.ToString() ?? "<null>", logger);

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
        
        public string getIndentedCollection(IEnumerable collection, Func<object, string> formatter)
        {
            var builder = new StringBuilder();
            ProcessIndentedCollection(
                collection,
                formatter,
                (f, a) => builder.AppendFormat("\n" + GetIndent() + f, a));
            return builder.ToString();
        }
        
        public string getIndentedCollection<T>(IEnumerable<T> collection)
            => getIndentedCollection(collection, x => x?.ToString() ?? "<null>");

        public void heading(string format, params object[] args)
        {
            if (LogLevel <= Level.Heading)
            {
                writeLine(ConsoleColor.White, ConsoleColor.Black, format, args);
            }
        }
        
        public void info(string format, params object[] args)
        {
            if (LogLevel <= Level.Info)
            {
                writeLine(ConsoleColor.Gray, ConsoleColor.Black, format, args);
            }
        }
        
        public void debug(string format, params object[] args)
        {
            if (LogLevel <= Level.Debug)
            {
                writeLine(ConsoleColor.DarkGray, ConsoleColor.Black, format, args);
            }
        }

        public void warn(string format, params object[] args)
        {
            if (LogLevel <= Level.Warn)
            {
                writeLine(ConsoleColor.DarkYellow, ConsoleColor.Black, "[WARNING] " + format, args);
            }
        }
        
        public void error(string format, params object[] args)
        {
            if (LogLevel <= Level.Error)
            {
                writeLine(ConsoleColor.Red, ConsoleColor.Black, "[ERROR] " + format, args);
            }
        }

        public void timer(Level level, string description, Stopwatch stopwatch, object data)
        {
            if (LogLevel <= level)
            {
                TimeSpan duration = stopwatch.Elapsed;
                ScopedTimer.TrackTimerResult(description, duration, data);
                timer(level, "Finished timer \"{0}\" with duration '{1:g}'",
                    description, duration.TotalSeconds);
            }
        }

        public void timer(Level level, string format, params object[] args)
        {
            if (LogLevel <= level)
            {
                writeLine(ConsoleColor.Blue, ConsoleColor.Black, format, args);
            }
        }
        
        public void writeLine(ConsoleColor foreground, ConsoleColor background, string format, params object[] args)
        {
            string msg = GetLine(format, args);

            if (buffer != null)
            {
                buffer.Add(() => WriteLineToConsole(foreground, background, msg));
            }
            else
            {
                WriteLineToConsole(foreground, background, msg);
            }
        }
        
        private static void WriteLineToConsole(ConsoleColor foreground, ConsoleColor background, string msg)
        {
            System.Diagnostics.Debug.WriteLine(msg);
            Console.ForegroundColor = foreground;
            Console.BackgroundColor = background;
            Console.WriteLine(msg);
            Console.ResetColor();
        }

        public string getLine(string format, params object[] args)
        {
            return $"{GetIndent()}{string.Format(format, args ?? new object[0])}";
        }

        public string getIndent()
        {
            return $"({Thread.CurrentThread.ManagedThreadId}){bufferName}:{"".PadRight(IndentLevel * INDENT_SIZE, ' ')}";
        }

        public static string GetIndentedCollection(IEnumerable collection, Func<object, string> formatter)
            => Instance.getIndentedCollection(collection, formatter);

        public static string GetIndentedCollection<T>(IEnumerable<T> collection)
            => Instance.getIndentedCollection(collection);

        public static void Heading(string format, params object[] args)
            => Instance.heading(format, args);

        public static void Info(string format, params object[] args)
            => Instance.info(format, args);

        public static void Debug(string format, params object[] args)
            => Instance.debug(format, args);

        public static void Warn(string format, params object[] args)
            => Instance.warn(format, args);

        public static void Error(string format, params object[] args)
            => Instance.error(format, args);

        public static void Timer(Level level, string description, Stopwatch timer, object data = null)
            => Instance.timer(level, description, timer, data);

        public static void Timer(Level level, string format, params object[] args)
            => Instance.timer(level, format, args);

        public static void WriteLine(ConsoleColor foreground, ConsoleColor background, string format,
            params object[] args)
            => Instance.writeLine(foreground, background, format, args);

        public static string GetLine(string format, params object[] args)
            => Instance.getLine(format, args);

        public static string GetIndent()
            => Instance.getIndent();
    }
}