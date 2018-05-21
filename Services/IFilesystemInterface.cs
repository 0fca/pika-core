using System.Threading.Tasks;

namespace FMS2.Services{
    public interface IFilesystemInterface
    {
        Task ZipDirectoryAsync(string absolutPath, string output);
        void Cancel();
    }
}