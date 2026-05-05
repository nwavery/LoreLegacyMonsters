namespace LoreLegacyMonsters
{
    /// <summary>Partial extensions; must stay in <see cref="LoreLegacyMonsters"/> namespace (not LoreLegacyMonsters.Item) to avoid clashing with the <see cref="Item"/> type.</summary>
    public static partial class ItemExtensions
    {
        public static int TotalQuantity(this Item i) => i?.quantity ?? 0;
    }
}
