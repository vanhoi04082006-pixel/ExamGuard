using System.Security.Cryptography;
using System.Text;

namespace ExamGuard.Core.Security;

public static class PasswordHasher
{
    private const int SaltSize = 16;

    public static (byte[] Salt, string Hash) Hash(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        string hash = ComputeHash(password, salt);
        return (salt, hash);
    }

    public static string ComputeHash(string password, byte[] salt)
    {
        byte[] salted = new byte[Encoding.UTF8.GetByteCount(password) + salt.Length];
        Buffer.BlockCopy(Encoding.UTF8.GetBytes(password), 0, salted, 0, salted.Length - salt.Length);
        Buffer.BlockCopy(salt, 0, salted, salted.Length - salt.Length, salt.Length);
        byte[] digest = SHA256.HashData(salted);
        return Convert.ToHexString(digest);
    }

    public static bool Verify(string password, byte[] salt, string expectedHash)
    {
        if (string.IsNullOrEmpty(expectedHash))
            return false;
        string actual = ComputeHash(password, salt);
        return CryptographicOperations.FixedTimeEquals(
            Convert.FromHexString(actual),
            Convert.FromHexString(expectedHash));
    }
}
