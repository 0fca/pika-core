using System.Threading.Tasks;

namespace PikaCore.Infrastructure.Services
{
    public interface IStaticContentService
    {
        Task<string> CopyToCdn(string physicalPath);
        void RemoveFromCdn(string id);
        void CleanCdn();

        bool IsInCdn(string physicalPath);

        string RetrieveFromCdn(string physicalPath);
    }
}