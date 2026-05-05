namespace LoreLegacyMonsters.World
{
    public enum WeatherType
    {
        Clear = 0,
        Cloudy = 1,
        Rainy = 2,
        Foggy = 3,
        Stormy = 4,
        Snowy = 5,
        Windy = 6,
        Sandstorm = 7
    }

    public static class WeatherTypeExtensions
    {
        public static string DisplayName(this WeatherType t) => t.ToString();
    }
}
