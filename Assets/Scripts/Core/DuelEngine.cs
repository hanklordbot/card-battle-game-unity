using System;
using System.Collections.Generic;
using System.Linq;

namespace CardBattle.Core
{
    public enum Phase { Draw, Standby, Main1, Battle, Main2, End }
    public enum BattleStep { Start, Battle, Damage, End }
    public enum DuelResult { Ongoing, Player1Win, Player2Win, Draw }

    public static class DuelConstants
    {
        public const int INITIAL_LP = 8000;
        public const int INITIAL_HAND_SIZE = 5;
        public const int HAND_LIMIT = 6;
        public const int MONSTER_ZONE_SIZE = 5;
        public const int SPELL_TRAP_ZONE_SIZE = 5;
    }

    [Serializable]
    public class PlayerState
    {
        public int lp;
        public List<CardData> hand;
        public List<CardData> deck;
        public List<CardData> graveyard;
        public List<CardData> banished;
        public List<CardData> extraDeck;
        public FieldCard[] monsterZone;
        public FieldCard[] spellTrapZone;
        public FieldCard fieldSpell;
        public bool normalSummonUsed;
        public int timeoutCount;

        public PlayerState()
        {
            lp = DuelConstants.INITIAL_LP;
            hand = new List<CardData>();
            deck = new List<CardData>();
            graveyard = new List<CardData>();
            banished = new List<CardData>();
            extraDeck = new List<CardData>();
            monsterZone = new FieldCard[DuelConstants.MONSTER_ZONE_SIZE];
            spellTrapZone = new FieldCard[DuelConstants.SPELL_TRAP_ZONE_SIZE];
            fieldSpell = null;
            normalSummonUsed = false;
            timeoutCount = 0;
        }
    }

    [Serializable]
    public class DuelState
    {
        public PlayerState[] players;
        public int turnPlayer;
        public int turnCount;
        public Phase phase;
        public BattleStep? battleStep;
        public DuelResult result;
        public bool firstTurn;

        public DuelState()
        {
            players = new PlayerState[2] { new PlayerState(), new PlayerState() };
            turnPlayer = 0;
            turnCount = 1;
            phase = Phase.Draw;
            battleStep = null;
            result = DuelResult.Ongoing;
            firstTurn = true;
        }
    }

    public static class DuelEngine
    {
        private static System.Random rng = new System.Random();

        public static DuelState CreateDuelState(List<CardData> deck1, List<CardData> deck2)
        {
            var state = new DuelState();
            state.players[0].deck = new List<CardData>(deck1);
            state.players[1].deck = new List<CardData>(deck2);

            ShuffleDeck(state, 0);
            ShuffleDeck(state, 1);
            DrawInitialHands(state);

            return state;
        }

        public static void DealDamage(DuelState state, int playerIndex, int amount)
        {
            if (amount <= 0) return;
            state.players[playerIndex].lp -= amount;
            if (state.players[playerIndex].lp <= 0)
            {
                state.players[playerIndex].lp = 0;
                state.result = playerIndex == 0 ? DuelResult.Player2Win : DuelResult.Player1Win;
            }
        }

        public static void HealLP(DuelState state, int playerIndex, int amount)
        {
            if (amount <= 0) return;
            state.players[playerIndex].lp += amount;
        }

        public static CardData DrawCard(DuelState state, int playerIndex)
        {
            var player = state.players[playerIndex];
            if (player.deck.Count == 0)
            {
                state.result = playerIndex == 0 ? DuelResult.Player2Win : DuelResult.Player1Win;
                return null;
            }

            var card = player.deck[0];
            player.deck.RemoveAt(0);
            player.hand.Add(card);
            return card;
        }

        public static void ShuffleDeck(DuelState state, int playerIndex)
        {
            var deck = state.players[playerIndex].deck;
            for (int i = deck.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                var temp = deck[i];
                deck[i] = deck[j];
                deck[j] = temp;
            }
        }

        public static void DrawInitialHands(DuelState state)
        {
            for (int i = 0; i < DuelConstants.INITIAL_HAND_SIZE; i++)
            {
                DrawCard(state, 0);
                DrawCard(state, 1);
            }
        }

        public static Phase AdvancePhase(DuelState state)
        {
            switch (state.phase)
            {
                case Phase.Draw:
                    state.phase = Phase.Standby;
                    break;
                case Phase.Standby:
                    state.phase = Phase.Main1;
                    break;
                case Phase.Main1:
                    if (state.firstTurn)
                    {
                        state.phase = Phase.End;
                    }
                    else
                    {
                        state.phase = Phase.Battle;
                        state.battleStep = BattleStep.Start;
                    }
                    break;
                case Phase.Battle:
                    state.phase = Phase.Main2;
                    state.battleStep = null;
                    break;
                case Phase.Main2:
                    state.phase = Phase.End;
                    break;
                case Phase.End:
                    EndTurn(state);
                    break;
            }
            return state.phase;
        }

        public static BattleStep? AdvanceBattleStep(DuelState state)
        {
            if (state.phase != Phase.Battle) return null;

            switch (state.battleStep)
            {
                case BattleStep.Start:
                    state.battleStep = BattleStep.Battle;
                    break;
                case BattleStep.Battle:
                    state.battleStep = BattleStep.Damage;
                    break;
                case BattleStep.Damage:
                    state.battleStep = BattleStep.End;
                    break;
                case BattleStep.End:
                    state.battleStep = BattleStep.Battle;
                    break;
            }
            return state.battleStep;
        }

        public static void EndBattlePhase(DuelState state)
        {
            if (state.phase == Phase.Battle)
            {
                state.phase = Phase.Main2;
                state.battleStep = null;
            }
        }

        public static int GetDiscardCount(DuelState state, int playerIndex)
        {
            int handSize = state.players[playerIndex].hand.Count;
            return handSize > DuelConstants.HAND_LIMIT ? handSize - DuelConstants.HAND_LIMIT : 0;
        }

        public static CardData DiscardFromHand(DuelState state, int playerIndex, int handIndex)
        {
            var player = state.players[playerIndex];
            if (handIndex < 0 || handIndex >= player.hand.Count) return null;

            var card = player.hand[handIndex];
            player.hand.RemoveAt(handIndex);
            player.graveyard.Add(card);
            return card;
        }

        public static CardData ExecuteDrawPhase(DuelState state)
        {
            if (state.firstTurn && state.turnPlayer == 0)
            {
                state.phase = Phase.Draw;
                AdvancePhase(state);
                return null;
            }

            var card = DrawCard(state, state.turnPlayer);
            AdvancePhase(state);
            return card;
        }

        private static void EndTurn(DuelState state)
        {
            var currentPlayer = state.players[state.turnPlayer];

            // Reset monster states for next turn
            for (int i = 0; i < DuelConstants.MONSTER_ZONE_SIZE; i++)
            {
                if (currentPlayer.monsterZone[i] != null)
                {
                    currentPlayer.monsterZone[i].canAttack = true;
                    currentPlayer.monsterZone[i].canChangePosition = true;
                    currentPlayer.monsterZone[i].hasAttackedThisTurn = false;
                }
            }

            // Switch turn player
            state.turnPlayer = 1 - state.turnPlayer;
            state.turnCount++;
            state.phase = Phase.Draw;
            state.battleStep = null;
            state.players[state.turnPlayer].normalSummonUsed = false;

            if (state.firstTurn)
            {
                state.firstTurn = false;
            }
        }
    }
}
