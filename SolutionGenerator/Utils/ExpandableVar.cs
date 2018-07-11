using SolutionGen.Generator.Model;

namespace SolutionGen.Utils
{
    public static class ExpandableVar
    {
        public const string VAR_MODULE_NAME = "MODULE_NAME";
        
        public static object Expand(object obj, string varName, string varExpansion)
        {
            switch (obj)
            {
                case IExpandable expandable:
                    expandable.ExpandVariable(varName, varExpansion);
                    break;
                case string str:
                    obj = str.Replace($"$({varName})", varExpansion);
                    break;
            }

            return obj;
        }

        public static object ExpandModuleName(object obj, string moduleName)
        {
            return Expand(obj, VAR_MODULE_NAME, moduleName);
        }
    }
}