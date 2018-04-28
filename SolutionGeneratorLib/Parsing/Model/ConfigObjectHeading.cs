namespace SolutionGenerator.Parsing.Model
{
    public class ConfigObjectHeading
    {
        public string Type { get; }
        public string Name { get; }
        public string InheritedObjectName { get; }

        public ConfigObjectHeading(string type, string name, string inheritedObjectName)
        {
            Type = type;
            Name = name;
            InheritedObjectName = inheritedObjectName;
        }
    }
}