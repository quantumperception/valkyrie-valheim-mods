using BepInEx;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace ValkyrieWeapons
{

    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.ClientMustHaveMod, VersionStrictness.Minor)]
    public partial class ValkyrieWeapons : BaseUnityPlugin
    {

        public const string PluginGUID = "com.valkyrie.weapons";
        public const string PluginName = "Valkyrie's Weapons";
        public const string PluginVersion = "0.0.1";
        private bool addedPrefabs = false;
        public static CustomLocalization Localization;
        public static AssetBundle bundle;
        public static GameObject[] weapons;
        public static List<StatusEffect> statusEffects = new List<StatusEffect>();
        private static readonly string pluginPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        private static readonly string configPath = BepInEx.Paths.ConfigPath;
        private static Harmony harm = new Harmony("ValkyrieWeapons");


        void Awake()
        {
            bundle = AssetUtils.LoadAssetBundleFromResources("weapons");
            weapons = bundle.LoadAllAssets<GameObject>();
            AddStatusEffects();
            PrefabManager.OnVanillaPrefabsAvailable += OnVanillaPrefabsAvailable;
            harm.PatchAll();
        }

        void OnVanillaPrefabsAvailable()
        {
            if (addedPrefabs) return;
            foreach (GameObject prefab in weapons)
            {
                if (prefab.GetComponent<ItemDrop>() == null)
                {
                    PrefabManager.Instance.AddPrefab(prefab);
                    continue;
                }
                SetupStats(prefab);
                CustomItem weapon = new CustomItem(prefab, false, new ItemConfig
                {
                    Enabled = true,
                    CraftingStation = CraftingStations.None,
                    Name = prefab.name,
                    Icon = RenderManager.Instance.Render(prefab, RenderManager.IsometricRotation),
                    Requirements = new RequirementConfig[]
                    {
                        new RequirementConfig()
                        {
                            Item = "Wood",
                            Amount = 1,
                        }
                    }
                });
                var shared = weapon.ItemDrop.m_itemData.m_shared;
                ItemManager.Instance.AddItem(weapon);
                Debug.Log("Added Weapon: " + prefab.name);
            }
            addedPrefabs = true;
        }
        void AddStatusEffects()
        {
            AddHealingAuraSE();
        }

        void AddHealingAuraSE()
        {
            SE_Stats effect = ScriptableObject.CreateInstance<SE_Stats>();
            effect.name = "Healing Aura";
            effect.m_name = "Healing Aura";
            effect.m_icon = Sprite.Create(new Texture2D(24, 24), new Rect { width = 10, height = 10 }, new Vector2());
            effect.m_startMessageType = MessageHud.MessageType.TopLeft;
            effect.m_startMessage = "Healing Aura";
            effect.m_stopMessageType = MessageHud.MessageType.TopLeft;
            effect.m_stopMessage = "Healing Aura expired";
            effect.m_healthOverTime = 20f;
            effect.m_healthOverTimeInterval = 0.5f;
            effect.m_ttl = 4f;
            GameObject healVfx = bundle.LoadAsset<GameObject>("vfx_QP_Healing");
            Debug.Log(healVfx);
            EffectList.EffectData[] startEffectPrefabs = { };
            EffectList.EffectData healEffect = new EffectList.EffectData()
            {
                m_prefab = healVfx,
                m_enabled = true,
                m_attach = true,
                m_variant = -1,
                m_scale = true
            };
            startEffectPrefabs.AddItem(healEffect);
            effect.m_startEffects.m_effectPrefabs = startEffectPrefabs;
            CustomStatusEffect cse = new CustomStatusEffect(effect, false);
            statusEffects.Add(cse.StatusEffect);
            ItemManager.Instance.AddStatusEffect(cse);
            Debug.Log("Added SE: " + effect.m_name);
        }

        void SetupStats(GameObject prefab)
        {
            switch (prefab.name)
            {
                case "QP_DruidStaff":
                    SetupDruidStaff(prefab);
                    break;
                default:
                    break;
            }
        }

        void SetupDruidStaff(GameObject prefab)
        {
            ItemDrop.ItemData.SharedData data = prefab.GetComponent<ItemDrop>().m_itemData.m_shared;
            data.m_itemType = ItemDrop.ItemData.ItemType.OneHandedWeapon;
            data.m_attackStatusEffect = GetStatusEffect("Healing Aura");
            data.m_attack.m_attackType = Attack.AttackType.Projectile;
            data.m_attack.m_attackAnimation = "swing_axe";
            data.m_attack.m_attackChainLevels = 3;
            data.m_attack.m_launchAngle = -15f;
            GameObject aoeHeal = PrefabManager.Instance.GetPrefab("QP_DruidStaff_heal_aoe");
            aoeHeal.transform.Find("collider").localScale = new Vector3(2.5f, 0f, 2.5f);
            Transform slash = aoeHeal.transform.Find("slash");
            slash.localScale = new Vector3(1f, 1f, 1f);
            Aoe aoe = aoeHeal.GetComponent<Aoe>();
            aoe.m_useAttackSettings = true;
            aoe.m_hitOwner = true;
            aoe.m_hitEnemy = true;
            aoe.m_ttl = 1f;
            data.m_attack.m_attackProjectile = aoeHeal;
        }

        static StatusEffect GetStatusEffect(string name)
        {
            return statusEffects.Find(se => se.name == "Healing Aura");
        }

        [HarmonyPatch]
        class Patches
        {
            [HarmonyPostfix]
            [HarmonyPatch(typeof(Attack), nameof(Attack.Start))]
            static void Attack_StartPatch(Attack __instance, Humanoid character)
            {
                if (Player.m_localPlayer == null || !character.IsPlayer()) return;
                CustomItem druidStaff = ItemManager.Instance.GetItem("QP_DruidStaff");
                Debug.Log($"Attack Start Patch | {druidStaff}");
                Debug.Log($"{Player.m_localPlayer.GetCurrentWeapon()}");
                if (Player.m_localPlayer.GetCurrentWeapon()?.m_shared.m_name != druidStaff.ItemDrop.m_itemData.m_shared.m_name) return;
                Debug.Log("Reflection 1 | " + Player.m_localPlayer);
                Type player = Player.m_localPlayer.GetType();
                Debug.Log("Reflection 2 | " + __instance);
                int attackChainLevel = (int)__instance?.GetType().GetField("m_currentAttackCainLevel", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(__instance);
                Debug.Log("Chain: " + attackChainLevel);
                bool rtl = attackChainLevel % 2 == 0;
                Debug.Log("Flipping DruidStaff Slash: " + !rtl);
                GameObject aoeHeal = PrefabManager.Instance.GetPrefab("QP_DruidStaff_heal_aoe");
                aoeHeal.transform.Find("slash").eulerAngles = new Vector3(0f, rtl ? 90f : 0f, rtl ? 180f : 0f);
            }
        }

    }
}