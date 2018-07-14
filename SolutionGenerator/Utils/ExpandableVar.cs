using System;
using System.Collections.Generic;
using System.Linq;
using SolutionGen.Generator.Model;
using SolutionGen.Generator.Reader;

namespace SolutionGen.Utils
{
    public static class ExpandableVar
    {
        public const string VAR_SOLUTION_NAME = "SOLUTION_NAME";
        public const string VAR_MODULE_NAME = "MODULE_NAME";
        public const string VAR_PROJECT_NAME = "PROJECT_NAME";
        
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
                prevVars = new Dictionary<string, string>(expandableVariables);
            }
            
            public void Dispose()
            {
                expandableVariables = prevVars;
            }
        }

        public static void SetExpandableVariable(string varName, string varExpansion)
        {
            expandableVariables.TryGetValue(varName, out string previous);
            
            Log.WriteLine("Setting expandable variable: {0} = {1} (previously {2})",
                varName,
                varExpansion != null ? $"'{varExpansion}'" : "<null>",
                previous != null ? $"'{previous}'" : "<null>");
            
            expandableVariables[varName] = varExpansion;
        }

        public static void ClearExpandableVariable(string varName)
        {
            Log.WriteLine("Clearing expandable variable: {0}", varName);
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
                    string newValue =  prevValue.Replace($"$({varName})", varExpansion);
                    obj = newValue;
                    if (prevValue != newValue)
                    {
                        didExpand = true;
                        Log.WriteLine("Expanding variable '{0}' in '{1}' to '{2}'", varName, prevValue, newValue);
                    }
                    break;
            }

            outObj = obj;
            return didExpand;
        }

        public static object ExpandAllInCopy(object obj, IReadOnlyDictionary<string, string> varExpansions)
        {
            obj = varExpansions.Aggregate(obj, (current, kvp) =>
            {
                ExpandInCopy(current, kvp.Key, kvp.Value, out object copy);
                return copy;
            });
            return obj;
        }

        public static object ExpandAllForProperty(string propertyName, object obj,
            IReadOnlyDictionary<string, string> varExpansions,
            Func<string, PropertyDefinition> propertyDefinitionGetter)
        {
            PropertyDefinition definition = propertyDefinitionGetter(propertyName);

            bool didExpand = false;
            obj = varExpansions.Aggregate(obj,
                (current, kvp) =>
                {
                    didExpand |= definition.ExpandVariable(current, kvp.Key, kvp.Value, out object copy);
                    return copy;
                });

            if (didExpand)
            {
                Log.WriteLine("Expanded all variables in property '{0}' => '{1}'", propertyName, obj);
            }
            
            return obj;
        }
        
        public static object ExpandModuleNameInCopy(object obj, string moduleName)
        {
            ExpandInCopy(obj, VAR_MODULE_NAME, moduleName, out object copy);
            return copy;
        }
    }
}