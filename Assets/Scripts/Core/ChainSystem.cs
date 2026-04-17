using System;
using System.Collections.Generic;
using System.Linq;

namespace CardBattle.Core
{
    public enum ChainError
    {
        ChainFull,
        SpeedTooLow,
        SameCardAlreadyInChain,
        ChainEmpty
    }

    [Serializable]
    public class ChainLink
    {
        public CardData card;
        public int activatingPlayer;
        public SpellSpeed spellSpeed;
        public int effectIndex;
    }

    [Serializable]
    public class ChainResolveResult
    {
        public ChainLink link;
        public int index;
    }

    public class ChainSystem
    {
        public const int MAX_CHAIN_LENGTH = 16;

        private List<ChainLink> links = new List<ChainLink>();

        public bool CanAdd(CardData card, SpellSpeed speed, out ChainError? error)
        {
            error = null;

            if (links.Count >= MAX_CHAIN_LENGTH)
            {
                error = ChainError.ChainFull;
                return false;
            }

            if (links.Count > 0)
            {
                var currentSpeed = CurrentSpellSpeed();
                if ((int)speed < (int)currentSpeed)
                {
                    error = ChainError.SpeedTooLow;
                    return false;
                }

                if (links.Any(l => l.card.id == card.id))
                {
                    error = ChainError.SameCardAlreadyInChain;
                    return false;
                }
            }

            return true;
        }

        public ChainError? Add(CardData card, int activatingPlayer, SpellSpeed speed, int effectIndex = 0)
        {
            ChainError? error;
            if (!CanAdd(card, speed, out error))
                return error;

            links.Add(new ChainLink
            {
                card = card,
                activatingPlayer = activatingPlayer,
                spellSpeed = speed,
                effectIndex = effectIndex
            });

            return null;
        }

        public List<ChainResolveResult> Resolve()
        {
            if (links.Count == 0) return null;

            var results = new List<ChainResolveResult>();
            for (int i = links.Count - 1; i >= 0; i--)
            {
                results.Add(new ChainResolveResult
                {
                    link = links[i],
                    index = i
                });
            }

            links.Clear();
            return results;
        }

        public List<ChainLink> GetLinks() => new List<ChainLink>(links);
        public void Clear() => links.Clear();
        public int Length => links.Count;
        public bool IsEmpty => links.Count == 0;

        public SpellSpeed CurrentSpellSpeed()
        {
            if (links.Count == 0) return SpellSpeed.Speed1;
            return links[links.Count - 1].spellSpeed;
        }
    }
}
