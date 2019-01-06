using System;
using System.IO;
using FMS.Exceptions;
using System.Diagnostics;
using System.Runtime.InteropServices;
using FMS2.Controllers;
using FMS2.Controllers.Helpers;
using Mono.Unix;

namespace FMS.Controllers.Helpers
{
    public static class UnixHelper
    {
        public static string GetParent(string path){
            string resultPath = "/";
            if(path.StartsWith("/")){
                if(Path.IsPathRooted(path)){
                    var pathParts = path.Split("/");
                    pathParts[pathParts.Length - 1] = null;

                    foreach(var part in pathParts){
                        if(!string.IsNullOrEmpty(part)){
                            resultPath = string.Concat(resultPath,"/",part);
                        }
                    }
                    return resultPath;
                }else{
                    throw new InvalidPathException("Path must be path rooted!");
                }
            }else{
                throw new InvalidPathException("Path must be a Unix-like path!");
            }
        }

        public static void ClearPath(ref string path){
            var pathParts = path.Split("/");
            
            if(Path.IsPathRooted(path)){
                path = "/";
                foreach(var part in pathParts){
                    if(!string.IsNullOrEmpty(part)){
                        if (path.Equals("/"))
                        {
                            path = string.Concat(path, part.Trim());
                        }
                        else
                        {
                            path = string.Concat(path, "/" ,part.Trim());
                        }
                    }
                }
            }else{
                throw new InvalidPathException("The path must be rooted!");
            }
        }

        public static string MapToPhysical(string currentPhysical, string inPath) {            
            if (Constants.OsName.ToLower().Equals("windows")) {
                inPath = inPath.Substring(1);
                inPath = inPath.Replace('/', Path.DirectorySeparatorChar);
            }
            
            return string.Concat(currentPhysical, inPath);
        }

        internal static bool HasAccess(string username, string absolutePath)
        {
            var userInfo = new UnixUserInfo(username);
            var oid = FileSystemAccessor.owner(absolutePath);
            return userInfo.UserId != oid;
        }
    }
}