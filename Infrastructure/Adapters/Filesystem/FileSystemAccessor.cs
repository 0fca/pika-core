using System.Runtime.InteropServices;

namespace Pika.Adapters.Filesystem
{
    internal static class FileSystemAccessor
    {
        [DllImport("libposixhlpr.so")]
        internal static extern string permission_str(string file);

        [DllImport("libposixhlpr.so")]
        internal static extern PikaCore.Infrastructure.Adapters.Filesystem.Perms permission_model(string file);

        [DllImport("libposixhlpr.so")]
        internal static extern int owner(string file);
    }
}
