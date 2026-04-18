using UnityEngine;
using UnityEngine.UI;
using CardBattle.Core;

namespace CardBattle.UI
{
    /// <summary>
    /// Right-side phase indicator panel. Shows all phases with current highlighted.
    /// </summary>
    public class PhaseIndicator : MonoBehaviour
    {
        private Text[] _labels;
        private Image _panelBg;

        private static readonly string[] PhaseNames = { "抽牌階段", "主要階段1", "戰鬥階段", "主要階段2", "結束階段" };
        private static readonly Phase[] Phases = { Phase.Draw, Phase.Main1, Phase.Battle, Phase.Main2, Phase.End };

        private readonly Color _activeColor = new Color(1f, 0.83f, 0.23f);
        private readonly Color _inactiveColor = new Color(0.45f, 0.45f, 0.5f);
        private readonly Color _pastColor = new Color(0.6f, 0.55f, 0.3f);

        private void Start()
        {
            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            // Panel
            var panel = new GameObject("PhasePanel");
            panel.transform.SetParent(canvas.transform, false);
            var prt = panel.AddComponent<RectTransform>();
            prt.anchorMin = new Vector2(1, 0.3f);
            prt.anchorMax = new Vector2(1, 0.7f);
            prt.anchoredPosition = new Vector2(-80, 0);
            prt.sizeDelta = new Vector2(140, 0);
            _panelBg = panel.AddComponent<Image>();
            _panelBg.color = new Color(0, 0, 0, 0.5f);

            _labels = new Text[PhaseNames.Length];
            float h = 1f / PhaseNames.Length;

            for (int i = 0; i < PhaseNames.Length; i++)
            {
                var go = new GameObject($"Phase_{i}");
                go.transform.SetParent(panel.transform, false);
                var rt = go.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, 1f - (i + 1) * h);
                rt.anchorMax = new Vector2(1, 1f - i * h);
                rt.offsetMin = new Vector2(8, 2);
                rt.offsetMax = new Vector2(-8, -2);

                var txt = go.AddComponent<Text>();
                txt.text = PhaseNames[i];
                txt.fontSize = 16;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.color = _inactiveColor;
                if (FontManager.CJKFont != null) txt.font = FontManager.CJKFont;
                else txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

                _labels[i] = txt;
            }
        }

        public void UpdatePhase(Phase current)
        {
            if (_labels == null) return;
            int currentIdx = System.Array.IndexOf(Phases, current);
            // Standby maps to Draw visually
            if (current == Phase.Standby) currentIdx = 0;

            for (int i = 0; i < _labels.Length; i++)
            {
                if (_labels[i] == null) continue;
                if (i == currentIdx)
                {
                    _labels[i].color = _activeColor;
                    _labels[i].fontStyle = FontStyle.Bold;
                    _labels[i].fontSize = 18;
                }
                else if (i < currentIdx)
                {
                    _labels[i].color = _pastColor;
                    _labels[i].fontStyle = FontStyle.Normal;
                    _labels[i].fontSize = 16;
                }
                else
                {
                    _labels[i].color = _inactiveColor;
                    _labels[i].fontStyle = FontStyle.Normal;
                    _labels[i].fontSize = 16;
                }
            }
        }
    }
}
