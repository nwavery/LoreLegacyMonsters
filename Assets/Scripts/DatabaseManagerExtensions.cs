namespace LoreLegacyMonsters
{
    public static class DatabaseManagerExtensions
    {
        public static bool HasKey(this DatabaseManager db, string key) =>
            db != null && db.TryGetBlob(key, out _);
    }
}
