using System;
using System.IO;
using System.Linq;
using InvalidPathException = PikaCore.Infrastructure.Adapters.Filesystem.Exceptions.InvalidPathException;

namespace PikaCore.Infrastructure.Adapters.Filesystem
{
    public static class UnixHelper
    {
        public static string DetectUnitBySize(long i)
        {
            string[] units = { "B", "kiB", "MiB", "GiB", "TiB" };
            var unitIndex = 0;
            for (var ptr = 1; ptr <= units.Length; ptr++)
            {
                if (!(i < Math.Pow(1024, ptr)) || i <= 1024) continue;
                unitIndex = ptr - 1;
                break;
            }
            var scaledSize = Math.Round(i / Math.Pow(1024, unitIndex), 2);
            return scaledSize + " " + units[unitIndex];
        }
    }
}
