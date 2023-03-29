using Microsoft.AspNetCore.DataProtection;

namespace PikaCore.Infrastructure.Security
{
    public class IdDataProtection
    {
        private readonly IDataProtector protector;
        public IdDataProtection(IDataProtectionProvider dataProtectionProvider, UniqueCode uniqueCode)
        {
            protector = dataProtectionProvider.CreateProtector(uniqueCode.BankIdRouteValue);
        }
        public string Encode(string data)
        {
            data = Base64Encode(data);
            return protector.Protect(data);
        }
        public string Decode(string data)
        {
            data = protector.Unprotect(data);
            return Base64Decode(data);
        }

        private static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        private static string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }
    }
}
