using System;
using System.Collections.Generic;
using UnityEngine;

namespace LoreLegacyMonsters.Monster
{
    [Serializable]
    public class MonsterInstance
    {
        public string instanceId;
        public string monsterDataId;
        public string nickname;
        public int level = 1;
        public int experience;
        public int currentHp;
        public int maxHp;
        public MonsterStatusEffect persistentStatus;
        public List<string> learnedMoveIds = new List<string>();

        public MonsterInstance() { }

        public MonsterInstance(string id, MonsterData data, int startingLevel = 1, string customName = null)
        {
            instanceId = Guid.NewGuid().ToString("N");
            monsterDataId = id;
            nickname = customName;
            level = Mathf.Max(1, startingLevel);
            persistentStatus = MonsterStatusEffect.None;
            RefreshLearnedMoves(data);
            SyncStats(data, true);
        }

        public string GetDisplayName(MonsterData data) =>
            !string.IsNullOrWhiteSpace(nickname) ? nickname :
            data != null && !string.IsNullOrWhiteSpace(data.DisplayName) ? data.DisplayName :
            "Monster";

        public int RequiredExperienceForNextLevel => 20 + Mathf.Max(0, level - 1) * 15;

        public int GetAttackStat(MonsterData data)
        {
            if (data == null) return 4;
            return Mathf.Max(1, data.BaseAttack + GetBiasBonus(data.GrowthBias, GrowthBias.AttackHeavy, 2) + (level - 1));
        }

        public int GetDefenseStat(MonsterData data)
        {
            if (data == null) return 3;
            return Mathf.Max(1, data.BaseDefense + GetBiasBonus(data.GrowthBias, GrowthBias.DefenseHeavy, 2) + (level - 1) / 2);
        }

        public int GetSpeedStat(MonsterData data)
        {
            if (data == null) return 4;
            return Mathf.Max(1, data.BaseSpeed + GetBiasBonus(data.GrowthBias, GrowthBias.SpeedHeavy, 3) + (level - 1) / 2);
        }

        public void SyncStats(MonsterData data, bool healToFull = false)
        {
            if (data == null) return;
            maxHp = data.BaseHp + (level - 1) * 3 + GetBiasBonus(data.GrowthBias, GrowthBias.HpHeavy, 4);
            if (healToFull || currentHp <= 0)
                currentHp = healToFull ? maxHp : Mathf.Clamp(currentHp, 0, maxHp);
            else
                currentHp = Mathf.Clamp(currentHp, 0, maxHp);
            RefreshLearnedMoves(data);
        }

        public bool AwardExperience(MonsterData data, int amount)
        {
            if (amount <= 0 || data == null) return false;
            experience += amount;
            var leveled = false;
            while (experience >= RequiredExperienceForNextLevel)
            {
                experience -= RequiredExperienceForNextLevel;
                level++;
                leveled = true;
            }

            if (leveled)
            {
                SyncStats(data, true);
                RefreshLearnedMoves(data);
            }
            return leveled;
        }

        public IReadOnlyList<string> GetAvailableMoveIds(MonsterData data)
        {
            RefreshLearnedMoves(data);
            return learnedMoveIds;
        }

        public void RefreshLearnedMoves(MonsterData data)
        {
            learnedMoveIds ??= new List<string>();
            if (data == null) return;

            if (!string.IsNullOrWhiteSpace(data.DefaultMoveId) && !learnedMoveIds.Contains(data.DefaultMoveId))
                learnedMoveIds.Add(data.DefaultMoveId);

            if (data.MoveLearnset != null)
            {
                foreach (var entry in data.MoveLearnset)
                {
                    if (entry == null || string.IsNullOrWhiteSpace(entry.moveId)) continue;
                    if (level >= Mathf.Max(1, entry.unlockLevel) && !learnedMoveIds.Contains(entry.moveId))
                        learnedMoveIds.Add(entry.moveId);
                }
            }

            if (!string.IsNullOrWhiteSpace(data.SignatureMoveId) && level >= 3 && !learnedMoveIds.Contains(data.SignatureMoveId))
                learnedMoveIds.Add(data.SignatureMoveId);
        }

        int GetBiasBonus(GrowthBias actual, GrowthBias target, int amount) =>
            actual == target ? amount : actual == GrowthBias.Balanced ? amount / 2 : 0;
    }
}
