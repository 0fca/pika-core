using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace PikaCore.Areas.Core.Services
{
    public interface IHashGenerator
    {
        string GenerateId(string stringToHash);
    }
}