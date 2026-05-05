using UnityEngine;

namespace LoreLegacyMonsters.Core
{
    /// <summary>Tester-facing hints for logs and diagnostics (standalone builds have no Unity Console).</summary>
    public static class AlphaDiagnostics
    {
        /// <summary>Typical Player.log path on Windows standalone.</summary>
        public static string WindowsPlayerLogHint()
        {
            var company = string.IsNullOrWhiteSpace(Application.companyName) ? "DefaultCompany" : Application.companyName;
            var product = string.IsNullOrWhiteSpace(Application.productName) ? "LoreLegacyMonsters" : Application.productName;
            return $"%USERPROFILE%\\AppData\\LocalLow\\{company}\\{product}\\Player.log";
        }

        public static string FormatTesterDiagnosticsBlock()
        {
            return "Diagnostics\n" +
                   $"- Product: {Application.productName}\n" +
                   $"- Version: {Application.version}\n" +
                   $"- Platform: {Application.platform}\n" +
                   "- Windows Player.log (typical): " + WindowsPlayerLogHint() + "\n" +
                   "- JSON saves folder: " + Application.persistentDataPath + "\\Saves";
        }
    }
}
