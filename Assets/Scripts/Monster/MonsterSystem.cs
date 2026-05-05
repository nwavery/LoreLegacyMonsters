using System.Collections.Generic;
using UnityEngine;
using LoreLegacyMonsters;
using LoreLegacyMonsters.Inventory;
using LoreLegacyMonsters.SaveSystem;
using LoreLegacyMonsters.Core;
using LoreLegacyMonsters.Achievements;

namespace LoreLegacyMonsters.Monster
{
    public class MonsterSystem : MonoBehaviour
    {
        [SerializeField] QuestManager questManager;
        [SerializeField] AchievementSystem achievementSystem;

        [SerializeField] List<MonsterInstance> party = new List<MonsterInstance>();
        [SerializeField] List<MonsterInstance> reserve = new List<MonsterInstance>();
        [SerializeField] int maxPartySize = 4;
        [SerializeField] int activeIndex;

        public IReadOnlyList<MonsterInstance> Party => party;
        public IReadOnlyList<MonsterInstance> Reserve => reserve;
        public int ActiveIndex => Mathf.Clamp(activeIndex, 0, Mathf.Max(0, party.Count - 1));

        void Awake() => ResolveQuestLinks();

        void ResolveQuestLinks()
        {
            questManager ??= FindFirstObjectByType<QuestManager>();
            achievementSystem ??= FindFirstObjectByType<AchievementSystem>();
        }

        public void LoadPartyFromSave(List<string> ids, List<MonsterSaveEntry> savedParty = null,
            AssetRegistryManager registry = null)
        {
            party = new List<MonsterInstance>();
            reserve = new List<MonsterInstance>();
            if (savedParty != null && savedParty.Count > 0)
            {
                LoadEntriesInto(party, savedParty, registry);
            }
            else if (ids != null)
            {
                foreach (var id in ids)
                {
                    if (string.IsNullOrEmpty(id)) continue;
                    var data = registry != null ? registry.GetMonster(id) : null;
                    party.Add(new MonsterInstance(id, data));
                }
            }

            activeIndex = Mathf.Clamp(activeIndex, 0, Mathf.Max(0, party.Count - 1));
        }

        public void LoadReserveFromSave(List<MonsterSaveEntry> savedReserve, AssetRegistryManager registry = null)
        {
            reserve = new List<MonsterInstance>();
            if (savedReserve != null && savedReserve.Count > 0)
                LoadEntriesInto(reserve, savedReserve, registry);
        }

        public List<string> GetPartySaveIds()
        {
            var ids = new List<string>();
            foreach (var m in party)
                if (m != null && !string.IsNullOrEmpty(m.monsterDataId))
                    ids.Add(m.monsterDataId);
            return ids;
        }

        public List<MonsterSaveEntry> ExportPartySave()
        {
            return ExportList(party);
        }

        public List<MonsterSaveEntry> ExportReserveSave() => ExportList(reserve);

        public void SetParty(List<string> ids) => LoadPartyFromSave(ids);

        public void EnsureStarterParty(AssetRegistryManager registry, string starterMonsterId)
        {
            if (party.Count > 0 || registry == null || string.IsNullOrEmpty(starterMonsterId)) return;
            var data = registry.GetMonster(starterMonsterId);
            if (data != null)
                party.Add(new MonsterInstance(starterMonsterId, data));
        }

        public MonsterInstance GetActiveMonster() =>
            party.Count == 0 ? null : party[Mathf.Clamp(activeIndex, 0, party.Count - 1)];

        public MonsterData GetActiveMonsterData(AssetRegistryManager registry)
        {
            var active = GetActiveMonster();
            return active != null && registry != null ? registry.GetMonster(active.monsterDataId) : null;
        }

        public bool SetActiveIndex(int index)
        {
            if (index < 0 || index >= party.Count) return false;
            activeIndex = index;
            return true;
        }

        public bool SwitchToNextHealthy(AssetRegistryManager registry)
        {
            if (party.Count <= 1) return false;
            for (var offset = 1; offset < party.Count; offset++)
            {
                var i = (ActiveIndex + offset) % party.Count;
                var m = party[i];
                if (m == null) continue;
                var data = registry != null ? registry.GetMonster(m.monsterDataId) : null;
                m.SyncStats(data);
                if (m.currentHp > 0)
                {
                    activeIndex = i;
                    return true;
                }
            }

            return false;
        }

        public MonsterInstance AddMonster(MonsterData data, int startingLevel = 1, string nickname = null)
        {
            if (data == null || string.IsNullOrEmpty(data.MonsterId)) return null;
            var instance = new MonsterInstance(data.MonsterId, data, startingLevel, nickname);
            instance.RefreshLearnedMoves(data);
            if (party.Count < maxPartySize) party.Add(instance);
            else reserve.Add(instance);
            return instance;
        }

        public bool GrantExperienceToActive(AssetRegistryManager registry, int amount)
        {
            var active = GetActiveMonster();
            var data = GetActiveMonsterData(registry);
            var leveled = active != null && data != null && active.AwardExperience(data, amount);
            if (leveled)
                GameEvents.RaiseMonsterLeveled(active.instanceId);
            return leveled;
        }

        public void HealAll(AssetRegistryManager registry)
        {
            foreach (var m in party)
            {
                if (m == null) continue;
                m.SyncStats(registry != null ? registry.GetMonster(m.monsterDataId) : null, true);
                m.persistentStatus = MonsterStatusEffect.None;
            }
        }

        /// <summary>Clears persistent status on the active party member when it matches <paramref name="target"/>, or any status when <paramref name="target"/> is None.</summary>
        public bool ClearStatusOnActive(MonsterStatusEffect target)
        {
            var active = GetActiveMonster();
            if (active == null) return false;
            if (active.persistentStatus == MonsterStatusEffect.None) return false;
            if (target != MonsterStatusEffect.None && active.persistentStatus != target) return false;
            active.persistentStatus = MonsterStatusEffect.None;
            return true;
        }

        public int ClearAllStatuses()
        {
            var n = 0;
            foreach (var m in party)
            {
                if (m == null || m.persistentStatus == MonsterStatusEffect.None) continue;
                m.persistentStatus = MonsterStatusEffect.None;
                n++;
            }

            return n;
        }

        /// <summary>Applies a consumable to the active monster (overworld). Returns false if the item cannot be used (wrong cure, etc.).</summary>
        public bool TryApplyConsumableFromInventory(string itemId, AssetRegistryManager registry, InventorySystem inventory)
        {
            if (string.IsNullOrEmpty(itemId) || inventory == null || registry == null) return false;
            if (inventory.Count(itemId) <= 0) return false;
            var item = registry.GetItem(itemId) as ConsumableData;
            if (item == null) return false;

            var active = GetActiveMonster();
            if (active == null) return false;

            switch (item.Effect)
            {
                case EffectType.Heal:
                    if (!inventory.TryRemove(itemId, 1)) return false;
                    var data = registry.GetMonster(active.monsterDataId);
                    active.SyncStats(data, false);
                    active.currentHp = Mathf.Min(active.maxHp, active.currentHp + Mathf.Max(0, item.HealAmount));
                    return true;
                case EffectType.CureStatus:
                    var ct = item.CureTarget;
                    if (ct == MonsterStatusEffect.None)
                    {
                        if (active.persistentStatus == MonsterStatusEffect.None) return false;
                        if (!inventory.TryRemove(itemId, 1)) return false;
                        active.persistentStatus = MonsterStatusEffect.None;
                        return true;
                    }

                    if (active.persistentStatus != ct) return false;
                    if (!inventory.TryRemove(itemId, 1)) return false;
                    active.persistentStatus = MonsterStatusEffect.None;
                    return true;
                default:
                    return false;
            }
        }

        public string GetPartySummary(AssetRegistryManager registry)
        {
            if (party.Count == 0) return "No monsters";
            var parts = new List<string>();
            for (var i = 0; i < party.Count; i++)
            {
                var m = party[i];
                if (m == null) continue;
                var data = registry != null ? registry.GetMonster(m.monsterDataId) : null;
                var statusText = m.persistentStatus != MonsterStatusEffect.None ? $" [{m.persistentStatus}]" : string.Empty;
                parts.Add($"{(i == ActiveIndex ? "*" : "-")} {m.GetDisplayName(data)} Lv{m.level} HP {m.currentHp}/{m.maxHp}{statusText}");
            }
            return string.Join("\n", parts);
        }

        public bool RenameMonster(int partyIndex, string newName)
        {
            if (partyIndex < 0 || partyIndex >= party.Count) return false;
            party[partyIndex].nickname = string.IsNullOrWhiteSpace(newName) ? null : newName.Trim();
            return true;
        }

        public bool ReorderParty(int fromIndex, int toIndex)
        {
            if (fromIndex < 0 || fromIndex >= party.Count || toIndex < 0 || toIndex >= party.Count) return false;
            if (fromIndex == toIndex) return true;
            var item = party[fromIndex];
            party.RemoveAt(fromIndex);
            party.Insert(toIndex, item);
            activeIndex = Mathf.Clamp(activeIndex, 0, Mathf.Max(0, party.Count - 1));
            return true;
        }

        public bool MoveReserveToParty(int reserveIndex)
        {
            if (reserveIndex < 0 || reserveIndex >= reserve.Count || party.Count >= maxPartySize) return false;
            var item = reserve[reserveIndex];
            reserve.RemoveAt(reserveIndex);
            party.Add(item);
            return true;
        }

        public bool MovePartyToReserve(int partyIndex)
        {
            if (partyIndex < 0 || partyIndex >= party.Count || party.Count <= 1) return false;
            var item = party[partyIndex];
            party.RemoveAt(partyIndex);
            reserve.Add(item);
            if (activeIndex >= party.Count) activeIndex = Mathf.Max(0, party.Count - 1);
            return true;
        }

        public int CountOwnedByMonsterId(string monsterDataId)
        {
            if (string.IsNullOrWhiteSpace(monsterDataId))
                return 0;
            var count = 0;
            for (var i = 0; i < party.Count; i++)
                if (party[i] != null && party[i].monsterDataId == monsterDataId)
                    count++;
            for (var i = 0; i < reserve.Count; i++)
                if (reserve[i] != null && reserve[i].monsterDataId == monsterDataId)
                    count++;
            return count;
        }

        public bool TryEvolve(int partyIndex, AssetRegistryManager registry, string itemId = null)
        {
            if (partyIndex < 0 || partyIndex >= party.Count || registry == null) return false;
            var monster = party[partyIndex];
            var currentData = registry.GetMonster(monster.monsterDataId);
            if (monster == null || currentData == null || !CanEvolve(monster, currentData, itemId)) return false;

            var nextData = registry.GetMonster(currentData.Evolution.targetMonsterId);
            if (nextData == null) return false;

            var hpPercent = monster.maxHp > 0 ? (float)monster.currentHp / monster.maxHp : 1f;
            monster.monsterDataId = nextData.MonsterId;
            monster.SyncStats(nextData, true);
            monster.currentHp = Mathf.Clamp(Mathf.RoundToInt(monster.maxHp * hpPercent), 1, monster.maxHp);
            monster.RefreshLearnedMoves(nextData);
            GameEvents.RaiseMonsterEvolved(monster.instanceId);
            ResolveQuestLinks();
            questManager?.ReportObjectiveEvent("evolve_monster", 1);
            questManager?.ReportObjectiveEvent(PhaseTwoIds.LumaEvolveMonsters, 1);
            achievementSystem?.Unlock(SampleAchievements.FirstEvolution);
            return true;
        }

        public bool CanEvolve(MonsterInstance monster, MonsterData data, string itemId = null)
        {
            if (monster == null || data == null || data.Evolution == null || !data.Evolution.HasEvolution)
                return false;

            return data.Evolution.method switch
            {
                EvolutionMethod.Level => monster.level >= Mathf.Max(1, data.Evolution.requiredLevel),
                EvolutionMethod.Item => !string.IsNullOrWhiteSpace(itemId) && itemId == data.Evolution.requiredItemId,
                _ => false
            };
        }

        static List<MonsterSaveEntry> ExportList(List<MonsterInstance> source)
        {
            var list = new List<MonsterSaveEntry>();
            foreach (var m in source)
            {
                if (m == null || string.IsNullOrEmpty(m.monsterDataId)) continue;
                list.Add(new MonsterSaveEntry
                {
                    instanceId = m.instanceId,
                    monsterDataId = m.monsterDataId,
                    nickname = m.nickname,
                    level = m.level,
                    experience = m.experience,
                    currentHp = m.currentHp,
                    status = (int)m.persistentStatus,
                    learnedMoveIds = m.learnedMoveIds != null ? new List<string>(m.learnedMoveIds) : new List<string>()
                });
            }

            return list;
        }

        static void LoadEntriesInto(List<MonsterInstance> target, List<MonsterSaveEntry> source, AssetRegistryManager registry)
        {
            foreach (var entry in source)
            {
                if (entry == null || string.IsNullOrEmpty(entry.monsterDataId)) continue;
                var data = registry != null ? registry.GetMonster(entry.monsterDataId) : null;
                var instance = new MonsterInstance
                {
                    instanceId = string.IsNullOrEmpty(entry.instanceId) ? System.Guid.NewGuid().ToString("N") : entry.instanceId,
                    monsterDataId = entry.monsterDataId,
                    nickname = entry.nickname,
                    level = Mathf.Max(1, entry.level),
                    experience = Mathf.Max(0, entry.experience),
                    currentHp = entry.currentHp,
                    persistentStatus = (MonsterStatusEffect)Mathf.Clamp(entry.status, 0, (int)MonsterStatusEffect.GuardBreak),
                    learnedMoveIds = entry.learnedMoveIds != null ? new List<string>(entry.learnedMoveIds) : new List<string>()
                };
                instance.SyncStats(data, entry.currentHp <= 0);
                instance.RefreshLearnedMoves(data);
                target.Add(instance);
            }
        }
    }
}
