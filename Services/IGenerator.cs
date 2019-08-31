using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace FMS2.Services
{
    public interface IGenerator
    {
        string GenerateId(string aboslutPath);
        void SetDerivationPrf(KeyDerivationPrf prf);
    }
}