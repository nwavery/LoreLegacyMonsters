namespace LoreLegacyMonsters.Weather
{
    public static class WeatherSystemExtensions
    {
        public static bool IsSevere(this WeatherSystem system) =>
            system != null && (system.Current == World.WeatherType.Stormy ||
                               system.Current == World.WeatherType.Sandstorm);
    }
}
