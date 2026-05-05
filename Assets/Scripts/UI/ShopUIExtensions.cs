namespace LoreLegacyMonsters.UI
{
    public static class ShopUIExtensions
    {
        public static bool IsOpen(this ShopUI ui) => ui != null && ui.isActiveAndEnabled;
    }
}
