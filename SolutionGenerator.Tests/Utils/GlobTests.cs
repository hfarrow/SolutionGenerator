using System.IO;
using System.Linq;
using Xunit;
using GLOB = Glob.Glob;

namespace SolutionGen.Tests.Utils
{
    public class GlobTests
    {
        [Theory]
        [InlineData("/Solution/Module/Public/Thing/File1.cs")]
        [InlineData("/Solution/Module/Public/Thing/File2.txt")]
        [InlineData("/Solution/Module/Tests/Thing/File1.cs")]
        [InlineData("/Solution/Module/Tests/Thing/File2.txt")]
        public void GlobIncludeAllWithExtensions(string filePath)
        {
            var include = new GLOB("*.{cs,txt,json,xml,md}");
            Assert.True(include.IsMatch(filePath));
        }

        [Theory]
        [InlineData("/Solution/Module/Tests/Thing/File1.cs")]
        [InlineData("/Solution/Module/Tests/Thing/File2.txt")]
        public void GlobIncludeOnlySpecificDirectory(string filePath)
        {
            var include = new GLOB("**/Tests/**");
            Assert.True(include.IsMatch(filePath));
        }
        
        [Theory]
        [InlineData("/Solution/Module/Public/Thing/File1.cs")]
        [InlineData("/Solution/Module/Public/Thing/File2.txt")]
        public void GlobExcludeSpecificDirectory(string filePath)
        {
            var include = new GLOB("**/Tests/**");
            Assert.False(include.IsMatch(filePath));
        }

        [Fact]
        public void GlobUtilCanMatchSingle()
        {
            var glob = new SolutionGen.Utils.Glob(new[] {"*.cs"}, new[] {"*.txt"});
            Assert.True(glob.IsMatch("/Path/To/File.cs"));
            Assert.False(glob.IsMatch("/Path/To/File.txt"));
        }
        
        [Fact]
        public void GlobUtilCanMatchMultiple()
        {
            var glob = new SolutionGen.Utils.Glob(new[] {"*.cs"}, new[] {"*.txt", "**/ExcludedDir/**"});
            string[] matches = glob.FilterMatches(new[]
            {
                "/Path/To/File1.cs",
                "/Path/To/File2.cs",
                "/Path/To/File1.txt",
                "/Path/To/File2.txt",
                "/Path/To/ExcludedDir/File.cs",
            }).ToArray();
            
            Assert.Equal(new[]{"/Path/To/File1.cs", "/Path/To/File2.cs"}, matches);
        }

        [Fact]
        public void GlobDirectoryInfoReturnsRelativePaths()
        {
            var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
            string[] results = new SolutionGen.Utils.Glob(new[] {"*"}, new string[0]).FilterMatches(dir).ToArray();
            Assert.Contains(results, s => s[0] != '/' && s[0] != '\\');
            Assert.True(results.All(r => new FileInfo(Path.Combine(dir.FullName, r)).Exists));
        }
    }
}