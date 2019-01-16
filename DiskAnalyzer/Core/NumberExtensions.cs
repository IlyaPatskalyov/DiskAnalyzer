using System.Globalization;

namespace DiskAnalyzer.Core
{
    public static class NumberExtensions
    {
        private static readonly NumberFormatInfo numberFormatInfo;

        static NumberExtensions()
        {
            numberFormatInfo = (NumberFormatInfo) CultureInfo.InvariantCulture.NumberFormat.Clone();
            numberFormatInfo.NumberGroupSeparator = " ";
        }

        public static string FormatSize(this long len)
        {
            string[] sizes = {"B", "KB", "MB", "GB", "TB"};
            int order = 0;
            decimal value = len;
            while (value >= 1024 && order < sizes.Length - 1)
            {
                order++;
                value = value / 1024;
            }

            return $"{value:0.##} {sizes[order]}";
        }

        public static string FormatNumber(this long value)
        {
            return value.ToString("n0", numberFormatInfo);
        }

        public static string FormatNumber(this int value)
        {
            return value.ToString("n0", numberFormatInfo);
        }
    }
}