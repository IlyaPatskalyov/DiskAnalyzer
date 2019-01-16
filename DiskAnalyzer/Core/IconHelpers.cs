using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using SharpShell.Interop;

namespace DiskAnalyzer.Core
{
    public static class IconHelpers
    {
        public static BitmapSource GetIconDll(string fileName)
        {
            try
            {
                using (var sysIcon = GetIcon(fileName))
                {
                    return Imaging.CreateBitmapSourceFromHIcon(
                        sysIcon.Handle,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromWidthAndHeight(34, 34));
                }
            }
            catch
            {
                return null;
            }
        }

        private static Icon GetIcon(string fileName)
        {
            var fileInfo = new SHFILEINFO();
            Shell32.SHGetFileInfo(fileName, 0, out fileInfo, (uint) Marshal.SizeOf(fileInfo),
                                  SHGFI.SHGFI_ICON | SHGFI.SHGFI_LARGEICON | SHGFI.SHGFI_TYPENAME);
            try
            {
                var icon = (Icon) Icon.FromHandle(fileInfo.hIcon).Clone();
                DestroyIcon(fileInfo.hIcon);
                return icon;
            }
            catch
            {
                return null;
            }
        }

        [DllImport("user32.dll")]
        private static extern int DestroyIcon(IntPtr hIcon);
    }
}