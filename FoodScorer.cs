using System.Collections.Generic;
using ItemData = ItemDrop.ItemData;

namespace HearthPantry
{
    internal static class FoodScorer
    {
        public static float Score(ItemData item)
        {
            if (item?.m_shared == null)
                return 0f;

            var s = item.m_shared;
            return PantryPolicy.Score(s.m_food, s.m_foodStamina, s.m_foodBurnTime, s.m_foodRegen, s.m_foodEitr,
                PluginConfig.ScoreHealthWeight.Value, PluginConfig.ScoreStaminaWeight.Value,
                PluginConfig.ScoreDurationWeight.Value, PluginConfig.ScoreRegenWeight.Value,
                PluginConfig.ScoreEitrWeight.Value);
        }

        public static ItemData PickBestFromInventory(Inventory inventory, HashSet<string> excludeNames)
        {
            if (inventory == null)
                return null;

            var consumables = new List<ItemData>();
            inventory.GetAllItems(ItemData.ItemType.Consumable, consumables);

            ItemData best = null;
            float bestScore = 0f;
            bool haveBest = false;
            bool preferBest = PluginConfig.EatBestFirst.Value;

            foreach (var item in consumables)
            {
                if (item?.m_shared == null || item.m_shared.m_food <= 0f)
                    continue;
                if (item.m_shared.m_consumeStatusEffect is SE_Puke)
                    continue;
                if (excludeNames != null && excludeNames.Contains(item.m_shared.m_name))
                    continue;

                float score = Score(item);
                if (!haveBest)
                {
                    best = item;
                    bestScore = score;
                    haveBest = true;
                    continue;
                }

                if (preferBest)
                {
                    if (score > bestScore)
                    {
                        best = item;
                        bestScore = score;
                    }
                }
                else if (score < bestScore)
                {
                    best = item;
                    bestScore = score;
                }
            }

            return best;
        }
    }
}
