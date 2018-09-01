using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using SolutionGen.Generator.Model;
using SolutionGen.Utils;

namespace SolutionGen.Builder
{
    public class SolutionBuilder
    {
        private readonly Solution solution;
        private readonly string masterConfiguration;

        public SolutionBuilder(Solution solution, string masterConfiguration)
        {
            this.solution = solution;

            if (string.IsNullOrEmpty(masterConfiguration))
            {
                masterConfiguration = solution.ConfigurationGroups.Keys.First();
            }

            if (!solution.ConfigurationGroups.ContainsKey(masterConfiguration))
            {
                var ex = new InvalidMasterConfigurationException(masterConfiguration,
                    solution.ConfigurationGroups.Keys.ToArray());
                Log.Error("{0}", ex);
                throw ex;
            }
                
            this.masterConfiguration = masterConfiguration;
        }

        public void BuildAllConfigurations()
        {
            Log.Info("Building all solution configurations");
            using (new CompositeDisposable(
                new Log.ScopedIndent(),
                new Log.ScopedTimer(Log.Level.Info, "Build All Configurations")))
            {
                foreach (Configuration configuration in
                    solution.ConfigurationGroups[masterConfiguration].Configurations.Values)
                {
                    BuildConfiguration(configuration);
                }
            }
        }

        public void BuildDefaultConfiguration()
        {
            BuildConfiguration(string.IsNullOrEmpty(masterConfiguration)
                ? solution.ConfigurationGroups.Values.First().Configurations.Values.First()
                : solution.ConfigurationGroups[masterConfiguration].Configurations.Values.First());
        }

        public void BuildConfiguration(string configurationStr)
        {
            if (solution.ConfigurationGroups[masterConfiguration].Configurations
                .TryGetValue(configurationStr, out Configuration configuration))
            {
                BuildConfiguration(configuration);
            }
            else
            {
                var ex = new InvalidConfigurationException(configurationStr,
                    solution.ConfigurationGroups[masterConfiguration].Configurations.Keys.ToArray());
                Log.Error("{0}", ex.ToString());
                throw ex;
            }
        }

        public void BuildConfiguration(Configuration configuration)
        {
            Log.Heading("Building solution configuration '{0} - {1}'", configuration.GroupName, configuration.Name);
            using (new CompositeDisposable(
                new Log.ScopedIndent(),
                new Log.ScopedTimer(Log.Level.Info, string.Format("Build Configuration '{0} - {1}'",
                    configuration.GroupName, configuration.Name)),
                new ExpandableVar.ScopedState()))
            {
                ExpandableVar.SetExpandableVariable(ExpandableVar.VAR_CONFIGURATION, configuration.Name);
                ExecuteCommands(solution.BeforeBuildCommands, Settings.PROP_BEFORE_BUILD_COMMANDS);
                ExecuteCommands(solution.BuildCommands, Settings.PROP_BUILD_COMMANDS);
                ExecuteCommands(solution.AfterBuildCommands, Settings.PROP_AFTER_BUILD_COMMANDS);
            }
        }

        private void ExecuteCommands(IReadOnlyCollection<string> commands, string commandType)
        {
            foreach (string command in commands)
            {
                ExecuteCommand(command, commandType);
            }
        }

        private void ExecuteCommand(string command, string commandType)
        {
            command = ExpandableVar.ExpandAllInString(command);
            command = ExpandableVar.ExpandToEmptyInString(command);
            Log.Info("Executing '{0}' process: {1}", commandType, command);

            string process = command;
            string args = "";
            int argsIndex = command.IndexOf(' ') + 1;
            if (argsIndex > 1)
            {
                process = command.Substring(0, argsIndex - 1);
                args = command.Substring(argsIndex);
            }

            var psi = new ProcessStartInfo(process, args)
            {
                WorkingDirectory = Directory.GetCurrentDirectory()
            };

            try
            {
                using (new Log.ScopedIndent())
                {
                    ShellUtil.StartProcess(psi, ConsoleOutputHandler, ConsoleErrorHandler, true, true);
                }
            }
            catch (Exception ex)
            {
                Log.Error("Failed to execute build command. See Exception below.");
                Log.Error(ex.ToString());
                throw;
            }
        }

        private static void ConsoleOutputHandler(object sendingProcess, DataReceivedEventArgs line)
        {
            using (new Log.ScopedIndent())
            {
                Log.Debug("> " + line.Data.TrimEnd().TrimEnd('\r', '\n'));
            }
        }

        private static void ConsoleErrorHandler(object sendingProcess, DataReceivedEventArgs line)
        {
            using (new Log.ScopedIndent())
            {
                Log.Error("> " + line.Data.TrimEnd().TrimEnd('\r', '\n'));
            }
        }
    }

    public sealed class InvalidConfigurationException : Exception
    {
        public InvalidConfigurationException(string configuration, string[] validConfigurations)
            : base($"'{configuration}' is not a valid configuration for this solution. " +
                   $"Valid configurations are [{string.Join(", ", validConfigurations)}].")
        {
            
        }
    }
    
    public sealed class InvalidMasterConfigurationException : Exception
    {
        public InvalidMasterConfigurationException(string configuration, string[] validConfigurations)
            : base($"'{configuration}' is not a valid master configuration for this solution. " +
                   $"Valid configurations are [{string.Join(", ", validConfigurations)}].")
        {
            
        }
    }
}