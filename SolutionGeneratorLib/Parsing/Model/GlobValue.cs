namespace SolutionGenerator.Parsing.Model
{
    public class GlobValue : PropertyValue
    {
        public string GlobStr { get; }
        
        public GlobValue(string value) : base(value)
        {
            GlobStr = value;
        }
    }
}