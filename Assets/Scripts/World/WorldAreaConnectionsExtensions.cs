namespace LoreLegacyMonsters.World
{
    public static class WorldAreaConnectionsExtensions
    {
        public static int ConnectionCount(this WorldArea a) => a?.Connections?.Count ?? 0;
    }
}
