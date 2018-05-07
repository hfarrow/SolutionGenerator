using System;
using SolutionGen.Compiling.Model;
using SolutionGen.Parsing.Model;

namespace SolutionGen.Compiling
{
    public class StringPropertyCompiler : PropertyCompiler
    {
        protected override bool UseEvaluatedConditionalToSkip => true;
        
        protected override Result CompileProperty(Settings settings,
            PropertyElement element, PropertyDefinition definition)
        {
            if (element.Action == PropertyAction.Set)
            {
                settings.SetProperty(element.FullName, element.ValueElement.Value);
            }
            else
            {
                throw new InvalidPropertyActionException(element,
                    $"Only the '{nameof(PropertyAction.Set)}' action is valid for a string property.");
            }
            
            return Result.Continue;
        }
    }

    public class InvalidPropertyActionException : Exception
    {
        public InvalidPropertyActionException(PropertyElement element, string message)
            : base(string.Format("Property action '{0}' is not valid for property '{1}'. {2}",
                element.Action, element, message))
        {
            
        }
    }
}