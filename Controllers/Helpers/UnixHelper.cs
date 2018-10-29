using System;
using System.IO;
using FMS.Exceptions;
using System.Diagnostics;

namespace FMS.Controllers.Helpers
{
    sealed public class UnixHelper
    {
        public static string GetParent(string path){
            string resultPath = "/";
            if(path.StartsWith("/")){
                if(Path.IsPathRooted(path)){
                    var pathParts = path.Split("/");
                    pathParts[pathParts.Length - 1] = null;

                    foreach(var part in pathParts){
                        if(!String.IsNullOrEmpty(part)){
                            resultPath = String.Concat(resultPath,"/",part);
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
                    if(!String.IsNullOrEmpty(part)){
                        path = String.Concat(path,"/",part.Trim());
                        Debug.WriteLine(path);
                    }
                }
            }else{
                throw new InvalidPathException("The path must be rooted!");
            }
        }
    }
}