using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CardBattle.Core
{
    [Serializable]
    public class CardDataArray { public CardData[] cards; }

    [Serializable]
    public class TestDeckData { public string[] mainDeck; }

    public class CardDatabase
    {
        private Dictionary<string, CardData> cards = new Dictionary<string, CardData>();

        public void LoadFromJson(string json)
        {
            var wrapper = JsonUtility.FromJson<CardDataArray>(json);
            cards.Clear();
            if (wrapper?.cards != null)
                foreach (var c in wrapper.cards) cards[c.id] = c;
        }

        public void LoadFromResources()
        {
            var asset = Resources.Load<TextAsset>("Cards/cards");
            if (asset != null) LoadFromJson(asset.text);
            else LoadDefaultCards();
        }

        public CardData GetCard(string id) => cards.TryGetValue(id, out var c) ? c : null;
        public List<CardData> GetAllCards() => new List<CardData>(cards.Values);

        public List<CardData> BuildTestDeck()
        {
            if (cards.Count == 0) LoadDefaultCards();
            var deck = new List<CardData>();
            var lowLevel = cards.Values.Where(c => CardHelper.IsMonster(c) && c.level <= 4 && !CardHelper.IsFusionMonster(c)).ToList();
            foreach (var m in lowLevel)
                for (int i = 0; i < 3; i++) deck.Add(m.Clone());
            // Add level 5 monster and spells/traps
            var lv5 = cards.Values.FirstOrDefault(c => c.id == "MON-012");
            if (lv5 != null) { deck.Add(lv5.Clone()); deck.Add(lv5.Clone()); }
            var spl1 = GetCard("SPL-001"); if (spl1 != null) { deck.Add(spl1.Clone()); deck.Add(spl1.Clone()); }
            var spl2 = GetCard("SPL-002"); if (spl2 != null) deck.Add(spl2.Clone());
            var trp1 = GetCard("TRP-001"); if (trp1 != null) { deck.Add(trp1.Clone()); deck.Add(trp1.Clone()); }
            // Pad to 40
            var filler = GetCard("MON-005");
            while (deck.Count < 40) deck.Add((filler ?? deck[0]).Clone());
            while (deck.Count > 40) deck.RemoveAt(deck.Count - 1);
            return deck;
        }

        public void LoadDefaultCards()
        {
            foreach (var c in GetDefaultCards()) cards[c.id] = c;
        }

        public static List<CardData> GetDefaultCards() => new List<CardData>
        {
            new CardData { id="MON-001", name="暗黑戰士", cardType=CardType.Monster, cardSubType=CardSubType.NormalMonster, attribute=Attribute.Dark, monsterType="戰士族", level=4, atk=1800, def=1200, rarity=Rarity.R, limitStatus=LimitStatus.Unlimited, flavorText="暗黑中的戰士，以強大的力量戰鬥。" },
            new CardData { id="MON-002", name="火焰龍", cardType=CardType.Monster, cardSubType=CardSubType.NormalMonster, attribute=Attribute.Fire, monsterType="龍族", level=4, atk=1700, def=1000, rarity=Rarity.N, limitStatus=LimitStatus.Unlimited, flavorText="噴射烈焰的小型龍。" },
            new CardData { id="MON-003", name="光之天使", cardType=CardType.Monster, cardSubType=CardSubType.NormalMonster, attribute=Attribute.Light, monsterType="天使族", level=4, atk=1600, def=1400, rarity=Rarity.N, limitStatus=LimitStatus.Unlimited, flavorText="守護光明的天使。" },
            new CardData { id="MON-004", name="鋼鐵巨人", cardType=CardType.Monster, cardSubType=CardSubType.NormalMonster, attribute=Attribute.Earth, monsterType="機械族", level=4, atk=1500, def=1800, rarity=Rarity.N, limitStatus=LimitStatus.Unlimited, flavorText="堅不可摧的鋼鐵巨人。" },
            new CardData { id="MON-005", name="風之精靈", cardType=CardType.Monster, cardSubType=CardSubType.NormalMonster, attribute=Attribute.Wind, monsterType="魔法使族", level=3, atk=1200, def=1000, rarity=Rarity.N, limitStatus=LimitStatus.Unlimited, flavorText="操控風之力的精靈。" },
            new CardData { id="MON-006", name="水之守護者", cardType=CardType.Monster, cardSubType=CardSubType.NormalMonster, attribute=Attribute.Water, monsterType="水族", level=4, atk=1400, def=1700, rarity=Rarity.N, limitStatus=LimitStatus.Unlimited, flavorText="守護水域的戰士。" },
            new CardData { id="MON-007", name="暗黑魔術師", cardType=CardType.Monster, cardSubType=CardSubType.EffectMonster, attribute=Attribute.Dark, monsterType="魔法使族", level=7, atk=2500, def=2100, rarity=Rarity.UR, limitStatus=LimitStatus.Unlimited, effectDescription="此卡召喚成功時，可以從牌組抽1張牌。", effectScripts=new[]{"EFF-001"} },
            new CardData { id="MON-008", name="青眼白龍", cardType=CardType.Monster, cardSubType=CardSubType.NormalMonster, attribute=Attribute.Light, monsterType="龍族", level=8, atk=3000, def=2500, rarity=Rarity.UR, limitStatus=LimitStatus.Unlimited, flavorText="傳說中的白龍，擁有毀滅一切的力量。" },
            new CardData { id="MON-009", name="小妖精", cardType=CardType.Monster, cardSubType=CardSubType.NormalMonster, attribute=Attribute.Earth, monsterType="獸族", level=2, atk=800, def=600, rarity=Rarity.N, limitStatus=LimitStatus.Unlimited, flavorText="森林中的小妖精。" },
            new CardData { id="MON-010", name="雷電鳥", cardType=CardType.Monster, cardSubType=CardSubType.NormalMonster, attribute=Attribute.Wind, monsterType="鳥獸族", level=4, atk=1600, def=1200, rarity=Rarity.R, limitStatus=LimitStatus.Unlimited, flavorText="帶著雷電飛翔的猛禽。" },
            new CardData { id="MON-011", name="岩石巨兵", cardType=CardType.Monster, cardSubType=CardSubType.NormalMonster, attribute=Attribute.Earth, monsterType="岩石族", level=3, atk=1000, def=1800, rarity=Rarity.N, limitStatus=LimitStatus.Unlimited, flavorText="堅硬如岩石的守衛。" },
            new CardData { id="MON-012", name="炎之劍士", cardType=CardType.Monster, cardSubType=CardSubType.NormalMonster, attribute=Attribute.Fire, monsterType="戰士族", level=5, atk=2100, def=1600, rarity=Rarity.SR, limitStatus=LimitStatus.Unlimited, flavorText="揮舞火焰之劍的勇者。" },
            new CardData { id="SPL-001", name="力量增幅", cardType=CardType.Spell, cardSubType=CardSubType.NormalSpell, rarity=Rarity.N, limitStatus=LimitStatus.Unlimited, effectDescription="選擇場上1隻怪獸，該怪獸攻擊力上升500。", artworkId="spl001" },
            new CardData { id="SPL-002", name="生命恢復", cardType=CardType.Spell, cardSubType=CardSubType.NormalSpell, rarity=Rarity.N, limitStatus=LimitStatus.Unlimited, effectDescription="回復1000點生命值。", artworkId="spl002" },
            new CardData { id="TRP-001", name="防護壁壘", cardType=CardType.Trap, cardSubType=CardSubType.NormalTrap, rarity=Rarity.R, limitStatus=LimitStatus.Unlimited, effectDescription="對方怪獸攻擊時，無效化該次攻擊。", artworkId="trp001" },
        };
    }
}
