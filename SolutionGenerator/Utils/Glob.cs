using System.Collections.Generic;
using System.IO;
using System.Linq;
using GLOB = Glob.Glob;

namespace SolutionGen.Utils
{
    public class Glob
    {
        public IReadOnlyCollection<string> IncludePatterns { get; }
        public IReadOnlyCollection<string> ExcludePatterns { get; }

        private readonly GLOB[] includeGlobs;
        private readonly GLOB[] excludeGlobs;
        
        public Glob(IReadOnlyCollection<string> includePatterns, IReadOnlyCollection<string> excludePatterns)
        {
            IncludePatterns = includePatterns;
            ExcludePatterns = excludePatterns;

            includeGlobs = includePatterns.Select(pattern => new GLOB(pattern)).ToArray();
            excludeGlobs = excludePatterns.Select(pattern => new GLOB(pattern)).ToArray();
        }

        public bool IsMatch(string path)
        {
            return !excludeGlobs.Any(g => g.IsMatch(path)) && includeGlobs.Any(g => g.IsMatch(path));
        }

        public IEnumerable<string> FilterMatches(IEnumerable<string> paths)
        {
            return paths.Where(IsMatch);
        }

        public IEnumerable<string> FilterMatches(DirectoryInfo dir) =>
            FilterMatches(dir.EnumerateFiles("*", SearchOption.AllDirectories)
                .Select(fi => fi.FullName.Substring(dir.FullName.Length + 1)));
    }
}