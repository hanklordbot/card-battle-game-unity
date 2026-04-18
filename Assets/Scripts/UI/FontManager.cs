using UnityEngine;
using UnityEngine.UI;

namespace CardBattle.UI
{
    /// <summary>
    /// Loads a CJK-capable system font at runtime and applies it to all Text components.
    /// Attach to a GameObject in the scene (runs before other UI scripts via execution order).
    /// </summary>
    public class FontManager : MonoBehaviour
    {
        public static Font CJKFont { get; private set; }

        private static readonly string[] CandidateFonts = new[]
        {
            // Windows
            "Microsoft JhengHei",  // 微軟正黑體 (Traditional Chinese)
            "Microsoft YaHei",     // 微軟雅黑 (Simplified Chinese)
            "Meiryo",
            // macOS
            "PingFang TC",
            "PingFang SC",
            "Hiragino Sans",
            // Linux
            "Noto Sans CJK TC",
            "Noto Sans CJK SC",
            "WenQuanYi Micro Hei",
            // Fallback
            "Arial Unicode MS",
            "Arial"
        };

        private void Awake()
        {
            if (CJKFont != null) return;

            string[] osfonts = Font.GetOSInstalledFontNames();
            var osFontSet = new System.Collections.Generic.HashSet<string>(osfonts);

            foreach (var name in CandidateFonts)
            {
                if (osFontSet.Contains(name))
                {
                    CJKFont = Font.CreateDynamicFontFromOSFont(name, 16);
                    Debug.Log($"[FontManager] Loaded CJK font: {name}");
                    break;
                }
            }

            if (CJKFont == null)
            {
                Debug.LogWarning("[FontManager] No CJK font found, using default");
                return;
            }

            // Apply to all existing Text components in the scene
            foreach (var text in FindObjectsByType<Text>(FindObjectsSortMode.None))
            {
                text.font = CJKFont;
            }
        }
    }
}
