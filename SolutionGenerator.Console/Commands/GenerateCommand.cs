using System;
using System.Collections.Generic;
using System.Linq;
using McMaster.Extensions.CommandLineUtils;
using SolutionGen.Parser;
using SolutionGen.Parser.Model;
using SolutionGen.Utils;
using Sprache;

namespace SolutionGen.Console.Commands
{
    [Command("gen", Description = "Generate solution from target configuration document")]
    public class GenerateCommand : Command
    {
        [Argument(1, Description = "The master configuration to apply to the generated project")]
        public string MasterConfiguration { get; set; }
        
        [Option("-d|--define", CommandOptionType.MultipleValue,
            Description = "Scripting define symbols to add to all configurations.")]
        public string[] DefineSymbolsRaw { get; set; }
        
        [Option("-v|--var|--variable", CommandOptionType.MultipleValue,
            Description = "User defined variables that can be reference by the solution config.")]
        public string[] VariablesRaw { get; set; }
        
        [Option("-p|--property", CommandOptionType.MultipleValue,
            Description = "User defined solution config property overrides.")]
        public string[] PropertyOverridesRaw { get; set; }

        private HashSet<string> defineSymbols;
        protected Dictionary<string, string> Variables;
        protected List<PropertyElement> PropertyOverrides;
        protected bool SkipGenerateCommand = false;
        private SolutionGenerator solution;

        protected override int OnExecute(CommandLineApplication app, IConsole console)
        {
            return new Func<ErrorCode>[]
                {
                    () => base.OnExecute(app, console),
                    SetDefineSymbols,
                    ParseExpandableVariables,
                    SetExpandableVars,
                    ParsePropertyOverrides,
                    GenerateSolution,
                    ClearExpandableVars,
                    () => LogDuration(typeof(GenerateCommand).Name),
                }
                .Select(step => step())
                .Any(errorCode => errorCode != ErrorCode.Success)
                ? ErrorCode.CliError
                : ErrorCode.Success;
        }

        private ErrorCode SetDefineSymbols()
        {
            if (DefineSymbolsRaw != null)
            {
                defineSymbols = DefineSymbolsRaw
                    .SelectMany(s => s.Split(','))
                    .Select(s => s.Trim().Replace(' ', '_').ToUpper())
                    .ToHashSet();
            }
            else
            {
                defineSymbols = new HashSet<string>();
            }
            return ErrorCode.Success;
        }

        private ErrorCode ParseExpandableVariables()
        {
            Variables = new Dictionary<string, string>();
            if (VariablesRaw != null)
            {
                IEnumerable<Tuple<string, string>> pairs = VariablesRaw
                    .SelectMany(s => s.Split(','))
                    .Select(s => s.Trim().Split('='))
                    .Select(arr => Tuple.Create(
                        arr[0].Replace(' ', '_').ToUpper(),
                        arr.Length > 1 ? arr[1] : "true"));

                
                foreach (Tuple<string,string> pair in pairs)
                {
                    string key = pair.Item1;
                    string value = pair.Item2;

                    if (Variables.ContainsKey(key))
                    {
                        Log.Warn("Variable named '{0}' is provided more than once.", key);
                    }

                    Variables[key] = value;
                }
            }
            
            return ErrorCode.Success;
        }

        private ErrorCode ParsePropertyOverrides()
        {
            PropertyOverrides = new List<PropertyElement>();
            if (PropertyOverridesRaw != null)
            {
                Parser<PropertyElement> parser = DocumentParser.PropertyArray.Or(DocumentParser.PropertySingleLine);
                foreach (string propertyStr in PropertyOverridesRaw)
                {
                    IResult<PropertyElement> result = parser.TryParse(propertyStr);
                    if (!result.WasSuccessful)
                    {
                        Log.Error("Could not parse user property override: {0}", propertyStr);
                        Log.Error("{0}", result.ToString());
                        return ErrorCode.CliError;
                    }

                    PropertyOverrides.Add(result.Value);                    
                }
            }
            return ErrorCode.Success;
        }

        private ErrorCode GenerateSolution()
        {
            if (SkipGenerateCommand)
            {
                return ErrorCode.Success;
            }
            
            Log.Debug("Config = " + SolutionConfigFile.FullName);
            Log.Debug("Master Configuration = " + MasterConfiguration);
            Log.Debug("Define Symbols =");
            if (defineSymbols.Count > 0)
            {
                Log.IndentedCollection(defineSymbols, Log.Debug);
            }
            Log.Debug("Variables =");
            if (Variables.Count > 0)
            {
                Log.IndentedCollection(Variables, kvp => $"{kvp.Key} => {kvp.Value}", Log.Debug);
            }

            try
            {
                solution = GetGenerator();
                solution.GenerateSolution(MasterConfiguration, defineSymbols.ToArray(), PropertyOverrides.ToArray());
                
                // If MasterConfiguration was null or empty, the generator will select a default.
                MasterConfiguration = solution.MasterConfiguration;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return ErrorCode.GeneratorException;
            }

            
            return ErrorCode.Success;
        }

        protected ErrorCode SetExpandableVars()
        {
            foreach (KeyValuePair<string, string> kvp in Variables)
            {
                ExpandableVar.SetExpandableVariable(kvp.Key, kvp.Value);
            }
            return ErrorCode.Success;
        }

        protected ErrorCode ClearExpandableVars()
        {
            foreach (KeyValuePair<string, string> kvp in Variables)
            {
                ExpandableVar.ClearExpandableVariable(kvp.Key);
            }
            return ErrorCode.Success;
        }

        protected SolutionGenerator GetGenerator()
        {
            if (solution?.Solution != null)
            {
                return solution;
            }
            
            solution = SolutionGenerator.FromPath(SolutionConfigFile.FullName);
            return solution;
        }
    }
}