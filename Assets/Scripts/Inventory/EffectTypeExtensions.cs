namespace LoreLegacyMonsters.Inventory
{
    public enum EffectType
    {
        None = 0,
        Heal = 1,
        Damage = 2,
        Buff = 3,
        CureStatus = 4
    }

    public static class EffectTypeExtensions
    {
        public static bool IsBeneficial(this EffectType e) =>
            e == EffectType.Heal || e == EffectType.Buff || e == EffectType.CureStatus;
    }
}
