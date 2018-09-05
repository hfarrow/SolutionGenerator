using System.Collections.Generic;

namespace SolutionGen.Console
{
    public sealed class ErrorCode
    {
        private static readonly Dictionary<int, ErrorCode> lookup = new Dictionary<int, ErrorCode>();
        
        public static readonly ErrorCode Success = new ErrorCode(0, nameof(Success));
        public static readonly ErrorCode CliError = new ErrorCode(1, nameof(CliError));
        public static readonly ErrorCode GeneratorException = new ErrorCode(2, nameof(GeneratorException));
        
        public readonly int Value;
        public readonly string DisplayName;

        private ErrorCode(int value, string displayName)
        {
            Value = value;
            DisplayName = displayName;
            lookup[value] = this;
        }

        public static implicit operator ErrorCode(int value) => lookup[value];
        public static implicit operator int(ErrorCode value) => value.Value;

        public override string ToString()
        {
            return $"({Value}) {DisplayName}";
        }
    }
}