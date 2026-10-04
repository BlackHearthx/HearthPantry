using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;
using ItemData = ItemDrop.ItemData;

namespace HearthPantry
{
    internal static class MeadManager
    {
        private const float MeleeRangeSq = 9f;

        private static float _lastEnemyHitTime = float.MinValue;
        private static readonly Queue<float> _frostHitTimes = new Queue<float>();
        private static readonly List<Character> _nearby = new List<Character>();
        private static Player _trackedPlayer;

        private static void RefreshState()
        {
            var player = Player.m_localPlayer;
            if (!ReferenceEquals(player, _trackedPlayer) || !PluginConfig.ModEnabled.Value
                || !FoodManager.CanAutomate(player))
            {
                _trackedPlayer = player;
                _lastEnemyHitTime = float.MinValue;
                _frostHitTimes.Clear();
            }
            float cutoff = Time.time - PluginConfig.FrostMeadTickWindow.Value;
            while (_frostHitTimes.Count > 0 && _frostHitTimes.Peek() < cutoff)
                _frostHitTimes.Dequeue();
            if (!PluginConfig.AutoFrostMead.Value)
                _frostHitTimes.Clear();
        }

        public static void OnDamage(Character character, HitData hit)
        {
            if (character != Player.m_localPlayer || hit == null)
                return;
            RefreshState();
            if (!PluginConfig.ModEnabled.Value || !FoodManager.CanAutomate(Player.m_localPlayer))
                return;
            var attacker = hit.GetAttacker();
            if (attacker != null && !attacker.IsPlayer() && BaseAI.IsEnemy(attacker, character))
                _lastEnemyHitTime = Time.time;

            if (PluginConfig.AutoFrostMead.Value && hit.m_damage.m_frost > 0f)
            {
                _frostHitTimes.Enqueue(Time.time);
                while (_frostHitTimes.Count > PluginConfig.FrostMeadTickCount.Value)
                    _frostHitTimes.Dequeue();
            }
        }

        public static void Tick()
        {
            RefreshState();
            if (!PluginConfig.ModEnabled.Value || !FoodManager.CanAutomate(Player.m_localPlayer))
                return;

            CheckHealthMead();
            CheckProximityResistMeads();
            CheckFrostResistMead();
        }

        private static void CheckHealthMead()
        {
            if (!PluginConfig.AutoHealthMead.Value)
                return;

            var player = Player.m_localPlayer;
            if (player == null)
                return;

            if (player.GetHealthPercentage() * 100f >= PluginConfig.HealthMeadThreshold.Value)
                return;

            if (PluginConfig.HealthMeadOnlyEnemyHit.Value
                && Time.time - _lastEnemyHitTime > PluginConfig.HealthMeadEnemyHitWindow.Value)
                return;

            var inventory = player.GetInventory();
            var seman = player.GetSEMan();
            float maxHealth = player.GetMaxHealth();

            foreach (var mead in GetHealthMeads(inventory))
            {
                var se = mead.m_shared.m_consumeStatusEffect;
                if (EffectAlreadyActive(seman, se))
                    continue;

                if (PluginConfig.HealthMeadRequireMaxHealth.Value
                    && se is SE_Stats stats
                    && maxHealth < stats.m_healthOverTime)
                    continue;

                if (!player.ConsumeItem(inventory, mead))
                    continue;

                if (PluginConfig.HealthMeadNotify.Value)
                    ShowHud(ModLocalization.L("hearthpantry_drank", Localized(mead)));
                break;
            }
        }

        private static void CheckProximityResistMeads()
        {
            bool wantPoison = PluginConfig.AutoPoisonMead.Value;
            bool wantFire = PluginConfig.AutoFireMead.Value;
            if (!wantPoison && !wantFire)
                return;

            var player = Player.m_localPlayer;
            if (player == null)
                return;

            float range = 0f;
            if (wantPoison)
                range = Mathf.Max(range, PluginConfig.PoisonMeadRange.Value);
            if (wantFire)
                range = Mathf.Max(range, PluginConfig.FireMeadRange.Value);

            bool needPoison = false;
            bool needFire = false;
            _nearby.Clear();
            Character.GetCharactersInRange(player.transform.position, range, _nearby);

            float poisonRangeSq = PluginConfig.PoisonMeadRange.Value * PluginConfig.PoisonMeadRange.Value;
            float fireRangeSq = PluginConfig.FireMeadRange.Value * PluginConfig.FireMeadRange.Value;

            foreach (var c in _nearby)
            {
                if (c == null || c.IsPlayer() || c.IsDead())
                    continue;

                float distSq = (c.transform.position - player.transform.position).sqrMagnitude;
                if (!IsThreatening(c, player, distSq))
                    continue;

                GetThreatDamage(c, out bool poison, out bool fire);

                if (wantPoison && !needPoison && distSq <= poisonRangeSq && poison)
                    needPoison = true;
                if (wantFire && !needFire && distSq <= fireRangeSq && fire)
                    needFire = true;

                if (needPoison && needFire)
                    break;
            }

            if (needPoison)
                ConsumeResistMead(player, PluginConfig.PoisonMeadNotify, HitData.DamageType.Poison);
            if (needFire)
                ConsumeResistMead(player, PluginConfig.FireMeadNotify, HitData.DamageType.Fire);
        }

        private static void CheckFrostResistMead()
        {
            if (!PluginConfig.AutoFrostMead.Value)
                return;

            var player = Player.m_localPlayer;
            if (player == null)
                return;

            float cutoff = Time.time - PluginConfig.FrostMeadTickWindow.Value;
            while (_frostHitTimes.Count > 0 && _frostHitTimes.Peek() < cutoff)
                _frostHitTimes.Dequeue();

            var seman = player.GetSEMan();
            bool freezing = seman.HaveStatusEffect("Freezing".GetStableHashCode());
            bool enoughHits = _frostHitTimes.Count >= PluginConfig.FrostMeadTickCount.Value;
            if (!freezing && !enoughHits)
                return;

            var inventory = player.GetInventory();
            foreach (var mead in GetResistMeads(inventory, HitData.DamageType.Frost))
            {
                var se = mead.m_shared.m_consumeStatusEffect;
                if (EffectAlreadyActive(seman, se))
                {
                    _frostHitTimes.Clear();
                    break;
                }

                if (!player.ConsumeItem(inventory, mead))
                    continue;

                _frostHitTimes.Clear();
                if (PluginConfig.FrostMeadNotify.Value)
                    ShowHud(ModLocalization.L("hearthpantry_drank", Localized(mead)));
                break;
            }
        }

        private static void ConsumeResistMead(Player player, ConfigEntry<bool> notify, HitData.DamageType type)
        {
            var inventory = player.GetInventory();
            var seman = player.GetSEMan();

            foreach (var mead in GetResistMeads(inventory, type))
            {
                var se = mead.m_shared.m_consumeStatusEffect;
                if (EffectAlreadyActive(seman, se))
                    break;

                if (!player.ConsumeItem(inventory, mead))
                    continue;

                if (notify.Value)
                    ShowHud(ModLocalization.L("hearthpantry_drank", Localized(mead)));
                break;
            }
        }

        private static bool IsThreatening(Character c, Player localPlayer, float distSq)
        {
            if (!BaseAI.IsEnemy(c, localPlayer))
                return false;
            if (distSq <= MeleeRangeSq)
                return true;

            var ai = c.GetComponent<MonsterAI>();
            if (ai != null)
                return ai.GetTargetCreature() == localPlayer;

            return false;
        }

        private static bool EffectAlreadyActive(SEMan seman, StatusEffect effect)
        {
            return effect != null && (seman.HaveStatusEffect(effect.NameHash())
                || (!string.IsNullOrEmpty(effect.m_category) && seman.HaveStatusEffectCategory(effect.m_category)));
        }

        private static void GetThreatDamage(Character c, out bool poison, out bool fire)
        {
            poison = false;
            fire = false;

            if (!(c is Humanoid humanoid))
            {
                return;
            }

            foreach (var item in humanoid.GetInventory().GetAllItems())
            {
                if (item.m_shared.m_damages.m_poison > 0f)
                    poison = true;
                if (item.m_shared.m_damages.m_fire > 0f)
                    fire = true;
            }

            var weapon = humanoid.GetCurrentWeapon();
            if (weapon != null)
            {
                if (weapon.m_shared.m_damages.m_poison > 0f)
                    poison = true;
                if (weapon.m_shared.m_damages.m_fire > 0f)
                    fire = true;
            }

        }

        private static List<ItemData> GetHealthMeads(Inventory inventory)
        {
            var list = new List<ItemData>();
            foreach (var item in inventory.GetAllItems())
            {
                if (item.m_shared.m_itemType != ItemData.ItemType.Consumable)
                    continue;
                if (item.m_shared.m_consumeStatusEffect is SE_Puke)
                    continue;
                if (item.m_shared.m_consumeStatusEffect is SE_Stats stats && stats.m_healthOverTime > 0f)
                    list.Add(item);
            }

            list.Sort((a, b) =>
            {
                float ha = ((SE_Stats)a.m_shared.m_consumeStatusEffect).m_healthOverTime;
                float hb = ((SE_Stats)b.m_shared.m_consumeStatusEffect).m_healthOverTime;
                return hb.CompareTo(ha);
            });
            return list;
        }

        private static List<ItemData> GetResistMeads(Inventory inventory, HitData.DamageType type)
        {
            var list = new List<ItemData>();
            foreach (var item in inventory.GetAllItems())
            {
                if (item.m_shared.m_itemType != ItemData.ItemType.Consumable)
                    continue;
                if (item.m_shared.m_consumeStatusEffect is SE_Puke)
                    continue;
                if (!(item.m_shared.m_consumeStatusEffect is SE_Stats stats))
                    continue;

                foreach (var mod in stats.m_mods)
                {
                    if (mod.m_type == type && IsProtective(mod.m_modifier))
                    {
                        list.Add(item);
                        break;
                    }
                }
            }

            return list;
        }

        internal static bool IsProtective(HitData.DamageModifier modifier)
        {
            return modifier == HitData.DamageModifier.Resistant
                || modifier == HitData.DamageModifier.VeryResistant
                || modifier == HitData.DamageModifier.SlightlyResistant
                || modifier == HitData.DamageModifier.Immune;
        }

        private static string Localized(ItemData item)
        {
            return Localization.instance.Localize(item.m_shared.m_name);
        }

        private static void ShowHud(string message)
        {
            if (MessageHud.instance == null)
                return;
            MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, message);
        }
    }
}
