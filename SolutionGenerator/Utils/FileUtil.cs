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
            var includePatterns = new HashSet<string>();
            var excludePatterns = new HashSet<string>();
            var includeFiles = new HashSet<string>();
            var excludeFiles = new HashSet<string>();

            if (includePaths != null)
            {
                ProcessFileValues(includePaths, includeFiles, includePatterns);
            }

            if (excludePaths != null)
            {
                ProcessFileValues(excludePaths, excludeFiles, excludePatterns);
            }

            var glob = new Glob(includePatterns, excludePatterns);
            // TODO: cache all files under RootPath instead of using DirectoryInfo
            string[] matches =
                glob.FilterMatches(new DirectoryInfo(rootDir)).ToArray();
            
            HashSet<string> finalMatches = matches
                .Concat(includeFiles)
                .Except(excludeFiles)
                .ToHashSet();
           
            using (new Log.ScopedIndent())
            {
                Log.WriteLine("include patterns:");
                Log.WriteIndentedCollection(includePatterns, s => s);
                Log.WriteLine("exclude patterns:");
                Log.WriteIndentedCollection(excludePatterns, s => s);
                Log.WriteLine("include literals:");
                Log.WriteIndentedCollection(includeFiles, s => s);
                Log.WriteLine("exclude literals:");
                Log.WriteIndentedCollection(excludeFiles, s => s);
                Log.WriteLine("matched files:");
                Log.WriteIndentedCollection(finalMatches, s => s);
            }

            return finalMatches;
        }
        
        public static void ProcessFileValues(IEnumerable<object> filesValues, ISet<string> files, ISet<string> globs)
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
                    default:
                        Log.WriteLineWarning(
                            "Unrecognized include files value type will be skipped: " +
                            includeFilesValue.GetType().FullName);
                        break;
                }
            }
        }
    }
}