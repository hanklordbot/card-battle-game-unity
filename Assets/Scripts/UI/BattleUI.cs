using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using CardBattle.Core;

namespace CardBattle.UI
{
    /// <summary>
    /// Complete battle UI: LP bars with animation, phase indicator, hand preview,
    /// action buttons, card detail panel, game log.
    /// </summary>
    public class BattleUI : MonoBehaviour
    {
        // === LP Display ===
        [Header("LP Display")]
        [SerializeField] private Slider player1LPBar;
        [SerializeField] private Slider player2LPBar;
        [SerializeField] private Text player1LPText;
        [SerializeField] private Text player2LPText;
        [SerializeField] private Text player1LPDelta;
        [SerializeField] private Text player2LPDelta;

        private int _displayLP1, _displayLP2;
        private Coroutine _lpAnim1, _lpAnim2;

        // === Phase Indicator ===
        [Header("Phase Indicator")]
        [SerializeField] private Text turnLabel;
        [SerializeField] private Text[] phaseLabels; // 6 elements: DP SP MP1 BP MP2 EP
        [SerializeField] private Color phaseActiveColor = new Color(1f, 0.83f, 0.23f);
        [SerializeField] private Color phaseInactiveColor = new Color(0.4f, 0.4f, 0.4f);
        [SerializeField] private Color phasePastColor = new Color(0.6f, 0.5f, 0.2f);

        private static readonly string[] PhaseNames = { "DP", "SP", "MP1", "BP", "MP2", "EP" };
        private static readonly Phase[] PhaseOrder = { Phase.Draw, Phase.Standby, Phase.Main1, Phase.Battle, Phase.Main2, Phase.End };

        // === Hand Area ===
        [Header("Hand Area")]
        [SerializeField] private Transform handContainer;
        [SerializeField] private GameObject handCardPrefab;

        private readonly List<GameObject> _handCards = new();
        private int _selectedHandIndex = -1;

        // === Card Preview ===
        [Header("Card Preview")]
        [SerializeField] private GameObject previewPanel;
        [SerializeField] private Text previewName;
        [SerializeField] private Text previewType;
        [SerializeField] private Text previewStats;
        [SerializeField] private Text previewEffect;
        [SerializeField] private Image previewArt;

        // === Card Detail ===
        [Header("Card Detail")]
        [SerializeField] private GameObject cardDetailPanel;
        [SerializeField] private Text cardNameText;
        [SerializeField] private Text cardDescText;
        [SerializeField] private Text cardStatsText;
        [SerializeField] private Text cardTypeText;

        // === Buttons ===
        [Header("Buttons")]
        [SerializeField] private Button endPhaseButton;
        [SerializeField] private Button attackButton;
        [SerializeField] private Button summonButton;
        [SerializeField] private Button surrenderButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button directAttackButton;
        [SerializeField] private Text endPhaseButtonText;

        // === Message Toast ===
        [Header("Message")]
        [SerializeField] private Text messageText;
        [SerializeField] private GameObject messagePanel;
        [SerializeField] private CanvasGroup messageCanvasGroup;
        private Coroutine _messageCoroutine;

        // === Game Log ===
        [Header("Game Log")]
        [SerializeField] private Transform logContainer;
        [SerializeField] private Text logEntryPrefab;
        [SerializeField] private ScrollRect logScrollRect;
        private readonly List<Text> _logEntries = new();
        private const int MAX_LOG_ENTRIES = 30;

        // === Events ===
        public System.Action<int> OnHandCardClicked;
        public System.Action OnEndPhaseClicked;
        public System.Action OnAttackClicked;
        public System.Action OnSummonClicked;
        public System.Action OnSurrenderClicked;
        public System.Action OnCancelClicked;
        public System.Action OnDirectAttackClicked;

        private void Start()
        {
            _displayLP1 = DuelConstants.INITIAL_LP;
            _displayLP2 = DuelConstants.INITIAL_LP;

            endPhaseButton?.onClick.AddListener(() => OnEndPhaseClicked?.Invoke());
            attackButton?.onClick.AddListener(() => OnAttackClicked?.Invoke());
            summonButton?.onClick.AddListener(() => OnSummonClicked?.Invoke());
            surrenderButton?.onClick.AddListener(() => OnSurrenderClicked?.Invoke());
            cancelButton?.onClick.AddListener(() => OnCancelClicked?.Invoke());
            directAttackButton?.onClick.AddListener(() => OnDirectAttackClicked?.Invoke());

            HidePreview();
            HideCardDetail();
            if (cancelButton != null) cancelButton.gameObject.SetActive(false);
            if (directAttackButton != null) directAttackButton.gameObject.SetActive(false);
        }

        // === LP ===

        public void UpdateLP(int p1LP, int p2LP)
        {
            if (p1LP != _displayLP1)
            {
                ShowLPDelta(player1LPDelta, p1LP - _displayLP1);
                if (_lpAnim1 != null) StopCoroutine(_lpAnim1);
                _lpAnim1 = StartCoroutine(AnimateLP(player1LPBar, player1LPText, _displayLP1, p1LP, 0.8f));
                _displayLP1 = p1LP;
            }
            if (p2LP != _displayLP2)
            {
                ShowLPDelta(player2LPDelta, p2LP - _displayLP2);
                if (_lpAnim2 != null) StopCoroutine(_lpAnim2);
                _lpAnim2 = StartCoroutine(AnimateLP(player2LPBar, player2LPText, _displayLP2, p2LP, 0.8f));
                _displayLP2 = p2LP;
            }
        }

        private IEnumerator AnimateLP(Slider bar, Text text, int from, int to, float duration)
        {
            float max = DuelConstants.INITIAL_LP;
            float t = 0;
            while (t < duration)
            {
                t += Time.deltaTime;
                float v = Mathf.SmoothStep(0, 1, t / duration);
                int current = Mathf.RoundToInt(Mathf.Lerp(from, to, v));
                if (bar != null) bar.value = current / max;
                if (text != null) text.text = current.ToString();
                yield return null;
            }
            if (bar != null) bar.value = to / max;
            if (text != null) text.text = to.ToString();
        }

        private void ShowLPDelta(Text deltaText, int delta)
        {
            if (deltaText == null) return;
            deltaText.text = delta > 0 ? $"+{delta}" : delta.ToString();
            deltaText.color = delta > 0 ? Color.green : Color.red;
            deltaText.gameObject.SetActive(true);
            StartCoroutine(FadeOutDelta(deltaText));
        }

        private IEnumerator FadeOutDelta(Text t)
        {
            yield return new WaitForSeconds(1.5f);
            t.gameObject.SetActive(false);
        }

        // === Phase ===

        public void UpdatePhase(Phase phase, bool isMyTurn, int turnCount)
        {
            if (turnLabel != null)
            {
                turnLabel.text = $"{(isMyTurn ? "我方回合" : "對方回合")} - Turn {turnCount}";
                turnLabel.color = isMyTurn ? new Color(0, 0.83f, 1f) : new Color(1f, 0.27f, 0.27f);
            }

            int currentIdx = System.Array.IndexOf(PhaseOrder, phase);
            if (phaseLabels == null) return;
            for (int i = 0; i < phaseLabels.Length && i < PhaseNames.Length; i++)
            {
                if (phaseLabels[i] == null) continue;
                phaseLabels[i].text = PhaseNames[i];
                if (i == currentIdx)
                {
                    phaseLabels[i].color = phaseActiveColor;
                    phaseLabels[i].fontSize = 18;
                    phaseLabels[i].fontStyle = FontStyle.Bold;
                }
                else if (i < currentIdx)
                {
                    phaseLabels[i].color = phasePastColor;
                    phaseLabels[i].fontSize = 14;
                    phaseLabels[i].fontStyle = FontStyle.Normal;
                }
                else
                {
                    phaseLabels[i].color = phaseInactiveColor;
                    phaseLabels[i].fontSize = 14;
                    phaseLabels[i].fontStyle = FontStyle.Normal;
                }
            }
        }

        public void UpdateEndPhaseButtonText(Phase phase)
        {
            if (endPhaseButtonText != null)
                endPhaseButtonText.text = phase == Phase.Draw ? "開始回合" : "下一階段";
        }

        // === Hand ===

        public void UpdateHand(List<CardData> hand)
        {
            // Clear old
            foreach (var go in _handCards) Destroy(go);
            _handCards.Clear();

            if (handContainer == null || handCardPrefab == null) return;

            for (int i = 0; i < hand.Count; i++)
            {
                var go = Instantiate(handCardPrefab, handContainer);
                var card = hand[i];
                int idx = i;

                // Set card name text
                var nameText = go.GetComponentInChildren<Text>();
                if (nameText != null)
                {
                    nameText.text = CardHelper.IsMonster(card) ? $"{card.name}\n{card.atk}/{card.def}" : card.name;
                }

                // Click handler
                var btn = go.GetComponent<Button>();
                if (btn != null) btn.onClick.AddListener(() => OnHandCardClicked?.Invoke(idx));

                _handCards.Add(go);
            }
        }

        public void SetSelectedHandCard(int index)
        {
            _selectedHandIndex = index;
            for (int i = 0; i < _handCards.Count; i++)
            {
                var outline = _handCards[i].GetComponent<Outline>();
                if (outline != null)
                {
                    outline.enabled = i == index;
                    outline.effectColor = i == index ? new Color(0, 1f, 0.53f) : Color.clear;
                }
            }
        }

        // === Preview ===

        public void ShowPreview(CardData card)
        {
            if (previewPanel == null) return;
            previewPanel.SetActive(true);
            if (previewName != null) previewName.text = card.name;
            if (previewType != null)
            {
                if (CardHelper.IsMonster(card))
                    previewType.text = $"{'★'.ToString()}{new string('★', Mathf.Min(card.level, 12))} {card.attribute} {card.monsterType}";
                else
                    previewType.text = card.cardSubType.ToString();
            }
            if (previewStats != null)
                previewStats.text = CardHelper.IsMonster(card) ? $"ATK/{card.atk}  DEF/{card.def}" : "";
            if (previewEffect != null)
                previewEffect.text = !string.IsNullOrEmpty(card.effectDescription) ? card.effectDescription : card.flavorText ?? "";
        }

        public void HidePreview()
        {
            if (previewPanel != null) previewPanel.SetActive(false);
        }

        // === Card Detail ===

        public void ShowCardDetail(CardData card)
        {
            if (cardDetailPanel == null) return;
            cardDetailPanel.SetActive(true);
            if (cardNameText != null) cardNameText.text = card.name;
            if (cardTypeText != null)
            {
                if (CardHelper.IsMonster(card))
                    cardTypeText.text = $"Lv.{card.level} {card.attribute} {card.monsterType}";
                else
                    cardTypeText.text = card.cardSubType.ToString();
            }
            if (cardStatsText != null)
                cardStatsText.text = CardHelper.IsMonster(card) ? $"ATK/{card.atk}  DEF/{card.def}" : "";
            if (cardDescText != null)
                cardDescText.text = !string.IsNullOrEmpty(card.effectDescription) ? card.effectDescription : card.flavorText ?? "無效果描述";
        }

        public void HideCardDetail()
        {
            if (cardDetailPanel != null) cardDetailPanel.SetActive(false);
        }

        // === Buttons ===

        public void SetButtonsInteractable(bool interactable)
        {
            if (endPhaseButton != null) endPhaseButton.interactable = interactable;
            if (attackButton != null) attackButton.interactable = interactable;
            if (summonButton != null) summonButton.interactable = interactable;
        }

        public void ShowCancelButton(bool show)
        {
            if (cancelButton != null) cancelButton.gameObject.SetActive(show);
        }

        public void ShowDirectAttackButton(bool show)
        {
            if (directAttackButton != null) directAttackButton.gameObject.SetActive(show);
        }

        // === Message Toast ===

        public void ShowMessage(string message, float duration = 2f)
        {
            if (messageText != null) messageText.text = message;
            if (messagePanel != null) messagePanel.SetActive(true);
            if (_messageCoroutine != null) StopCoroutine(_messageCoroutine);
            _messageCoroutine = StartCoroutine(HideMessageAfter(duration));
        }

        private IEnumerator HideMessageAfter(float duration)
        {
            if (messageCanvasGroup != null)
            {
                messageCanvasGroup.alpha = 1f;
                yield return new WaitForSeconds(duration - 0.5f);
                float t = 0;
                while (t < 0.5f)
                {
                    t += Time.deltaTime;
                    messageCanvasGroup.alpha = 1f - (t / 0.5f);
                    yield return null;
                }
            }
            else
            {
                yield return new WaitForSeconds(duration);
            }
            if (messagePanel != null) messagePanel.SetActive(false);
        }

        // === Game Log ===

        public void AddLogEntry(string message)
        {
            if (logContainer == null) return;

            Text entry;
            if (_logEntries.Count >= MAX_LOG_ENTRIES)
            {
                entry = _logEntries[0];
                _logEntries.RemoveAt(0);
                entry.transform.SetAsLastSibling();
            }
            else if (logEntryPrefab != null)
            {
                entry = Instantiate(logEntryPrefab, logContainer);
            }
            else
            {
                var go = new GameObject("LogEntry");
                go.transform.SetParent(logContainer);
                entry = go.AddComponent<Text>();
                entry.fontSize = 13;
                entry.color = new Color(0.55f, 0.55f, 0.6f);
            }

            entry.text = message;
            _logEntries.Add(entry);

            // Auto-scroll to bottom
            if (logScrollRect != null)
                StartCoroutine(ScrollToBottom());
        }

        private IEnumerator ScrollToBottom()
        {
            yield return null; // wait one frame for layout
            if (logScrollRect != null) logScrollRect.normalizedPosition = Vector2.zero;
        }

        public void ClearLog()
        {
            foreach (var t in _logEntries) if (t != null) Destroy(t.gameObject);
            _logEntries.Clear();
        }
    }
}
