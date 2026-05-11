using System;
using LoreLegacyMonsters;
using LoreLegacyMonsters.Core;
using UnityEngine;

namespace LoreLegacyMonsters.World
{
    /// <summary>
    /// Tracks walked distance and emits encounters at story-aware pacing.
    /// </summary>
    public sealed class OverworldEncounterPacer
    {
        float walkedSinceEncounter;
        string walkedEncounterAreaId;

        public bool TryStep(
            float baseEncounterDistance,
            PlayerController player,
            WorldManager worldManager,
            EncounterService encounters,
            AssetRegistryManager registry,
            WeatherSystem weather,
            CombatManager combat,
            Action<string> setStatus)
        {
            if (player == null || worldManager == null || encounters == null || registry == null || weather == null || combat == null)
                return false;

            if (walkedEncounterAreaId != worldManager.CurrentAreaId)
            {
                walkedEncounterAreaId = worldManager.CurrentAreaId;
                walkedSinceEncounter = 0f;
            }

            if (!encounters.CanEncounterAt(worldManager))
                return false;

            var dynamicEncounterStep = baseEncounterDistance;
            var ionaOutcome = StoryState.GetOutcome(StoryState.IonaOutcomeKey);
            var varoOutcome = StoryState.GetOutcome(StoryState.VaroOutcomeKey);
            if (ionaOutcome == StoryState.IonaWithdraw &&
                (worldManager.CurrentAreaId == DefaultGameContent.RouteId ||
                 worldManager.CurrentAreaId == DefaultGameContent.ForestId ||
                 worldManager.CurrentAreaId == DefaultGameContent.GroveId))
            {
                dynamicEncounterStep *= 0.72f;
            }

            var currentRegion = WorldMapLayout.Get(worldManager.CurrentAreaId);
            if (varoOutcome == StoryState.VaroAlly && currentRegion.PhaseTwo)
                dynamicEncounterStep *= 1.45f;

            var loadoutMods = GameManager.Instance != null ? GameManager.Instance.Loadout?.Snapshot : null;
            var encounterRate = loadoutMods != null ? Mathf.Max(0.2f, loadoutMods.EncounterRateMult) : 1f;
            dynamicEncounterStep /= encounterRate;

            walkedSinceEncounter += player.DistanceMovedThisFrame;
            if (walkedSinceEncounter < dynamicEncounterStep)
                return false;

            walkedSinceEncounter = 0f;
            if (varoOutcome == StoryState.VaroAlly && currentRegion.PhaseTwo && UnityEngine.Random.value < 0.35f)
            {
                setStatus?.Invoke("Containment markers calm nearby wild nests.");
                return false;
            }

            if (!encounters.TryRollWildEncounter(registry, worldManager, weather, out var monster,
                    rng: null, loadout: loadoutMods))
                return false;

            combat.BeginBattle(monster);
            setStatus?.Invoke($"Encountered {monster.DisplayName}.");
            return true;
        }
    }
}
