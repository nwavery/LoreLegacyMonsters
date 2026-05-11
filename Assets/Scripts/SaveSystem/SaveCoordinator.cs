using System.Collections.Generic;
using LoreLegacyMonsters;
using LoreLegacyMonsters.Inventory;
using LoreLegacyMonsters.Core;
using LoreLegacyMonsters.Achievements;
using LoreLegacyMonsters.Dialog.Llm;
using LoreLegacyMonsters.Monster;
using LoreLegacyMonsters.World;

namespace LoreLegacyMonsters.SaveSystem
{
    /// <summary>
    /// Modular save apply/capture; extend by registering additional <see cref="ISaveContributor"/> instances.
    /// </summary>
    public interface ISaveContributor
    {
        void ApplySave(SaveInfo save);
        void CaptureSave(SaveInfo save);
    }

    public sealed class SaveCoordinator
    {
        readonly List<ISaveContributor> _contributors = new List<ISaveContributor>();

        public void Register(ISaveContributor contributor)
        {
            if (contributor != null && !_contributors.Contains(contributor))
                _contributors.Add(contributor);
        }

        public void ApplyAll(SaveInfo save)
        {
            if (save == null) return;
            foreach (var c in _contributors)
                c.ApplySave(save);
        }

        public void CaptureAll(SaveInfo save)
        {
            if (save == null) return;
            foreach (var c in _contributors)
                c.CaptureSave(save);
        }

        /// <summary>Default stack used by <see cref="GameManager"/> at runtime.</summary>
        public static SaveCoordinator CreateDefault(
            GameManager gm,
            InventorySystem inventory,
            QuestManager quests,
            WorldManager world,
            AchievementSystem achievements,
            MonsterSystem monsters,
            WeatherSystem weather,
            NpcMemoryService npcMemories,
            LoadoutSystem loadout = null)
        {
            var coord = new SaveCoordinator();
            coord.Register(new GoldContributor(gm));
            coord.Register(new InventoryContributor(inventory));
            if (loadout != null)
                coord.Register(new LoadoutContributor(loadout));
            coord.Register(new QuestContributor(quests));
            coord.Register(new WorldContributor(world));
            coord.Register(new AchievementContributor(achievements));
            coord.Register(new PartyContributor(monsters, gm != null ? gm.Assets : null));
            coord.Register(new WeatherContributor(weather));
            coord.Register(new NpcMemoryContributor(npcMemories));
            coord.Register(new StoryFlagsContributor());
            return coord;
        }

        sealed class GoldContributor : ISaveContributor
        {
            readonly GameManager _gm;

            public GoldContributor(GameManager gm) => _gm = gm;

            public void ApplySave(SaveInfo save)
            {
                if (_gm != null) _gm.PlayerGold = save.Gold;
            }

            public void CaptureSave(SaveInfo save)
            {
                if (_gm != null) save.Gold = _gm.PlayerGold;
            }
        }

        sealed class InventoryContributor : ISaveContributor
        {
            readonly InventorySystem _inv;

            public InventoryContributor(InventorySystem inv) => _inv = inv;

            public void ApplySave(SaveInfo save) => _inv?.LoadFromSave(save.Inventory);

            public void CaptureSave(SaveInfo save) =>
                save.Inventory = _inv?.ToSaveDto() ?? new List<ItemStackDto>();
        }

        sealed class LoadoutContributor : ISaveContributor
        {
            readonly LoadoutSystem _loadout;

            public LoadoutContributor(LoadoutSystem loadout) => _loadout = loadout;

            public void ApplySave(SaveInfo save)
            {
                if (_loadout != null && save != null)
                    _loadout.ApplyFromDto(save.Loadout);
            }

            public void CaptureSave(SaveInfo save)
            {
                if (_loadout != null && save != null)
                    save.Loadout = _loadout.ToDto();
            }
        }

        sealed class QuestContributor : ISaveContributor
        {
            readonly QuestManager _quests;

            public QuestContributor(QuestManager quests) => _quests = quests;

            public void ApplySave(SaveInfo save) =>
                _quests?.LoadFromSave(save.ActiveQuestIds, save.CompletedQuestIds, save.ActiveQuestProgress);

            public void CaptureSave(SaveInfo save)
            {
                save.ActiveQuestIds = _quests?.GetActiveIds() ?? new List<string>();
                save.CompletedQuestIds = _quests?.GetCompletedIds() ?? new List<string>();
                save.ActiveQuestProgress = _quests?.ExportQuestProgress() ?? new List<QuestSaveEntry>();
            }
        }

        sealed class WorldContributor : ISaveContributor
        {
            readonly WorldManager _world;

            public WorldContributor(WorldManager world) => _world = world;

            public void ApplySave(SaveInfo save)
            {
                _world?.SetCurrentArea(save.CurrentAreaId);
                _world?.SetCurrentPlayerPosition(new UnityEngine.Vector2(save.PlayerPositionX, save.PlayerPositionY));
                _world?.ApplyDiscoveredAreaIds(save.DiscoveredAreaIds);
            }

            public void CaptureSave(SaveInfo save)
            {
                save.CurrentAreaId = _world?.CurrentAreaId ?? "town";
                if (_world != null)
                {
                    save.PlayerPositionX = _world.CurrentPlayerPosition.x;
                    save.PlayerPositionY = _world.CurrentPlayerPosition.y;
                }
                save.DiscoveredAreaIds = _world?.GetDiscoveredAreaIds() ?? new List<string> { save.CurrentAreaId };
            }
        }

        sealed class AchievementContributor : ISaveContributor
        {
            readonly AchievementSystem _ach;

            public AchievementContributor(AchievementSystem ach) => _ach = ach;

            public void ApplySave(SaveInfo save) => _ach?.LoadFromSave(save.UnlockedAchievementIds);

            public void CaptureSave(SaveInfo save) =>
                save.UnlockedAchievementIds = _ach?.GetUnlockedIds() ?? new List<string>();
        }

        sealed class PartyContributor : ISaveContributor
        {
            readonly MonsterSystem _monsters;
            readonly AssetRegistryManager _registry;

            public PartyContributor(MonsterSystem monsters, AssetRegistryManager registry)
            {
                _monsters = monsters;
                _registry = registry;
            }

            public void ApplySave(SaveInfo save)
            {
                _monsters?.LoadPartyFromSave(save.PartyMonsterIds, save.Party, _registry);
                _monsters?.LoadReserveFromSave(save.Reserve, _registry);
            }

            public void CaptureSave(SaveInfo save)
            {
                save.PartyMonsterIds = _monsters?.GetPartySaveIds() ?? new List<string>();
                save.Party = _monsters?.ExportPartySave() ?? new List<MonsterSaveEntry>();
                save.Reserve = _monsters?.ExportReserveSave() ?? new List<MonsterSaveEntry>();
            }
        }

        sealed class WeatherContributor : ISaveContributor
        {
            readonly WeatherSystem _weather;

            public WeatherContributor(WeatherSystem weather) => _weather = weather;

            public void ApplySave(SaveInfo save) => _weather?.ApplySave(save.Weather);

            public void CaptureSave(SaveInfo save) =>
                save.Weather = _weather?.ToSaveDto() ?? new WeatherTypeDto();
        }

        sealed class NpcMemoryContributor : ISaveContributor
        {
            readonly NpcMemoryService _svc;

            public NpcMemoryContributor(NpcMemoryService svc) => _svc = svc;

            public void ApplySave(SaveInfo save) => _svc?.ApplySave(save.NpcMemories);

            public void CaptureSave(SaveInfo save) =>
                save.NpcMemories = _svc?.ExportSave() ?? new List<NpcMemorySaveEntry>();
        }

        sealed class StoryFlagsContributor : ISaveContributor
        {
            public void ApplySave(SaveInfo save) => StoryFlags.ApplySave(save.StoryFlags);

            public void CaptureSave(SaveInfo save) => save.StoryFlags = StoryFlags.ExportSave();
        }
    }
}
