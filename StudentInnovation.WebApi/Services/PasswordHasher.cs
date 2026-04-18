using System.Security.Cryptography;
using System.Text;

namespace StudentInnovation.WebApi.Services;

public interface IPasswordHasher
{
    string Hash(string rawPassword);
    bool Verify(string rawPassword, string hash);
}

public class PasswordHasher : IPasswordHasher
{
    public string Hash(string rawPassword)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawPassword));
        return Convert.ToHexString(bytes);
    }

    public bool Verify(string rawPassword, string hash)
    {
        return string.Equals(Hash(rawPassword), hash, StringComparison.OrdinalIgnoreCase);
    }
}
