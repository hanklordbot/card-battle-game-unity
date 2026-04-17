using System;
using System.Linq;

namespace CardBattle.Core
{
    [Serializable]
    public class BattleResult
    {
        public bool attackerDestroyed;
        public bool defenderDestroyed;
        public int damageToAttacker;
        public int damageToDefender;
    }

    public static class BattleCalculator
    {
        public static BattleResult CalculateBattle(FieldCard attacker, FieldCard defender)
        {
            var result = new BattleResult();
            int atkValue = attacker.card.atk;

            if (defender.position == Position.FaceUpAttack)
            {
                int defAtkValue = defender.card.atk;
                if (atkValue > defAtkValue)
                {
                    result.defenderDestroyed = true;
                    result.damageToDefender = atkValue - defAtkValue;
                }
                else if (atkValue < defAtkValue)
                {
                    result.attackerDestroyed = true;
                    result.damageToAttacker = defAtkValue - atkValue;
                }
                else
                {
                    result.attackerDestroyed = true;
                    result.defenderDestroyed = true;
                }
            }
            else
            {
                int defValue = defender.card.def;
                if (atkValue > defValue)
                {
                    result.defenderDestroyed = true;
                }
                else if (atkValue < defValue)
                {
                    result.damageToAttacker = defValue - atkValue;
                }
            }

            return result;
        }

        public static BattleResult CalculateDirectAttack(FieldCard attacker)
        {
            return new BattleResult
            {
                attackerDestroyed = false,
                defenderDestroyed = false,
                damageToAttacker = 0,
                damageToDefender = attacker.card.atk
            };
        }

        public static BattleResult ExecuteBattle(DuelState state, int attackerPlayer, int attackerIndex, int defenderIndex)
        {
            int defenderPlayer = 1 - attackerPlayer;
            var attacker = state.players[attackerPlayer].monsterZone[attackerIndex];
            var defender = state.players[defenderPlayer].monsterZone[defenderIndex];

            if (attacker == null || defender == null) return null;

            // Flip face-down defender
            if (defender.position == Position.FaceDownDefense)
            {
                defender.position = Position.FaceUpDefense;
            }

            var result = CalculateBattle(attacker, defender);

            if (result.attackerDestroyed)
            {
                state.players[attackerPlayer].graveyard.Add(attacker.card);
                state.players[attackerPlayer].monsterZone[attackerIndex] = null;
            }

            if (result.defenderDestroyed)
            {
                state.players[defenderPlayer].graveyard.Add(defender.card);
                state.players[defenderPlayer].monsterZone[defenderIndex] = null;
            }

            if (result.damageToAttacker > 0)
            {
                DuelEngine.DealDamage(state, attackerPlayer, result.damageToAttacker);
            }

            if (result.damageToDefender > 0)
            {
                DuelEngine.DealDamage(state, defenderPlayer, result.damageToDefender);
            }

            attacker.hasAttackedThisTurn = true;
            attacker.canAttack = false;

            return result;
        }

        public static BattleResult ExecuteDirectAttack(DuelState state, int attackerPlayer, int attackerIndex)
        {
            int defenderPlayer = 1 - attackerPlayer;
            var attacker = state.players[attackerPlayer].monsterZone[attackerIndex];

            if (attacker == null) return null;

            // Check if defender has any monsters
            bool hasMonsters = state.players[defenderPlayer].monsterZone.Any(m => m != null);
            if (hasMonsters) return null;

            var result = CalculateDirectAttack(attacker);
            DuelEngine.DealDamage(state, defenderPlayer, result.damageToDefender);

            attacker.hasAttackedThisTurn = true;
            attacker.canAttack = false;

            return result;
        }
    }
}
