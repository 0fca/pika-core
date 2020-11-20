using System;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PikaCore.Areas.Core.Controllers.App;

namespace PikaCore.Areas.Core.Pages.Home
{
    public class AboutModel : PageModel
    {
        public string? Version;
        public string? Os;
        public string? FrameworkVer;
        public string? Instance { get; set; }

        public void OnGet()
        {
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                var longString = System.Runtime.InteropServices.RuntimeInformation.OSDescription;
                if (longString.Contains("Linux")
                && System.IO.File.Exists("/etc/os-release"))
                {
                    var strArr = System.IO.File.ReadAllLines("/etc/os-release");
                    var friendlyName = strArr[0].Split("=")[1];
                    Os = friendlyName.Substring(1, friendlyName.Length - 2);
                }
                else 
                {
                    Os = PlatformID.Unix.ToString();
                }
            }
            else
            {
                Os = Environment.OSVersion.Platform.ToString();
            }

            Instance = Constants.Instance;
            FrameworkVer = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
            Version = (Assembly.GetEntryAssembly() ?? throw new InvalidOperationException()).GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        }
    }
}