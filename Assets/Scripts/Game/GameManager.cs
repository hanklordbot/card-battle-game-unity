using System;
using System.Collections;
using UnityEngine;
using CardBattle.Core;
using CardBattle.UI;

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
        private int _selectedHandIndex = -1;
        private int _selectedAttackerIndex = -1;

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
