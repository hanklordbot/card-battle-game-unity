using NUnit.Framework;
using System.Collections.Generic;
using CardBattle.Core;

namespace CardBattle.Tests
{
    public class DuelEngineTests
    {
        private List<CardData> MakeDeck(int count = 40)
        {
            var deck = new List<CardData>();
            for (int i = 0; i < count; i++)
                deck.Add(new CardData { id = $"T-{i}", name = $"Test{i}", cardType = CardType.Monster, level = 4, atk = 1000 + i * 100, def = 800 });
            return deck;
        }

        [Test]
        public void CreateDuelState_DrawsInitialHands()
        {
            var state = DuelEngine.CreateDuelState(MakeDeck(), MakeDeck());
            Assert.AreEqual(5, state.players[0].hand.Count);
            Assert.AreEqual(5, state.players[1].hand.Count);
            Assert.AreEqual(35, state.players[0].deck.Count);
            Assert.AreEqual(35, state.players[1].deck.Count);
        }

        [Test]
        public void CreateDuelState_InitialValues()
        {
            var state = DuelEngine.CreateDuelState(MakeDeck(), MakeDeck());
            Assert.AreEqual(8000, state.players[0].lp);
            Assert.AreEqual(8000, state.players[1].lp);
            Assert.AreEqual(Phase.Draw, state.phase);
            Assert.AreEqual(0, state.turnPlayer);
            Assert.AreEqual(1, state.turnCount);
            Assert.AreEqual(DuelResult.Ongoing, state.result);
            Assert.IsTrue(state.firstTurn);
        }

        [Test]
        public void DrawCard_RemovesFromDeckAddsToHand()
        {
            var state = DuelEngine.CreateDuelState(MakeDeck(), MakeDeck());
            int handBefore = state.players[0].hand.Count;
            int deckBefore = state.players[0].deck.Count;
            var card = DuelEngine.DrawCard(state, 0);
            Assert.IsNotNull(card);
            Assert.AreEqual(handBefore + 1, state.players[0].hand.Count);
            Assert.AreEqual(deckBefore - 1, state.players[0].deck.Count);
        }

        [Test]
        public void DrawCard_EmptyDeck_LosesGame()
        {
            var state = DuelEngine.CreateDuelState(MakeDeck(5), MakeDeck(5));
            // Both players drew 5, decks empty
            Assert.AreEqual(0, state.players[0].deck.Count);
            DuelEngine.DrawCard(state, 0);
            Assert.AreEqual(DuelResult.Player2Win, state.result);
        }

        [Test]
        public void AdvancePhase_FullCycle()
        {
            var state = DuelEngine.CreateDuelState(MakeDeck(), MakeDeck());
            state.firstTurn = false;
            Assert.AreEqual(Phase.Draw, state.phase);
            DuelEngine.AdvancePhase(state); // → Standby
            Assert.AreEqual(Phase.Standby, state.phase);
            DuelEngine.AdvancePhase(state); // → Main1
            Assert.AreEqual(Phase.Main1, state.phase);
            DuelEngine.AdvancePhase(state); // → Battle
            Assert.AreEqual(Phase.Battle, state.phase);
            DuelEngine.AdvancePhase(state); // → Main2
            Assert.AreEqual(Phase.Main2, state.phase);
            DuelEngine.AdvancePhase(state); // → End
            Assert.AreEqual(Phase.End, state.phase);
            DuelEngine.AdvancePhase(state); // → next turn Draw
            Assert.AreEqual(Phase.Draw, state.phase);
            Assert.AreEqual(1, state.turnPlayer); // switched
            Assert.AreEqual(2, state.turnCount);
        }

        [Test]
        public void FirstTurn_SkipsBattlePhase()
        {
            var state = DuelEngine.CreateDuelState(MakeDeck(), MakeDeck());
            Assert.IsTrue(state.firstTurn);
            DuelEngine.AdvancePhase(state); // Draw→Standby
            DuelEngine.AdvancePhase(state); // Standby→Main1
            DuelEngine.AdvancePhase(state); // Main1→End (skip Battle)
            Assert.AreEqual(Phase.End, state.phase);
        }

        [Test]
        public void DealDamage_ReducesLP()
        {
            var state = DuelEngine.CreateDuelState(MakeDeck(), MakeDeck());
            DuelEngine.DealDamage(state, 0, 3000);
            Assert.AreEqual(5000, state.players[0].lp);
            Assert.AreEqual(DuelResult.Ongoing, state.result);
        }

        [Test]
        public void DealDamage_LethalEndsGame()
        {
            var state = DuelEngine.CreateDuelState(MakeDeck(), MakeDeck());
            DuelEngine.DealDamage(state, 0, 9000);
            Assert.AreEqual(0, state.players[0].lp);
            Assert.AreEqual(DuelResult.Player2Win, state.result);
        }

        [Test]
        public void DiscardFromHand_MovesToGraveyard()
        {
            var state = DuelEngine.CreateDuelState(MakeDeck(), MakeDeck());
            var card = state.players[0].hand[0];
            var discarded = DuelEngine.DiscardFromHand(state, 0, 0);
            Assert.AreEqual(card.id, discarded.id);
            Assert.AreEqual(4, state.players[0].hand.Count);
            Assert.AreEqual(1, state.players[0].graveyard.Count);
        }
    }
}
