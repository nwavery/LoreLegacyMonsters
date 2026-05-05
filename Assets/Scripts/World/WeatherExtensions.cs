using LoreLegacyMonsters.SaveSystem;

namespace LoreLegacyMonsters.World
{
    public static class WeatherExtensions
    {
        public static WeatherTypeDto ToDto(this WeatherType t) => new WeatherTypeDto { Type = (int)t };
    }
}
