using System.Text.Json;
using System.Text.Json.Serialization;
using ExamGuard.Core.Security;

namespace ExamGuard.Core.Configuration;

public sealed class AppConfig
{
    public string SaltBase64 { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    [JsonIgnore]
    public bool HasPassword => !string.IsNullOrEmpty(SaltBase64) && !string.IsNullOrEmpty(PasswordHash);

    public int UnlockMinutes { get; set; } = 60;

    /// <summary>
    /// When true (default), the service rewrites its own DACL to deny termination
    /// to everyone, so Task Manager "End task" is refused. Teacher/admin can still
    /// reset the DACL or disable this flag.
    /// </summary>
    public bool Unkillable { get; set; } = true;

    public static AppConfig CreateWithPassword(string password)
    {
        var (salt, hash) = PasswordHasher.Hash(password);
        return new AppConfig
        {
            SaltBase64 = Convert.ToBase64String(salt),
            PasswordHash = hash
        };
    }

    public void SetPassword(string password)
    {
        var (salt, hash) = PasswordHasher.Hash(password);
        SaltBase64 = Convert.ToBase64String(salt);
        PasswordHash = hash;
    }

    public bool VerifyPassword(string password)
    {
        if (!HasPassword)
            return false;
        byte[] salt = Convert.FromBase64String(SaltBase64);
        return PasswordHasher.Verify(password, salt, PasswordHash);
    }
}
