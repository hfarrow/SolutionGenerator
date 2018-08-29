using System;
using System.Collections.Generic;
using System.IO;

namespace SolutionGen.Utils
{
    public static class DirectoryInfoExtensions
    {
        public static IEnumerable<FileInfo> GetFilesSafeRecursive(this DirectoryInfo dir, string searchPattern)
        {
            var pending = new Stack<string>();
            pending.Push(dir.FullName);
            while (pending.Count != 0)
            {
                string path = pending.Pop();
                string[] next = null;
                
                try
                {
                    next = Directory.GetFiles(path, searchPattern);
                }
                catch (Exception ex)
                {
                    Log.Warn("Cannot read files from directory '{0}'. User probably does not have read access. " +
                             "The exception is logged below when Debug level is active.", path);
                    Log.Debug(ex.ToString());
                }

                if (next != null && next.Length != 0)
                {
                    foreach (string file in next)
                    {
                        yield return new FileInfo(file);
                    }
                }
                
                try
                {
                    next = Directory.GetDirectories(path);
                    foreach (string subdir in next)
                    {
                        pending.Push(subdir);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn("Cannot read directories from directory '{0}'. User probably does not have read access. " +
                             "The exception is logged below when Debug level is active.", path);
                    Log.Debug(ex.ToString());
                }
            }
        } 
        
        public static IEnumerable<DirectoryInfo> GetDirectoriesSafeRecursive(this DirectoryInfo dir, string searchPattern)
        {
            var pending = new Stack<string>();
            pending.Push(dir.FullName);
            while (pending.Count != 0)
            {
                string path = pending.Pop();
                yield return new DirectoryInfo(path);
                
                try
                {
                    string[] next = Directory.GetDirectories(path);
                    foreach (string subdir in next)
                    {
                        pending.Push(subdir);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn("Cannot read directories from directory '{0}'. User probably does not have read access. " +
                             "The exception is logged below when Debug level is active.", path);
                    Log.Debug(ex.ToString());
                }
            }
        } 
    }
}