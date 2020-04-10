using FMS.Exceptions;
using Mono.Unix;
using System;
using System.IO;
using System.Linq;

namespace PikaCore.Controllers.Helpers
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
            pathParts[pathParts.Length - 1] = null;

            return pathParts.Where(part => !string.IsNullOrEmpty(part)).Aggregate(resultPath, (current, part) => string.Concat(current, separator, part));
        }

        public static void ClearPath(ref string path)
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

        internal static bool HasAccess(string username, string absolutePath)
        {
            if (Environment.OSVersion.Platform != PlatformID.Unix)
                throw new InvalidOperationException("This method cannot be ran on non-Unix OS.");
            var userInfo = new UnixUserInfo(username);
            var oid = FileSystemAccessor.owner(absolutePath);
            return userInfo.UserId == oid;

        }
    }
}
