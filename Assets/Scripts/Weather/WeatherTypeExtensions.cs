using LoreLegacyMonsters.World;

namespace LoreLegacyMonsters.Weather
{
    public static class WeatherPackageTypeExtensions
    {
        public static float MovementMultiplier(this WeatherType t)
        {
            return t switch
            {
                WeatherType.Snowy => 0.85f,
                WeatherType.Sandstorm => 0.9f,
                _ => 1f
            };
        }
    }
}
