using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace SolutionGenerator.Tests.ExampleBuildTask
{
    public class ExampleTask : Task
    {
        public override bool Execute()
        {
            Log.LogMessage(MessageImportance.High, "Executing Example Build Task");
            return true;
        }
    }
}