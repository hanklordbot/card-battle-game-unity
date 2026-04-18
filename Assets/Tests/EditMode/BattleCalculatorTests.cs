using NUnit.Framework;
using System.Collections.Generic;
using CardBattle.Core;

namespace CardBattle.Tests
{
    public class BattleCalculatorTests
    {
        private FieldCard MakeMonster(int atk, int def, Position pos = Position.FaceUpAttack)
        {
            return new FieldCard(new CardData { id = "M", name = "Mon", cardType = CardType.Monster, level = 4, atk = atk, def = def }, pos, 1);
        }

        [Test]
        public void AttackVsAttack_AttackerWins()
        {
            var atk = MakeMonster(2000, 1000);
            var def = MakeMonster(1500, 1200);
            var result = BattleCalculator.CalculateBattle(atk, def);
            Assert.IsTrue(result.defenderDestroyed);
            Assert.IsFalse(result.attackerDestroyed);
            Assert.AreEqual(500, result.damageToDefender);
            Assert.AreEqual(0, result.damageToAttacker);
        }

        [Test]
        public void AttackVsAttack_DefenderWins()
        {
            var atk = MakeMonster(1000, 800);
            var def = MakeMonster(1800, 1200);
            var result = BattleCalculator.CalculateBattle(atk, def);
            Assert.IsFalse(result.defenderDestroyed);
            Assert.IsTrue(result.attackerDestroyed);
            Assert.AreEqual(0, result.damageToDefender);
            Assert.AreEqual(800, result.damageToAttacker);
        }

        [Test]
        public void AttackVsAttack_Tie()
        {
            var atk = MakeMonster(1500, 1000);
            var def = MakeMonster(1500, 1000);
            var result = BattleCalculator.CalculateBattle(atk, def);
            Assert.IsTrue(result.defenderDestroyed);
            Assert.IsTrue(result.attackerDestroyed);
            Assert.AreEqual(0, result.damageToDefender);
            Assert.AreEqual(0, result.damageToAttacker);
        }

        [Test]
        public void AttackVsDefense_AttackerWins()
        {
            var atk = MakeMonster(2000, 1000);
            var def = MakeMonster(1000, 1500, Position.FaceUpDefense);
            var result = BattleCalculator.CalculateBattle(atk, def);
            Assert.IsTrue(result.defenderDestroyed);
            Assert.IsFalse(result.attackerDestroyed);
            Assert.AreEqual(0, result.damageToDefender); // no damage in defense
        }

        [Test]
        public void AttackVsDefense_DefenderSurvives()
        {
            var atk = MakeMonster(1000, 800);
            var def = MakeMonster(500, 2000, Position.FaceUpDefense);
            var result = BattleCalculator.CalculateBattle(atk, def);
            Assert.IsFalse(result.defenderDestroyed);
            Assert.IsFalse(result.attackerDestroyed);
            Assert.AreEqual(1000, result.damageToAttacker); // recoil
        }

        [Test]
        public void DirectAttack_DealsDamage()
        {
            var atk = MakeMonster(2500, 2000);
            var result = BattleCalculator.CalculateDirectAttack(atk);
            Assert.AreEqual(2500, result.damageToDefender);
        }

        [Test]
        public void ExecuteBattle_UpdatesState()
        {
            var deck = new List<CardData>();
            for (int i = 0; i < 40; i++)
                deck.Add(new CardData { id = $"T{i}", name = $"T{i}", cardType = CardType.Monster, level = 4, atk = 1500, def = 1000 });
            var state = DuelEngine.CreateDuelState(new List<CardData>(deck), new List<CardData>(deck));
            state.players[0].monsterZone[0] = MakeMonster(2000, 1000);
            state.players[1].monsterZone[0] = MakeMonster(1000, 800);
            var result = BattleCalculator.ExecuteBattle(state, 0, 0, 0);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.defenderDestroyed);
            Assert.IsNull(state.players[1].monsterZone[0]);
            Assert.AreEqual(7000, state.players[1].lp);
        }
    }
}
