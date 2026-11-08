using System.Security.Cryptography;
using System.Text;

namespace VeraMPIN;

internal static class SecretStore
{
    private static string SecretDirectory => Path.Combine(AppDataPaths.CurrentDirectory, "mPIN");

    private static string SecretPath(Guid profileId) =>
        Path.Combine(SecretDirectory, $"{profileId:N}.dpapi");

    private static string LegacySecretPath => Path.Combine(
        AppDataPaths.CurrentDirectory,
        "veracrypt-password.dpapi");

    public static bool Exists(Guid profileId) => File.Exists(SecretPath(profileId));

    public static bool AnyExists(IEnumerable<MountProfile> profiles) =>
        profiles.Any(profile => Exists(profile.Id));

    public static void Save(Guid profileId, string password)
    {
        var plainBytes = Encoding.UTF8.GetBytes(password);
        try
        {
            var protectedBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
            try
            {
                Directory.CreateDirectory(SecretDirectory);
                File.WriteAllBytes(SecretPath(profileId), protectedBytes);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(protectedBytes);
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plainBytes);
        }
    }

    public static string Read(Guid profileId)
    {
        var path = SecretPath(profileId);
        var protectedBytes = File.ReadAllBytes(path);
        byte[]? plainBytes = null;
        try
        {
            plainBytes = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(plainBytes);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(protectedBytes);
            if (plainBytes is not null)
            {
                CryptographicOperations.ZeroMemory(plainBytes);
            }
        }
    }

    public static void Delete(Guid profileId)
    {
        var path = SecretPath(profileId);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    public static void DeleteAll(IEnumerable<MountProfile> profiles)
    {
        foreach (var profile in profiles)
        {
            Delete(profile.Id);
        }
    }

    public static void MigrateLegacySecret(Guid profileId)
    {
        if (!File.Exists(LegacySecretPath))
        {
            return;
        }

        if (Exists(profileId))
        {
            File.Delete(LegacySecretPath);
            return;
        }

        var protectedBytes = File.ReadAllBytes(LegacySecretPath);
        byte[]? plainBytes = null;
        try
        {
            plainBytes = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);
            var password = Encoding.UTF8.GetString(plainBytes);
            Save(profileId, password);
            File.Delete(LegacySecretPath);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(protectedBytes);
            if (plainBytes is not null)
            {
                CryptographicOperations.ZeroMemory(plainBytes);
            }
        }
    }
}
