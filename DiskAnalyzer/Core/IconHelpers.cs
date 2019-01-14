using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace DiskAnalyzer.Core
{
    public static class IconHelpers
    {
        public static BitmapSource GetIconDll(string fileName)
        {
            BitmapSource myIcon = null;

            var validDrive = false;
            foreach (var d in DriveInfo.GetDrives())
                if (fileName == d.Name)
                    validDrive = true;

            if (File.Exists(fileName) || Directory.Exists(fileName) || validDrive)
                using (var sysIcon = GetIcon(fileName))
                {
                    try
                    {
                        myIcon = Imaging.CreateBitmapSourceFromHIcon(
                            sysIcon.Handle,
                            Int32Rect.Empty,
                            BitmapSizeOptions.FromWidthAndHeight(34, 34));
                    }
                    catch
                    {
                        myIcon = null;
                    }
                }

            return myIcon;
        }

        private static Icon GetIcon(string fileName)
        {
            var shinfo = new SHFILEINFO();
            Win32.SHGetFileInfo(fileName, 0, ref shinfo, (uint) Marshal.SizeOf(shinfo),
                                Win32.SHGFI_ICON | Win32.SHGFI_LARGEICON | Win32.SHGFI_DISPLAYNAME | Win32.SHGFI_TYPENAME);
            try
            {
                var icon = (Icon) Icon.FromHandle(shinfo.hIcon).Clone();
                Win32.DestroyIcon(shinfo.hIcon);
                return icon;
            }
            catch
            {
                return null;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SHFILEINFO
        {
            public readonly IntPtr hIcon;
            public readonly IntPtr iIcon;
            public readonly uint dwAttributes;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public readonly string szDisplayName;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public readonly string szTypeName;
        }

        private class Win32
        {
            public const uint SHGFI_ICON = 0x100;
            public const uint SHGFI_LARGEICON = 0x0; // Large icon
            public const uint SHGFI_SMALLICON = 0x1; // Small icon
            public const uint SHGFI_DISPLAYNAME = 0x000000200; // Small icon
            public const uint USEFILEATTRIBUTES = 0x000000010; // when the full path isn't available
            public const uint SHGFI_TYPENAME = 0x000000400;

            [DllImport("shell32.dll")]
            public static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);

            [DllImport("User32.dll")]
            public static extern int DestroyIcon(IntPtr hIcon);
        }
    }
}