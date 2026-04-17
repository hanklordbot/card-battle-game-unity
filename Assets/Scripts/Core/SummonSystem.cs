using System;
using System.Linq;

namespace CardBattle.Core
{
    public enum SummonError
    {
        NotMainPhase,
        NotMonster,
        NormalSummonUsed,
        NotEnoughTributes,
        MonsterZoneFull,
        CardNotInHand,
        CannotFlipSummon,
        InvalidTributeIndex,
        FusionCannotNormalSummon
    }

    [Serializable]
    public class SummonResult
    {
        public bool success;
        public SummonError? error;

        public static SummonResult Success() => new SummonResult { success = true, error = null };
        public static SummonResult Fail(SummonError err) => new SummonResult { success = false, error = err };
    }

    public static class SummonSystem
    {
        public static SummonResult NormalSummon(DuelState state, int playerIndex, int handIndex, int[] tributeIndices = null)
        {
            var player = state.players[playerIndex];

            if (state.phase != Phase.Main1 && state.phase != Phase.Main2)
                return SummonResult.Fail(SummonError.NotMainPhase);

            if (handIndex < 0 || handIndex >= player.hand.Count)
                return SummonResult.Fail(SummonError.CardNotInHand);

            var card = player.hand[handIndex];

            if (!CardHelper.IsMonster(card))
                return SummonResult.Fail(SummonError.NotMonster);

            if (CardHelper.IsFusionMonster(card))
                return SummonResult.Fail(SummonError.FusionCannotNormalSummon);

            if (player.normalSummonUsed)
                return SummonResult.Fail(SummonError.NormalSummonUsed);

            int tributesNeeded = CardHelper.GetTributeCount(card.level);

            if (tributesNeeded > 0)
            {
                if (tributeIndices == null || tributeIndices.Length < tributesNeeded)
                    return SummonResult.Fail(SummonError.NotEnoughTributes);

                foreach (int idx in tributeIndices)
                {
                    if (idx < 0 || idx >= DuelConstants.MONSTER_ZONE_SIZE || player.monsterZone[idx] == null)
                        return SummonResult.Fail(SummonError.InvalidTributeIndex);
                }

                // Send tributes to graveyard
                foreach (int idx in tributeIndices.OrderByDescending(i => i))
                {
                    player.graveyard.Add(player.monsterZone[idx].card);
                    player.monsterZone[idx] = null;
                }
            }

            int emptySlot = FindEmptyMonsterZone(player);
            if (emptySlot == -1)
                return SummonResult.Fail(SummonError.MonsterZoneFull);

            player.hand.RemoveAt(handIndex);
            player.monsterZone[emptySlot] = new FieldCard(card, Position.FaceUpAttack, state.turnCount);
            player.normalSummonUsed = true;

            return SummonResult.Success();
        }

        public static SummonResult SetMonster(DuelState state, int playerIndex, int handIndex)
        {
            var player = state.players[playerIndex];

            if (state.phase != Phase.Main1 && state.phase != Phase.Main2)
                return SummonResult.Fail(SummonError.NotMainPhase);

            if (handIndex < 0 || handIndex >= player.hand.Count)
                return SummonResult.Fail(SummonError.CardNotInHand);

            var card = player.hand[handIndex];

            if (!CardHelper.IsMonster(card))
                return SummonResult.Fail(SummonError.NotMonster);

            if (CardHelper.IsFusionMonster(card))
                return SummonResult.Fail(SummonError.FusionCannotNormalSummon);

            if (player.normalSummonUsed)
                return SummonResult.Fail(SummonError.NormalSummonUsed);

            if (card.level > 4)
                return SummonResult.Fail(SummonError.NotEnoughTributes);

            int emptySlot = FindEmptyMonsterZone(player);
            if (emptySlot == -1)
                return SummonResult.Fail(SummonError.MonsterZoneFull);

            player.hand.RemoveAt(handIndex);
            player.monsterZone[emptySlot] = new FieldCard(card, Position.FaceDownDefense, state.turnCount);
            player.normalSummonUsed = true;

            return SummonResult.Success();
        }

        public static SummonResult SpecialSummon(DuelState state, int playerIndex, CardData card, Position position)
        {
            var player = state.players[playerIndex];

            if (!CardHelper.IsMonster(card))
                return SummonResult.Fail(SummonError.NotMonster);

            int emptySlot = FindEmptyMonsterZone(player);
            if (emptySlot == -1)
                return SummonResult.Fail(SummonError.MonsterZoneFull);

            player.monsterZone[emptySlot] = new FieldCard(card, position, state.turnCount);
            return SummonResult.Success();
        }

        public static SummonResult FlipSummon(DuelState state, int playerIndex, int monsterIndex)
        {
            var player = state.players[playerIndex];

            if (state.phase != Phase.Main1 && state.phase != Phase.Main2)
                return SummonResult.Fail(SummonError.NotMainPhase);

            if (monsterIndex < 0 || monsterIndex >= DuelConstants.MONSTER_ZONE_SIZE)
                return SummonResult.Fail(SummonError.CannotFlipSummon);

            var fieldCard = player.monsterZone[monsterIndex];
            if (fieldCard == null)
                return SummonResult.Fail(SummonError.CannotFlipSummon);

            if (fieldCard.position != Position.FaceDownDefense)
                return SummonResult.Fail(SummonError.CannotFlipSummon);

            if (fieldCard.turnPlaced == state.turnCount)
                return SummonResult.Fail(SummonError.CannotFlipSummon);

            fieldCard.position = Position.FaceUpAttack;
            fieldCard.canAttack = true;

            return SummonResult.Success();
        }

        private static int FindEmptyMonsterZone(PlayerState player)
        {
            for (int i = 0; i < DuelConstants.MONSTER_ZONE_SIZE; i++)
            {
                if (player.monsterZone[i] == null) return i;
            }
            return -1;
        }
    }
}
