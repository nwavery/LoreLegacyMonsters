namespace LoreLegacyMonsters
{
    public static class CharacterCreationSystemExtensions
    {
        public static bool HasName(this CharacterCreationSystem c) =>
            c != null && !string.IsNullOrEmpty(c.PlayerName);
    }
}
