using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace PikaCore.Services
{
    public class HashUrlGeneratorService : IUrlGenerator
    {
        private static KeyDerivationPrf Prf { get; set; } = KeyDerivationPrf.HMACSHA256;
        public string GenerateId(string aboslutePath)
        {
            return Hash(aboslutePath);
        }



        private static string Hash(string input)
        {
            var salt = new byte[128 / 8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            var hash = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: input,
                salt: salt,
                prf: Prf,
                iterationCount: 10000,
                numBytesRequested: 256 / 8));
            PrepareHashString(ref hash);
            return hash;
        }

        private static void PrepareHashString(ref string hash)
        {
            var dictionary = new Dictionary<char, char>
            {
                { '+', '_' },
                { '=', '-' },
                { '\\', '$' },
                { '/', '.' }
            };

            foreach (var c in hash.ToCharArray())
            {
                if (!dictionary.ContainsKey(c)) continue;
                dictionary.TryGetValue(c, out var replaceChar);
                hash = hash.Replace(c, replaceChar);
            }

            hash = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(hash));
        }

        public void SetDerivationPrf(KeyDerivationPrf prf)
        {
            Prf = prf;
        }
    }
}