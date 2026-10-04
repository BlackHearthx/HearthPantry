using System.Collections;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn;
using Jotunn.Utils;
using UnityEngine;

namespace HearthPantry
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.NotEnforced, VersionStrictness.None)]
    public class HearthPantryPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.blackhearthx.hearthpantry";
        public const string PluginName = "HearthPantry";
        public const string PluginVersion = "1.0.4";

        internal static ManualLogSource Log { get; private set; }

        private void Awake()
        {
            Log = Logger;
            PluginConfig.Bind(Config);
            ModLocalization.Register();

            var harmony = new Harmony(PluginGUID);
            harmony.PatchAll(typeof(HearthPantryPlugin).Assembly);

            StartCoroutine(FoodLoop());
            StartCoroutine(MeadLoop());

            Jotunn.Logger.LogInfo($"{PluginName} {PluginVersion} loaded");
        }

        private void Update()
        {
            if (!PluginConfig.ModEnabled.Value)
                return;

            if (!PluginConfig.ToggleHotkey.Value.IsDown())
                return;

            PluginConfig.AutoEat.Value = !PluginConfig.AutoEat.Value;
            if (MessageHud.instance != null)
            {
                string msg = PluginConfig.AutoEat.Value
                    ? ModLocalization.L("hearthpantry_toggle_on")
                    : ModLocalization.L("hearthpantry_toggle_off");
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, msg);
            }
        }

        private IEnumerator FoodLoop()
        {
            var wait = new WaitForSeconds(1f);
            while (true)
            {
                yield return wait;
                try
                {
                    FoodManager.Tick();
                }
                catch (System.Exception ex)
                {
                    Log.LogError($"FoodManager tick failed: {ex}");
                }
            }
        }

        private IEnumerator MeadLoop()
        {
            var wait = new WaitForSeconds(0.5f);
            while (true)
            {
                yield return wait;
                try
                {
                    MeadManager.Tick();
                }
                catch (System.Exception ex)
                {
                    Log.LogError($"MeadManager tick failed: {ex}");
                }
            }
        }
    }
}
