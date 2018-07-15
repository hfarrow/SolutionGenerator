namespace SolutionGen.Parser.Model
{
    public class ValueElement
    {
        public object Value { get; }
        
        public ValueElement(object value)
        {
            Value = value;
        }

        public override string ToString()
        {
            return Value?.ToString() ?? "<none>";
        }
    }
}