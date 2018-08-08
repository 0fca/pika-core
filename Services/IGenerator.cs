using System.Threading.Tasks;

namespace FMS2.Services{
    public interface IGenerator
    {
        string GenerateId(string aboslutPath);
    }
}