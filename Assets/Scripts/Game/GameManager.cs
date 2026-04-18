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
            StartGame();
        }

        public void StartGame()
        {
            var deck1 = CardDB.BuildTestDeck();
            var deck2 = CardDB.BuildTestDeck();
            DuelState = DuelEngine.CreateDuelState(deck1, deck2);
            SetState(GameState.InBattle);
            Log("Duel started!");
            RefreshUI();
        }

        public void RefreshUI()
        {
            if (_ui == null || DuelState == null) return;
            _ui.UpdateLP(DuelState.players[0].lp, DuelState.players[1].lp);
            _ui.UpdateHand(DuelState.players[0].hand);
            _ui.UpdatePhase(DuelState.phase, DuelState.turnPlayer == 0, DuelState.turnCount);
            _ui.UpdateEndPhaseButtonText(DuelState.phase);
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
