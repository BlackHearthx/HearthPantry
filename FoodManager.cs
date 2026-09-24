using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Bootstrap;
using HarmonyLib;
using UnityEngine;
using ItemData = ItemDrop.ItemData;

namespace HearthPantry
{
    internal static class FoodManager
    {
        private const int MaxFoodSlots = 3;
        private const float WorkbenchRange = 20f;

        private static readonly HashSet<string> _expiryShown = new HashSet<string>();
        private static readonly HashSet<string> _lowSupplyShown = new HashSet<string>();
        private static readonly HashSet<string> _seenThisTick = new HashSet<string>();

        private static bool _nearWorkbench;
        private static bool _onBoat;

        private static bool _foodRateResolved;
        private static AccessTools.FieldRef<float> _foodRateRef;

        private static bool _tcResolved;
        private static MethodInfo _tcMultiplierGetter;
        private const string TimeControlGuid = "drummercraig.time_control";

        public static bool NearWorkbench => _nearWorkbench;
        public static bool OnBoat => _onBoat;

        public static void Tick()
        {
            var player = Player.m_localPlayer;
            _nearWorkbench = PluginConfig.ModEnabled.Value
                             && player != null
                             && PluginConfig.PauseNearWorkbench.Value
                             && IsNearWorkbench(player);
            _onBoat = PluginConfig.ModEnabled.Value
                      && player != null
                      && PluginConfig.PauseOnBoat.Value
                      && player.IsAttachedToShip();

            if (!PluginConfig.ModEnabled.Value || player == null)
                return;

            if (!PluginConfig.AutoEat.Value && !PluginConfig.FillEmptySlots.Value && !PluginConfig.ExpiryNotify.Value)
                return;

            CheckFoods(player);
        }

        public static void OnFoodEaten(Player player, ItemData item)
        {
            if (!PluginConfig.ModEnabled.Value
                || !PluginConfig.ScaleWithTimeControl.Value
                || player != Player.m_localPlayer
                || item?.m_shared == null)
                return;

            float mult = GetTimeControlMultiplier();
            if (mult <= 0f || Mathf.Abs(mult - 1f) <= 0.001f)
                return;

            string name = item.m_shared.m_name;
            foreach (var food in player.GetFoods())
            {
                if (food?.m_item?.m_shared != null && food.m_item.m_shared.m_name == name)
                {
                    food.m_time = food.m_item.m_shared.m_foodBurnTime * mult;
                    break;
                }
            }
        }

        public static List<Player.Food> CaptureFoodsForDeath(Player player)
        {
            if (!PluginConfig.ModEnabled.Value || !PluginConfig.KeepFoodOnDeath.Value || player != Player.m_localPlayer)
                return null;

            var foods = player.GetFoods();
            if (foods == null || foods.Count == 0)
                return null;

            return new List<Player.Food>(foods);
        }

        public static void RestoreFoodsAfterDeath(Player player, List<Player.Food> saved)
        {
            if (saved == null || saved.Count == 0 || player == null)
                return;

            var foods = player.GetFoods();
            if (foods == null)
                return;

            foods.Clear();
            foods.AddRange(saved);
        }

        private static void CheckFoods(Player player)
        {
            var inventory = player.GetInventory();
            var foods = player.GetFoods();
            if (inventory == null || foods == null)
                return;

            _seenThisTick.Clear();
            float foodRate = GetFoodRate();

            // 1) Refresh active foods that are eligible
            for (int i = 0; i < foods.Count; i++)
            {
                var food = foods[i];
                if (food?.m_item?.m_shared == null)
                    continue;

                string name = food.m_item.m_shared.m_name;
                _seenThisTick.Add(name);

                float burn = food.m_item.m_shared.m_foodBurnTime;
                float pct = burn > 0f ? food.m_time / burn * 100f : 0f;

                if (!food.CanEatAgain())
                {
                    _expiryShown.Remove(name);
                    _lowSupplyShown.Remove(name);
                    continue;
                }

                if (PluginConfig.ExpiryNotify.Value
                    && !_expiryShown.Contains(name)
                    && pct <= PluginConfig.ExpiryPercent.Value)
                {
                    string label = Localization.instance.Localize(name);
                    int mins = Mathf.CeilToInt(food.m_time / foodRate / 60f);
                    ShowHud(mins <= 1
                        ? ModLocalization.L("hearthpantry_expiry_almost", label)
                        : ModLocalization.L("hearthpantry_expiry_mins", label, mins));
                    _expiryShown.Add(name);
                }

                if (!PluginConfig.AutoEat.Value || pct > PluginConfig.AutoEatPercent.Value)
                    continue;

                var stack = inventory.GetItem(name, -1, false);
                if (stack == null || !player.ConsumeItem(inventory, stack))
                    continue;

                _expiryShown.Remove(name);

                if (PluginConfig.AutoEatNotify.Value)
                {
                    string label = Localization.instance.Localize(name);
                    ShowHud(ModLocalization.L("hearthpantry_reeaten", label));
                }

                MaybeLowSupply(inventory, name);
            }

            // 2) Fill empty slots with scored inventory food
            if (PluginConfig.AutoEat.Value && PluginConfig.FillEmptySlots.Value)
            {
                // Re-read after possible re-eats
                foods = player.GetFoods();
                while (foods != null && foods.Count < MaxFoodSlots)
                {
                    var exclude = new HashSet<string>();
                    foreach (var f in foods)
                    {
                        if (f?.m_item?.m_shared != null)
                            exclude.Add(f.m_item.m_shared.m_name);
                    }

                    var pick = FoodScorer.PickBestFromInventory(inventory, exclude);
                    if (pick == null)
                        break;

                    string pickName = pick.m_shared.m_name;
                    if (!player.ConsumeItem(inventory, pick))
                        break;

                    if (PluginConfig.AutoEatNotify.Value)
                    {
                        string label = Localization.instance.Localize(pickName);
                        ShowHud(ModLocalization.L("hearthpantry_ate_fill", label));
                    }

                    MaybeLowSupply(inventory, pickName);
                    foods = player.GetFoods();
                }
            }

            _expiryShown.RemoveWhere(n => !_seenThisTick.Contains(n));
            _lowSupplyShown.RemoveWhere(n => !_seenThisTick.Contains(n));
        }

        private static void MaybeLowSupply(Inventory inventory, string name)
        {
            if (!PluginConfig.LowSupplyNotify.Value || PluginConfig.LowSupplyCount.Value <= 0)
                return;
            if (_lowSupplyShown.Contains(name))
                return;

            int count = inventory.CountItems(name, -1, true);
            if (count > PluginConfig.LowSupplyCount.Value)
                return;

            string label = Localization.instance.Localize(name);
            ShowHud(count <= 0
                ? ModLocalization.L("hearthpantry_out", label)
                : count == 1
                    ? ModLocalization.L("hearthpantry_last", label)
                    : ModLocalization.L("hearthpantry_low", count, label));
            _lowSupplyShown.Add(name);
        }

        private static bool IsNearWorkbench(Player player)
        {
            return CraftingStation.FindClosestStationInRange(
                       "$piece_workbench",
                       player.transform.position,
                       WorkbenchRange) != null;
        }

        private static void ShowHud(string message)
        {
            if (MessageHud.instance == null)
                return;
            MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, message);
        }

        private static float GetFoodRate()
        {
            if (!_foodRateResolved)
            {
                _foodRateResolved = true;
                try
                {
                    var field = AccessTools.Field(typeof(Game), "m_foodRate");
                    if (field != null)
                        _foodRateRef = AccessTools.StaticFieldRefAccess<float>(field);
                }
                catch (Exception ex)
                {
                    _foodRateRef = null;
                    HearthPantryPlugin.Log.LogWarning($"resolving Game.m_foodRate failed: {ex.Message}");
                }
            }

            if (_foodRateRef == null)
                return 1f;

            try
            {
                float rate = _foodRateRef();
                return rate > 0f ? rate : 1f;
            }
            catch
            {
                _foodRateRef = null;
                return 1f;
            }
        }

        private static float GetTimeControlMultiplier()
        {
            if (!_tcResolved)
            {
                _tcResolved = true;
                try
                {
                    if (Chainloader.PluginInfos.ContainsKey(TimeControlGuid))
                    {
                        var type = AccessTools.TypeByName("TimeControl.TimeControlConfig");
                        if (type != null)
                            _tcMultiplierGetter = AccessTools.PropertyGetter(type, "EffectiveMultiplier");

                        HearthPantryPlugin.Log.LogInfo(
                            _tcMultiplierGetter != null
                                ? "TimeControl detected; food duration will scale with its multiplier."
                                : "TimeControl detected but EffectiveMultiplier could not be resolved.");
                    }
                }
                catch (Exception ex)
                {
                    _tcMultiplierGetter = null;
                    HearthPantryPlugin.Log.LogWarning($"TimeControl detection failed: {ex.Message}");
                }
            }

            if (_tcMultiplierGetter == null)
                return 1f;

            try
            {
                return (float)_tcMultiplierGetter.Invoke(null, null);
            }
            catch
            {
                return 1f;
            }
        }
    }
}
