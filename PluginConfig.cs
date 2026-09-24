using BepInEx.Configuration;
using UnityEngine;

namespace HearthPantry
{
    internal static class PluginConfig
    {
        public static ConfigEntry<bool> ModEnabled;
        public static ConfigEntry<bool> AutoEat;
        public static ConfigEntry<int> AutoEatPercent;
        public static ConfigEntry<bool> AutoEatNotify;
        public static ConfigEntry<bool> FillEmptySlots;
        public static ConfigEntry<bool> EatBestFirst;
        public static ConfigEntry<bool> ExpiryNotify;
        public static ConfigEntry<int> ExpiryPercent;
        public static ConfigEntry<bool> LowSupplyNotify;
        public static ConfigEntry<int> LowSupplyCount;
        public static ConfigEntry<bool> PauseNearWorkbench;
        public static ConfigEntry<bool> PauseOnBoat;
        public static ConfigEntry<bool> KeepFoodOnDeath;
        public static ConfigEntry<bool> ScaleWithTimeControl;
        public static ConfigEntry<KeyboardShortcut> ToggleHotkey;

        public static ConfigEntry<int> ScoreHealthWeight;
        public static ConfigEntry<int> ScoreStaminaWeight;
        public static ConfigEntry<int> ScoreDurationWeight;
        public static ConfigEntry<int> ScoreRegenWeight;

        public static ConfigEntry<bool> AutoHealthMead;
        public static ConfigEntry<int> HealthMeadThreshold;
        public static ConfigEntry<bool> HealthMeadOnlyEnemyHit;
        public static ConfigEntry<float> HealthMeadEnemyHitWindow;
        public static ConfigEntry<bool> HealthMeadRequireMaxHealth;
        public static ConfigEntry<bool> HealthMeadNotify;

        public static ConfigEntry<bool> AutoPoisonMead;
        public static ConfigEntry<float> PoisonMeadRange;
        public static ConfigEntry<bool> PoisonMeadNotify;

        public static ConfigEntry<bool> AutoFireMead;
        public static ConfigEntry<float> FireMeadRange;
        public static ConfigEntry<bool> FireMeadNotify;

        public static ConfigEntry<bool> AutoFrostMead;
        public static ConfigEntry<int> FrostMeadTickCount;
        public static ConfigEntry<float> FrostMeadTickWindow;
        public static ConfigEntry<bool> FrostMeadNotify;

        public static void Bind(ConfigFile config)
        {
            ModEnabled = config.Bind("General", "01. Mod Enabled", true,
                "Turns the whole pantry on or off.");
            AutoEat = config.Bind("General", "02. Auto-Eat", true,
                "Reaches into your bag and eats for you when a meal is about to fade.");
            AutoEatPercent = config.Bind("General", "03. Auto-Eat Percent", 45,
                new ConfigDescription(
                    "How much of the timer is left when it re-eats. The game only lets you eat again once the food starts flashing (50%). Forty-five keeps the buff near full without wasting a stack on the last seconds.",
                    new AcceptableValueRange<int>(1, 49)));
            AutoEatNotify = config.Bind("General", "04. Auto-Eat Notify", true,
                "Shows a short line on screen when the pantry eats for you.");
            FillEmptySlots = config.Bind("General", "05. Fill Empty Slots", true,
                "If you have fewer than three meals going, picks something good from your inventory and fills the gap.");
            EatBestFirst = config.Bind("General", "06. Eat Best Foods First", true,
                "When filling empty slots, take the strongest food first. Turn this off if you want to save the good stuff and burn the weak meals.");
            ExpiryNotify = config.Bind("General", "07. Expiry Notify", true,
                "A quiet heads-up when a meal is getting thin, even if you prefer to eat by hand.");
            ExpiryPercent = config.Bind("General", "08. Expiry Percent", 50,
                new ConfigDescription(
                    "When that heads-up appears, as a percent of time left. Keep it at or above Auto-Eat Percent so you hear about it before the pantry acts.",
                    new AcceptableValueRange<int>(1, 50)));
            LowSupplyNotify = config.Bind("General", "09. Low Supply Notify", true,
                "Warns you when a stack is nearly gone after the pantry eats from it.");
            LowSupplyCount = config.Bind("General", "10. Low Supply Count", 1,
                "How many pieces left counts as \"running low\".");
            PauseNearWorkbench = config.Bind("General", "11. Pause Near Workbench", true,
                "While you are at the workbench, food timers hold still. Healing and stamina from the meal still work.");
            PauseOnBoat = config.Bind("General", "12. Pause On Boat", true,
                "Same pause while you are sailing. Effects keep ticking; only the countdown freezes.");
            KeepFoodOnDeath = config.Bind("General", "13. Keep Food On Death", false,
                "Come back from death with the same meals still in your belly. Off by default so death still costs something.");
            ScaleWithTimeControl = config.Bind("General", "14. Scale With TimeControl", true,
                "If you use DrummerCraig's TimeControl, food lasts in step with the longer day. Harmless if that mod is not installed.");
            ToggleHotkey = config.Bind("General", "15. Toggle Hotkey",
                new KeyboardShortcut(KeyCode.T, KeyCode.LeftShift),
                "Quick key to pause or resume the pantry without opening the config.");

            ScoreHealthWeight = config.Bind("Pantry Scoring", "01. Health Weight", 50,
                new ConfigDescription("How much health matters when the pantry chooses a meal for an empty slot.", new AcceptableValueRange<int>(0, 100)));
            ScoreStaminaWeight = config.Bind("Pantry Scoring", "02. Stamina Weight", 50,
                new ConfigDescription("How much stamina matters in that choice.", new AcceptableValueRange<int>(0, 100)));
            ScoreDurationWeight = config.Bind("Pantry Scoring", "03. Duration Weight", 50,
                new ConfigDescription("How much long burn time matters.", new AcceptableValueRange<int>(0, 100)));
            ScoreRegenWeight = config.Bind("Pantry Scoring", "04. Regen Weight", 50,
                new ConfigDescription("How much regen matters.", new AcceptableValueRange<int>(0, 100)));

            AutoHealthMead = config.Bind("Health Mead", "01. Auto Health Mead", true,
                "Drinks a healing mead when you are hurt badly enough.");
            HealthMeadThreshold = config.Bind("Health Mead", "02. Health Threshold", 30,
                new ConfigDescription("Drink when your health falls below this percent.", new AcceptableValueRange<int>(1, 99)));
            HealthMeadOnlyEnemyHit = config.Bind("Health Mead", "03. Only On Enemy Hit", true,
                "Only after an enemy hit you, so a fall or lava does not waste the bottle.");
            HealthMeadEnemyHitWindow = config.Bind("Health Mead", "04. Enemy Hit Window", 5f,
                new ConfigDescription("How many seconds after that hit the mead is still allowed to fire.", new AcceptableValueRange<float>(1f, 30f)));
            HealthMeadRequireMaxHealth = config.Bind("Health Mead", "05. Require Sufficient Max Health", false,
                "Skip a mead that heals more than your max health and try a smaller one instead.");
            HealthMeadNotify = config.Bind("Health Mead", "06. Notify", true,
                "Tell you when a health mead was drunk.");

            AutoPoisonMead = config.Bind("Poison Resist Mead", "01. Auto Poison Mead", true,
                "Drinks poison resist when something venomous is coming for you.");
            PoisonMeadRange = config.Bind("Poison Resist Mead", "02. Detection Range", 15f,
                new ConfigDescription("How far to look for that kind of threat.", new AcceptableValueRange<float>(1f, 50f)));
            PoisonMeadNotify = config.Bind("Poison Resist Mead", "03. Notify", true,
                "Tell you when poison resist was drunk.");

            AutoFireMead = config.Bind("Fire Resist Mead", "01. Auto Fire Mead", true,
                "Drinks fire resist when something fiery is on you.");
            FireMeadRange = config.Bind("Fire Resist Mead", "02. Detection Range", 15f,
                new ConfigDescription("How far to look for fire threats.", new AcceptableValueRange<float>(1f, 50f)));
            FireMeadNotify = config.Bind("Fire Resist Mead", "03. Notify", true,
                "Tell you when fire resist was drunk.");

            AutoFrostMead = config.Bind("Frost Resist Mead", "01. Auto Frost Mead", true,
                "Drinks frost resist when you are freezing or taking repeated frost hits.");
            FrostMeadTickCount = config.Bind("Frost Resist Mead", "02. Frost Tick Count", 3,
                new ConfigDescription("How many frost hits in the window before it drinks.", new AcceptableValueRange<int>(1, 10)));
            FrostMeadTickWindow = config.Bind("Frost Resist Mead", "03. Frost Tick Window", 10f,
                new ConfigDescription("Seconds those frost hits are counted across.", new AcceptableValueRange<float>(2f, 60f)));
            FrostMeadNotify = config.Bind("Frost Resist Mead", "04. Notify", true,
                "Tell you when frost resist was drunk.");
        }
    }
}
