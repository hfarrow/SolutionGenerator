namespace SolutionGenerator.Parsing.Model
{
    public class PropertyValue
    {
        public object Value { get; }
        
        public PropertyValue(object value)
        {
            Value = value;
        }
    }
}