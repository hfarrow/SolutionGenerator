using System.Collections.Generic;
using System.IO;
using System.Linq;
using Path = System.IO.Path;
using GLOB = Glob.Glob;

namespace SolutionGen.Utils
{
    public static class FileUtil
    {
        private struct LiteralMatch
        {
            public readonly string File;
            public readonly string RootDirectory;

            public LiteralMatch(string file, string rootDirectory)
            {
                File = file;
                RootDirectory = rootDirectory;
            }
        }
        
        public static HashSet<string> GetFiles(string searchableDirectory,
            IEnumerable<IPattern> includePaths, IEnumerable<IPattern> excludePaths, string basePath = null)
        {
            basePath = basePath ?? searchableDirectory;
            return GetFiles(new[] {searchableDirectory}, includePaths, excludePaths, basePath);
        }
        
        public static HashSet<string> GetFiles(IEnumerable<string> searchableDirectories,
            IEnumerable<IPattern> includePaths, IEnumerable<IPattern> excludePaths, string basePath = null)
        {
            basePath = basePath ?? Directory.GetCurrentDirectory();
            searchableDirectories = searchableDirectories as string[] ?? searchableDirectories.ToArray();
            Log.WriteLine("Getting files using base path '{0}' and provided include and exclude paths:", basePath);
            Log.WriteIndentedCollection(searchableDirectories, d => d);

            HashSet<string> includeFiles;
            HashSet<GLOB> includeGlobs;
            HashSet<RegexPattern> includeRegexes;
            HashSet<string> excludeFiles;
            HashSet<GLOB> excludeGlobs;
            HashSet<RegexPattern> excludeRegexes;
                
            (includeFiles, includeGlobs, includeRegexes) = ProcessFileValues(includePaths);
            (excludeFiles, excludeGlobs, excludeRegexes) = ProcessFileValues(excludePaths);

            IEnumerable<string> finalMatches = new List<string>();
            var literalMatches = new Dictionary<string, List<LiteralMatch>>();
            
            foreach (string rootDir in searchableDirectories)
            {
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
                string[] includeGlobMatches = includeGlob.FilterMatches(dir)
                    .Select(m => Path.GetRelativePath(basePath, Path.Combine(dir.FullName, m)))
                    .ToArray();

                IEnumerable<string> tempMatches = includeGlobMatches;
                if (includeFiles != null)
                {
                    var validIncludeFiles = new List<string>();
                    foreach (string includeFile in includeFiles.ToArray())
                    {
                        string file = includeFile;
                        string[] matchesForFile =
                            (from dirInfo in allDirs
                                select Path.Combine(dirInfo.FullName, file)
                                into includeFilePath
                                where File.Exists(includeFilePath)
                                select Path.GetRelativePath(basePath, includeFilePath)).ToArray();

                        if (matchesForFile.Length > 0)
                        {
                            if (!literalMatches.TryGetValue(includeFile, out List<LiteralMatch> allMatches))
                            {
                                allMatches = new List<LiteralMatch>();
                                literalMatches[includeFile] = allMatches;
                                validIncludeFiles.Add(matchesForFile[0]);
                            }
                            allMatches.AddRange(matchesForFile.Select(m => new LiteralMatch(m, rootDir)));
                            
                            if (allMatches.Count > 1)
                            {
                                Log.WriteLineWarning(
                                    "Multiple matches were found for literal file include '{0}' while searching path '{1}'. " +
                                    "Only the first match '{2}' will be used. " +
                                    "See below the conflicting matches.",
                                    includeFile, rootDir, allMatches[0].File);
                                Log.WriteIndentedCollection(allMatches, p => $"{p.File} (from {p.RootDirectory})");
                            }
                            
                        }
                    }

                    tempMatches = tempMatches.Concat(validIncludeFiles);
                }

                if (includeRegexes != null)
                {
                    tempMatches = tempMatches.Concat(
                        includeRegexes.SelectMany(r => r.FilterMatches(allFiles))
                            .Select(m => Path.GetRelativePath(basePath, m)));
                }

                #endregion

                #region excludes

                var excludeGlob = new CompositeGlob(excludeGlobs, null);
                string[] excludeGlobMatches = excludeGlob.FilterMatches(dir)
                    .Select(m => Path.GetRelativePath(basePath, Path.Combine(dir.FullName, m)))
                    .ToArray();

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
                                select Path.GetRelativePath(basePath, excludeFilePath)).ToArray();

                        validExcludeFiles.AddRange(matchesForFile);
                    }

                    tempMatches = tempMatches.Except(validExcludeFiles);
                }

                if (excludeRegexes != null)
                {
                    tempMatches = tempMatches.Except(
                        excludeRegexes.SelectMany(r => r.FilterMatches(allFiles))
                            .Select(m => Path.GetRelativePath(basePath, m)));
                }

                #endregion
                
                finalMatches = finalMatches.Concat(tempMatches);
            }

            if (includeFiles != null)
            {
                foreach (string includeFile in includeFiles)
                {
                    if (!literalMatches.ContainsKey(includeFile))
                    {
                        Log.WriteLineWarning(
                            "No file matches were found for literal pattern '{0}' in any searched directories. " +
                            "Please consider fixing the literal pattern or removing it entirely.",
                            includeFile);
                    }
                }
            }

            HashSet<string> allMatchedFiles = finalMatches.ToHashSet();           
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
                Log.WriteIndentedCollection(allMatchedFiles, s => s);
            }

            return allMatchedFiles;
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