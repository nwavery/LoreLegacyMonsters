using System;
using System.Collections.Generic;
using LoreLegacyMonsters.Monster;
using UnityEngine;

namespace LoreLegacyMonsters.Inventory
{
    /// <summary>
    /// Aggregated modifier snapshot from outfit + charms. Multipliers compose multiplicatively; bonuses additively where noted.
    /// </summary>
    public sealed class LoadoutModifiers
    {
        public float MoveSpeedMult { get; }
        public float EncounterRateMult { get; }
        public float MonsterAggressionMult { get; }
        public float CaptureRateBonus { get; }
        public float GoldGainMult { get; }
        public float XpGainMult { get; }
        public float LuckMult { get; }
        public float InitiativeBonus { get; }
        public float GlobalStatusResistMult { get; }
        public IReadOnlyDictionary<MonsterElement, float> TypeDamageMults { get; }
        public IReadOnlyDictionary<MonsterElement, float> EncounterElementBias { get; }
        public IReadOnlyDictionary<MonsterStatusEffect, float> StatusResistMults { get; }
        public Color AuraTint { get; }
        public IReadOnlyList<string> VibeTags { get; }

        LoadoutModifiers(float move, float enc, float agg, float cap, float gold, float xp, float luck, float init,
            float globalResist, Dictionary<MonsterElement, float> typeMults,
            Dictionary<MonsterElement, float> encBias, Dictionary<MonsterStatusEffect, float> statusResist, Color aura,
            List<string> vibeTags)
        {
            MoveSpeedMult = move;
            EncounterRateMult = enc;
            MonsterAggressionMult = agg;
            CaptureRateBonus = cap;
            GoldGainMult = gold;
            XpGainMult = xp;
            LuckMult = luck;
            InitiativeBonus = init;
            GlobalStatusResistMult = globalResist;
            TypeDamageMults = typeMults;
            EncounterElementBias = encBias;
            StatusResistMults = statusResist;
            AuraTint = aura;
            VibeTags = vibeTags;
        }

        public float TypeDamageMult(MonsterElement el) =>
            TypeDamageMults != null && TypeDamageMults.TryGetValue(el, out var m) ? m : 1f;

        public float EncounterBiasFor(MonsterElement el) =>
            EncounterElementBias != null && EncounterElementBias.TryGetValue(el, out var b) ? b : 0f;

        public float StatusResistFor(MonsterStatusEffect st)
        {
            var per = StatusResistMults != null && StatusResistMults.TryGetValue(st, out var m) ? m : 1f;
            return GlobalStatusResistMult * per;
        }

        public static LoadoutModifiers Empty { get; } = new LoadoutModifiers(1f, 1f, 1f, 0f, 1f, 1f, 1f, 0f, 1f,
            new Dictionary<MonsterElement, float>(), new Dictionary<MonsterElement, float>(),
            new Dictionary<MonsterStatusEffect, float>(), new Color(0.5f, 0.55f, 0.65f, 0.35f),
            new List<string>());

        public static LoadoutModifiers FromGearItems(IEnumerable<GearItemData> items)
        {
            if (items == null) return Empty;

            float move = 1f, enc = 1f, agg = 1f, gold = 1f, xp = 1f, luck = 1f;
            float cap = 0f, init = 0f;
            float globalResist = 1f;
            var typeMults = new Dictionary<MonsterElement, float>();
            var encBias = new Dictionary<MonsterElement, float>();
            var statusResist = new Dictionary<MonsterStatusEffect, float>();
            var vibeSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Color auraAcc = Color.clear;
            var auraCount = 0;

            foreach (var gear in items)
            {
                if (gear == null) continue;
                foreach (var t in gear.VibeTags)
                    if (!string.IsNullOrWhiteSpace(t))
                        vibeSet.Add(t.Trim());
                if (gear.AuraColor.a > 0.01f)
                {
                    auraAcc += gear.AuraColor;
                    auraCount++;
                }

                foreach (var fx in gear.Effects)
                    ApplyEffect(fx, ref move, ref enc, ref agg, ref cap, ref gold, ref xp, ref luck, ref init,
                        ref globalResist, typeMults, encBias, statusResist);
            }

            var aura = auraCount > 0
                ? new Color(auraAcc.r / auraCount, auraAcc.g / auraCount, auraAcc.b / auraCount,
                    Mathf.Clamp01(auraAcc.a / auraCount))
                : Empty.AuraTint;

            var vibeList = new List<string>(vibeSet);
            vibeList.Sort(StringComparer.OrdinalIgnoreCase);
            return new LoadoutModifiers(move, enc, agg, cap, gold, xp, luck, init, globalResist, typeMults, encBias,
                statusResist, aura, vibeList);
        }

        static void ApplyEffect(GearEffect fx, ref float move, ref float enc, ref float agg, ref float cap,
            ref float gold, ref float xp, ref float luck, ref float init, ref float globalResist,
            Dictionary<MonsterElement, float> typeMults, Dictionary<MonsterElement, float> encBias,
            Dictionary<MonsterStatusEffect, float> statusResist)
        {
            switch (fx.Kind)
            {
                case GearEffectKind.MoveSpeedMult:
                    move *= fx.Magnitude;
                    break;
                case GearEffectKind.EncounterRateMult:
                    enc *= fx.Magnitude;
                    break;
                case GearEffectKind.EncounterTypeBias:
                    if (fx.RelatedElement != MonsterElement.None && fx.RelatedElement != MonsterElement.Neutral)
                    {
                        if (!encBias.ContainsKey(fx.RelatedElement)) encBias[fx.RelatedElement] = 0f;
                        encBias[fx.RelatedElement] += fx.Magnitude;
                    }

                    break;
                case GearEffectKind.MonsterAggressionMult:
                    agg *= fx.Magnitude;
                    break;
                case GearEffectKind.CaptureRateBonus:
                    cap += fx.Magnitude;
                    break;
                case GearEffectKind.TypeDamageMult:
                    if (fx.RelatedElement != MonsterElement.None && fx.RelatedElement != MonsterElement.Neutral)
                    {
                        if (!typeMults.ContainsKey(fx.RelatedElement)) typeMults[fx.RelatedElement] = 1f;
                        typeMults[fx.RelatedElement] *= fx.Magnitude;
                    }

                    break;
                case GearEffectKind.StatusResistMult:
                    if (fx.RelatedStatus == MonsterStatusEffect.None)
                        globalResist *= fx.Magnitude;
                    else
                    {
                        if (!statusResist.ContainsKey(fx.RelatedStatus)) statusResist[fx.RelatedStatus] = 1f;
                        statusResist[fx.RelatedStatus] *= fx.Magnitude;
                    }

                    break;
                case GearEffectKind.GoldGainMult:
                    gold *= fx.Magnitude;
                    break;
                case GearEffectKind.XpGainMult:
                    xp *= fx.Magnitude;
                    break;
                case GearEffectKind.InitiativeBonus:
                    init += fx.Magnitude;
                    break;
                case GearEffectKind.LuckMult:
                    luck *= fx.Magnitude;
                    break;
            }
        }
    }
}
