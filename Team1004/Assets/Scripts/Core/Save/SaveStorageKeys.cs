internal static class SaveStorageKeys
{
    private const string BackupSuffix = ".backup";
    private const string CorruptSuffix = ".corrupt";

    public static string Backup(string storageKey)
    {
        return storageKey + BackupSuffix;
    }

    public static string Corrupt(string storageKey)
    {
        return storageKey + CorruptSuffix;
    }
}
