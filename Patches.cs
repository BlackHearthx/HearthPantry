using System.Collections.Generic;
using HarmonyLib;
using ItemData = ItemDrop.ItemData;

namespace HearthPantry
{
    [HarmonyPatch]
    internal static class Patches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player), nameof(Player.UpdateFood))]
        private static void UpdateFood_Postfix(Player __instance)
        {
            if ((!FoodManager.NearWorkbench && !FoodManager.OnBoat) || __instance != Player.m_localPlayer)
                return;

            float dt = UnityEngine.Time.deltaTime * GetFoodRateSafe();
            foreach (var food in __instance.GetFoods())
            {
                if (food?.m_item != null)
                    food.m_time += dt;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Player), nameof(Player.OnDeath))]
        private static void OnDeath_Prefix(Player __instance, out List<Player.Food> __state)
        {
            __state = FoodManager.CaptureFoodsForDeath(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player), nameof(Player.OnDeath))]
        private static void OnDeath_Postfix(Player __instance, List<Player.Food> __state)
        {
            FoodManager.RestoreFoodsAfterDeath(__instance, __state);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player), nameof(Player.EatFood))]
        private static void EatFood_Postfix(Player __instance, ItemData item, bool __result)
        {
            if (__result)
                FoodManager.OnFoodEaten(__instance, item);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
        private static void RPC_Damage_Prefix(Character __instance, HitData hit)
        {
            MeadManager.OnDamage(__instance, hit);
        }

        private static float GetFoodRateSafe()
        {
            // Match FoodManager: prefer Game.m_foodRate when present.
            try
            {
                var field = AccessTools.Field(typeof(Game), "m_foodRate");
                if (field != null)
                {
                    float rate = (float)field.GetValue(null);
                    return rate > 0f ? rate : 1f;
                }
            }
            catch
            {
                // ignored
            }

            return 1f;
        }
    }
}
