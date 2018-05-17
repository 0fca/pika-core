using System.IO.Compression;
using System.Threading.Tasks;

namespace FMS2.Services{
    public class ArchiveService : IFilesystemInterface
    {
        public Task ZipDirectoryAsync(string absolutePath, string output)
        {
            return Task.Factory.StartNew(() =>{
                ZipFile.CreateFromDirectory(absolutePath, output);
            });
        }
    }
}