using System.Threading.Tasks;

namespace PikaCore.Infrastructure.Services
{
    public interface ISchedulerService
    {
        Task Init();
        Task StartJob(string name);
        void Dispose();
    }
}
