using LoreLegacyMonsters.Monster;
using UnityEngine;

namespace LoreLegacyMonsters.Inventory
{
    [CreateAssetMenu(menuName = "LLM/Consumable", fileName = "Consumable")]
    public class ConsumableData : ItemData
    {
        [SerializeField] int healAmount;
        [SerializeField] EffectType effect = EffectType.Heal;
        [Tooltip("For CureStatus: which status to remove. Use None to clear any status on the active monster.")]
        [SerializeField] MonsterStatusEffect cureTarget = MonsterStatusEffect.None;

        public int HealAmount => healAmount;
        public EffectType Effect => effect;
        public MonsterStatusEffect CureTarget => cureTarget;

        public void ConfigureConsumable(string id, string name, int heal, EffectType fx = EffectType.Heal,
            MonsterStatusEffect cure = MonsterStatusEffect.None)
        {
            Configure(id, name, ItemType.Consumable);
            healAmount = heal;
            effect = fx;
            cureTarget = cure;
        }
    }
}

