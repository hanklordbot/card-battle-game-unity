using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CardBattle.Core;
using CardBattle.UI;
using CardBattle.Scene;

namespace CardBattle.Game
{
    public enum GameState { MainMenu, InBattle, GameOver }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameState CurrentState { get; private set; } = GameState.MainMenu;
        public DuelState DuelState { get; private set; }
        public CardDatabase CardDB { get; private set; }

        public event Action<GameState> OnStateChanged;
        public event Action<string> OnLogMessage;

        [SerializeField] private float aiTurnDelay = 1.0f;

        private BattleUI _ui;
        private FieldRenderer _field;
        private PhaseIndicator _phaseIndicator;
        private BattleFieldSetup _fieldSetup;
        private int _selectedHandIndex = -1;
        private int _selectedAttackerIndex = -1;

        // 3D field card objects
        private GameObject[] _p1FieldCards = new GameObject[DuelConstants.MONSTER_ZONE_SIZE];
        private GameObject[] _p2FieldCards = new GameObject[DuelConstants.MONSTER_ZONE_SIZE];

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            CardDB = new CardDatabase();
            CardDB.LoadDefaultCards();
        }

        private void Start()
        {
            _ui = FindFirstObjectByType<BattleUI>();
            _field = FindFirstObjectByType<FieldRenderer>();
            _phaseIndicator = FindFirstObjectByType<PhaseIndicator>();
            _fieldSetup = FindFirstObjectByType<BattleFieldSetup>();
            if (_ui != null)
            {
                _ui.OnEndPhaseClicked += HandleEndPhase;
                _ui.OnAttackClicked += HandleAttack;
                _ui.OnSummonClicked += HandleSummon;
                _ui.OnSurrenderClicked += HandleSurrender;
                _ui.OnHandCardClicked += HandleHandCardClicked;
            }
            StartGame();
        }

        public void StartGame()
        {
            var deck1 = CardDB.BuildTestDeck();
            var deck2 = CardDB.BuildTestDeck();
            DuelState = DuelEngine.CreateDuelState(deck1, deck2);
            _selectedHandIndex = -1;
            _selectedAttackerIndex = -1;
            SetState(GameState.InBattle);
            Log("對戰開始！");
            RefreshUI();
        }

        public void RefreshUI()
        {
            if (_ui == null || DuelState == null) return;
            _ui.UpdateLP(DuelState.players[0].lp, DuelState.players[1].lp);
            _ui.UpdateHand(DuelState.players[0].hand);
            _ui.UpdatePhase(DuelState.phase, DuelState.turnPlayer == 0, DuelState.turnCount);
            _ui.UpdateEndPhaseButtonText(DuelState.phase);
            _ui.SetButtonsInteractable(DuelState.turnPlayer == 0 && DuelState.result == DuelResult.Ongoing);
            _field?.UpdateField(DuelState);
            _phaseIndicator?.UpdatePhase(DuelState.phase);
            SyncFieldCards3D();
        }

        private void SyncFieldCards3D()
        {
            if (DuelState == null) return;
            SyncZone3D(DuelState.players[0].monsterZone, _p1FieldCards, 0);
            SyncZone3D(DuelState.players[1].monsterZone, _p2FieldCards, 1);
        }

        private void SyncZone3D(FieldCard[] zone, GameObject[] visuals, int player)
        {
            for (int i = 0; i < DuelConstants.MONSTER_ZONE_SIZE; i++)
            {
                // Remove visual if slot is now empty
                if (zone[i] == null)
                {
                    if (visuals[i] != null) { Destroy(visuals[i]); visuals[i] = null; }
                    continue;
                }

                // Already has correct visual
                if (visuals[i] != null) continue;

                // Get slot position from scene zone parents
                Transform slot = GetSlotTransform(player, i);
                if (slot == null) continue;

                var card = zone[i].card;
                bool faceUp = zone[i].position == Position.FaceUpAttack || zone[i].position == Position.FaceUpDefense;

                // Create 3D card quad at slot position
                var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                go.name = $"FieldCard_{player}_{i}_{card.name}";
                go.transform.position = slot.position + Vector3.up * 0.02f;
                go.transform.rotation = Quaternion.Euler(90, 0, 0);
                go.transform.localScale = new Vector3(0.65f, 0.95f, 1f);

                var rend = go.GetComponent<Renderer>();
                var mat = new Material(Shader.Find("Standard"));
                if (faceUp)
                {
                    mat.color = CardHelper.IsMonster(card)
                        ? new Color(0.8f, 0.65f, 0.2f)
                        : new Color(0.2f, 0.6f, 0.3f);
                }
                else
                {
                    mat.color = new Color(0.3f, 0.15f, 0.1f);
                }
                rend.material = mat;

                // Add floating text via TextMesh
                if (faceUp)
                {
                    var textGo = new GameObject("Label");
                    textGo.transform.SetParent(go.transform, false);
                    textGo.transform.localPosition = new Vector3(0, 0, -0.01f);
                    textGo.transform.localRotation = Quaternion.identity;
                    textGo.transform.localScale = new Vector3(0.08f, 0.06f, 1f);
                    var tm = textGo.AddComponent<TextMesh>();
                    tm.text = $"{card.name}\n{card.atk}/{card.def}";
                    tm.fontSize = 32;
                    tm.characterSize = 0.5f;
                    tm.anchor = TextAnchor.MiddleCenter;
                    tm.alignment = TextAlignment.Center;
                    tm.color = Color.white;
                    // Use CJK font if available
                    if (FontManager.CJKFont != null) tm.font = FontManager.CJKFont;
                }

                visuals[i] = go;
            }
        }

        private Transform GetSlotTransform(int player, int slotIndex)
        {
            // Try BattleFieldSetup references first
            if (_fieldSetup != null)
            {
                var slot = _fieldSetup.GetMonsterSlot(player, slotIndex);
                if (slot != null) return slot;
            }
            // Fallback: find by name
            string zoneName = player == 0 ? "PlayerMonsterZone" : "OpponentMonsterZone";
            var zone = GameObject.Find(zoneName);
            if (zone == null) return null;
            var child = zone.transform.Find($"Slot_{slotIndex}");
            return child;
        }

        // === Button Handlers ===

        private void HandleEndPhase()
        {
            if (DuelState == null || DuelState.result != DuelResult.Ongoing) return;
            if (DuelState.turnPlayer != 0) return; // not player's turn

            // If in Draw phase, execute draw first
            if (DuelState.phase == Phase.Draw)
            {
                var drawn = DuelEngine.ExecuteDrawPhase(DuelState);
                if (drawn != null) Log($"抽到: {drawn.name}");
            }
            else
            {
                DuelEngine.AdvancePhase(DuelState);
            }

            _selectedHandIndex = -1;
            _selectedAttackerIndex = -1;

            // If we advanced past End phase, it's now AI's turn
            if (DuelState.turnPlayer == 1)
            {
                RefreshUI();
                _ui?.SetButtonsInteractable(false);
                _ui?.ShowMessage("對方回合...", 1.5f);
                RunAITurn();
                return;
            }

            if (DuelState.result != DuelResult.Ongoing)
            {
                EndGame(DuelState.result);
                return;
            }

            RefreshUI();
        }

        private void HandleSummon()
        {
            if (DuelState == null || DuelState.turnPlayer != 0) return;
            if (_selectedHandIndex < 0) { _ui?.ShowMessage("請先選擇手牌", 1.5f); return; }
            if (DuelState.phase != Phase.Main1 && DuelState.phase != Phase.Main2)
            {
                _ui?.ShowMessage("只能在主要階段召喚", 1.5f);
                return;
            }

            var hand = DuelState.players[0].hand;
            if (_selectedHandIndex >= hand.Count) return;
            var card = hand[_selectedHandIndex];

            if (!CardHelper.IsMonster(card))
            {
                _ui?.ShowMessage("只能召喚怪獸卡", 1.5f);
                return;
            }

            int tributes = CardHelper.GetTributeCount(card.level);
            int[] tributeIndices = null;
            if (tributes > 0)
            {
                // Auto-select weakest monsters as tributes
                var player = DuelState.players[0];
                var available = new System.Collections.Generic.List<(int idx, int atk)>();
                for (int i = 0; i < DuelConstants.MONSTER_ZONE_SIZE; i++)
                    if (player.monsterZone[i] != null)
                        available.Add((i, player.monsterZone[i].card.atk));
                available.Sort((a, b) => a.atk.CompareTo(b.atk));
                if (available.Count < tributes)
                {
                    _ui?.ShowMessage($"需要 {tributes} 隻怪獸作為祭品", 1.5f);
                    return;
                }
                tributeIndices = new int[tributes];
                for (int i = 0; i < tributes; i++) tributeIndices[i] = available[i].idx;
            }

            var result = SummonSystem.NormalSummon(DuelState, 0, _selectedHandIndex, tributeIndices);
            if (result.success)
            {
                Log($"召喚: {card.name} (ATK {card.atk})");
                _selectedHandIndex = -1;
            }
            else
            {
                _ui?.ShowMessage($"召喚失敗: {result.error}", 1.5f);
            }
            RefreshUI();
        }

        private void HandleAttack()
        {
            if (DuelState == null || DuelState.turnPlayer != 0) return;
            if (DuelState.phase != Phase.Battle)
            {
                _ui?.ShowMessage("只能在戰鬥階段攻擊", 1.5f);
                return;
            }

            var player = DuelState.players[0];
            var opp = DuelState.players[1];

            // Find first available attacker
            int atkIdx = -1;
            for (int i = 0; i < DuelConstants.MONSTER_ZONE_SIZE; i++)
            {
                var m = player.monsterZone[i];
                if (m != null && m.canAttack && m.position == Position.FaceUpAttack && !m.hasAttackedThisTurn)
                {
                    atkIdx = i;
                    break;
                }
            }

            if (atkIdx < 0) { _ui?.ShowMessage("沒有可攻擊的怪獸", 1.5f); return; }

            bool oppHasMonsters = false;
            int defIdx = -1;
            for (int i = 0; i < DuelConstants.MONSTER_ZONE_SIZE; i++)
            {
                if (opp.monsterZone[i] != null) { oppHasMonsters = true; defIdx = i; break; }
            }

            if (!oppHasMonsters)
            {
                var res = BattleCalculator.ExecuteDirectAttack(DuelState, 0, atkIdx);
                if (res != null) Log($"{player.monsterZone[atkIdx]?.card.name ?? "怪獸"} 直接攻擊! 造成 {res.damageToDefender} 傷害");
            }
            else
            {
                var res = BattleCalculator.ExecuteBattle(DuelState, 0, atkIdx, defIdx);
                if (res != null)
                {
                    Log("攻擊!");
                    if (res.defenderDestroyed) Log("對方怪獸被破壞!");
                    if (res.attackerDestroyed) Log("我方怪獸被破壞!");
                    if (res.damageToDefender > 0) Log($"對方受到 {res.damageToDefender} 傷害");
                    if (res.damageToAttacker > 0) Log($"我方受到 {res.damageToAttacker} 傷害");
                }
            }

            if (DuelState.result != DuelResult.Ongoing) { EndGame(DuelState.result); return; }
            RefreshUI();
        }

        private void HandleSurrender()
        {
            if (DuelState == null) return;
            DuelState.result = DuelResult.Player2Win;
            Log("玩家投降");
            EndGame(DuelState.result);
        }

        private void HandleHandCardClicked(int index)
        {
            _selectedHandIndex = index;
            _ui?.SetSelectedHandCard(index);
            if (DuelState != null && index < DuelState.players[0].hand.Count)
            {
                var card = DuelState.players[0].hand[index];
                _ui?.ShowPreview(card);
            }
        }

        public void EndGame(DuelResult result)
        {
            SetState(GameState.GameOver);
            string msg = result == DuelResult.Player1Win ? "Player 1 wins!" :
                         result == DuelResult.Player2Win ? "Player 2 wins!" : "Draw!";
            Log(msg);
        }

        public void RunAITurn()
        {
            if (DuelState == null || DuelState.result != DuelResult.Ongoing) return;
            StartCoroutine(AITurnCoroutine());
        }

        private IEnumerator AITurnCoroutine()
        {
            yield return new WaitForSeconds(aiTurnDelay);
            AI.SimpleAI.RunAITurn(DuelState, Log);

            if (DuelState.result != DuelResult.Ongoing)
            {
                EndGame(DuelState.result);
            }
            else
            {
                _ui?.ShowMessage("我方回合", 1.5f);
            }
            RefreshUI();
        }

        private void SetState(GameState state)
        {
            CurrentState = state;
            OnStateChanged?.Invoke(state);
        }

        private void Log(string message)
        {
            OnLogMessage?.Invoke(message);
            Debug.Log($"[GameManager] {message}");
        }
    }
}
