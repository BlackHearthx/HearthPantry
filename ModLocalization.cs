using System;
using System.Collections.Generic;
using System.IO;
using Jotunn.Managers;

namespace HearthPantry
{
    internal static class ModLocalization
    {
        private static readonly string[] Languages =
        {
            "English",
            "Portuguese_Brazilian",
            "Portuguese_European",
            "German",
            "French",
            "Spanish",
            "Russian",
            "Polish",
            "Dutch",
            "Italian",
            "Swedish",
            "Turkish",
            "Ukrainian",
            "Chinese",
            "Chinese_Trad",
            "Japanese",
            "Korean"
        };

        internal static void Register()
        {
            var loc = LocalizationManager.Instance.GetLocalization();
            var loaded = 0;
            bool englishLoaded = false;

            foreach (var lang in Languages)
            {
                if (TryLoad(loc, lang))
                {
                    loaded++;
                    if (lang == "English") englishLoaded = true;
                }
            }

            if (!englishLoaded)
            {
                RegisterInlineEnglish(loc);
                Jotunn.Logger.LogWarning("HearthPantry: Translations folder missing — English inline fallback");
            }
            else
            {
                Jotunn.Logger.LogInfo($"HearthPantry: loaded localization for {loaded} language(s)");
            }
        }

        private static bool TryLoad(Jotunn.Entities.CustomLocalization loc, string language)
        {
            try
            {
                var path = FindPath(language);
                if (string.IsNullOrEmpty(path) || !File.Exists(path))
                    return false;

                var json = File.ReadAllText(path);
                if (string.IsNullOrEmpty(json))
                    return false;

                loc.AddJsonFile(language, json);
                return true;
            }
            catch (Exception ex)
            {
                Jotunn.Logger.LogWarning($"HearthPantry: failed loading {language} — {ex.Message}");
                return false;
            }
        }

        private static string FindPath(string language)
        {
            var file = Path.Combine("Translations", language, "hearthpantry.json");
            var nextToDll = Path.Combine(
                Path.GetDirectoryName(typeof(HearthPantryPlugin).Assembly.Location) ?? "",
                file);
            if (File.Exists(nextToDll))
                return nextToDll;

            var cwd = Path.Combine(Directory.GetCurrentDirectory(), file);
            return File.Exists(cwd) ? cwd : null;
        }

        private static void RegisterInlineEnglish(Jotunn.Entities.CustomLocalization loc)
        {
            loc.AddTranslation("English", new Dictionary<string, string>
            {
                { "hearthpantry_expiry_almost", "{0} is almost gone" },
                { "hearthpantry_expiry_mins", "{0} is running low — about {1} min left" },
                { "hearthpantry_reeaten", "Had another {0}" },
                { "hearthpantry_ate_fill", "Ate {0} from the pantry" },
                { "hearthpantry_out", "Out of {0}" },
                { "hearthpantry_last", "Last {0} in the bag" },
                { "hearthpantry_low", "Only {0} {1} left" },
                { "hearthpantry_drank", "Drank {0}" },
                { "hearthpantry_toggle_on", "Pantry is watching your food" },
                { "hearthpantry_toggle_off", "Pantry paused — eat when you want" }
            });
        }

        internal static string L(string token, params object[] args)
        {
            var raw = Localization.instance != null
                ? Localization.instance.Localize("$" + token)
                : "$" + token;

            if (args == null || args.Length == 0)
                return raw;

            try
            {
                return string.Format(raw, args);
            }
            catch
            {
                return raw;
            }
        }
    }
}
