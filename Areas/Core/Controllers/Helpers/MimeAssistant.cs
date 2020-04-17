using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PikaCore.Controllers.Helpers
{
    public static class MimeAssistant
    {
        public static string GetMimeType(string fileName)
        {
            try
            {
                if (Path.IsPathFullyQualified(fileName)
                && Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    return ReadMimeUsingFile(fileName).Result.Trim();
                }
                return "unknown/unknown";
            }
            catch(Exception e) 
            {
                Debug.WriteLine(e.Message);
                return e.Message;
            }
        }

        public static string RecognizeIconByMime(string mime)
        {
            if (string.IsNullOrEmpty(mime)) 
            {
                return "exclamation";
            }

            if (mime.Equals("unknown/unknown")) 
            {
                return "times";
            }

            var translationParts = new Dictionary<string, string>
            {
                { "zip", "archive" },
                { "7zip", "archive" },
                { "tar", "archive" },
                { "pptx", "powerpoint" },
                { "xlsx", "spreadsheet" },
                { "docx", "word" },
                { "text", "file-alt" },
                { "audio", "file-audio" },
                { "pdf" , "file-pdf" },
                { "x-rar", "archive" }
            };
        
            var parts = mime.Split("/");

            if (!translationParts.Keys.Any(x => parts.Contains(x))) return "file";
            {
                translationParts.TryGetValue(translationParts.Keys.Single(x => parts.Contains(x)), out var value);
                return value;
            }
        }

        private static async Task<string> ReadMimeUsingFile(string fileName) 
        {
            var process = new Process
            {
                StartInfo =
                {
                    FileName = "file",
                    Arguments = $"--mime-type \"{fileName}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                }
            };

            process.Start();

            var output = await process.StandardOutput.ReadToEndAsync();
            output = output.Split(":")[1];
            process.WaitForExit();
            process.Close();
            return output;
        }
    }
}
