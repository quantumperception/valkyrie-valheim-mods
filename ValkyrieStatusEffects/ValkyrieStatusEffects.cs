using BepInEx;
using HarmonyLib;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using JSON = SimpleJson.SimpleJson;

namespace ValkyrieStatusEffects
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.ClientMustHaveMod, VersionStrictness.Minor)]
    public partial class Main : BaseUnityPlugin
    {

        public const string PluginGUID = "com.valkyrie.statuseffects";
        public const string PluginName = "Valkyrie's Status Effects";
        public const string PluginVersion = "0.0.1";
        public static Dictionary<int, StatusEffect> statusEffects = new Dictionary<int, StatusEffect>();
        private static readonly string pluginPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        private static readonly string configPath = BepInEx.Paths.ConfigPath;
        private static readonly string seFolderPath = Path.Combine(configPath, "ValkyrieStatusEffects");
        private static readonly string iconsPath = Path.Combine(seFolderPath, "Icons");
        private static Harmony harm = new Harmony("ValkyrieStatusEffects");

        void Awake()
        {
            SetupFiles();
            DefaultJSON();
            PrefabManager.OnVanillaPrefabsAvailable += () =>
            {
                CustomItem ci = new CustomItem("KerekMegChest", "ArmorBronzeChest");
                ci.ItemDrop.m_itemData.m_shared.m_equipStatusEffect = statusEffects.GetValueSafe("Kerek Meg".GetStableHashCode());
                Debug.Log("Kerek Meg: " + ci.ItemDrop.m_itemData.m_shared.m_equipStatusEffect);
                ItemManager.Instance.AddItem(ci);
            };
            harm.PatchAll();
        }

        void SetupFiles()
        {
            if (!Directory.Exists(seFolderPath)) Directory.CreateDirectory(seFolderPath);
        }
        void LoadAllSE()
        {
            foreach (string dir in Directory.GetDirectories(seFolderPath))
            {
                foreach (string file in Directory.GetFiles(dir))
                {
                    LoadSE(file);
                }
            }
            foreach (string file in Directory.GetFiles(seFolderPath))
            {
                LoadSE(file);
            }
        }

        void LoadSE(string path)
        {
            if (!File.Exists(path)) return;
        }

        void DefaultJSON()
        {
            ExtendedStatusEffect ese = ScriptableObject.CreateInstance<ExtendedStatusEffect>();
            ese.name = "Test Extended SE";
            ese.m_name = "Test Extended SE";
            ese.m_attributes = StatusEffect.StatusAttribute.DoubleImpactDamage;
            ese.m_mods = new List<HitData.DamageModPair>()
            {
                new HitData.DamageModPair() { m_modifier = HitData.DamageModifier.Immune, m_type = HitData.DamageType.Blunt},
                new HitData.DamageModPair() { m_modifier = HitData.DamageModifier.VeryWeak, m_type = HitData.DamageType.Elemental},
                new HitData.DamageModPair() { m_modifier = HitData.DamageModifier.Resistant, m_type = HitData.DamageType.Chop},
            };
            ese.m_raiseSkill = Skills.SkillType.BloodMagic;
            ese.m_maxEitr = 50;
            ese.m_maxStamina = 75;
            ese.m_maxHealth = 100;
            string json = JSON.SerializeObject(SerializeSE(ese));
            string defaultJsonPath = Path.Combine(seFolderPath, "SE_Default.json");
            File.WriteAllText(defaultJsonPath, json);
            DeserializeSE(File.ReadAllText(defaultJsonPath));
            CustomStatusEffect cse = new CustomStatusEffect(ese, false);
            statusEffects.Add(ese.name.GetStableHashCode(), cse.StatusEffect);
            ItemManager.Instance.AddStatusEffect(cse);
        }

        ExtendedStatusEffect DeserializeSE(Dictionary<string, object> sse)
        {
            return new ExtendedStatusEffect()
            {
                name = sse.GetTypedValue<string>("name"),
                m_name = sse.GetTypedValue<string>("m_name"),
                m_activationAnimation = sse.GetTypedValue<string>("m_activationAnimation"),
                m_attributes = DeserializeEnum<StatusEffect.StatusAttribute>(sse.GetTypedValue<string>("m_attributes")),
                m_category = sse.GetTypedValue<string>("m_category"),
                m_cooldown = sse.GetTypedValue<float>("m_cooldown"),
                m_repeatInterval = sse.GetTypedValue<float>("m_repeatInterval"),
                m_repeatMessage = sse.GetTypedValue<string>("m_repeatMessage"),
                m_repeatMessageType = DeserializeEnum<MessageHud.MessageType>(sse.GetTypedValue<string>("m_repeatMessageType")),
                m_startMessage = sse.GetTypedValue<string>("m_startMessage"),
                m_startMessageType = DeserializeEnum<MessageHud.MessageType>(sse.GetTypedValue<string>("m_startMessageType")),
                m_stopMessage = sse.GetTypedValue<string>("m_stopMessage"),
                m_stopMessageType = DeserializeEnum<MessageHud.MessageType>(sse.GetTypedValue<string>("m_stopMessageType")),
                m_tooltip = sse.GetTypedValue<string>("m_tooltip"),
                m_ttl = sse.GetTypedValue<float>("m_ttl"),
                m_tickInterval = sse.GetTypedValue<float>("m_tickInterval"),
                m_maxHealth = sse.GetTypedValue<float>("m_maxHealth"),
                m_maxStamina = sse.GetTypedValue<float>("m_maxStamina"),
                m_maxEitr = sse.GetTypedValue<float>("m_maxEitr"),
                m_maxMaxFallSpeed = sse.GetTypedValue<float>("m_maxMaxFallSpeed"),
                m_addMaxCarryWeight = sse.GetTypedValue<float>("m_addMaxCarryWeight"),
                m_damageModifier = sse.GetTypedValue<float>("m_damageModifier"),
                m_eitrOverTime = sse.GetTypedValue<float>("m_eitrOverTime"),
                m_eitrOverTimeDuration = sse.GetTypedValue<float>("m_eitrOverTimeDuration"),
                m_eitrRegenMultiplier = sse.GetTypedValue<float>("m_eitrRegenMultiplier"),
                m_fallDamageModifier = sse.GetTypedValue<float>("m_fallDamageModifier"),
                m_healthOverTime = sse.GetTypedValue<float>("m_healthOverTime"),
                m_healthOverTimeDuration = sse.GetTypedValue<float>("m_healthOverTimeDuration"),
                m_healthOverTimeInterval = sse.GetTypedValue<float>("m_healthOverTimeInterval"),
                m_healthPerTick = sse.GetTypedValue<float>("m_healthPerTick"),
                m_healthPerTickMinHealthPercentage = sse.GetTypedValue<float>("m_healthPerTickMinHealthPercentage"),
                m_healthRegenMultiplier = sse.GetTypedValue<float>("m_healthRegenMultiplier"),
                m_jumpModifier = ParseVector3(sse.GetTypedValue<string>("m_jumpModifier")),
                m_jumpStaminaUseModifier = sse.GetTypedValue<float>("m_jumpStaminaUseModifier"),
                m_modifyAttackSkill = DeserializeEnum<Skills.SkillType>(sse.GetTypedValue<string>("m_modifyAttackSkill")),
                m_mods = DeserializeDamageModifiers((Dictionary<string, string>)JSON.DeserializeObject(sse.GetTypedValue<string>("m_mods"))),
                m_noiseModifier = sse.GetTypedValue<float>("m_noiseModifier"),
                m_raiseSkill = DeserializeEnum<Skills.SkillType>(sse.GetTypedValue<string>("m_raiseSkill")),
                m_raiseSkillModifier = sse.GetTypedValue<float>("m_raiseSkillModifier"),
                m_runStaminaDrainModifier = sse.GetTypedValue<float>("m_runStaminaDrainModifier"),
                m_skillLevel = DeserializeEnum<Skills.SkillType>(sse.GetTypedValue<string>("m_skillLevel")),
                m_skillLevelModifier = sse.GetTypedValue<float>("m_skillLevelModifier"),
                m_skillLevel2 = DeserializeEnum<Skills.SkillType>(sse.GetTypedValue<string>("m_skillLevel2")),
                m_skillLevelModifier2 = sse.GetTypedValue<float>("m_skillLevelModifier2"),
                m_speedModifier = sse.GetTypedValue<float>("m_speedModifier"),
                m_staminaDrainPerSec = sse.GetTypedValue<float>("m_staminaDrainPerSec"),
                m_staminaOverTime = sse.GetTypedValue<float>("m_staminaOverTime"),
                m_staminaOverTimeDuration = sse.GetTypedValue<float>("m_staminaOverTimeDuration"),
                m_staminaRegenMultiplier = sse.GetTypedValue<float>("m_staminaRegenMultiplier"),
                m_stealthModifier = sse.GetTypedValue<float>("m_stealthModifier"),
                m_icon = Sprite.Create(LoadTexture(sse.GetTypedValue<string>("m_icon")), new Rect(Vector2.zero, new Vector2(48, 48)), new Vector2(24, 24)),
            };
        }

        string SerializeSE(ExtendedStatusEffect se)
        {
            Dictionary<string, object> dict = new Dictionary<string, object>
            {
                { "name", se.name },
                { "m_name", se.m_name },
                { "m_activationAnimation", se.m_activationAnimation },
                { "m_attributes", SerializeEnum(se.m_attributes) },
                { "m_category", se.m_category },
                { "m_cooldown", se.m_cooldown },
                { "m_repeatInterval", se.m_repeatInterval },
                { "m_repeatMessage", se.m_repeatMessage },
                { "m_repeatMessageType", SerializeEnum(se.m_repeatMessageType) },
                { "m_startMessage", se.m_startMessage },
                { "m_startMessageType", SerializeEnum(se.m_startMessageType) },
                { "m_stopMessage", se.m_stopMessage },
                { "m_stopMessageType", SerializeEnum(se.m_stopMessageType) },
                { "m_tooltip", se.m_tooltip },
                { "m_ttl", se.m_ttl },
                { "m_tickInterval", se.m_tickInterval },
                { "m_maxHealth", se.m_maxHealth },
                { "m_maxStamina", se.m_maxStamina },
                { "m_maxEitr", se.m_maxEitr },
                { "m_maxMaxFallSpeed", se.m_maxMaxFallSpeed },
                { "m_addMaxCarryWeight", se.m_addMaxCarryWeight },
                { "m_damageModifier", se.m_damageModifier },
                { "m_eitrOverTime", se.m_eitrOverTime },
                { "m_eitrOverTimeDuration", se.m_eitrOverTimeDuration },
                { "m_eitrRegenMultiplier", se.m_eitrRegenMultiplier },
                { "m_fallDamageModifier", se.m_fallDamageModifier },
                { "m_healthOverTime", se.m_healthOverTime },
                { "m_healthOverTimeDuration", se.m_healthOverTimeDuration },
                { "m_healthOverTimeInterval", se.m_healthOverTimeInterval },
                { "m_healthPerTick", se.m_healthPerTick },
                { "m_healthPerTickMinHealthPercentage", se.m_healthPerTickMinHealthPercentage },
                { "m_healthRegenMultiplier", se.m_healthRegenMultiplier },
                { "m_jumpModifier", $"{se.m_jumpModifier.x},{se.m_jumpModifier.y},{se.m_jumpModifier.z}" },
                { "m_jumpStaminaUseModifier", se.m_jumpStaminaUseModifier },
                { "m_modifyAttackSkill", se.m_modifyAttackSkill },
                { "m_mods", JSON.SerializeObject(SerializeDamageModifiers(se.m_mods)) },
                { "m_noiseModifier", se.m_noiseModifier },
                { "m_raiseSkill", SerializeEnum(se.m_raiseSkill) },
                { "m_raiseSkillModifier", se.m_raiseSkillModifier },
                { "m_runStaminaDrainModifier", se.m_runStaminaDrainModifier },
                { "m_skillLevel", SerializeEnum(se.m_skillLevel) },
                { "m_skillLevelModifier", se.m_skillLevelModifier },
                { "m_skillLevel2", SerializeEnum(se.m_skillLevel2) },
                { "m_skillLevelModifier2", se.m_skillLevelModifier2 },
                { "m_speedModifier", se.m_speedModifier },
                { "m_staminaDrainPerSec", se.m_staminaDrainPerSec },
                { "m_staminaOverTime", se.m_staminaOverTime },
                { "m_staminaOverTimeDuration", se.m_staminaOverTimeDuration },
                { "m_staminaRegenMultiplier", se.m_staminaRegenMultiplier },
                { "m_stealthModifier", se.m_stealthModifier },
                { "m_icon", se.m_icon?.name }
        };
            return JSON.SerializeObject(dict);
        }

        Dictionary<string, string> SerializeDamageModifiers(List<HitData.DamageModPair> dmps)
        {
            Dictionary<string, string> serialized = new Dictionary<string, string>();
            foreach (HitData.DamageModPair dmp in dmps)
            {
                Debug.Log($"Serializing DMP: {SerializeEnum(dmp.m_type)}, {SerializeEnum(dmp.m_modifier)}");
                serialized.Add(SerializeEnum(dmp.m_type), SerializeEnum(dmp.m_modifier));
            }
            return serialized;
        }

        List<HitData.DamageModPair> DeserializeDamageModifiers(Dictionary<string, string> dmps)
        {
            List<HitData.DamageModPair> list = new List<HitData.DamageModPair>();
            foreach (KeyValuePair<string, string> dmp in dmps)
            {
                list.Add(new HitData.DamageModPair()
                {
                    m_type = DeserializeEnum<HitData.DamageType>(dmp.Key),
                    m_modifier = DeserializeEnum<HitData.DamageModifier>(dmp.Value),
                });
            }
            return list;
        }

        public T DeserializeEnum<T>(string e)
        {
            return (T)System.Enum.Parse(typeof(T), e);
        }

        public string SerializeEnum<T>(T e)
        {
            return System.Enum.GetName(typeof(T), e);
        }


        Vector3 ParseVector3(string v)
        {
            string[] axis = v.Split(',');
            if (axis.Length != 3) return Vector3.zero;
            return new Vector3(float.Parse(axis[0]), float.Parse(axis[1]), float.Parse(axis[2]));
        }

        Texture2D LoadTexture(string path, int w = 48, int h = 48)
        {
            Texture2D tex = new Texture2D(w, h);
            if (!File.Exists(path)) return tex;
            ImageConversion.LoadImage(tex, File.ReadAllBytes(path));
            return tex;
        }

        StatusEffect DeserializeSE(string json)
        {
            return null;
        }

        [HarmonyPatch]
        class Patches
        {
            static readonly Converter<StatusEffect, ExtendedStatusEffect> SEtoESEConverter = new Converter<StatusEffect, ExtendedStatusEffect>(se => (ExtendedStatusEffect)se);
            static List<ExtendedStatusEffect> GetExtendedSEs(SEMan seman)
            {
                return seman.GetStatusEffects().FindAll(se => se.GetType() == typeof(ExtendedStatusEffect)).ConvertAll(SEtoESEConverter);
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(Attack), nameof(Attack.ModifyDamage))]
            static void Attack_ModifyDamagePatch(ref HitData hitData, ref float damageFactor)
            {
                List<ExtendedStatusEffect> eses = GetExtendedSEs(hitData.GetAttacker().GetSEMan());
                foreach (ExtendedStatusEffect ese in eses)
                {
                    foreach (KeyValuePair<Skills.SkillType, float> wdm in ese.m_weaponDamageModifiers)
                    {
                        if (hitData.m_skill != wdm.Key) continue;
                        hitData.m_damage.Modify(1 + wdm.Value);
                    }
                }
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
            static void Character_RPC_DamagePatch(Character __instance, long sender, ref HitData hit)
            {
                List<ExtendedStatusEffect> eses = GetExtendedSEs(__instance.GetSEMan());
                float damageReductionPercentage = 0f;
                foreach (ExtendedStatusEffect ese in eses)
                {
                    damageReductionPercentage += ese.m_damageReduction;
                }
                float damageReduction = 1f - damageReductionPercentage;
                hit.m_damage.Modify(damageReduction);
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(Player), nameof(Player.GetTotalFoodValue))]
            static bool Player_UpdateStatsPatch(Player __instance, out float hp, out float stamina, out float eitr)
            {
                hp = __instance.m_baseHP;
                stamina = __instance.m_baseStamina;
                eitr = 0f;
                foreach (Player.Food food in __instance.GetFoods())
                {
                    hp += food.m_health;
                    stamina += food.m_stamina;
                    eitr += food.m_eitr;
                };
                List<ExtendedStatusEffect> eses = GetExtendedSEs(__instance.GetSEMan());
                foreach (ExtendedStatusEffect ese in eses)
                {
                    hp += ese.m_maxHealth;
                    stamina += ese.m_maxStamina;
                    eitr += ese.m_maxEitr;
                }
                return false;
            }
        }
    }
}
