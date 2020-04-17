using System.Threading.Tasks;

namespace PikaCore.Areas.Core.Services
{
    public interface IArchiveService
    {
        Task<Task> ZipDirectoryAsync(string absolutPath, string output);
        void Cancel();
    }
}