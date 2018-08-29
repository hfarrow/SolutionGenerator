using System;
using System.Collections.Generic;
using System.IO;
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
        [Argument(1, Description = "The master configuration to apply to the generated project and build configuration delmited by ':'.")]
        public string ConfigurationRaw { private get; set; }
        
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
        protected Dictionary<string, string> Variables { get; private set; }
        protected List<PropertyElement> PropertyOverrides { get; private set; }
        protected bool SkipGenerateCommand { get; set; }
        protected string MasterConfiguration { get; private set; }
        protected string BuildConfiguration { get; private set; }

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
                    ParseConfiguration,
                    GenerateSolution,
                    ClearExpandableVars,
                    () => LogDuration(typeof(GenerateCommand).Name),
                }
                .Select(step => step())
                .Any(errorCode => errorCode != ErrorCode.Success)
                ? ErrorCode.CliError
                : ErrorCode.Success;
        }
        
        protected ErrorCode CheckGenerateSolution(bool forceGenerate)
        {
            ErrorCode foundConfig = FindSolutionConfigFile();
            if(!forceGenerate &&
               foundConfig == ErrorCode.Success &&
               File.Exists(GetGenerator().Solution.Name + ".sln"))
            {
                SkipGenerateCommand = true;
            }

            return foundConfig;
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
            // TODO: change solution overrides to -p 'MySolution/property = test' to match module and template overrides below
            // TODO: allow override of nested dictionary properties: -p 'MySolution/configurations/everything/Debug = [debug,test]'
            //     Check for valid property at each path separator. Once you find a dictionary property the path component becomes the key indexer
            // TODO: allow override of nested object properties: -p 'MySolution/template defaults/root namespace = my.namespace'
            // TODO: allow override of module or template object properties/settings objects: -p 'MyTemplate/project/root namespace = my.namespace'
            // Store all overrides in one data structure that is injected into SolutionGenerator. When reading an object check for overrides to
            // apply based on property path. SettingsReader.ApplyPropertyOverrides already exists.
            
            PropertyOverrides = new List<PropertyElement>();
            if (PropertyOverridesRaw != null)
            {
                Parser<PropertyElement> parser = DocumentParser
                    .PropertyDictionary
                    .Or(DocumentParser.PropertyArray)
                    .Or(DocumentParser.PropertySingleLine);
                
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

        private ErrorCode ParseConfiguration()
        {
            if (!string.IsNullOrEmpty(ConfigurationRaw))
            {
                string[] parts = ConfigurationRaw.Split(':');
                MasterConfiguration = parts[0];
                if (parts.Length > 1)
                {
                    BuildConfiguration = parts[1];
                }
            }
            
            Log.Debug("Master Configuration = " + MasterConfiguration);
            Log.Debug("Build Configuration = " + BuildConfiguration);
            return ErrorCode.Success;
        }

        private ErrorCode GenerateSolution()
        {
            if (SkipGenerateCommand)
            {
                return ErrorCode.Success;
            }
            
            Log.Debug("Config = " + SolutionConfigFile.FullName);
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
                Log.Error("{0}", ex.ToString());
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