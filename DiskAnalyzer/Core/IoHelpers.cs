using System;
using System.IO;

namespace DiskAnalyzer.Core
{
    public static class IoHelpers
    {
        public static string[] SplitPath(string path)
        {
            return path.Split(new[] {Path.DirectorySeparatorChar}, 2, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}