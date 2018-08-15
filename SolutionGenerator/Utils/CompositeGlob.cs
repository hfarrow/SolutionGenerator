using System.Collections.Generic;
using System.IO;
using System.Linq;
using GLOB = Glob.Glob;

namespace SolutionGen.Utils
{
    public class CompositeGlob
    {
        public IEnumerable<string> IncludePatterns { get; }
        public IEnumerable<string> ExcludePatterns { get; }

        private readonly GLOB[] includeGlobs;
        private readonly GLOB[] excludeGlobs;
        
        public CompositeGlob(IEnumerable<string> includePatterns, IEnumerable<string> excludePatterns)
        {
            if (excludePatterns == null)
            {
                excludePatterns = new List<string>();
            }
            
            IncludePatterns = includePatterns.ToArray();
            ExcludePatterns = excludePatterns.ToArray();

            includeGlobs = IncludePatterns.Select(pattern => new GLOB(pattern)).ToArray();
            excludeGlobs = ExcludePatterns.Select(pattern => new GLOB(pattern)).ToArray();
        }

        public CompositeGlob(IEnumerable<GLOB> includeGlobs, IEnumerable<GLOB> excludeGlobs)
        {
            if (excludeGlobs == null)
            {
                excludeGlobs = new List<GLOB>();
            }

            this.includeGlobs = includeGlobs as GLOB[] ?? includeGlobs.ToArray();
            this.excludeGlobs = excludeGlobs as GLOB[] ?? excludeGlobs.ToArray();
            
            IncludePatterns = this.includeGlobs.Select(g => g.Pattern);
            ExcludePatterns = this.excludeGlobs.Select(g => g.Pattern);
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