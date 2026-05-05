namespace LoreLegacyMonsters.World
{
    public static class WorldAreaExtensions
    {
        public static bool ConnectsTo(this WorldArea a, string otherId)
        {
            if (a?.Connections == null) return false;
            foreach (var c in a.Connections)
                if (c == otherId) return true;
            return false;
        }
    }
}
