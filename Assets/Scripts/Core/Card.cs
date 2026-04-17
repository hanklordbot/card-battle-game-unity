using System;
using System.Collections.Generic;

namespace CardBattle.Core
{
    public enum CardType { Monster, Spell, Trap }

    public enum CardSubType
    {
        NormalMonster, EffectMonster, FusionMonster,
        NormalSpell, QuickSpell, ContinuousSpell, EquipSpell, FieldSpell,
        NormalTrap, ContinuousTrap, CounterTrap
    }

    public enum Attribute { Light, Dark, Fire, Water, Wind, Earth, Divine }
    public enum Rarity { N, R, SR, UR }
    public enum LimitStatus { Unlimited, SemiLimited, Limited, Forbidden }
    public enum Position { FaceUpAttack, FaceUpDefense, FaceDownDefense }
    public enum SpellSpeed { Speed1 = 1, Speed2 = 2, Speed3 = 3 }

    [Serializable]
    public class CardData
    {
        public string id;
        public string name;
        public string effectDescription;
        public string artworkId;
        public string flavorText;
        public CardType cardType;
        public CardSubType cardSubType;
        public Attribute attribute;
        public string monsterType;
        public int level;
        public int atk;
        public int def;
        public Rarity rarity;
        public LimitStatus limitStatus;
        public string[] effectScripts;

        public CardData Clone()
        {
            return new CardData
            {
                id = id,
                name = name,
                effectDescription = effectDescription,
                artworkId = artworkId,
                flavorText = flavorText,
                cardType = cardType,
                cardSubType = cardSubType,
                attribute = attribute,
                monsterType = monsterType,
                level = level,
                atk = atk,
                def = def,
                rarity = rarity,
                limitStatus = limitStatus,
                effectScripts = effectScripts != null ? (string[])effectScripts.Clone() : null
            };
        }
    }

    [Serializable]
    public class FieldCard
    {
        public CardData card;
        public Position position;
        public bool canAttack;
        public bool canChangePosition;
        public bool hasAttackedThisTurn;
        public int turnPlaced;

        public FieldCard(CardData card, Position position, int turnPlaced)
        {
            this.card = card;
            this.position = position;
            this.canAttack = false;
            this.canChangePosition = false;
            this.hasAttackedThisTurn = false;
            this.turnPlaced = turnPlaced;
        }
    }

    public static class CardHelper
    {
        public static bool IsMonster(CardData card)
        {
            return card.cardType == CardType.Monster;
        }

        public static bool IsSpell(CardData card)
        {
            return card.cardType == CardType.Spell;
        }

        public static bool IsTrap(CardData card)
        {
            return card.cardType == CardType.Trap;
        }

        public static bool IsFusionMonster(CardData card)
        {
            return card.cardSubType == CardSubType.FusionMonster;
        }

        public static SpellSpeed GetSpellSpeed(CardData card)
        {
            switch (card.cardSubType)
            {
                case CardSubType.NormalSpell:
                case CardSubType.ContinuousSpell:
                case CardSubType.EquipSpell:
                case CardSubType.FieldSpell:
                    return SpellSpeed.Speed1;
                case CardSubType.QuickSpell:
                case CardSubType.NormalTrap:
                case CardSubType.ContinuousTrap:
                    return SpellSpeed.Speed2;
                case CardSubType.CounterTrap:
                    return SpellSpeed.Speed3;
                default:
                    return SpellSpeed.Speed1;
            }
        }

        public static int GetTributeCount(int level)
        {
            if (level <= 4) return 0;
            if (level <= 6) return 1;
            return 2;
        }
    }
}
