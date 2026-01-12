using System.Security.Cryptography;
using System.Text;

namespace YourApp.Utilities
{
    public static class PasswordHelper
    {
        public static string ComputeHash(string rawPassword)
        {
            if (string.IsNullOrEmpty(rawPassword)) return "";

            using var sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawPassword));
            var builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }
            return builder.ToString();
        }
    }
}
