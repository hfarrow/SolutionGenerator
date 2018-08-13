using System.Collections.Generic;
using System.IO;
using System.Linq;
using SolutionGen.Generator.Model;

namespace SolutionGen.Utils
{
    public static class FileUtil
    {
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
            
            #region includes
            var includeGlob = new Glob(includeGlobs, null);
            
            // TODO: cache all files under RootPath instead of using DirectoryInfo
            string[] includeGlobMatches = includeGlob.FilterMatches(dir).ToArray();
            
            IEnumerable<string> tempMatches = includeGlobMatches;
            if (includeFiles != null)
            {
                tempMatches = tempMatches.Concat(includeFiles);
            }
            
            string[] allFiles = dir.GetFiles("*", SearchOption.AllDirectories)
                .Select(f => f.FullName.Substring(dir.FullName.Length + 1))
                .ToArray();
            
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
                tempMatches = tempMatches.Except(excludeFiles);
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
                Log.WriteLine("exclude globs:");
                Log.WriteIndentedCollection(excludeGlobs, s => s);
                Log.WriteLine("include regexes:");
                Log.WriteIndentedCollection(includeRegexes, r => r.Value);
                Log.WriteLine("include literals:");
                Log.WriteIndentedCollection(includeFiles, s => s);
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