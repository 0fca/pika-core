using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace PikaCore.Services
{
    public interface IGenerator
    {
        string GenerateId(string aboslutPath);
        void SetDerivationPrf(KeyDerivationPrf prf);
    }
}