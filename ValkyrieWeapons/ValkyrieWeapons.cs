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
        private static Skills.SkillType HealingSkill = 0;
        public static CustomLocalization Localization;
        public static AssetBundle bundle;
        public static GameObject[] assets;
        public static List<GameObject> weapons = new List<GameObject>();
        public static List<StatusEffect> statusEffects = new List<StatusEffect>();
        public static Dictionary<string, GameObject> vfx = new Dictionary<string, GameObject>();
        private static readonly string pluginPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        private static readonly string configPath = BepInEx.Paths.ConfigPath;
        private static Harmony harm = new Harmony("ValkyrieWeapons");


        void Awake()
        {
            bundle = AssetUtils.LoadAssetBundleFromResources("weapons");
            assets = bundle.LoadAllAssets<GameObject>();
            PrefabManager.OnVanillaPrefabsAvailable += OnVanillaPrefabsAvailable;
            harm.PatchAll();
            AddHealingSkill();
        }
        void AddHealingSkill()
        {
            
            SkillConfig hs= new SkillConfig()
            {
                Identifier = "com.valkyrie.weapons.healing",
                Name = "Healing",
                Description = "Enhances healing weapons",
                IconPath = "healing-sprite.png",
                IncreaseStep = 1f
            };
            HealingSkill = SkillManager.Instance.AddSkill(hs);
            Debug.Log("Added Healing Skill");
        }

        void OnVanillaPrefabsAvailable()
        {
            if (addedPrefabs) return;
            foreach (GameObject prefab in assets)
            {
                Debug.Log("Asset: " + prefab.name);
                if (prefab.GetComponent<ItemDrop>() != null) { Debug.Log("Adding to weapon list: " + prefab.name); weapons.Add(prefab); continue; }
                PrefabManager.Instance.AddPrefab(prefab);
            }
            AddVFX();
            AddStatusEffects();
            foreach (GameObject weapon in weapons)
            {
                CustomItem weaponItem = new CustomItem(weapon, false, new ItemConfig
                {
                    Enabled = true,
                    CraftingStation = CraftingStations.None,
                    Name = weapon.name,
                    Icon = RenderManager.Instance.Render(weapon, RenderManager.IsometricRotation),
                    Requirements = new RequirementConfig[]
                    {
                        new RequirementConfig()
                        {
                            Item = "Wood",
                            Amount = 1,
                        }
                    },
                });
                SetupStats(weapon);
                ItemManager.Instance.AddItem(weaponItem);
                Debug.Log("Added Weapon: " + weapon.name);
            }
            addedPrefabs = true;
        }

        void AddVFX()
        {
            AddHealingAuraVFX();
        }
        void AddHealingAuraVFX()
        {
            GameObject healVfx = PrefabManager.Instance.GetPrefab("vfx_QP_Healing");
            ParticleSystem particleSystem = healVfx.GetComponent<ParticleSystem>();
            ParticleSystem.MainModule ps = particleSystem.main;
            ParticleSystem.ShapeModule sm = particleSystem.shape;
            ParticleSystem.ColorOverLifetimeModule col = particleSystem.colorOverLifetime;
            sm.radius = 0.6f;
            ps.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.2f);
            Color transparentGreen = new Color(5, 255, 0, 67);
            ps.startColor = transparentGreen;
            col.color.gradient.alphaKeys = new GradientAlphaKey[] {
            new GradientAlphaKey(0f, 0f),
            new GradientAlphaKey(1f, 0.2f),
            new GradientAlphaKey(0f, 1f)
            };
            col.color.gradient.colorKeys = new GradientColorKey[]
            {
                new GradientColorKey(Color.clear, 0f),
                new GradientColorKey(transparentGreen, 0.2f),
                new GradientColorKey(transparentGreen, 1f),
            };
            vfx.Add("HealingAura", healVfx);
        }
        void AddStatusEffects()
        {
            AddHealingAuraSE();
            AddHolyGroundSE();
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
            EffectList.EffectData[] startEffectPrefabs = new EffectList.EffectData[1];
            EffectList.EffectData healEffect = new EffectList.EffectData()
            {
                m_prefab = vfx.GetValueSafe("HealingAura"),
                m_enabled = true,
                m_attach = true,
                m_inheritParentRotation = false,
                m_variant = -1,
                m_scale = true
            };
            startEffectPrefabs[0] = healEffect;
            effect.m_startEffects.m_effectPrefabs = startEffectPrefabs;
            CustomStatusEffect cse = new CustomStatusEffect(effect, false);
            statusEffects.Add(cse.StatusEffect);
            ItemManager.Instance.AddStatusEffect(cse);
            Debug.Log("Added SE: " + effect.m_name);
        }

        void AddHolyGroundSE()
        {
            SE_Stats effect = ScriptableObject.CreateInstance<SE_Stats>();
            effect.name = "Holy Ground";
            effect.m_name = "Holy Ground";
            effect.m_icon = Sprite.Create(new Texture2D(24, 24), new Rect { width = 10, height = 10 }, new Vector2());
            effect.m_startMessageType = MessageHud.MessageType.TopLeft;
            effect.m_startMessage = "Holy Ground";
            effect.m_stopMessageType = MessageHud.MessageType.TopLeft;
            effect.m_stopMessage = "Exited Holy Ground";
            effect.m_healthOverTime = 20f;
            effect.m_healthOverTimeInterval = 0.5f;
            effect.m_ttl = 0.1f;
            EffectList.EffectData[] startEffectPrefabs = new EffectList.EffectData[1];
            EffectList.EffectData healEffect = new EffectList.EffectData()
            {
                m_prefab = vfx.GetValueSafe("HealingAura"),
                m_enabled = true,
                m_attach = true,
                m_inheritParentRotation = false,
                m_variant = -1,
                m_scale = true
            };
            startEffectPrefabs[0] = healEffect;
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
            Debug.Log("SetupStats: " + prefab.name);
        }

        void SetupDruidStaff(GameObject prefab)
        {
            ItemDrop.ItemData.SharedData data = prefab.GetComponent<ItemDrop>().m_itemData.m_shared;
            data.m_skillType = HealingSkill;
            data.m_itemType = ItemDrop.ItemData.ItemType.OneHandedWeapon;
            data.m_attackStatusEffect = GetStatusEffect("Healing Aura");
            data.m_attack.m_attackType = Attack.AttackType.Projectile;
            data.m_attack.m_attackAnimation = "swing_axe";
            data.m_attack.m_attackChainLevels = 3;
            data.m_attack.m_launchAngle = -15f;
            data.m_attack.m_destroyPreviousProjectile = true;
            data.m_attack.m_attackEitr = 20f;

            Debug.Log("Getting HolySlash AOE Heal");
            GameObject aoeHeal = PrefabManager.Instance.GetPrefab("QP_DruidStaff_HolySlash_heal_aoe");
            aoeHeal.transform.Find("collider").localScale = new Vector3(2.5f, 0.2f, 2.5f);
            Debug.Log("Edit slash");
            Transform slash = aoeHeal.transform.Find("slash");
            slash.localScale = new Vector3(1f, 1f, 1f);
            Debug.Log("Edit Heal AOE");
            Aoe healAoe = aoeHeal.GetComponent<Aoe>();
            healAoe.m_useAttackSettings = false;
            healAoe.m_statusEffect = "Healing Aura";
            healAoe.m_hitOwner = false;
            healAoe.m_hitEnemy = false;
            healAoe.m_ttl = 1f;

            Debug.Log("Getting HolyGround AOE");
            GameObject aoeGround = PrefabManager.Instance.GetPrefab("QP_DruidStaff_HolyGround_aoe");
            Aoe groundAoe = aoeGround.GetComponent<Aoe>();
            groundAoe.m_useAttackSettings = false;
            groundAoe.m_statusEffect = "Holy Ground";
            groundAoe.m_hitEnemy = false;
            groundAoe.m_radius = 7f;
            groundAoe.m_ttl = 10f;
            groundAoe.m_hitInterval = 0.1f;
            data.m_secondaryAttack.m_attackProjectile = aoeGround;
            data.m_secondaryAttack.m_attackAngle = 0f;
            data.m_secondaryAttack.m_useCharacterFacing = true;
            data.m_secondaryAttack.m_useCharacterFacingYAim = false;
            data.m_secondaryAttack.m_destroyPreviousProjectile = true;
        }

        static StatusEffect GetStatusEffect(string name)
        {
            return statusEffects.Find(se => se.name == name);
        }

        [HarmonyPatch]
        class Patches
        {
            [HarmonyPostfix]
            [HarmonyPatch(typeof(Attack), nameof(Attack.Start))]
            static void Attack_StartPatch(Attack __instance, Humanoid character)
            {
                if (Player.m_localPlayer == null || !character.IsPlayer()) return;
                GameObject druidStaff = PrefabManager.Instance.GetPrefab("QP_DruidStaff");
                ItemDrop itemDrop = druidStaff.GetComponent<ItemDrop>();
                Debug.Log($"{character.GetCurrentWeapon()}  |  {druidStaff}  |  {itemDrop}");
                if (character.GetCurrentWeapon()?.m_shared.m_name != itemDrop.m_itemData.m_shared.m_name) return;
                Debug.Log("Attack reflection");
                Type attack = __instance.GetType();
                int attackChainLevel = (int)attack?.GetField("m_currentAttackCainLevel", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(__instance);
                Debug.Log("Chain: " + attackChainLevel);
                bool rtl = attackChainLevel % 2 == 0;
                GameObject aoeHeal = PrefabManager.Instance.GetPrefab("QP_DruidStaff_HolySlash_heal_aoe");
                aoeHeal.transform.Find("slash").eulerAngles = new Vector3(0f, rtl ? 90f : 0f, rtl ? 180f : 0f);
                character.RaiseSkill(HealingSkill, 1);
            }
        }

    }
}