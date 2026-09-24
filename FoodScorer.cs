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
            return s.m_food * PluginConfig.ScoreHealthWeight.Value
                 + s.m_foodStamina * PluginConfig.ScoreStaminaWeight.Value
                 + s.m_foodBurnTime * PluginConfig.ScoreDurationWeight.Value
                 + s.m_foodRegen * PluginConfig.ScoreRegenWeight.Value;
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
