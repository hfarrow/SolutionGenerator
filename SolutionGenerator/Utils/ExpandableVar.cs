using System;
using SolutionGen.Generator.Model;

namespace SolutionGen.Utils
{
    public static class ExpandableVar
    {
        public const string VAR_MODULE_NAME = "MODULE_NAME";
        
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