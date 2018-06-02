namespace SolutionGen.Utils
{
    public class Box<T>
        where T : struct
    {
        public T Value;
        
        public Box(T value)
        {
            Value = value;
        }
    }
}