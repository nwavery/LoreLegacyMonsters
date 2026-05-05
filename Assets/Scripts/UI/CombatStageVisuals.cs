using LoreLegacyMonsters.Core;
using UnityEngine;

namespace LoreLegacyMonsters.UI
{
    /// <summary>
    /// Area-themed battle backdrops from <c>Resources/Sprites/Combat</c>.
    /// </summary>
    public static class CombatStageVisuals
    {
        static Sprite Load(string name)
        {
            var sprite = Resources.Load<Sprite>($"Sprites/Combat/{name}");
            if (sprite == null)
            {
                var texture = Resources.Load<Texture2D>($"Sprites/Combat/{name}");
                if (texture != null)
                {
                    texture.filterMode = FilterMode.Point;
                    sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.35f), 32f);
                }
            }

            return sprite;
        }

        public static Sprite BackdropForArea(string areaId)
        {
            var key = areaId switch
            {
                DefaultGameContent.TownId => "bg_town",
                DefaultGameContent.RouteId => "bg_route",
                DefaultGameContent.ForestId => "bg_forest",
                DefaultGameContent.GroveId => "bg_grove",
                DefaultGameContent.MarshId => "bg_marsh",
                DefaultGameContent.RuinsId => "bg_ruins",
                DefaultGameContent.DeltaId => "bg_delta",
                DefaultGameContent.RidgeId => "bg_ridge",
                DefaultGameContent.SpireId => "bg_spire",
                _ => "bg_route"
            };

            return Load(key) ?? Load("bg_route");
        }
    }
}
