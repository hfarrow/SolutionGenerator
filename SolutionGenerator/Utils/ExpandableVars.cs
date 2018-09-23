using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using SolutionGen.Generator.Model;

namespace SolutionGen.Utils
{
    public class ExpandableVars
    {
        public static ExpandableVars Init(Dictionary<string, string> baseVars)
        {
            instance.Value = new ExpandableVars {variables = baseVars};
            return instance.Value;
        }
        
        private static readonly AsyncLocal<ExpandableVars> instance = new AsyncLocal<ExpandableVars> { Value = new ExpandableVars() };
        public static ExpandableVars Instance => instance.Value;

        public const string VAR_SOLUTION_NAME = "SOLUTION_NAME";
        public const string VAR_SOLUTION_PATH = "SOLUTION_PATH";
        public const string VAR_CONFIG_DIR = "CONFIG_DIR";
        public const string VAR_MODULE_NAME = "MODULE_NAME";
        public const string VAR_PROJECT_NAME = "PROJECT_NAME";
        public const string VAR_CONFIGURATION = "CONFIGURATION";
        
        private Dictionary<string, string> variables = new Dictionary<string, string>();
        public IReadOnlyDictionary<string, string> Variables => variables;

        public class ScopedVariable : IDisposable
        {
            private readonly ExpandableVars container;
            private readonly string varName;
            private readonly string prevExpansion;

            public ScopedVariable(ExpandableVars container, string varName, string varExpansion)
            {
                this.container = container;
                this.varName = varName;
                container.variables.TryGetValue(varName, out prevExpansion);
                container.SetExpandableVariable(varName, varExpansion);
            }
            
            public void Dispose()
            {
                if (prevExpansion == null)
                {
                    container.ClearExpandableVariable(varName);
                }
                else
                {
                    container.SetExpandableVariable(varName, prevExpansion);
                }
            }
        }

        public class ScopedState : IDisposable
        {
            private readonly ExpandableVars container;
            private readonly Dictionary<string, string> prevVars;
            
            public ScopedState(ExpandableVars container)
            {
                this.container = container;
                Log.Debug("Saving expandable variable state:");
                Log.IndentedCollection(container.variables, kvp => $"{kvp.Key} => {kvp.Value}", Log.Debug);
                prevVars = new Dictionary<string, string>(container.variables);
            }
            
            public void Dispose()
            {
                container.variables = prevVars;
                Log.Debug("Restoring expandable variable state:");
                Log.IndentedCollection(container.variables, kvp => $"{kvp.Key} => {kvp.Value}", Log.Debug);
            }
        }

        public void SetExpandableVariable(string varName, string varExpansion)
        {
            variables.TryGetValue(varName, out string previous);
            
            Log.Debug("Setting expandable variable: {0} = {1} (previously {2})",
                varName,
                varExpansion != null ? $"'{varExpansion}'" : "<null>",
                previous != null ? $"'{previous}'" : "<null>");
            
            variables[varName] = varExpansion;
        }

        public void ClearExpandableVariable(string varName)
        {
            Log.Debug("Clearing expandable variable: {0}", varName);
            variables.Remove(varName);
        }
        
        public void ExpandInPlace(object obj, string varName, string varExpansion)
        {
            switch (obj)
            {
                case IExpandable expandable:
                    expandable.ExpandVariableInPlace(varName, varExpansion);
                    break;
                case string _:
                    throw new InvalidOperationException($"Type {typeof(string)} cannot be expanded in place. Use ExpandInCopy instead.");
            }
        }

        public static bool ExpandInCopy(object obj, string varName, string varExpansion, out object outObj)
        {
            bool didExpand = false;
            switch (obj)
            {
                case IExpandable expandable:
                    if (expandable.ExpandVairableInCopy(varName, varExpansion, out IExpandable copy))
                    {
                        didExpand = true;
                    }
                    obj = copy;
                    break;
                case string prevValue:
                    string newValue = ReplaceOccurences(varName, varExpansion, prevValue);
                    obj = newValue;
                    if (prevValue != newValue)
                    {
                        didExpand = true;
                        Log.Debug("Expanding variable '{0}' in '{1}' to '{2}'", varName, prevValue, newValue);
                    }
                    break;
            }

            outObj = obj;
            return didExpand;
        }

        public static bool StripEscapedVariablesInCopy(object obj, out object outObj)
        {
            bool didStrip = false;
            switch (obj)
            {
                case IExpandable expandable:
                    if (expandable.StripEscapedVariablesInCopy(out IExpandable copy))
                    {
                        didStrip = true;
                    }
                    obj = copy;
                    break;
                case string prevValue:
                    string newValue = StripEscapedVariables(prevValue);
                    obj = newValue;
                    if (prevValue != newValue)
                    {
                        didStrip = true;
                        Log.Debug("Stripping escaped variables in '{0}' to '{1}'", prevValue, newValue);
                    }
                    break;
            }

            outObj = obj;
            return didStrip;
        }

        public string ExpandAllInString(string obj) =>
            (string) ExpandAllInCopy(obj, variables);

        public object ExpandAllInCopy(object obj, IReadOnlyDictionary<string, string> varExpansions)
        {
            obj = varExpansions.Aggregate(obj, (current, kvp) =>
            {
                ExpandInCopy(current, kvp.Key, kvp.Value, out object copy);
                return copy;
            });
            return obj;
        }

        public static object ExpandAllForProperty(string propertyName, object property,
            IReadOnlyDictionary<string, string> varExpansions,
            Func<string, PropertyDefinition> propertyDefinitionGetter)
        {
            PropertyDefinition definition = propertyDefinitionGetter(propertyName);

            bool didExpand = false;
            property = varExpansions.Aggregate(property,
                (current, kvp) =>
                {
                    didExpand |= definition.ExpandVariable(current, kvp.Key, kvp.Value, out object copy);
                    return copy;
                });

            if (didExpand)
            {
                Log.Debug("Expanded all variables in property '{0}' => '{1}'", propertyName, property);
            }

            if (definition.StripEscapedVariables(property, out property))
            {
                Log.Debug("Stripped all escaped variables in property '{0}' => '{1}'", propertyName, property);
            }
            
            return property;
        }

        public static string StripEscapedVariables(string input) => input.Replace("\\$", "$");
        
        public static string ReplaceOccurences(string varName, string varExpansion, string input)
        {
            string fullVarName = $"$({varName})";
            var builder = new StringBuilder();

            int segmentStart = 0;
            while (segmentStart < input.Length)
            {
                int varStart = input.IndexOf(fullVarName, segmentStart, StringComparison.Ordinal);
                if (varStart < 0)
                {
                    varStart = input.Length;
                }

                builder.Append(input.Substring(segmentStart, varStart - segmentStart));
                if (varStart != input.Length)
                {
                    if (varStart > 0 && input[varStart - 1] == '\\')
                    {
                        // var was escaped so leave the escaped var in the result
                        builder.Append(fullVarName);
                    }
                    else
                    {
                        builder.Append(varExpansion);
                    }
                }

                segmentStart = varStart + fullVarName.Length;
            }

            return builder.ToString();
        }

        public static string ExpandToEmptyInString(string str)
        {
            return Regex.Replace(str, @"\$\(.*\)", string.Empty);
        }
        
        public object ExpandModuleNameInCopy(object obj, string moduleName)
        {
            ExpandInCopy(obj, VAR_MODULE_NAME, moduleName, out object copy);
            return copy;
        }
    }
}