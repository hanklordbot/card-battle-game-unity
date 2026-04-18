using NUnit.Framework;
using System.Collections.Generic;
using CardBattle.Core;

namespace CardBattle.Tests
{
    public class SummonSystemTests
    {
        private DuelState SetupState()
        {
            var deck = new List<CardData>();
            for (int i = 0; i < 40; i++)
                deck.Add(new CardData { id = $"T-{i}", name = $"Test{i}", cardType = CardType.Monster, cardSubType = CardSubType.NormalMonster, level = 4, atk = 1500, def = 1200, limitStatus = LimitStatus.Unlimited });
            var state = DuelEngine.CreateDuelState(new List<CardData>(deck), new List<CardData>(deck));
            state.phase = Phase.Main1;
            return state;
        }

        [Test]
        public void NormalSummon_Level4_Success()
        {
            var state = SetupState();
            var card = state.players[0].hand[0];
            var result = SummonSystem.NormalSummon(state, 0, 0);
            Assert.IsTrue(result.success);
            Assert.AreEqual(4, state.players[0].hand.Count);
            Assert.IsNotNull(state.players[0].monsterZone[0]);
            Assert.AreEqual(card.id, state.players[0].monsterZone[0].card.id);
        }

        [Test]
        public void NormalSummon_AlreadyUsed_Fails()
        {
            var state = SetupState();
            SummonSystem.NormalSummon(state, 0, 0);
            var result = SummonSystem.NormalSummon(state, 0, 0);
            Assert.IsFalse(result.success);
        }

        [Test]
        public void NormalSummon_Level5_NeedsTribute()
        {
            var state = SetupState();
            // Put a lv5 monster in hand
            state.players[0].hand[0] = new CardData { id = "LV5", name = "Lv5", cardType = CardType.Monster, cardSubType = CardSubType.NormalMonster, level = 5, atk = 2000, def = 1500 };
            // No monsters on field to tribute
            var result = SummonSystem.NormalSummon(state, 0, 0);
            Assert.IsFalse(result.success);
        }

        [Test]
        public void NormalSummon_Level5_WithTribute_Success()
        {
            var state = SetupState();
            // Place a monster on field first
            SummonSystem.NormalSummon(state, 0, 0);
            state.players[0].normalSummonUsed = false; // reset for test
            // Put lv5 in hand
            state.players[0].hand[0] = new CardData { id = "LV5", name = "Lv5", cardType = CardType.Monster, cardSubType = CardSubType.NormalMonster, level = 5, atk = 2000, def = 1500 };
            var result = SummonSystem.NormalSummon(state, 0, 0, new[] { 0 });
            Assert.IsTrue(result.success);
        }

        [Test]
        public void NormalSummon_SpellCard_Fails()
        {
            var state = SetupState();
            state.players[0].hand[0] = new CardData { id = "SPL", name = "Spell", cardType = CardType.Spell, cardSubType = CardSubType.NormalSpell };
            var result = SummonSystem.NormalSummon(state, 0, 0);
            Assert.IsFalse(result.success);
        }

        [Test]
        public void NormalSummon_FullField_Fails()
        {
            var state = SetupState();
            for (int i = 0; i < 5; i++)
                state.players[0].monsterZone[i] = new FieldCard(new CardData { id = $"F{i}", name = $"F{i}", cardType = CardType.Monster, level = 4, atk = 1000, def = 1000 }, Position.FaceUpAttack, 1);
            var result = SummonSystem.NormalSummon(state, 0, 0);
            Assert.IsFalse(result.success);
        }
    }
}
