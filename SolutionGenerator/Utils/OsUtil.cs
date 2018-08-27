using System.Runtime.InteropServices;

namespace SolutionGen.Utils
{
    public enum OS
    {
        Windows,
        Mac,
        Linux,
        Unknown
    }
    
    public static class OsUtil
    {
        public static bool IsWindows() =>
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        public static bool IsMacOS() =>
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

        public static bool IsLinux() =>
            RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

        public static OS Get()
        {
            if (IsWindows()) return OS.Windows;
            if (IsMacOS()) return OS.Mac;
            if (IsLinux()) return OS.Linux;
            return OS.Unknown;
        }
    }
}