using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace FMS2.Services{
    public class UrlGeneratorService : IGenerator
    {
        public string GenerateId(string aboslutPath)
        {
            return Hash(aboslutPath);
        }

        private string Hash(string input)
        {
             byte[] salt = new byte[128 / 8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            string hash = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: input,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA1,
                iterationCount: 10000,
                numBytesRequested: 256 / 8));

            return string.Join("", hash).Replace("+","=");
        }
    }
}