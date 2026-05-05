using UnityEngine;
using LoreLegacyMonsters.SaveSystem;

namespace LoreLegacyMonsters
{
    public partial class WeatherSystem : MonoBehaviour
    {
        [SerializeField] World.WeatherType current = World.WeatherType.Clear;

        public World.WeatherType Current => current;

        public void SetWeather(World.WeatherType type) => current = type;

        public void ApplySave(WeatherTypeDto dto)
        {
            if (dto == null) return;
            current = (World.WeatherType)Mathf.Clamp(dto.Type, 0, 7);
        }

        public WeatherTypeDto ToSaveDto() => new WeatherTypeDto { Type = (int)current };
    }
}
