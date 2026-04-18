using NUnit.Framework;
using CardBattle.Core;

namespace CardBattle.Tests
{
    public class ChainSystemTests
    {
        private CardData MakeCard(string id) => new CardData { id = id, name = id, cardType = CardType.Trap, cardSubType = CardSubType.NormalTrap };

        [Test]
        public void EmptyChain_IsEmpty()
        {
            var chain = new ChainSystem();
            Assert.IsTrue(chain.IsEmpty);
            Assert.AreEqual(0, chain.Length);
        }

        [Test]
        public void Add_IncreasesLength()
        {
            var chain = new ChainSystem();
            var err = chain.Add(MakeCard("A"), 0, SpellSpeed.Speed2);
            Assert.IsNull(err);
            Assert.AreEqual(1, chain.Length);
        }

        [Test]
        public void Add_SpeedTooLow_Fails()
        {
            var chain = new ChainSystem();
            chain.Add(MakeCard("A"), 0, SpellSpeed.Speed2);
            ChainError? err;
            bool can = chain.CanAdd(MakeCard("B"), SpellSpeed.Speed1, out err);
            Assert.IsFalse(can);
            Assert.AreEqual(ChainError.SpeedTooLow, err);
        }

        [Test]
        public void Add_SameCard_Fails()
        {
            var chain = new ChainSystem();
            chain.Add(MakeCard("A"), 0, SpellSpeed.Speed2);
            ChainError? err;
            bool can = chain.CanAdd(MakeCard("A"), SpellSpeed.Speed2, out err);
            Assert.IsFalse(can);
            Assert.AreEqual(ChainError.SameCardAlreadyInChain, err);
        }

        [Test]
        public void Resolve_ReturnsReverseOrder()
        {
            var chain = new ChainSystem();
            chain.Add(MakeCard("A"), 0, SpellSpeed.Speed1);
            chain.Add(MakeCard("B"), 1, SpellSpeed.Speed2);
            var results = chain.Resolve();
            Assert.AreEqual(2, results.Count);
            Assert.AreEqual("B", results[0].link.card.id); // last added resolves first
            Assert.AreEqual("A", results[1].link.card.id);
            Assert.IsTrue(chain.IsEmpty);
        }

        [Test]
        public void Resolve_EmptyChain_ReturnsNull()
        {
            var chain = new ChainSystem();
            Assert.IsNull(chain.Resolve());
        }

        [Test]
        public void ChainFull_At16()
        {
            var chain = new ChainSystem();
            for (int i = 0; i < 16; i++)
                chain.Add(MakeCard($"C{i}"), 0, SpellSpeed.Speed2);
            ChainError? err;
            bool can = chain.CanAdd(MakeCard("X"), SpellSpeed.Speed2, out err);
            Assert.IsFalse(can);
            Assert.AreEqual(ChainError.ChainFull, err);
        }
    }
}
