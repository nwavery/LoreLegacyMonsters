using System;
using LoreLegacyMonsters.Monster;
using UnityEngine;

namespace LoreLegacyMonsters.Inventory
{
    /// <summary>Passive effect on a gear item; aggregated across outfit + charms.</summary>
    public enum GearEffectKind
    {
        MoveSpeedMult,
        EncounterRateMult,
        /// <summary>Magnitude biases encounter pool toward <see cref="GearEffect.RelatedElement"/> (use with EncounterRateMult).</summary>
        EncounterTypeBias,
        MonsterAggressionMult,
        /// <summary>Additive to capture probability (e.g. 0.03f = +3%).</summary>
        CaptureRateBonus,
        TypeDamageMult,
        StatusResistMult,
        GoldGainMult,
        XpGainMult,
        /// <summary>Additive weight for player first strike.</summary>
        InitiativeBonus,
        /// <summary>Multiplier on rare gear drop roll chance.</summary>
        LuckMult
    }

    [Serializable]
    public struct GearEffect
    {
        [SerializeField] GearEffectKind kind;
        [SerializeField] float magnitude;
        [SerializeField] MonsterElement relatedElement;
        [SerializeField] MonsterStatusEffect relatedStatus;

        public GearEffectKind Kind => kind;
        public float Magnitude => magnitude;
        public MonsterElement RelatedElement => relatedElement;
        public MonsterStatusEffect RelatedStatus => relatedStatus;

        public GearEffect(GearEffectKind k, float mag, MonsterElement el = MonsterElement.None,
            MonsterStatusEffect st = MonsterStatusEffect.None)
        {
            kind = k;
            magnitude = mag;
            relatedElement = el;
            relatedStatus = st;
        }
    }
}
