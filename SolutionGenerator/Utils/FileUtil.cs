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

            public class Comparer : IEqualityComparer<LiteralMatch>
            {
                public bool Equals(LiteralMatch x, LiteralMatch y)
                {
                    return x.File == y.File;
                }

                public int GetHashCode(LiteralMatch obj)
                {
                    return obj.File.GetHashCode();
                }
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
            Log.Debug("Getting files using base path '{0}' and provided include/exclude paths:", basePath);
            using (new Log.ScopedIndent())
            {
                Log.Debug("search paths:");
                Log.IndentedCollection(searchableDirectories, Log.Debug);
                
                var matchComparer = new LiteralMatch.Comparer();

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
                    if (!dir.Exists)
                    {
                        Log.Debug("Skipping searchable directory because it does not exist: {0}'", rootDir);
                        continue;
                    }

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

                                if (allMatches.Distinct(matchComparer).Count() > 1)
                                {
                                    // TODO: Keep better track of the matches for a pattern in each search path
                                    // because this code can print the warning multiple times if the same file is found
                                    // when searching multiple paths. The warning should only be triggered once after
                                    // all includes an excludes are processed. Currently, exludes are not considered
                                    // which could produce false positive warnings.
                                    Log.Warn(
                                        "Multiple matches were found for literal file include '{0}' while searching path '{1}'. " +
                                        "Only the first match '{2}' will be used. " +
                                        "See below the conflicting matches.",
                                        includeFile, rootDir, allMatches[0].File);
                                    Log.IndentedCollection(allMatches,
                                        p => $"{p.File} (from {p.RootDirectory})",
                                        Log.Warn);
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
                            Log.Warn(
                                "No file matches were found for literal pattern '{0}' in any searched directories. " +
                                "Please consider fixing the literal pattern or removing it entirely.",
                                includeFile);
                        }
                    }
                }

                HashSet<string> allMatchedFiles = finalMatches.ToHashSet();
                Log.Debug("include globs:");
                Log.IndentedCollection(includeGlobs, s => s.Pattern, Log.Debug);
                Log.Debug("include regexes:");
                Log.IndentedCollection(includeRegexes, r => r.Value, Log.Debug);
                Log.Debug("include literals:");
                Log.IndentedCollection(includeFiles, Log.Debug);
                Log.Debug("exclude globs:");
                Log.IndentedCollection(excludeGlobs, s => s.Pattern, Log.Debug);
                Log.Debug("exclude regexes:");
                Log.IndentedCollection(excludeRegexes, r => r.Value, Log.Debug);
                Log.Debug("exclude literals:");
                Log.IndentedCollection(excludeFiles, Log.Debug);
                Log.Debug("matched files:");
                Log.IndentedCollection(allMatchedFiles, Log.Debug);

                return allMatchedFiles;
            }
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
                            Log.Warn(
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