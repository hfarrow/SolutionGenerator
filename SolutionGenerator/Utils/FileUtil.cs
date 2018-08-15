﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using Path = System.IO.Path;
using GLOB = Glob.Glob;

namespace SolutionGen.Utils
{
    public static class FileUtil
    {
        public static HashSet<string> GetFilesInSearchPaths(IEnumerable<string> searchableDirectories,
            IEnumerable<IPattern> includePaths, IEnumerable<IPattern> excludePaths)
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
            IEnumerable<IPattern> includePaths, IEnumerable<IPattern> excludePaths)
        {
            Log.WriteLine("Getting files at '{0}' using provided include and exclude paths.", rootDir);

            HashSet<string> includeFiles;
            HashSet<GLOB> includeGlobs;
            HashSet<RegexPattern> includeRegexes;
            HashSet<string> excludeFiles;
            HashSet<GLOB> excludeGlobs;
            HashSet<RegexPattern> excludeRegexes;
                
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
            var includeGlob = new CompositeGlob(includeGlobs, null);
            
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
                tempMatches = tempMatches.Concat(includeRegexes.SelectMany(r => r.FilterMatches(allFiles)));
            }
            #endregion
            
            #region excludes
            var excludeGlob = new CompositeGlob(excludeGlobs, null);
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
                tempMatches = tempMatches.Except(excludeRegexes.SelectMany(r => r.FilterMatches(allFiles)));
            }
            #endregion
            
            HashSet<string> finalMatches = tempMatches.ToHashSet();
           
            using (new Log.ScopedIndent())
            {
                Log.WriteLine("include globs:");
                Log.WriteIndentedCollection(includeGlobs, s => s.Pattern);
                Log.WriteLine("include regexes:");
                Log.WriteIndentedCollection(includeRegexes, r => r.Value);
                Log.WriteLine("include literals:");
                Log.WriteIndentedCollection(includeFiles, s => s);
                Log.WriteLine("exclude globs:");
                Log.WriteIndentedCollection(excludeGlobs, s => s.Pattern);
                Log.WriteLine("exclude regexes:");
                Log.WriteIndentedCollection(excludeRegexes, r => r.Value);
                Log.WriteLine("exclude literals:");
                Log.WriteIndentedCollection(excludeFiles, s => s);
                Log.WriteLine("matched files:");
                Log.WriteIndentedCollection(finalMatches, s => s);
            }

            return finalMatches.ToHashSet();
        }
        
        public static (HashSet<string> files, HashSet<GLOB> globs, HashSet<RegexPattern> regexes)
            ProcessFileValues(IEnumerable<IPattern> filesValues)
        {
            var files = new HashSet<string>();
            var globs = new HashSet<GLOB>();
            var regexes = new HashSet<RegexPattern>();
            
            if (filesValues != null)
            {
                foreach (IPattern includeFilesValue in filesValues)
                {
                    switch (includeFilesValue)
                    {
                        case GlobPattern glob when includeFilesValue is GlobPattern:
                            globs.Add(glob.Glob);
                            break;
                        case LiteralPattern file when includeFilesValue is LiteralPattern:
                            files.Add(file.Value);
                            break;
                        case RegexPattern regex when includeFilesValue is RegexPattern:
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