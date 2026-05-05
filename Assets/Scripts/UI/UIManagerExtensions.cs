using LoreLegacyMonsters;

namespace LoreLegacyMonsters.UI
{
    public static class UIManagerExtensions
    {
        public static bool HasCanvas(this UIManager ui) => ui != null && ui.Root != null;
    }
}
