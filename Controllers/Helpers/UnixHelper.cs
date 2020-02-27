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
            if (!path.Equals("/") && path.EndsWith("/"))
            {
                path = path.Remove(path.Length - 1, 1);
            }

            const string resultPath = "/";
            if (!path.StartsWith("/")) throw new InvalidPathException("Path must be a Unix-like path!");
            if (!Path.IsPathRooted(path)) throw new InvalidPathException("Path must be path rooted!");
            var pathParts = path.Split("/");
            pathParts[pathParts.Length - 1] = null;

            return pathParts.Where(part => !string.IsNullOrEmpty(part)).Aggregate(resultPath, (current, part) => string.Concat(current, "/", part));
        }

        public static void ClearPath(ref string path)
        {
            var pathParts = path.Split("/");

            if (Path.IsPathRooted(path))
            {
                path = pathParts.Where(part => !string.IsNullOrEmpty(part)).Aggregate("/", (current, part) => (current.Equals("/") ? string.Concat(current, part.Trim()) : string.Concat(current, "/", part.Trim())));
            }
            else
            {
                throw new InvalidPathException("The path must be rooted!");
            }
        }

        public static string MapToPhysical(string currentPhysical, string inPath)
        {
            if (!Constants.OsName.ToLower().Equals("windows")) return string.Concat(currentPhysical, inPath);
            inPath = inPath.Substring(1);
            inPath = inPath.Replace('/', Path.DirectorySeparatorChar);

            return string.Concat(currentPhysical, inPath);
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
                return Environment.OSVersion.Platform == PlatformID.Win32NT;
            var userInfo = new UnixUserInfo(username);
            var oid = FileSystemAccessor.owner(absolutePath);
            return userInfo.UserId == oid;

        }
    }
}
