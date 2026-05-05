using NUnit.Framework;
using UnityEngine;
using LoreLegacyMonsters;
using LoreLegacyMonsters.World;
using Object = UnityEngine.Object;

namespace LoreLegacyMonsters.Tests.Editor
{
    public class WeatherSystemTests
    {
        [Test]
        public void WeatherSystem_SetWeather_PersistsInSaveDto()
        {
            var go = new GameObject("w");
            var w = go.AddComponent<WeatherSystem>();
            w.SetWeather(WeatherType.Rainy);
            var dto = w.ToSaveDto();
            Assert.AreEqual((int)WeatherType.Rainy, dto.Type);
            Object.DestroyImmediate(go);
        }
    }
}
