using UnityEngine;
using UnityEngine.UI;
using CardBattle.Core;
using CardBattle.Game;

namespace CardBattle.UI
{
    public class DebugPanel : MonoBehaviour
    {
        private GameObject _panel;
        private Text _text;
        private bool _visible;

        private void Start()
        {
            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            _panel = new GameObject("DebugPanel");
            _panel.transform.SetParent(canvas.transform, false);
            var rt = _panel.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0.5f);
            rt.anchorMax = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(120, 0);
            rt.sizeDelta = new Vector2(220, 0);

            var bg = _panel.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.6f);
            bg.raycastTarget = false;

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(_panel.transform, false);
            var trt = textGo.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(8, 8);
            trt.offsetMax = new Vector2(-8, -8);

            _text = textGo.AddComponent<Text>();
            _text.fontSize = 13;
            _text.color = Color.white;
            _text.alignment = TextAnchor.UpperLeft;
            _text.raycastTarget = false;
            if (FontManager.CJKFont != null) _text.font = FontManager.CJKFont;
            else _text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            _panel.SetActive(false);
            _visible = false;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F12) || Input.GetKeyDown(KeyCode.BackQuote))
            {
                _visible = !_visible;
                _panel?.SetActive(_visible);
            }

            if (_visible && _text != null)
                _text.text = BuildDebugText();
        }

        private string BuildDebugText()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.DuelState == null) return "No DuelState";
            var s = gm.DuelState;
            var p0 = s.players[0];
            var p1 = s.players[1];

            int p0m = 0, p1m = 0;
            for (int i = 0; i < DuelConstants.MONSTER_ZONE_SIZE; i++)
            {
                if (p0.monsterZone[i] != null) p0m++;
                if (p1.monsterZone[i] != null) p1m++;
            }

            return $"[DEBUG]\n" +
                   $"回合: {s.turnCount} ({(s.turnPlayer == 0 ? "玩家" : "AI")})\n" +
                   $"階段: {s.phase}\n" +
                   $"結果: {s.result}\n" +
                   $"---\n" +
                   $"玩家 LP: {p0.lp}\n" +
                   $"手牌: {p0.hand.Count} 牌組: {p0.deck.Count}\n" +
                   $"場上怪獸: {p0m}\n" +
                   $"---\n" +
                   $"AI LP: {p1.lp}\n" +
                   $"手牌: {p1.hand.Count} 牌組: {p1.deck.Count}\n" +
                   $"場上怪獸: {p1m}";
        }
    }
}
