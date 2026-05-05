using UnityEngine;

namespace LoreLegacyMonsters.Monster
{
    public enum MoveEffectType
    {
        Attack,
        HealSelf,
        Guard,
        ApplyStatus
    }

    [CreateAssetMenu(menuName = "LLM/Move Data", fileName = "MoveData")]
    public class MoveData : ScriptableObject
    {
        [SerializeField] string moveId;
        [SerializeField] string displayName;
        [SerializeField] MonsterElement element = MonsterElement.Neutral;
        [SerializeField] MoveEffectType effectType = MoveEffectType.Attack;
        [SerializeField] int power = 6;
        [SerializeField] float accuracy = 1f;
        [SerializeField] float critChance = 0.1f;
        [SerializeField] int healAmount;
        [SerializeField] int guardBonus;
        [SerializeField] MonsterStatusEffect inflictedStatus = MonsterStatusEffect.None;
        [SerializeField] float statusChance;
        [SerializeField] int priority;

        public string MoveId => moveId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? moveId : displayName;
        public MonsterElement Element => element;
        public MoveEffectType EffectType => effectType;
        public int Power => power;
        public float Accuracy => accuracy;
        public float CritChance => critChance;
        public int HealAmount => healAmount;
        public int GuardBonus => guardBonus;
        public MonsterStatusEffect InflictedStatus => inflictedStatus;
        public float StatusChance => statusChance;
        public int Priority => priority;

        public void Configure(string id, string title, MonsterElement type, MoveEffectType effect, int basePower = 6, float critChanceValue = 0.1f)
        {
            moveId = id;
            displayName = title;
            element = type;
            effectType = effect;
            power = basePower;
            accuracy = 1f;
            critChance = critChanceValue;
        }
    }
}
