using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        
        public static HashSet<string> GetFiles(string searchableDirectory,
            IEnumerable<IPattern> includePaths, IEnumerable<IPattern> excludePaths, string basePath = null)
        {
            basePath = basePath ?? searchableDirectory;
            return GetFiles(new[] {searchableDirectory}, includePaths, excludePaths, basePath);
        }

        public static HashSet<string> GetFiles(IEnumerable<string> searchablePaths,
            IEnumerable<IPattern> includePaths, IEnumerable<IPattern> excludePaths, string basePath = null)
        {
            basePath = basePath ?? Directory.GetCurrentDirectory();
            if (basePath == "./" || basePath == ".")
            {
                // new DirectoryInfo is inconsistent... dir.FullName will include a trailing slash when
                // Directory.GetCurrentDirectory is passed in but will not contain a trailing slash if
                // "./" is passed in. If "./" is used, convert it to Directory.GetCurrentDirectory() first.
                basePath = Directory.GetCurrentDirectory();
            }
            var basePathInfo = new DirectoryInfo(basePath);
            
            searchablePaths = searchablePaths as string[] ?? searchablePaths.ToArray();
            Log.Debug("Getting files using base path '{0}' and provided include/exclude paths:", basePath);
            using (new Log.ScopedIndent())
            {
                Log.Debug("search paths:");
                Log.IndentedCollection(searchablePaths, Log.Debug);

                var matchComparer = new PatternMatch.Comparer();

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

                    // new DirectoryInfo is inconsistent... dir.FullName will include a trailing slash when
                    // Directory.GetCurrentDirectory is passed in but will not contain a trailing slash if
                    // "./" is passed in. If "./" is used, convert it to Directory.GetCurrentDirectory() first.
                    var dir = new DirectoryInfo(currentSearchPath == "." || currentSearchPath == "./"
                        ? Directory.GetCurrentDirectory()
                        : currentSearchPath);
                    if (!dir.Exists)
                    {
                        Log.Debug("Skipping searchable directory because it does not exist: {0}'", currentSearchPath);
                        continue;
                    }

                    string[] allFiles = dir.GetFiles("*", SearchOption.AllDirectories)
                        .Select(f => f.FullName.Substring(basePathInfo.FullName.Length + 1))
                        .ToArray();

                    DirectoryInfo[] allDirs = dir.GetDirectories("*", SearchOption.AllDirectories)
                        .Concat(new[] {dir})
                        .ToArray();

                    #region includes

                    if (includeGlobs != null && includeGlobs.Count > 0)
                    {
                        foreach (GLOB glob in includeGlobs)
                        {
                            IEnumerable<string> matchesForGlob = allFiles.Where(f => glob.IsMatch(f))
                                .Select(f => Path.GetRelativePath(basePath, f));
                            
                            currentMatches.AddRange(matchesForGlob.Select(
                                m => new PatternMatch(m, currentSearchPath, "glob \"" + glob.Pattern + "\"")));
                        }
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
                                    select Path.GetRelativePath(basePath, includeFilePath)).ToArray();

                            PatternMatch[] patternMatchesForFile = matchesForFile.Select(
                                m => new PatternMatch(m, currentSearchPath, filePattern))
                                .ToArray();

                            if (patternMatchesForFile.Length > 0)
                            {
                                literalPatternMatches[filePattern] = patternMatchesForFile;
                                currentMatches.AddRange(patternMatchesForFile);
                            }
                        }
                    }

                    if (includeRegexes != null && includeRegexes.Count > 0)
                    {
                        IEnumerable<PatternMatch> matchesForRegex = includeRegexes.SelectMany(
                            r => r.FilterMatches(allFiles).Select(
                                m => new PatternMatch(m,
                                    currentSearchPath,
                                    "regex \"" + r.Value + "\"")));

                        currentMatches.AddRange(matchesForRegex);
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

                    if (excludeFiles != null && excludeFiles.Count > 0)
                    {
                        tempMatches = tempMatches.Where(m => excludeFiles.All(x => x != Path.GetFileName(m.File)))
                            .ToList(); // Helps with debugging
                    }

                    if (excludeRegexes != null && excludeRegexes.Count > 0)
                    {
                        tempMatches = tempMatches.Where(m => excludeRegexes.All(r => !r.Regex.IsMatch(m.File)))
                            .ToList(); // Helps with debugging
                    }

                    #endregion

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
                }

                IEnumerable<PatternMatch> allMatches = searchPathMatches.Values.SelectMany(v => v);
                IEnumerable<PatternMatch> finalMatches = ValidateMatches(allMatches);
                HashSet<string> allMatchedFiles = finalMatches.Select(m => m.File).ToHashSet();
                
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

        private static IEnumerable<PatternMatch> ValidateMatches(IEnumerable<PatternMatch> allMatches)
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
            
            return patterMatches.Except(invalidMatches).ToHashSet();
        }
    }
}



















