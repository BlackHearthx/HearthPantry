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
        private static Player _trackedPlayer;

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
            if (!ReferenceEquals(player, _trackedPlayer))
            {
                _trackedPlayer = player;
                _expiryShown.Clear();
                _lowSupplyShown.Clear();
            }
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

            if (!CanAutomate(player))
                return;

            if (!PluginConfig.AutoEat.Value && !PluginConfig.FillEmptySlots.Value && !PluginConfig.ExpiryNotify.Value)
                return;

            CheckFoods(player);
        }

        internal static bool IsVomiting(Player player)
        {
            var effects = player?.GetSEMan()?.GetStatusEffects();
            if (effects == null)
                return false;

            // Check the effect type so renamed/modded puke effects are covered too.
            foreach (var effect in effects)
            {
                if (effect is SE_Puke)
                    return true;
            }

            return false;
        }

        internal static bool CanAutomate(Player player)
        {
            return player != null && !player.IsDead() && !player.IsTeleporting()
                && !player.InCutscene() && !IsVomiting(player);
        }

        internal static bool ShouldPauseTimers(Player player)
        {
            return PluginConfig.ModEnabled.Value && player == Player.m_localPlayer
                && CanAutomate(player)
                && ((PluginConfig.PauseOnBoat.Value && player.IsAttachedToShip())
                    || (PluginConfig.PauseNearWorkbench.Value && _nearWorkbench));
        }

        internal static float ScaleFoodRate(float rate, Player player)
        {
            if (!PluginConfig.ModEnabled.Value || player != Player.m_localPlayer)
                return rate;
            if (ShouldPauseTimers(player))
                return 0f;
            return PantryPolicy.ScaleRate(rate,
                PluginConfig.ScaleWithTimeControl.Value ? GetTimeControlMultiplier() : 1f);
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
                if (stack == null || stack.m_shared.m_consumeStatusEffect is SE_Puke
                    || !player.ConsumeItem(inventory, stack))
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
                int attempts = 0;
                int maxAttempts = inventory.GetAllItems().Count;
                var rejected = new HashSet<string>();
                while (foods != null && foods.Count < MaxFoodSlots && attempts++ < maxAttempts)
                {
                    var exclude = new HashSet<string>(rejected);
                    foreach (var f in foods)
                    {
                        if (f?.m_item?.m_shared != null)
                            exclude.Add(f.m_item.m_shared.m_name);
                    }

                    var pick = FoodScorer.PickBestFromInventory(inventory, exclude);
                    if (pick == null)
                        break;

                    string pickName = pick.m_shared.m_name;
                    int previousCount = foods.Count;
                    if (!player.ConsumeItem(inventory, pick))
                    {
                        rejected.Add(pickName);
                        continue;
                    }

                    foods = player.GetFoods();
                    if (foods == null || foods.Count <= previousCount || IsVomiting(player))
                        break;

                    _seenThisTick.Add(pickName);

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
                float rate = ScaleFoodRate(_foodRateRef(), Player.m_localPlayer);
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
                float multiplier = (float)_tcMultiplierGetter.Invoke(null, null);
                return PantryPolicy.ValidMultiplier(multiplier);
            }
            catch
            {
                return 1f;
            }
        }
    }
}
