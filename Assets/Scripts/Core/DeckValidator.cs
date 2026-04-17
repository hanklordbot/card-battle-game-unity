using System;
using System.Collections.Generic;
using System.Linq;

namespace CardBattle.Core
{
    [Serializable]
    public class DeckValidationError
    {
        public string code;
        public string message;
        public string cardId;

        public DeckValidationError(string code, string message, string cardId = null)
        {
            this.code = code;
            this.message = message;
            this.cardId = cardId;
        }
    }

    [Serializable]
    public class DeckValidationResult
    {
        public bool valid;
        public List<DeckValidationError> errors;

        public DeckValidationResult()
        {
            valid = true;
            errors = new List<DeckValidationError>();
        }
    }

    public static class DeckValidator
    {
        public const int MAIN_DECK_MIN = 40;
        public const int MAIN_DECK_MAX = 60;
        public const int EXTRA_DECK_MAX = 15;
        public const int SIDE_DECK_MAX = 15;
        public const int DEFAULT_COPY_LIMIT = 3;

        public static DeckValidationResult Validate(
            List<CardData> mainDeck,
            List<CardData> extraDeck = null,
            List<CardData> sideDeck = null)
        {
            var result = new DeckValidationResult();
            extraDeck = extraDeck ?? new List<CardData>();
            sideDeck = sideDeck ?? new List<CardData>();

            // Main deck size
            if (mainDeck.Count < MAIN_DECK_MIN)
            {
                result.errors.Add(new DeckValidationError(
                    "MAIN_DECK_TOO_SMALL",
                    $"Main deck must have at least {MAIN_DECK_MIN} cards, has {mainDeck.Count}"));
            }

            if (mainDeck.Count > MAIN_DECK_MAX)
            {
                result.errors.Add(new DeckValidationError(
                    "MAIN_DECK_TOO_LARGE",
                    $"Main deck must have at most {MAIN_DECK_MAX} cards, has {mainDeck.Count}"));
            }

            // Extra deck size
            if (extraDeck.Count > EXTRA_DECK_MAX)
            {
                result.errors.Add(new DeckValidationError(
                    "EXTRA_DECK_TOO_LARGE",
                    $"Extra deck must have at most {EXTRA_DECK_MAX} cards, has {extraDeck.Count}"));
            }

            // Side deck size
            if (sideDeck.Count > SIDE_DECK_MAX)
            {
                result.errors.Add(new DeckValidationError(
                    "SIDE_DECK_TOO_LARGE",
                    $"Side deck must have at most {SIDE_DECK_MAX} cards, has {sideDeck.Count}"));
            }

            // Check copy limits across all decks
            var allCards = new List<CardData>();
            allCards.AddRange(mainDeck);
            allCards.AddRange(extraDeck);
            allCards.AddRange(sideDeck);

            var cardCounts = new Dictionary<string, int>();
            foreach (var card in allCards)
            {
                if (!cardCounts.ContainsKey(card.id))
                    cardCounts[card.id] = 0;
                cardCounts[card.id]++;
            }

            foreach (var kvp in cardCounts)
            {
                var card = allCards.First(c => c.id == kvp.Key);
                int maxCopies = GetMaxCopies(card);

                if (kvp.Value > maxCopies)
                {
                    result.errors.Add(new DeckValidationError(
                        "COPY_LIMIT_EXCEEDED",
                        $"Card '{card.name}' has {kvp.Value} copies, max allowed is {maxCopies}",
                        card.id));
                }
            }

            // Check for forbidden cards
            foreach (var card in allCards)
            {
                if (card.limitStatus == LimitStatus.Forbidden)
                {
                    result.errors.Add(new DeckValidationError(
                        "FORBIDDEN_CARD",
                        $"Card '{card.name}' is forbidden",
                        card.id));
                }
            }

            // Check fusion monsters not in main deck
            foreach (var card in mainDeck)
            {
                if (CardHelper.IsFusionMonster(card))
                {
                    result.errors.Add(new DeckValidationError(
                        "FUSION_IN_MAIN_DECK",
                        $"Fusion monster '{card.name}' must be in extra deck",
                        card.id));
                }
            }

            result.valid = result.errors.Count == 0;
            return result;
        }

        private static int GetMaxCopies(CardData card)
        {
            switch (card.limitStatus)
            {
                case LimitStatus.Forbidden: return 0;
                case LimitStatus.Limited: return 1;
                case LimitStatus.SemiLimited: return 2;
                case LimitStatus.Unlimited: return DEFAULT_COPY_LIMIT;
                default: return DEFAULT_COPY_LIMIT;
            }
        }
    }
}
