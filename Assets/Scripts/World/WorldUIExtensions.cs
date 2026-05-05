namespace LoreLegacyMonsters.World
{
    public static class WorldUIExtensions
    {
        public static string AreaLabel(WorldArea a) => a != null ? a.DisplayName : "";
    }
}
