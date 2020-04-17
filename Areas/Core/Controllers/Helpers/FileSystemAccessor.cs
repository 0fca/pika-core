using System;
using System.Runtime.InteropServices;
using PikaCore.Controllers.Helpers;

namespace PikaCore.Areas.Core.Controllers.Helpers
{
    internal static class FileSystemAccessor
    {
        [DllImport("libposixhlpr.so")]
        internal static extern string permission_str(string file);

        [DllImport("libposixhlpr.so")]
        internal static extern Perms permission_model(string file);

        [DllImport("libposixhlpr.so")]
        internal static extern int owner(string file);

        public static string DetectUnitBySize(long i)
        {
            string[] units = { "B", "kB", "MB", "GB", "TB" };
            int unitIndex = 0;
            for (int ptr = 1; ptr <= units.Length; ptr++)
            {
                if (i < Math.Pow(1024, ptr) && i > 1024)
                {
                    unitIndex = ptr - 1;
                    break;
                }
            }
            double scaledSize = Math.Round(i / Math.Pow(1024, unitIndex), 2);
            return scaledSize + " " + units[unitIndex];
        }
    }
}
