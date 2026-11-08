namespace VeraMPIN;

internal static class AppDataPaths
{
    public static string CurrentDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Vera-mPIN");

    private static string LegacyDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ExternalSsdUnlock");

    public static void MigrateLegacyData()
    {
        MigrateLegacyData(LegacyDirectory, CurrentDirectory);
    }

    internal static void MigrateLegacyData(string legacyDirectory, string currentDirectory)
    {
        if (!Directory.Exists(legacyDirectory))
        {
            return;
        }

        if (!Directory.Exists(currentDirectory))
        {
            Directory.Move(legacyDirectory, currentDirectory);
            return;
        }

        foreach (var sourcePath in Directory.EnumerateFiles(
                     legacyDirectory, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(legacyDirectory, sourcePath);
            var destinationPath = Path.Combine(currentDirectory, relativePath);
            if (File.Exists(destinationPath))
            {
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
            File.Move(sourcePath, destinationPath);
        }

        if (Directory.EnumerateFiles(
                legacyDirectory, "*", SearchOption.AllDirectories).Any())
        {
            throw new IOException(
                $"Some legacy app data could not be migrated because matching files already exist. The remaining files are preserved at: {legacyDirectory}");
        }

        Directory.Delete(legacyDirectory, recursive: true);
    }
}
