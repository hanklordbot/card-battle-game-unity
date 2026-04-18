using NUnit.Framework;
using System.Collections.Generic;
using CardBattle.Core;

namespace CardBattle.Tests
{
    public class DeckValidatorTests
    {
        private List<CardData> MakeDeck(int count)
        {
            var deck = new List<CardData>();
            for (int i = 0; i < count; i++)
                deck.Add(new CardData { id = $"C-{i % 20}", name = $"Card{i % 20}", cardType = CardType.Monster, level = 4, atk = 1000, def = 1000, limitStatus = LimitStatus.Unlimited });
            return deck;
        }

        [Test]
        public void ValidDeck_Passes()
        {
            var result = DeckValidator.Validate(MakeDeck(40));
            Assert.IsTrue(result.valid);
        }

        [Test]
        public void TooSmallDeck_Fails()
        {
            var result = DeckValidator.Validate(MakeDeck(30));
            Assert.IsFalse(result.valid);
            Assert.IsTrue(result.errors.Exists(e => e.code == "MAIN_DECK_TOO_SMALL"));
        }

        [Test]
        public void TooLargeDeck_Fails()
        {
            var result = DeckValidator.Validate(MakeDeck(61));
            Assert.IsFalse(result.valid);
            Assert.IsTrue(result.errors.Exists(e => e.code == "MAIN_DECK_TOO_LARGE"));
        }

        [Test]
        public void ForbiddenCard_Fails()
        {
            var deck = MakeDeck(40);
            deck[0].limitStatus = LimitStatus.Forbidden;
            var result = DeckValidator.Validate(deck);
            Assert.IsFalse(result.valid);
            Assert.IsTrue(result.errors.Exists(e => e.code == "FORBIDDEN_CARD"));
        }

        [Test]
        public void CopyLimitExceeded_Fails()
        {
            var deck = new List<CardData>();
            for (int i = 0; i < 40; i++)
                deck.Add(new CardData { id = "SAME", name = "Same", cardType = CardType.Monster, level = 4, atk = 1000, def = 1000, limitStatus = LimitStatus.Unlimited });
            var result = DeckValidator.Validate(deck);
            Assert.IsFalse(result.valid);
            Assert.IsTrue(result.errors.Exists(e => e.code == "COPY_LIMIT_EXCEEDED"));
        }

        [Test]
        public void FusionInMainDeck_Fails()
        {
            var deck = MakeDeck(40);
            deck[0].cardSubType = CardSubType.FusionMonster;
            var result = DeckValidator.Validate(deck);
            Assert.IsFalse(result.valid);
            Assert.IsTrue(result.errors.Exists(e => e.code == "FUSION_IN_MAIN_DECK"));
        }
    }
}
