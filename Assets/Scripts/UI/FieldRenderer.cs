using UnityEngine;
using UnityEngine.UI;
using CardBattle.Core;

namespace CardBattle.UI
{
    /// <summary>
    /// Renders monster/spell zones as UI elements on the Canvas.
    /// Auto-creates zone panels in Start() if not assigned.
    /// </summary>
    public class FieldRenderer : MonoBehaviour
    {
        [SerializeField] private RectTransform playerMonsterZone;
        [SerializeField] private RectTransform opponentMonsterZone;

        private GameObject[] _playerSlots = new GameObject[DuelConstants.MONSTER_ZONE_SIZE];
        private GameObject[] _opponentSlots = new GameObject[DuelConstants.MONSTER_ZONE_SIZE];

        private void Start()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;
            var parent = canvas.transform;

            if (playerMonsterZone == null)
                playerMonsterZone = CreateZonePanel(parent, "PlayerFieldZone", -160f);
            if (opponentMonsterZone == null)
                opponentMonsterZone = CreateZonePanel(parent, "OpponentFieldZone", 160f);
        }

        private RectTransform CreateZonePanel(Transform parent, string name, float yPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.1f, 0.5f);
            rt.anchorMax = new Vector2(0.7f, 0.5f);
            rt.anchoredPosition = new Vector2(0, yPos);
            rt.sizeDelta = new Vector2(0, 80);
            return rt;
        }

        public void UpdateField(DuelState state)
        {
            if (state == null) return;
            UpdateZone(state.players[0].monsterZone, _playerSlots, playerMonsterZone);
            UpdateZone(state.players[1].monsterZone, _opponentSlots, opponentMonsterZone);
        }

        private void UpdateZone(FieldCard[] zone, GameObject[] slots, RectTransform parent)
        {
            if (parent == null) return;

            for (int i = 0; i < DuelConstants.MONSTER_ZONE_SIZE; i++)
            {
                // Destroy old
                if (slots[i] != null) { Destroy(slots[i]); slots[i] = null; }

                if (zone[i] == null) continue;

                var card = zone[i].card;
                bool faceUp = zone[i].position == Position.FaceUpAttack || zone[i].position == Position.FaceUpDefense;
                bool defense = zone[i].position == Position.FaceUpDefense || zone[i].position == Position.FaceDownDefense;

                var go = new GameObject($"Field_{i}");
                go.transform.SetParent(parent, false);
                var rt = go.AddComponent<RectTransform>();
                float slotWidth = 90f;
                float spacing = 100f;
                float totalWidth = spacing * (DuelConstants.MONSTER_ZONE_SIZE - 1);
                rt.anchoredPosition = new Vector2(i * spacing - totalWidth / 2f, 0);
                rt.sizeDelta = defense ? new Vector2(70, 50) : new Vector2(50, 70);

                var img = go.AddComponent<Image>();
                if (!faceUp)
                    img.color = new Color(0.3f, 0.15f, 0.1f, 1f); // face-down brown
                else
                    img.color = new Color(0.7f, 0.55f, 0.2f, 1f); // face-up gold

                if (faceUp)
                {
                    var textGo = new GameObject("Text");
                    textGo.transform.SetParent(go.transform, false);
                    var trt = textGo.AddComponent<RectTransform>();
                    trt.anchorMin = Vector2.zero;
                    trt.anchorMax = Vector2.one;
                    trt.sizeDelta = Vector2.zero;
                    trt.offsetMin = new Vector2(2, 2);
                    trt.offsetMax = new Vector2(-2, -2);
                    var txt = textGo.AddComponent<Text>();
                    txt.text = $"{card.name}\n{card.atk}/{card.def}";
                    txt.fontSize = 9;
                    txt.color = Color.white;
                    txt.alignment = TextAnchor.MiddleCenter;
                    if (FontManager.CJKFont != null) txt.font = FontManager.CJKFont;
                    else txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }

                slots[i] = go;
            }
        }
    }
}
