using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace FMS2.Services{
    public class HashGeneratorService : IGenerator
    {
        private static KeyDerivationPrf Prf { get; set; } = KeyDerivationPrf.HMACSHA256;
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
                prf: Prf,
                iterationCount: 10000,
                numBytesRequested: 256 / 8));
            PrepareHashString(ref hash);
            return  hash;
        }

        private void PrepareHashString(ref string hash)
        {
            Dictionary<char, char> dictionary = new Dictionary<char, char>
            {
                { '+', '_' },
                { '=', '-' },
                { '\\', '$' },
                { '/', '%' }
            };

            foreach (char c in hash.ToCharArray()) {
                if (dictionary.ContainsKey(c)) {
                    dictionary.TryGetValue(c, out char replaceChar);
                    hash.Replace(c, replaceChar);
                }
            }
        }

        public void SetDerivationPrf(KeyDerivationPrf prf)
        {
            Prf = prf;
        }
    }
}