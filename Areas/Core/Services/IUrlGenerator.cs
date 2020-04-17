using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace PikaCore.Areas.Core.Services
{
    public interface IUrlGenerator
    {
        string GenerateId(string aboslutPath);
        void SetDerivationPrf(KeyDerivationPrf prf);
    }
}