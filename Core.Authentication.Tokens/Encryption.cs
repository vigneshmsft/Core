using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Core.Authentication.Tokens
{
    public class Encryption
    {
        //TODO: maybe store and pull this from KeyVault.
        private static readonly string SecretKey = System.Environment.GetEnvironmentVariable("PrivateEncryptionSecret");

        public static string Encrypt(string text)
        {
            var data = Encoding.UTF8.GetBytes(text);
            SHA256 sha = new SHA256Managed();

            var result = sha.ComputeHash(data);

            var encryptedString = new StringBuilder();

            foreach (byte x in result)
            {
                encryptedString.AppendFormat("{0:x}", x);
            }

            return encryptedString.ToString();
        }

        public static SecurityKey PrivateKey { get; } = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
    }
}