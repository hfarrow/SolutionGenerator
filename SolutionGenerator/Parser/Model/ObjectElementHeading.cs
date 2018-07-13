namespace SolutionGen.Parser.Model
{
    public class ObjectElementHeading
    {
        public string Type { get; }
        public string Name { get; }
        public string InheritedObjectName { get; }

        public ObjectElementHeading(string type, string name, string inheritedObjectName)
        {
            Type = type;
            Name = name;
            InheritedObjectName = inheritedObjectName;
        }

        public override string ToString()
        {
            return string.IsNullOrEmpty(InheritedObjectName)
                ? $"{{{Type} {Name}}}"
                : $"{{{Type} {Name} : {InheritedObjectName}}}";
        }
    }
}