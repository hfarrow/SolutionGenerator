using System;
using System.Linq;
using McMaster.Extensions.CommandLineUtils;
using SolutionGen.Utils;

namespace SolutionGen.Console.Commands
{
    [Command("gen", Description = "Generate solution from target configuration document")]
    public class GenerateCommand : Command
    {
        [Argument(1, Description = "The master configuration to apply to the generated project")]
        public string MasterConfiguration { get; set; }

        protected override int OnExecute(CommandLineApplication app, IConsole console)
        {
            return new Func<ErrorCode>[]
               {
                   () => base.OnExecute(app, console),
                   GenerateSolution,
               }
               .Select(step => step())
               .Any(errorCode => errorCode != ErrorCode.Success) ? ErrorCode.CliError : ErrorCode.Success;
        }

        private ErrorCode GenerateSolution()
        {
            Log.Debug("Config = " + SolutionConfigFile.FullName);
            Log.Debug("Master Configuration = " + MasterConfiguration);

            try
            {
                SolutionGenerator gen = SolutionGenerator.FromPath(SolutionConfigFile.FullName);
                gen.GenerateSolution(MasterConfiguration);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return ErrorCode.GeneratorException;
            }

            return ErrorCode.Success;
        }
    }
}