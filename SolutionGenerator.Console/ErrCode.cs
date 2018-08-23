using System.Collections.Generic;

namespace SolutionGen.Console
{
    public sealed class ErrorCode
    {
        private static readonly Dictionary<int, ErrorCode> lookup = new Dictionary<int, ErrorCode>();
        
        public static readonly ErrorCode Success = new ErrorCode(0);
        public static readonly ErrorCode CliError = new ErrorCode(1);
        public static readonly ErrorCode GeneratorException = new ErrorCode(2);
        
        private readonly int value;

        private ErrorCode(int value)
        {
            this.value = value;
            lookup[value] = this;
        }

        public static implicit operator ErrorCode(int value) => lookup[value];
        public static implicit operator int(ErrorCode value) => value.value;

        public override string ToString()
        {
            return $"ErrorCode({value})";
        }
    }
}