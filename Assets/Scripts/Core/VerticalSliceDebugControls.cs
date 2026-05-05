using UnityEngine;
using UnityEngine.InputSystem;
using LoreLegacyMonsters;
using LoreLegacyMonsters.World;

namespace LoreLegacyMonsters.Core
{
    /// <summary>
    /// Optional dev helpers (off by default): F6 town, F7 forest, F8 encounter+battle, F10 debug attack.
    /// Does not run unless <c>enableHotkeys</c> is enabled so F5/F9 quick save/load on the overworld stay unambiguous for alpha testers.
    /// </summary>
    public class VerticalSliceDebugControls : MonoBehaviour
    {
        [Tooltip("Must be enabled explicitly; keeps alpha builds safe if this component is present.")]
        [SerializeField] bool enableHotkeys;

        [SerializeField] WorldManager world;
        [SerializeField] EncounterService encounters;
        [SerializeField] CombatManager combat;
        [SerializeField] AssetRegistryManager registry;
        [SerializeField] WeatherSystem weather;

        void Update()
        {
            if (!enableHotkeys) return;
            var kb = Keyboard.current;
            if (kb == null) return;
            world ??= FindFirstObjectByType<WorldManager>();
            encounters ??= FindFirstObjectByType<EncounterService>();
            combat ??= FindFirstObjectByType<CombatManager>();
            registry ??= FindFirstObjectByType<AssetRegistryManager>();
            weather ??= FindFirstObjectByType<WeatherSystem>();

            if (kb.f6Key.wasPressedThisFrame) DebugTravel(DefaultGameContent.TownId);
            if (kb.f7Key.wasPressedThisFrame) DebugTravel(DefaultGameContent.ForestId);
            if (kb.f8Key.wasPressedThisFrame && encounters != null && combat != null && registry != null)
            {
                if (encounters.TryRollWildEncounter(registry, world, weather, out var m))
                    combat.BeginBattle(m);
            }

            if (kb.f10Key.wasPressedThisFrame && combat != null) combat.PlayerAttack();
        }

        void DebugTravel(string areaId)
        {
            if (world == null) return;
            if (!world.TryTravelTo(areaId) && world.GetArea(areaId) != null)
                world.SetCurrentArea(areaId);
            var player = FindFirstObjectByType<PlayerController>();
            if (player == null) return;
            var spawn = WorldMapLayout.SpawnPoint(areaId);
            player.transform.position = new Vector3(spawn.x, spawn.y, 0f);
            world.SetCurrentPlayerPosition(spawn);
        }
    }
}
