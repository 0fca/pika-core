using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace FMS2.Services{
    public class UrlGeneratorService : IGenerator
    {
        public string GenerateId(string aboslutPath)
        {
            return Hash(aboslutPath);
        }

        private string Hash(string input)
        {
            var hash = (new SHA1Managed()).ComputeHash(Encoding.UTF8.GetBytes(input));
            return string.Join("", hash);
        }
    }
}