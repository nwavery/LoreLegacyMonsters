using System.Collections.Generic;
using LoreLegacyMonsters.Monster;
using UnityEngine;

namespace LoreLegacyMonsters.Inventory
{
    [CreateAssetMenu(menuName = "LLM/Gear Item", fileName = "GearItem")]
    public class GearItemData : ItemData
    {
        [SerializeField] GearSlot gearSlot = GearSlot.Charm;
        [SerializeField] Rarity rarity = Rarity.Common;
        [SerializeField] List<GearEffect> effects = new List<GearEffect>();
        [SerializeField] List<string> vibeTags = new List<string>();
        [TextArea] [SerializeField] string flavorText;
        [SerializeField] string cosmeticSpriteName;
        [SerializeField] Color auraColor = new Color(0.6f, 0.7f, 0.95f, 0.55f);

        public GearSlot Slot => gearSlot;
        public Rarity Rarity => rarity;
        public IReadOnlyList<GearEffect> Effects => effects;
        public IReadOnlyList<string> VibeTags => vibeTags;
        public string FlavorText => flavorText;
        public string CosmeticSpriteName => cosmeticSpriteName;
        public Color AuraColor => auraColor;

        public void ConfigureGear(string id, string name, GearSlot slot, Rarity r,
            IReadOnlyList<GearEffect> fx, IReadOnlyList<string> tags = null, string flavor = null,
            string spriteName = null, Color? aura = null)
        {
            Configure(id, name, ItemType.Equipment);
            gearSlot = slot;
            rarity = r;
            effects.Clear();
            if (fx != null)
                effects.AddRange(fx);
            vibeTags.Clear();
            if (tags != null)
                vibeTags.AddRange(tags);
            flavorText = flavor ?? string.Empty;
            cosmeticSpriteName = spriteName ?? string.Empty;
            if (aura.HasValue) auraColor = aura.Value;
        }
    }
}
