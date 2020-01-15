using System.Threading.Tasks;

namespace PikaCore.Services
{
    public interface IZipper
    {
        Task<Task> ZipDirectoryAsync(string absolutPath, string output);
        void Cancel();
    }
}