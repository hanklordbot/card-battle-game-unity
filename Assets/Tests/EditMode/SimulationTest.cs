using NUnit.Framework;
using System.Collections.Generic;
using CardBattle.Core;

namespace CardBattle.Tests
{
    public class SimulationTest
    {
        [Test]
        public void FullGame_CompletesWithoutException()
        {
            var db = new CardDatabase();
            db.LoadDefaultCards();
            var deck1 = db.BuildTestDeck();
            var deck2 = db.BuildTestDeck();
            var state = DuelEngine.CreateDuelState(deck1, deck2);
            var log = new List<string>();

            int maxTurns = 100;
            int turns = 0;

            while (state.result == DuelResult.Ongoing && turns < maxTurns)
            {
                turns++;
                int player = state.turnPlayer;
                log.Add($"--- Turn {state.turnCount}, Player {player} ---");

                // Draw
                var drawn = DuelEngine.ExecuteDrawPhase(state);
                if (drawn != null) log.Add($"  Drew: {drawn.name}");
                if (state.result != DuelResult.Ongoing) break;

                // Standby → Main1
                DuelEngine.AdvancePhase(state);
                DuelEngine.AdvancePhase(state);

                // Try summon strongest from hand
                var hand = state.players[player].hand;
                int bestIdx = -1; int bestAtk = -1;
                for (int i = 0; i < hand.Count; i++)
                {
                    if (CardHelper.IsMonster(hand[i]) && !CardHelper.IsFusionMonster(hand[i]) && hand[i].level <= 4 && hand[i].atk > bestAtk)
                    {
                        bool hasSlot = false;
                        for (int s = 0; s < DuelConstants.MONSTER_ZONE_SIZE; s++)
                            if (state.players[player].monsterZone[s] == null) { hasSlot = true; break; }
                        if (hasSlot) { bestIdx = i; bestAtk = hand[i].atk; }
                    }
                }
                if (bestIdx >= 0)
                {
                    var r = SummonSystem.NormalSummon(state, player, bestIdx);
                    if (r.success) log.Add($"  Summoned: {state.players[player].hand.Count} cards left");
                }

                // Battle
                DuelEngine.AdvancePhase(state);
                if (state.phase == Phase.Battle)
                {
                    int opp = 1 - player;
                    for (int a = 0; a < DuelConstants.MONSTER_ZONE_SIZE; a++)
                    {
                        if (state.result != DuelResult.Ongoing) break;
                        var mon = state.players[player].monsterZone[a];
                        if (mon == null || !mon.canAttack || mon.position != Position.FaceUpAttack) continue;

                        bool oppHas = false;
                        int defIdx = -1;
                        for (int d = 0; d < DuelConstants.MONSTER_ZONE_SIZE; d++)
                            if (state.players[opp].monsterZone[d] != null) { oppHas = true; defIdx = d; break; }

                        if (oppHas)
                            BattleCalculator.ExecuteBattle(state, player, a, defIdx);
                        else
                            BattleCalculator.ExecuteDirectAttack(state, player, a);
                    }
                    DuelEngine.EndBattlePhase(state);
                }
                if (state.result != DuelResult.Ongoing) break;

                // Main2 → End → next turn
                DuelEngine.AdvancePhase(state);
                if (state.phase == Phase.Main2) DuelEngine.AdvancePhase(state);
                DuelEngine.AdvancePhase(state);

                // Discard
                while (DuelEngine.GetDiscardCount(state, player) > 0)
                    DuelEngine.DiscardFromHand(state, player, 0);
            }

            log.Add($"=== Result: {state.result} after {turns} turns ===");
            log.Add($"P1 LP: {state.players[0].lp}, P2 LP: {state.players[1].lp}");

            // Output log
            UnityEngine.Debug.Log(string.Join("\n", log));

            Assert.AreNotEqual(DuelResult.Ongoing, state.result, $"Game should end within {maxTurns} turns");
        }
    }
}
