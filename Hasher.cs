using System.Security.Cryptography;
using System.Text;

namespace Server;

public static class Hasher
{
    public static string Hash(string input)
    {
        using var sha256 = SHA256.Create();
        byte[] bytes = Encoding.UTF8.GetBytes(input);
        byte[] hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash); // .NET 5+ (или BitConverter.ToString(hash).Replace("-", "").ToLower())
    }
}