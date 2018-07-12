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
        
        private static readonly Dictionary<string, string> expandableVariables = new Dictionary<string, string>();
        public static IReadOnlyDictionary<string, string> ExpandableVariables => expandableVariables;

        public static void SetExpandableVariable(string varName, string varExpansion)
        {
            expandableVariables.TryGetValue(varName, out string previous);
            
            Log.WriteLine("Setting expandable variable: {0} = '{1}' (previously {2})",
                varName, varExpansion, previous != null ? $"'{previous}'" : "<null>");
            
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

        public static object ExpandInCopy(object obj, string varName, string varExpansion)
        {
            switch (obj)
            {
                case IExpandable expandable:
                    obj = expandable.ExpandVairableInCopy(varName, varExpansion);
                    break;
                case string str:
                    obj = str.Replace($"$({varName})", varExpansion);
                    break;
            }

            return obj;
        }

        public static object ExpandAllInCopy(object obj, IReadOnlyDictionary<string, string> varExpansions)
        {
            string previousValue = obj.ToString();
            obj = varExpansions.Aggregate(obj, (current, kvp) => ExpandInCopy(current, kvp.Key, kvp.Value));
            string newValue = obj.ToString();
            if (previousValue != newValue)
            {
                Log.WriteLine("Expanding all variables in '{0}' to '{1}'", previousValue, obj);
            }
            return obj;
        }

        public static object ExpandAllForPropertyInCopy(string propertyName, object obj,
            IReadOnlyDictionary<string, string> varExpansions)
        {
            string previousValue = obj.ToString();
            PropertyDefinition definition = SettingsReader.GetPropertyDefinition(propertyName);
            
            obj = varExpansions.Aggregate(obj,
                (current, kvp) => definition.ExpandVariable(current, kvp.Key, kvp.Value));
            
            string newValue = obj.ToString();
            if (previousValue != newValue)
            {
                Log.WriteLine("Expanding all variables in property '{0}' => '{1}' to '{2}'",
                    propertyName, previousValue, obj);
            }
            
            return obj;
        }

        public static void ExpandModuleNameInPlace(object obj, string moduleName)
        {
            ExpandInPlace(obj, VAR_MODULE_NAME, moduleName);
        }
        
        public static object ExpandModuleNameInCopy(object obj, string moduleName)
        {
            return ExpandInCopy(obj, VAR_MODULE_NAME, moduleName);
        }
    }
}