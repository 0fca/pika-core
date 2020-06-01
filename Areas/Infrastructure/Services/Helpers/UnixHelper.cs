using System;
using System.IO;
using System.Linq;
using Mono.Unix;
using PikaCore.Areas.Core.Controllers.App;
using PikaCore.Areas.Core.Exceptions;
using Serilog;

namespace PikaCore.Areas.Infrastructure.Services.Helpers
{
    public static class UnixHelper
    {
        public static string GetParent(string path)
        {
            var separator = Path.DirectorySeparatorChar.ToString();
            if (!path.Equals(separator) && path.EndsWith(separator))
            {
                path = path.Remove(path.Length - 1, 1);
            }
            
            var resultPath = separator;
            var pathParts = path.Split(separator);
            pathParts[^1] = null;

            return pathParts.Where(part => !string.IsNullOrEmpty(part)).Aggregate(resultPath, (current, part) => string.Concat(current, separator, part));
        }

        private static void ClearPath(ref string path)
        {
            var separator = Path.DirectorySeparatorChar.ToString();
            var pathParts = path.Split(separator);

            if (Path.IsPathRooted(path))
            {
                path = pathParts.Where(part => !string.IsNullOrEmpty(part)).Aggregate(separator,
                    (current, part) =>
                        (current.Equals(separator)
                            ? string.Concat(current, part.Trim())
                            : string.Concat(current, separator, part.Trim())));
            }
            else
            {
                throw new InvalidPathException("The path must be rooted!");
            }
        }

        public static string MapToSystemPath(string hostPath)
        {
            var systemPath = string.Concat("/", hostPath.Split(Constants.FileSystemRoot)[1]);
            if (systemPath.Contains("\\"))
            {
                systemPath = systemPath.Replace('\\', '/');
            }

            ClearPath(ref systemPath);

            return systemPath;
        }

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
        
        internal static bool HasAccess(string username, string absolutePath)
        {
            if (Environment.OSVersion.Platform != PlatformID.Unix)
                return true;
            var userInfo = new UnixUserInfo(username);
            var oid = FileSystemAccessor.owner(absolutePath);
            Log.Information($"{userInfo.UserId} : {oid}");
            return userInfo.UserId == oid;
        }
    }
}
