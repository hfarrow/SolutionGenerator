using System;
using SolutionGen.Parsing.Model;

namespace SolutionGen.Compiling
{
    public class PropertyDefinition
    {
        public string Name { get; }
        public Type Type { get; }
        public object DefaultValueObj { get; }
        public ElementCompiler<PropertyElement, PropertyDefinition> Compiler { get; }

        public PropertyDefinition(string name, Type type, object defaultValue,
            ElementCompiler<PropertyElement, PropertyDefinition> compiler)
        {
            Name = name;
            Type = type;
            DefaultValueObj = defaultValue;
            Compiler = compiler;
        }
    }

    public class PropertyDefinition<TValue, TCompiler> : PropertyDefinition
        where TCompiler : ElementCompiler<PropertyElement, PropertyDefinition>, new()
    {
        public TValue DefaultValue => (TValue) DefaultValueObj;
        
        public PropertyDefinition(string name, TValue defaultValue)
            : base(name, typeof(TValue), defaultValue, new TCompiler())
        {
        }
        
        public PropertyDefinition(string name)
            : base(name, typeof(TValue), Activator.CreateInstance<TValue>(), new TCompiler())
        {
        }
    }
}