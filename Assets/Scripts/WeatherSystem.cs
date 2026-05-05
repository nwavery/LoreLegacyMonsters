namespace LoreLegacyMonsters
{
    /// <summary>Additional weather helpers (partial with World/WeatherSystem.cs).</summary>
    public partial class WeatherSystem
    {
        public float EncounterRateMultiplier =>
            Current == World.WeatherType.Stormy ? 1.15f : 1f;
    }
}
