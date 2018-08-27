using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SolutionGen.Generator.Model;

namespace SolutionGen.Utils
{
    public static class ExpandableVar
    {
        public const string VAR_SOLUTION_NAME = "SOLUTION_NAME";
        public const string VAR_MODULE_NAME = "MODULE_NAME";
        public const string VAR_PROJECT_NAME = "PROJECT_NAME";
        public const string VAR_CONFIGURATION = "CONFIGURATION";
        
        private static Dictionary<string, string> expandableVariables = new Dictionary<string, string>();
        public static IReadOnlyDictionary<string, string> ExpandableVariables => expandableVariables;

        public class ScopedVariable : IDisposable
        {
            private readonly string varName;
            private readonly string prevExpansion;

            public ScopedVariable(string varName, string varExpansion)
            {
                this.varName = varName;
                expandableVariables.TryGetValue(varName, out prevExpansion);
                
                SetExpandableVariable(varName, varExpansion);
            }
            
            public void Dispose()
            {
                if (prevExpansion == null)
                {
                    ClearExpandableVariable(varName);
                }
                else
                {
                    SetExpandableVariable(varName, prevExpansion);
                }
            }
        }

        public class ScopedState : IDisposable
        {
            private readonly Dictionary<string, string> prevVars;
            
            public ScopedState()
            {
                Log.Debug("Saving expandable variable state:");
                Log.IndentedCollection(expandableVariables, kvp => $"{kvp.Key} => {kvp.Value}", Log.Debug);
                prevVars = new Dictionary<string, string>(expandableVariables);
            }
            
            public void Dispose()
            {
                expandableVariables = prevVars;
                Log.Debug("Restoring expandable variable state:");
                Log.IndentedCollection(expandableVariables, kvp => $"{kvp.Key} => {kvp.Value}", Log.Debug);
            }
        }

        public static void SetExpandableVariable(string varName, string varExpansion)
        {
            expandableVariables.TryGetValue(varName, out string previous);
            
            Log.Debug("Setting expandable variable: {0} = {1} (previously {2})",
                varName,
                varExpansion != null ? $"'{varExpansion}'" : "<null>",
                previous != null ? $"'{previous}'" : "<null>");
            
            expandableVariables[varName] = varExpansion;
        }

        public static void ClearExpandableVariable(string varName)
        {
            Log.Debug("Clearing expandable variable: {0}", varName);
            expandableVariables.Remove(varName);
        }
        
        public static void ExpandInPlace(object obj, string varName, string varExpansion)
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

        public static string ExpandAllInString(string obj) =>
            (string) ExpandAllInCopy(obj, expandableVariables);

        public static object ExpandAllInCopy(object obj, IReadOnlyDictionary<string, string> varExpansions)
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
        
        public static object ExpandModuleNameInCopy(object obj, string moduleName)
        {
            ExpandInCopy(obj, VAR_MODULE_NAME, moduleName, out object copy);
            return copy;
        }
    }
}