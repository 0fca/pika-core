using System.Collections.Generic;
using System.Threading.Tasks;
using PikaCore.Infrastructure.Data;

namespace PikaCore.Infrastructure.Services
{
    public interface ISystemService
    {
        public Task<SystemDescriptor> GetDescriptorByName(string name);
        public Task<SystemDescriptor> GetDescriptorById(int id);
        public Task<string> GetNameById(int id);
        public Task<int> GetIdByName(string name);

        public Task<List<string>> GetAllSystemNames();

        public Task<List<SystemDescriptor>> GetAll();
    }
}