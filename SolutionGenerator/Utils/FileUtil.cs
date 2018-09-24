using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Path = System.IO.Path;
using GLOB = Glob.Glob;

namespace SolutionGen.Utils
{
    public static class FileUtil
    {
        private struct PatternMatch
        {
            public readonly string File;
            public readonly string SearchPath;
            public readonly string Pattern;

            public PatternMatch(string file, string searchPath, string pattern)
            {
                File = file;
                SearchPath = searchPath;
                Pattern = pattern;
            }

            public class Comparer : IEqualityComparer<PatternMatch>
            {
                public bool Equals(PatternMatch x, PatternMatch y)
                {
                    return x.File == y.File;
                }

                public int GetHashCode(PatternMatch obj)
                {
                    return obj.File.GetHashCode();
                }
            }
        }

        public class ResultCache
        {
            private class Result
            {
                public HashSet<string> Files;
                public EventWaitHandle Handle;
            }
            
            private readonly Dictionary<int, Result> cache =
                new Dictionary<int, Result>();

            public void CacheResults(HashSet<string> files, int hash)
            {
                EventWaitHandle handle;
                lock (cache)
                {
                    Result entry = cache[hash];
                    handle = entry.Handle;
                    entry.Files = files;
                    entry.Handle = null;
                }

                handle.Set();
            }

            public (int hash, bool hasResults, HashSet<string> files) GetResultsOrReserve(
                IEnumerable<string> searchablePaths,
                IEnumerable<IPattern> includePaths,
                IEnumerable<IPattern> excludePaths,
                string basePath)
            {
                int hash = GetQueryHashCode(searchablePaths, includePaths, excludePaths, basePath);

                EventWaitHandle pendingHandle = null;
                bool isHandlerOwner = false;
                lock (cache)
                {
                    // If another task is already working on the same query wait for it to finish so that work is not
                    // duplicated the current task.
                    if (cache.TryGetValue(hash, out Result entry))
                    {
                        if (entry.Handle != null && entry.Files == null)
                        {
                            pendingHandle = entry.Handle;
                        }
                    }
                    else
                    {
                        cache[hash] = new Result {Files = null, Handle = new ManualResetEvent(false)};
                        isHandlerOwner = true;
                    }
                }

                if (pendingHandle != null)
                {
                    using (new Log.ScopedTimer(Log.Level.Debug, "GetFiles(...) waiting for query", $"{{{hash}}}"))
                    {
                        Log.Debug("Waiting for another task to complete identical query with hash {{{0}}}", hash);
                        pendingHandle.WaitOne();

                    }
                }

                lock (cache)
                {
                    return isHandlerOwner 
                        // The caller is expected to get the files and call CacheResults.
                        ? (hash, false, null)
                        // Results are cached, return a copy so that caller can modify the collection as needed.
                        : (hash, true, new HashSet<string>(cache[hash].Files));
                }
            }

            private static int GetQueryHashCode(
                IEnumerable<string> searchablePaths,
                IEnumerable<IPattern> includePaths,
                IEnumerable<IPattern> excludePaths,
                string basePath)
            {
                int hash = 13;

                hash = searchablePaths.Aggregate(hash, (a, b) => (a * 7) + b.GetHashCode());
                hash = includePaths.Aggregate(hash, (a, b) => (a * 7) + b.ToString().GetHashCode());
                hash = excludePaths.Aggregate(hash, (a, b) => (a * 7) + b.ToString().GetHashCode());
                hash = (hash * 7) + basePath.GetHashCode();

                return hash;
            }
        }
        
        public static HashSet<string> GetFiles(
            ResultCache cache,
            string searchableDirectory,
            IEnumerable<IPattern> includePaths,
            IEnumerable<IPattern> excludePaths,
            string basePath = null)
        {
            return GetFiles(cache, new[] {searchableDirectory}, includePaths, excludePaths, basePath);
        }

        public static HashSet<string> GetFiles(
            ResultCache cache,
            IEnumerable<string> searchablePaths,
            IEnumerable<IPattern> includePaths,
            IEnumerable<IPattern> excludePaths,
            string basePath = null)
        {
            searchablePaths = searchablePaths as string[] ?? searchablePaths.ToArray();
            includePaths = includePaths as IPattern[] ?? includePaths.ToArray();

            if (excludePaths != null)
            {
                excludePaths = excludePaths as IPattern[] ?? excludePaths.ToArray();
            }
            else
            {
                excludePaths = new List<IPattern>();
            }
            
            basePath = basePath ?? "./";
            bool returnAbsolutePaths = Path.IsPathRooted(basePath);
            string absBasePath;
            if (basePath == "./" || basePath == ".")
            {
                absBasePath = new DirectoryInfo(Directory.GetCurrentDirectory()).FullName;
            }
            else
            {
                absBasePath = new DirectoryInfo(basePath).FullName;
            }

            (int hash, bool hasResults, HashSet<string> files) cacheRequest =
                cache?.GetResultsOrReserve(
                    searchablePaths,
                    includePaths,
                    excludePaths,
                    basePath)
                ?? (0, false, null);

            if (cacheRequest.hasResults)
            {
                Log.Debug("Getting files for queury hash {{{0}}} from cached results for base path '{1}':",
                    cacheRequest.hash, basePath);
                
                Log.IndentedCollection(cacheRequest.files, Log.Debug);
                return cacheRequest.files;
            }
            
            Log.Debug("Getting files for query hash {{{0}}} using base path '{1}' and provided include/exclude paths:",
                cacheRequest.hash, basePath);
            
            using (new CompositeDisposable(
                new Log.ScopedIndent(),
                new Log.ScopedTimer(Log.Level.Debug, "GetFiles(...)", $"{{{cacheRequest.hash,-11}}} {basePath}")))
            {
                Log.Debug("search paths:");
                Log.IndentedCollection(searchablePaths, Log.Debug);

                HashSet<string> includeFiles;
                HashSet<GLOB> includeGlobs;
                HashSet<RegexPattern> includeRegexes;
                HashSet<string> excludeFiles;
                HashSet<GLOB> excludeGlobs;
                HashSet<RegexPattern> excludeRegexes;

                (includeFiles, includeGlobs, includeRegexes) = ProcessFileValues(includePaths);
                (excludeFiles, excludeGlobs, excludeRegexes) = ProcessFileValues(excludePaths);
                var searchPathMatches = new Dictionary<string, List<PatternMatch>>();
                
                foreach (string currentSearchPath in searchablePaths)
                {
                    var currentMatches = new List<PatternMatch>();

                    var dir = new DirectoryInfo(new DirectoryInfo(
                        currentSearchPath == "./" || currentSearchPath == "."
                            ? Directory.GetCurrentDirectory()
                            : currentSearchPath)
                        .FullName);

                    bool makeCandidatesAbsolute = Path.IsPathRooted(currentSearchPath);
                    
                    if (!dir.Exists)
                    {
                        Log.Debug("Skipping searchable directory because it does not exist: {0}'", currentSearchPath);
                        continue;
                    }

                    string[] candidates = dir.GetFilesSafeRecursive("*")
                        .Select(f => GetPath(makeCandidatesAbsolute, f.FullName, absBasePath))
                        .Reverse()
                        .ToArray();

                    DirectoryInfo[] allDirs = dir.GetDirectoriesSafeRecursive("*")
                        .Reverse()
                        .Concat(new[] {dir})
                        .ToArray();

                    #region includes
                    if (includeGlobs != null && includeGlobs.Count > 0)
                    {
                        foreach (GLOB glob in includeGlobs)
                        {
                            IEnumerable<string> matchesForGlob = candidates.Where(f => glob.IsMatch(f));
                            
                            currentMatches.AddRange(matchesForGlob.Select(
                                f => new PatternMatch(f, currentSearchPath, "glob \"" + glob.Pattern + "\"")));
                        }
                    }

                    if (includeRegexes != null && includeRegexes.Count > 0)
                    {
                        IEnumerable<PatternMatch> matchesForRegex = includeRegexes.SelectMany(
                            r => r.FilterMatches(candidates).Select(
                                f => new PatternMatch(f,
                                    currentSearchPath,
                                    r.ToString())));

                        currentMatches.AddRange(matchesForRegex);
                    }
                    
                    var literalPatternMatches = new Dictionary<string, PatternMatch[]>();
                    if (includeFiles != null && includeFiles.Count > 0)
                    {
                        foreach (string filePattern in includeFiles.ToArray())
                        {
                            string[] matchesForFile =
                                (from dirInfo in allDirs
                                    select Path.Combine(dirInfo.FullName, filePattern)
                                    into includeFilePath
                                    where File.Exists(includeFilePath)
                                    select GetPath(makeCandidatesAbsolute, includeFilePath, absBasePath))
                                .ToArray();

                            PatternMatch[] patternMatchesForFile = matchesForFile.Select(
                                    f => new PatternMatch(f, currentSearchPath, filePattern))
                                .ToArray();

                            if (patternMatchesForFile.Length > 0)
                            {
                                literalPatternMatches[filePattern] = patternMatchesForFile;
                                currentMatches.AddRange(patternMatchesForFile);
                            }
                        }
                    }
                    #endregion

                    #region excludes
                    IEnumerable<PatternMatch> tempMatches = currentMatches;
                    if (excludeGlobs != null && excludeGlobs.Count > 0)
                    {
                        var excludeGlob = new CompositeGlob(excludeGlobs, null);
                        tempMatches = tempMatches.Where(m => !excludeGlob.IsMatch(m.File))
                            .ToList(); // Helps with debugging
                    }
                    
                    if (excludeRegexes != null && excludeRegexes.Count > 0)
                    {
                        tempMatches = tempMatches.Where(m => excludeRegexes.All(r => !r.Regex.IsMatch(m.File)))
                            .ToList(); // Helps with debugging
                    }

                    if (excludeFiles != null && excludeFiles.Count > 0)
                    {
                        tempMatches = tempMatches.Where(m => excludeFiles.All(x => x != Path.GetFileName(m.File)))
                            .ToList(); // Helps with debugging
                    }
                    #endregion

                    #region literal validation
                    currentMatches = tempMatches.ToList();
                    foreach (KeyValuePair<string, PatternMatch[]> kvp in literalPatternMatches)
                    {
                        PatternMatch[] patternMatchesForFile = kvp.Value;
                        if (patternMatchesForFile.Length > 1
                            && currentMatches.Intersect(patternMatchesForFile).Count() > 1)
                        {
                            Log.Warn("The literal pattern '{0}' matched more than one file.",
                                kvp.Key);
                            Log.Warn("Only the first match '{0}' will be returned. See all matches below.",
                                patternMatchesForFile.First().File);

                            int count = 0;
                            Log.IndentedCollection(patternMatchesForFile,
                                p => $"{p.File} (from {p.SearchPath}) {(count++ == 0 ? "*" : "")}",
                                Log.Warn);

                            currentMatches = currentMatches.Except(patternMatchesForFile.Skip(1)).ToList();
                        }
                    }
                    
                    searchPathMatches[currentSearchPath] = currentMatches;
                    #endregion
                }

                #region validation
                IEnumerable<PatternMatch> allMatches = searchPathMatches.Values.SelectMany(v => v);
                IEnumerable<PatternMatch> finalMatches = ValidateMatches(allMatches, includePaths);
                #endregion
                
                #region results
                HashSet<string> allMatchedFiles =
                    finalMatches.Select(m => GetPath(returnAbsolutePaths, m.File, absBasePath)).ToHashSet();
                
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

                cache?.CacheResults(allMatchedFiles, cacheRequest.hash);
                return allMatchedFiles;
                #endregion
            }
        }

        private static string GetPath(bool makeAbsolute, string filePath, string absBasePath)
        {
            string absFilePath = Path.IsPathRooted(filePath)
                ? filePath
                : new FileInfo(Path.Combine(absBasePath, filePath)).FullName;
            
            return makeAbsolute
                ? absFilePath
                : Path.GetRelativePath(absBasePath, absFilePath);
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

        private static IEnumerable<PatternMatch> ValidateMatches(IEnumerable<PatternMatch> allMatches,
            IEnumerable<IPattern> allPatterns)
        {
            IEnumerable<PatternMatch> patterMatches = allMatches as PatternMatch[] ?? allMatches.ToArray();
            var invalidMatches = new List<PatternMatch>();
            
            // Group all matches by pattern
            Dictionary<string, PatternMatch[]> patternGroups = patterMatches
                .GroupBy(m => m.Pattern)
                .ToDictionary(g => g.Key, g => g.ToArray());

            // Find patterns that matched files across more than one search path.
            // Only return files from the first search path
            foreach (KeyValuePair<string, PatternMatch[]> patternGroup in patternGroups)
            {
                Dictionary<string, PatternMatch[]> searchPathGroups = patternGroup.Value
                    .GroupBy(m => m.SearchPath)
                    .Where(g => g.Any())
                    .ToDictionary(g => g.Key, g => g.ToArray());

                if (searchPathGroups.Count > 1)
                {
                    IEnumerable<PatternMatch> candidates = searchPathGroups.SelectMany(kvp => kvp.Value).ToArray();

                    if (candidates.Distinct(new PatternMatch.Comparer()).Count() > 1)
                    {
                        KeyValuePair<string, PatternMatch[]> first = searchPathGroups.First();
                        Log.Warn("The pattern '{0}' matched file(s) in multiple provided search paths. ",
                            patternGroup.Key);
                        Log.Warn("Only the matches from '{0}' will be returned. See all matches below.",
                            first.Key);

                        Log.IndentedCollection(candidates,
                            p => $"{p.File} (from {p.SearchPath}) {(p.SearchPath == first.Key ? "*" : "")}",
                            Log.Warn);

                        invalidMatches.AddRange(searchPathGroups.Skip(1).SelectMany(g => g.Value));
                    }
                }
            }
            
            string[] patternsWithNoMatches = allPatterns
                .Select(p => p.ToString())
                .Except(patternGroups.Keys)
                .ToArray();
                
            foreach (string pattern in patternsWithNoMatches)
            {
                Log.Warn("Pattern '{0}' produced zero matches across all provided search paths.",
                    pattern);
            }
            
            return patterMatches.Except(invalidMatches).ToHashSet();
        }
    }
}



















