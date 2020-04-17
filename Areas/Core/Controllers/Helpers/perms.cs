using System;
using System.Runtime.InteropServices;

namespace PikaCore.Areas.Core.Controllers.Helpers
{
    [StructLayout(LayoutKind.Sequential, Size = 512), Serializable]
    public struct Perms
    {
        [MarshalAs(UnmanagedType.LPStr)]
        public string filename;
        public int ow_perms;
        public int gr_perms;
        public int oth_perms;
    }
}