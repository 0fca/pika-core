using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PikaCore.Pages.Home
{
    public class AboutModel : PageModel
    {
        public string Version;
        public string OS;
        public string FrameworkVer;

        public void OnGet()
        {
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                var longString = System.Runtime.InteropServices.RuntimeInformation.OSDescription.Split(" ");
                OS = $"GNU/Linux {longString[4]} {longString[0]} {longString[1]}";
            }
            else
            {
                OS = Environment.OSVersion.Platform.ToString();
            }
            FrameworkVer = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
            Version = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
        }
    }
}