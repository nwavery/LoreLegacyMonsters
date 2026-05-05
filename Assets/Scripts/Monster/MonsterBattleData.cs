using System;
using System.Collections.Generic;

namespace LoreLegacyMonsters.Monster
{
    public enum MonsterRole
    {
        Striker,
        Tank,
        Support,
        Trickster
    }

    public enum MonsterElement
    {
        None,
        Neutral,
        Fire,
        Water,
        Nature,
        Lightning,
        Stone,
        Shadow
    }

    public enum GrowthBias
    {
        Balanced,
        HpHeavy,
        AttackHeavy,
        DefenseHeavy,
        SpeedHeavy
    }

    public enum MonsterStatusEffect
    {
        None,
        Burn,
        Poison,
        Shock,
        GuardBreak
    }

    public enum EvolutionMethod
    {
        None,
        Level,
        Item
    }

    [Serializable]
    public class MonsterMoveLearnEntry
    {
        public string moveId;
        public int unlockLevel = 1;
    }

    [Serializable]
    public class MonsterEvolutionRule
    {
        public EvolutionMethod method;
        public string targetMonsterId;
        public int requiredLevel = 1;
        public string requiredItemId;

        public bool HasEvolution =>
            method != EvolutionMethod.None && !string.IsNullOrWhiteSpace(targetMonsterId);
    }
}
