using System;
using System.Linq;
using CardBattle.Core;

namespace CardBattle.AI
{
    /// <summary>
    /// AI opponent with complete turn flow, optimized tribute selection,
    /// and smart attack targeting (prioritize weakest beatable target).
    /// </summary>
    public static class SimpleAI
    {
        public static void RunAITurn(DuelState state, Action<string> log)
        {
            int ai = state.turnPlayer;
            var player = state.players[ai];
            if (state.result != DuelResult.Ongoing) return;

            // Draw Phase
            var drawn = DuelEngine.ExecuteDrawPhase(state);
            if (drawn != null) log?.Invoke($"AI drew a card.");
            if (state.result != DuelResult.Ongoing) return;

            // Standby → Main1
            DuelEngine.AdvancePhase(state);
            DuelEngine.AdvancePhase(state);

            // Main Phase 1
            TrySummonBest(state, ai, log);
            SetSpellsTraps(state, ai, log);

            // Battle Phase
            DuelEngine.AdvancePhase(state);
            if (state.phase == Phase.Battle)
            {
                ExecuteSmartAttacks(state, ai, log);
                DuelEngine.EndBattlePhase(state);
            }
            if (state.result != DuelResult.Ongoing) return;

            // Main2 → End → Next turn
            DuelEngine.AdvancePhase(state); // Main2 or End
            if (state.phase == Phase.Main2)
            {
                // Second chance to summon if we haven't
                TrySummonBest(state, ai, log);
                DuelEngine.AdvancePhase(state); // End
            }
            DuelEngine.AdvancePhase(state); // Next turn

            // Discard to hand limit
            while (DuelEngine.GetDiscardCount(state, ai) > 0)
            {
                // Discard weakest monster or first non-monster
                int discardIdx = FindWorstHandCard(player);
                DuelEngine.DiscardFromHand(state, ai, discardIdx);
                log?.Invoke("AI discarded a card.");
            }
        }

        private static void TrySummonBest(DuelState state, int ai, Action<string> log)
        {
            var player = state.players[ai];
            if (player.normalSummonUsed) return;

            // Score each summonable monster: prefer highest ATK, but account for tribute cost
            int bestIdx = -1;
            int bestScore = -1;

            for (int i = 0; i < player.hand.Count; i++)
            {
                var card = player.hand[i];
                if (!CardHelper.IsMonster(card) || CardHelper.IsFusionMonster(card)) continue;

                int tributes = CardHelper.GetTributeCount(card.level);
                int available = player.monsterZone.Count(m => m != null);
                bool hasEmptyZone = player.monsterZone.Any(m => m == null);

                // Need tributes + at least 1 empty zone after tributing (or tribute frees a zone)
                if (tributes > available) continue;
                if (tributes == 0 && !hasEmptyZone) continue;

                // Score = ATK, but penalize if we sacrifice strong monsters
                int tributeLoss = 0;
                if (tributes > 0)
                {
                    var weakest = player.monsterZone
                        .Where(m => m != null)
                        .OrderBy(m => m.card.atk)
                        .Take(tributes);
                    tributeLoss = weakest.Sum(m => m.card.atk);
                }

                int score = card.atk * 2 - tributeLoss; // net gain
                if (score > bestScore)
                {
                    bestScore = score;
                    bestIdx = i;
                }
            }

            if (bestIdx < 0) return;

            var summonCard = player.hand[bestIdx];
            int tributesNeeded = CardHelper.GetTributeCount(summonCard.level);
            int[] tributeIndices = null;

            if (tributesNeeded > 0)
            {
                // Pick weakest monsters as tributes
                tributeIndices = player.monsterZone
                    .Select((m, idx) => new { m, idx })
                    .Where(x => x.m != null)
                    .OrderBy(x => x.m.card.atk)
                    .Take(tributesNeeded)
                    .Select(x => x.idx)
                    .ToArray();
            }

            var result = SummonSystem.NormalSummon(state, ai, bestIdx, tributeIndices);
            if (result.success)
            {
                string summonType = tributesNeeded > 0 ? "tribute summoned" : "summoned";
                log?.Invoke($"AI {summonType}: {summonCard.name} (ATK {summonCard.atk})");
            }
        }

        private static void SetSpellsTraps(DuelState state, int ai, Action<string> log)
        {
            var player = state.players[ai];
            for (int i = player.hand.Count - 1; i >= 0; i--)
            {
                var card = player.hand[i];
                if (!CardHelper.IsSpell(card) && !CardHelper.IsTrap(card)) continue;

                int emptySlot = -1;
                for (int s = 0; s < DuelConstants.SPELL_TRAP_ZONE_SIZE; s++)
                {
                    if (player.spellTrapZone[s] == null) { emptySlot = s; break; }
                }
                if (emptySlot == -1) break;

                player.hand.RemoveAt(i);
                player.spellTrapZone[emptySlot] = new FieldCard(card, Position.FaceDownDefense, state.turnCount);
                log?.Invoke("AI set a card face-down.");
            }
        }

        private static void ExecuteSmartAttacks(DuelState state, int ai, Action<string> log)
        {
            int opp = 1 - ai;
            var oppPlayer = state.players[opp];

            // Sort our attackers by ATK descending (strongest attacks first)
            var attackers = state.players[ai].monsterZone
                .Select((m, idx) => new { m, idx })
                .Where(x => x.m != null && x.m.canAttack && x.m.position == Position.FaceUpAttack)
                .OrderByDescending(x => x.m.card.atk)
                .ToList();

            foreach (var atk in attackers)
            {
                if (state.result != DuelResult.Ongoing) return;

                bool oppHasMonsters = oppPlayer.monsterZone.Any(m => m != null);

                if (!oppHasMonsters)
                {
                    // Direct attack
                    var result = BattleCalculator.ExecuteDirectAttack(state, ai, atk.idx);
                    if (result != null)
                        log?.Invoke($"AI's {atk.m.card.name} attacks directly for {result.damageToDefender}!");
                    continue;
                }

                // Find best target: prefer weakest monster we can beat
                int bestTarget = -1;
                int bestTargetValue = int.MaxValue;
                bool canBeat = false;

                for (int d = 0; d < DuelConstants.MONSTER_ZONE_SIZE; d++)
                {
                    var def = oppPlayer.monsterZone[d];
                    if (def == null) continue;

                    int defValue = def.position == Position.FaceUpAttack ? def.card.atk : def.card.def;
                    bool wouldWin = def.position == Position.FaceUpAttack
                        ? atk.m.card.atk > defValue
                        : atk.m.card.atk > defValue; // ATK > DEF destroys in defense

                    if (wouldWin && defValue < bestTargetValue)
                    {
                        bestTargetValue = defValue;
                        bestTarget = d;
                        canBeat = true;
                    }
                    else if (!canBeat && defValue < bestTargetValue)
                    {
                        // If we can't beat anything, still track weakest (might attack for chip damage)
                        bestTargetValue = defValue;
                        bestTarget = d;
                    }
                }

                // Only attack if we can win the battle (don't suicide)
                if (bestTarget >= 0 && canBeat)
                {
                    var result = BattleCalculator.ExecuteBattle(state, ai, atk.idx, bestTarget);
                    if (result != null)
                    {
                        log?.Invoke($"AI's {atk.m.card.name} attacks!");
                        if (result.defenderDestroyed) log?.Invoke("Opponent's monster destroyed!");
                        if (result.damageToDefender > 0) log?.Invoke($"Opponent takes {result.damageToDefender} damage!");
                    }
                }
            }
        }

        private static int FindWorstHandCard(PlayerState player)
        {
            // Discard lowest ATK monster, or first spell/trap
            int worstIdx = 0;
            int worstAtk = int.MaxValue;

            for (int i = 0; i < player.hand.Count; i++)
            {
                var card = player.hand[i];
                if (!CardHelper.IsMonster(card)) return i; // discard non-monsters first
                if (card.atk < worstAtk)
                {
                    worstAtk = card.atk;
                    worstIdx = i;
                }
            }
            return worstIdx;
        }
    }
}
