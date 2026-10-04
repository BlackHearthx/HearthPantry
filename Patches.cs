using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace HearthPantry
{
    [HarmonyPatch]
    internal static class Patches
    {
        private static readonly System.Reflection.MethodInfo TotalFood = AccessTools.Method(typeof(Player), "GetTotalFoodValue");
        // SetMaxEitr is private in the game, even though build references expose it.
        private static readonly System.Reflection.MethodInfo SetMaxEitr =
            AccessTools.Method(typeof(Player), "SetMaxEitr", new[] { typeof(float), typeof(bool) });

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(Player), "UpdateFood")]
        private static IEnumerable<CodeInstruction> UpdateFood_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var rate = AccessTools.Field(typeof(Game), "m_foodRate");
            var scale = AccessTools.Method(typeof(FoodManager), nameof(FoodManager.ScaleFoodRate));
            foreach (var instruction in instructions)
            {
                yield return instruction;
                if (instruction.LoadsField(rate))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, scale);
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Player), "UpdateFood")]
        private static void UpdateFood_Prefix(Player __instance, out Dictionary<Player.Food, float> __state)
        {
            __state = null;
            if (!FoodManager.ShouldPauseTimers(__instance)) return;
            __state = new Dictionary<Player.Food, float>();
            foreach (var food in __instance.GetFoods()) __state[food] = food.m_time;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player), "UpdateFood")]
        private static void UpdateFood_Postfix(Player __instance, Dictionary<Player.Food, float> __state)
        {
            if (__state == null) return;
            var foods = __instance.GetFoods();
            foreach (var entry in __state)
            {
                var food = entry.Key;
                food.m_time = entry.Value;
                if (!foods.Contains(food)) foods.Add(food);
                float fraction = UnityEngine.Mathf.Pow(UnityEngine.Mathf.Clamp01(food.m_time / food.m_item.m_shared.m_foodBurnTime), 0.3f);
                food.m_health = food.m_item.m_shared.m_food * fraction;
                food.m_stamina = food.m_item.m_shared.m_foodStamina * fraction;
                food.m_eitr = food.m_item.m_shared.m_foodEitr * fraction;
            }
            object[] values = { 0f, 0f, 0f };
            TotalFood.Invoke(__instance, values);
            __instance.SetMaxHealth((float)values[0], false);
            __instance.SetMaxStamina((float)values[1], false);
            SetMaxEitr.Invoke(__instance, new object[] { values[2], false });
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

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Character), nameof(Character.ApplyDamage))]
        private static void ApplyDamage_Prefix(Character __instance, out float __state)
        {
            __state = __instance == Player.m_localPlayer ? __instance.GetHealth() : float.NaN;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Character), nameof(Character.ApplyDamage))]
        private static void ApplyDamage_Postfix(Character __instance, HitData hit, float __state)
        {
            if (__instance == Player.m_localPlayer && __instance.GetHealth() < __state)
                MeadManager.OnDamage(__instance, hit);
        }
    }
}
