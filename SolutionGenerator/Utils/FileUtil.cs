using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SolutionGen.Generator.Model;
using Path = System.IO.Path;

namespace SolutionGen.Utils
{
    public static class FileUtil
    {
        public static HashSet<string> GetFilesInSearchPath(IEnumerable<string> searchableDirectories,
            IEnumerable<IPath> includePaths, IEnumerable<IPath> excludePaths)
        {
            IEnumerable<string> results = searchableDirectories
                .Where(Directory.Exists)
                .Aggregate(
                    (IEnumerable<string>) new HashSet<string>(),
                    (current, directory) => current.Concat(
                        GetFiles(directory, includePaths, excludePaths)
                            .Select(p => Path.Combine(directory, p))));

            return results.ToHashSet();
        }
        
        public static HashSet<string> GetFiles(string rootDir,
            IEnumerable<IPath> includePaths, IEnumerable<IPath> excludePaths)
        {
            Log.WriteLine("Getting files at '{0}' using provided include and exclude paths.", rootDir);

            HashSet<string> includeFiles;
            HashSet<string> includeGlobs;
            HashSet<RegexPath> includeRegexes;
            HashSet<string> excludeFiles;
            HashSet<string> excludeGlobs;
            HashSet<RegexPath> excludeRegexes;
                
            (includeFiles, includeGlobs, includeRegexes) = ProcessFileValues(includePaths);
            (excludeFiles, excludeGlobs, excludeRegexes) = ProcessFileValues(excludePaths);

            var dir = new DirectoryInfo(rootDir);
            
            string[] allFiles = dir.GetFiles("*", SearchOption.AllDirectories)
                .Select(f => f.FullName.Substring(dir.FullName.Length + 1))
                .ToArray();

            DirectoryInfo[] allDirs = dir.GetDirectories("*", SearchOption.AllDirectories)
                .Concat(new[] {dir})
                .ToArray();
            
            #region includes
            var includeGlob = new Glob(includeGlobs, null);
            
            // TODO: cache all files under RootPath instead of using DirectoryInfo
            string[] includeGlobMatches = includeGlob.FilterMatches(dir).ToArray();
            
            IEnumerable<string> tempMatches = includeGlobMatches;
            if (includeFiles != null)
            {
                var validIncludeFiles = new List<string>();
                foreach (string includeFile in includeFiles)
                {
                    string[] matchesForFile =
                        (from dirInfo in allDirs
                            select Path.Combine(dirInfo.FullName, includeFile)
                            into includeFilePath
                            where File.Exists(includeFilePath)
                            select Path.GetRelativePath(rootDir, includeFilePath)).ToArray();
                    
                    if (matchesForFile.Length > 0)
                    {
                        validIncludeFiles.Add(matchesForFile[0]);
                    }
                    
                    if (matchesForFile.Length > 1)
                    {
                        Log.WriteLineWarning(
                            "Multiple matches were found for literal file include '{0}' while recursively searching the root directory. " +
                            "Only the first match '{1}' will be used. See below for other matches.",
                            includeFile, matchesForFile[0]);
                        Log.WriteIndentedCollection(matchesForFile, p => p, true);
                    }
                }
                tempMatches = tempMatches.Concat(validIncludeFiles);
            }
            
            if (includeRegexes != null)
            {
                // TODO: test regex search paths
                tempMatches = tempMatches.Concat(allFiles.Where(f => includeRegexes.Any(r => r.Regex.IsMatch(f))));
            }
            #endregion
            
            #region excludes
            var excludeGlob = new Glob(excludeGlobs, null);
            string[] excludeGlobMatches = excludeGlob.FilterMatches(dir).ToArray();

            tempMatches = tempMatches.Except(excludeGlobMatches);

            if (excludeFiles != null)
            {
                var validExcludeFiles = new List<string>();
                foreach (string excludeFile in excludeFiles)
                {
                    string[] matchesForFile =
                        (from dirInfo in allDirs
                            select Path.Combine(dirInfo.FullName, excludeFile)
                            into excludeFilePath
                            where File.Exists(excludeFilePath)
                            select Path.GetRelativePath(rootDir, excludeFilePath)).ToArray();
                    
                    validExcludeFiles.AddRange(matchesForFile);
                }
                tempMatches = tempMatches.Except(validExcludeFiles);
            }

            if (excludeRegexes != null)
            {
                tempMatches = tempMatches.Except(allFiles.Where(f => excludeRegexes.Any(r => r.Regex.IsMatch(f))));
            }
            #endregion
            
            HashSet<string> finalMatches = tempMatches.ToHashSet();
           
            using (new Log.ScopedIndent())
            {
                Log.WriteLine("include globs:");
                Log.WriteIndentedCollection(includeGlobs, s => s);
                Log.WriteLine("include regexes:");
                Log.WriteIndentedCollection(includeRegexes, r => r.Value);
                Log.WriteLine("include literals:");
                Log.WriteIndentedCollection(includeFiles, s => s);
                Log.WriteLine("exclude globs:");
                Log.WriteIndentedCollection(excludeGlobs, s => s);
                Log.WriteLine("exclude regexes:");
                Log.WriteIndentedCollection(excludeRegexes, r => r.Value);
                Log.WriteLine("exclude literals:");
                Log.WriteIndentedCollection(excludeFiles, s => s);
                Log.WriteLine("matched files:");
                Log.WriteIndentedCollection(finalMatches, s => s);
            }

            return finalMatches.ToHashSet();
        }
        
        public static (HashSet<string> files, HashSet<string> globs, HashSet<RegexPath> regexes) ProcessFileValues(IEnumerable<object> filesValues)
        {
            var files = new HashSet<string>();
            var globs = new HashSet<string>();
            var regexes = new HashSet<RegexPath>();
            
            if (filesValues != null)
            {
                foreach (object includeFilesValue in filesValues)
                {
                    switch (includeFilesValue)
                    {
                        case GlobPath glob when includeFilesValue is GlobPath:
                            globs.Add(glob.Value);
                            break;
                        case LiteralPath file when includeFilesValue is LiteralPath:
                            files.Add(file.Value);
                            break;
                        case RegexPath regex when includeFilesValue is RegexPath:
                            regexes.Add(regex);

                            break;
                        default:
                            Log.WriteLineWarning(
                                "Unrecognized include files value type will be skipped: " +
                                includeFilesValue.GetType().FullName);
                            break;
                    }
                }
            }

            return (files, globs, regexes);
        }
    }
}