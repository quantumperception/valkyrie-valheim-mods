using BepInEx;
using BlacksmithTools;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using ValkyrieStatusEffects;
using JotunnItemManager = Jotunn.Managers.ItemManager;

namespace ValkyrieArmors
{

    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [BepInDependency(BlacksmithTools.Main.GUID)]
    [BepInDependency(ValkyrieStatusEffects.Main.PluginGUID)]
    [NetworkCompatibility(CompatibilityLevel.ClientMustHaveMod, VersionStrictness.Minor)]
    public partial class ValkyrieArmors : BaseUnityPlugin
    {
        private enum PlayerBone
        {
            Hips,
            Spine,
            Spine1,
            Spine2,
            Neck,
            Head,
            Jaw,
            LeftShoulder,
            LeftArm,
            LeftForeArm,
            LeftHand,
            LeftHandThumb1,
            LeftHandThumb2,
            LeftHandThumb3,
            LeftHandIndex1,
            LeftHandIndex2,
            LeftHandIndex3,
            LeftHandMiddle1,
            LeftHandMiddle2,
            LeftHandMiddle3,
            LeftHandRing1,
            LeftHandRing2,
            LeftHandRing3,
            LeftHandPinky1,
            LeftHandPinky2,
            LeftHandPinky3,
            RightShoulder,
            RightArm,
            RightForeArm,
            RightHand,
            RightHandThumb1,
            RightHandThumb2,
            RightHandThumb3,
            RightHandIndex1,
            RightHandIndex2,
            RightHandIndex3,
            RightHandMiddle1,
            RightHandMiddle2,
            RightHandMiddle3,
            RightHandRing1,
            RightHandRing2,
            RightHandRing3,
            RightHandPinky1,
            RightHandPinky2,
            RightHandPinky3,
            LeftUpLeg,
            LeftLeg,
            LeftFoot,
            LeftToeBase,
            RightUpLeg,
            RightLeg,
            RightFoot,
            RightToeBase,
        }



        private enum SetPieceType
        {
            Helmet,
            Armor,
            Legs,
            Cape
        }

        public const string PluginGUID = "com.valkyrie.armors";
        public const string PluginName = "Valkyrie's Armors";
        public const string PluginVersion = "0.0.1";
        private bool addedPrefabs = false;
        private bool addedArmors = false;
        public static CustomLocalization Localization;
        public static AssetBundle bundle;
        public static readonly string[] setTypes = new[] {
            "leather",
            "bronze",
            "iron",
            "silver",
            "blackmetal",
            "carapace",
            };
        public static GameObject[] armors;
        public static List<string> parsedFiles = new List<string>();
        public static List<string> customArmors = new List<string>();
        public static List<string> armorSets = new List<string>();
        public static List<GameObject> addedBorders = new List<GameObject>();
        private static readonly string configPath = BepInEx.Paths.ConfigPath;
        private static readonly string assetsPath = Path.Combine(BepInEx.Paths.PluginPath, "Assets", "ValkyrieArmors");
        private static readonly Texture2D borderTex = AssetUtils.LoadTexture(Path.Combine(assetsPath, "item-border.png"));
        private static readonly string armorConfigPath = Path.Combine(configPath, "ValkyrieArmors");
        private static readonly Harmony harm = new Harmony("ValkyrieArmors");
        private static readonly List<string> unisexChestPrefabs = new List<string>()
        {
            "EspartaPecho",
            "MayaPecho",
        };
        private static readonly List<string> unisexLegPrefabs = new List<string>()
        {
            "EspartaGrebas",
            "MayaGrebas",
        };

        private static readonly Dictionary<string, List<int>> boneConfigs = new Dictionary<string, List<int>>()
        {
            { "EspartaPecho", new List<int>() { (int)PlayerBone.Spine, (int)PlayerBone.Spine1  } },
            { "EspartaGrebas", new List<int>() { (int)PlayerBone.LeftFoot, (int)PlayerBone.LeftToeBase, (int)PlayerBone.RightFoot, (int)PlayerBone.RightToeBase }},
        };

        private static readonly Dictionary<string, List<BodypartSystem.bodyPart>> bodypartConfigs = new Dictionary<string, List<BodypartSystem.bodyPart>>()
        {
             { "PersiaPecho", new List<BodypartSystem.bodyPart>() {
                BodypartSystem.bodyPart.Torso,
                BodypartSystem.bodyPart.ArmUpperLeft,
                BodypartSystem.bodyPart.ArmLowerLeft,
                BodypartSystem.bodyPart.ArmUpperRight,
                BodypartSystem.bodyPart.ArmLowerRight,
             }},
             { "PersiaGrebas", new List<BodypartSystem.bodyPart>() {
                BodypartSystem.bodyPart.LegUpperLeft,
                BodypartSystem.bodyPart.LegLowerLeft,
                BodypartSystem.bodyPart.FootLeft,
                BodypartSystem.bodyPart.LegUpperRight,
                BodypartSystem.bodyPart.LegLowerRight,
                BodypartSystem.bodyPart.FootRight
             }},
            { "JaponPecho", new List<BodypartSystem.bodyPart>() {
                BodypartSystem.bodyPart.Torso,
                BodypartSystem.bodyPart.ArmUpperLeft,
                BodypartSystem.bodyPart.ArmLowerLeft,
                BodypartSystem.bodyPart.ArmUpperRight,
                BodypartSystem.bodyPart.ArmLowerRight
            } },
            { "JaponGrebas", new List<BodypartSystem.bodyPart>() {
                BodypartSystem.bodyPart.LegUpperLeft,
                BodypartSystem.bodyPart.LegLowerLeft,
                BodypartSystem.bodyPart.FootLeft,
                BodypartSystem.bodyPart.LegUpperRight,
                BodypartSystem.bodyPart.LegLowerRight,
                BodypartSystem.bodyPart.FootRight,
            } },
            { "NordicPecho", new List<BodypartSystem.bodyPart>() {
                BodypartSystem.bodyPart.ArmUpperLeft,
                BodypartSystem.bodyPart.ArmLowerLeft,
                BodypartSystem.bodyPart.ArmUpperRight,
                BodypartSystem.bodyPart.ArmLowerRight
            } },
            { "NordicGrebas", new List<BodypartSystem.bodyPart>() {
                BodypartSystem.bodyPart.LegUpperLeft,
                BodypartSystem.bodyPart.LegLowerLeft,
                BodypartSystem.bodyPart.FootLeft,
                BodypartSystem.bodyPart.LegUpperRight,
                BodypartSystem.bodyPart.LegLowerRight,
                BodypartSystem.bodyPart.FootRight,
            } },
            { "VikingPecho", new List<BodypartSystem.bodyPart>() {
                BodypartSystem.bodyPart.Torso,
                BodypartSystem.bodyPart.ArmUpperLeft,
                BodypartSystem.bodyPart.ArmLowerLeft,
                BodypartSystem.bodyPart.ArmUpperRight,
                BodypartSystem.bodyPart.ArmLowerRight
            } },
            { "VikingGrebas", new List<BodypartSystem.bodyPart>() {
                BodypartSystem.bodyPart.LegUpperLeft,
                BodypartSystem.bodyPart.LegLowerLeft,
                BodypartSystem.bodyPart.FootLeft,
                BodypartSystem.bodyPart.LegUpperRight,
                BodypartSystem.bodyPart.LegLowerRight,
                BodypartSystem.bodyPart.FootRight,
            } },
            { "ShogunGrebas", new List<BodypartSystem.bodyPart>() {
                BodypartSystem.bodyPart.LegUpperLeft,
                BodypartSystem.bodyPart.LegLowerLeft,
                BodypartSystem.bodyPart.FootLeft,
                BodypartSystem.bodyPart.LegUpperRight,
                BodypartSystem.bodyPart.LegLowerRight,
                BodypartSystem.bodyPart.FootRight,
            } },
            { "RomaPecho", new List<BodypartSystem.bodyPart>() {
                BodypartSystem.bodyPart.Torso,
            } },
            { "RomaGrebas", new List<BodypartSystem.bodyPart>() {
                BodypartSystem.bodyPart.LegLowerLeft,
                BodypartSystem.bodyPart.FootLeft,
                BodypartSystem.bodyPart.LegLowerRight,
                BodypartSystem.bodyPart.FootRight,
            } }
        };

        void Awake()
        {
            CreateConfigValues();
            bundle = AssetUtils.LoadAssetBundleFromResources("armor");
            armors = bundle.LoadAllAssets<GameObject>();
            PrefabManager.OnVanillaPrefabsAvailable += OnVanillaPrefabsAvailable;
            PrefabManager.OnPrefabsRegistered += OnPrefabsRegistered;
            Jotunn.Utils.BoneReorder.ApplyOnEquipmentChanged();
            harm.PatchAll();
        }

        public static string GetSetType(string setName)
        {
            string result = null;
            if (setName.ToLower().Contains("leather")) return "leather";
            if (setName.ToLower().Contains("bronze")) return "bronze";
            if (setName.ToLower().Contains("iron")) return "iron";
            if (setName.ToLower().Contains("silver")) return "silver";
            if (setName.ToLower().Contains("blackmetal")) return "blackmetal";
            if (setName.ToLower().Contains("carapace")) return "carapace";
            return result;
        }

        public static Color GetSetColor(string setName)
        {
            Color result = Color.white;
            if (setName.ToLower().Contains("leather")) ColorUtility.TryParseHtmlString("#" + LeatherSetColor.Value, out result);
            if (setName.ToLower().Contains("bronze")) ColorUtility.TryParseHtmlString("#" + BronzeSetColor.Value, out result);
            if (setName.ToLower().Contains("iron")) ColorUtility.TryParseHtmlString("#" + IronSetColor.Value, out result);
            if (setName.ToLower().Contains("silver")) ColorUtility.TryParseHtmlString("#" + SilverSetColor.Value, out result);
            if (setName.ToLower().Contains("blackmetal")) ColorUtility.TryParseHtmlString("#" + BlackmetalSetColor.Value, out result);
            if (setName.ToLower().Contains("carapace")) ColorUtility.TryParseHtmlString("#" + CarapaceSetColor.Value, out result);
            return result;
        }

        string GetSetPieceName(string verboseSetName, SetPieceType pieceType)
        {
            string result = "";
            if (pieceType == SetPieceType.Helmet)
                result = verboseSetName.Replace("Armadura", "Casco").Replace("improvisada", "improvisado").Replace("reforzada", "reforzado");
            if (pieceType == SetPieceType.Armor)
                result = verboseSetName;
            if (pieceType == SetPieceType.Legs)
                result = verboseSetName.Replace("Armadura", "Grebas").Replace("improvisada", "improvisadas").Replace("reforzada", "reforzadas");
            if (pieceType == SetPieceType.Cape)
                result = "";
            return result;
        }


        void ReadArmorConfigs(bool isAwake)
        {
            if (addedArmors) return;
            if (!Directory.Exists(armorConfigPath)) Directory.CreateDirectory(armorConfigPath);
            foreach (string file in Directory.GetFiles(armorConfigPath))
            {
                if (parsedFiles.Contains(file)) continue;
                string json = File.ReadAllText(file);
                bool didParse = ParseArmorConfig(json);
                if (didParse) parsedFiles.Add(file);
            }
            foreach (string dir in Directory.GetDirectories(armorConfigPath))
            {
                foreach (string file in Directory.GetFiles(dir))
                {
                    if (parsedFiles.Contains(file)) continue;
                    string json = File.ReadAllText(file);
                    bool didParse = ParseArmorConfig(json);
                    if (didParse) parsedFiles.Add(file);
                }
            }
            if (!isAwake) addedArmors = true;
        }

        public T DeserializeEnum<T>(string e)
        {
            return (T)System.Enum.Parse(typeof(T), e);
        }

        public string SerializeEnum<T>(T e)
        {
            return System.Enum.GetName(typeof(T), e);
        }

        private void ApplySetColor(Material mat, Color setColor)
        {
            if (mat == null || setColor == null) return;
            if (mat.HasProperty("_ChestTex"))
                mat.color = setColor * Color.white;
            if (mat.HasProperty("_LegsTex"))
                mat.color = setColor * Color.white;
            if (mat.HasProperty("_Color"))
                mat.color = setColor * Color.white * (mat.color.a != 1f ? mat.color.a : 0.6f);
        }


        void CreateClonedBodypartConfig(string originalPrefab, string clonedPrefab)
        {
            if (unisexChestPrefabs.Contains(originalPrefab) || unisexLegPrefabs.Contains(originalPrefab))
            {
                GameObject femalePrefab = PrefabManager.Instance.CreateClonedPrefab(originalPrefab, clonedPrefab + "_F");
                PrefabManager.Instance.AddPrefab(femalePrefab);
            }
            AddBodypartHiding(clonedPrefab, originalPrefab);
        }

        void ParseHelmet(ArmorConfig armor, GameObject helmetPrefab, string setName, string verboseSetName, int setSize, Color setColor)
        {
            Debug.Log("Helmet"); ;
            string helmetName = setName + "_Helmet";
            CustomItem helmet = new CustomItem(PrefabManager.Instance.CreateClonedPrefab(helmetName, helmetPrefab), true, new ItemConfig
            {
                Name = GetSetPieceName(verboseSetName, SetPieceType.Helmet),
                Description = " ",
                Requirements = new RequirementConfig[]
                {
                                new RequirementConfig
                                {
                                    Item = "SwordCheat",
                                    Amount = 9999,
                                    AmountPerLevel = 9999,
                                }
                }
            });
            ItemDrop.ItemData.SharedData shared = helmet.ItemDrop.m_itemData.m_shared;
            shared.m_setName = setName;
            shared.m_setStatusEffect = ValkyrieStatusEffects.Main.statusEffects[("SE_" + setName).GetStableHashCode()];
            shared.m_armor = armor.armor;
            shared.m_armorPerLevel = armor.armorPerLevel;
            shared.m_setSize = setSize;
            shared.m_attackStaminaModifier = 0f;
            shared.m_blockStaminaModifier = 0f;
            shared.m_damageModifiers = new List<HitData.DamageModPair>();
            shared.m_dodgeStaminaModifier = 0f;
            shared.m_eitrRegenModifier = 0f;
            shared.m_equipStatusEffect = null;
            shared.m_homeItemsStaminaModifier = 0f;
            shared.m_jumpStaminaModifier = 0f;
            shared.m_movementModifier = 0f;
            shared.m_runStaminaModifier = 0f;
            shared.m_sneakStaminaModifier = 0f;
            shared.m_swimStaminaModifier = 0f;
            Renderer[] renderers = helmet.ItemPrefab.transform.Find("attach")?.GetComponentsInChildren<Renderer>();
            if (renderers != null && renderers.Length > 0)
                foreach (Renderer smr in renderers)
                    foreach (Material mat in smr.materials) ApplySetColor(mat, setColor);
            else Debug.LogWarning("No renderers found for " + helmetPrefab);
            JotunnItemManager.Instance.AddItem(helmet);
            customArmors.Add(helmetName);
            Debug.Log("Added " + helmetName);
        }

        void ParseChest(ArmorConfig armor, GameObject chestPrefab, string setName, string verboseSetName, int setSize, Color setColor)
        {
            Debug.Log("Chest");
            string chestName = setName + "_Chest";
            CustomItem chest = new CustomItem(PrefabManager.Instance.CreateClonedPrefab(chestName, chestPrefab), true, new ItemConfig
            {
                Name = GetSetPieceName(verboseSetName, SetPieceType.Armor),
                Description = " ",
                Requirements = new RequirementConfig[]
                {
                                new RequirementConfig
                                {
                                    Item = "SwordCheat",
                                    Amount = 9999,
                                    AmountPerLevel = 9999,
                                }
                }
            });
            ItemDrop.ItemData.SharedData shared = chest.ItemDrop.m_itemData.m_shared;
            shared.m_setName = setName;
            shared.m_setStatusEffect = ValkyrieStatusEffects.Main.statusEffects[("SE_" + setName).GetStableHashCode()];
            shared.m_armor = armor.armor;
            shared.m_armorPerLevel = armor.armorPerLevel;
            shared.m_setSize = setSize;
            shared.m_attackStaminaModifier = 0f;
            shared.m_blockStaminaModifier = 0f;
            shared.m_damageModifiers = new List<HitData.DamageModPair>();
            shared.m_dodgeStaminaModifier = 0f;
            shared.m_eitrRegenModifier = 0f;
            shared.m_equipStatusEffect = null;
            shared.m_homeItemsStaminaModifier = 0f;
            shared.m_jumpStaminaModifier = 0f;
            shared.m_movementModifier = 0f;
            shared.m_runStaminaModifier = 0f;
            shared.m_sneakStaminaModifier = 0f;
            shared.m_swimStaminaModifier = 0f;
            ApplySetColor(shared.m_armorMaterial, setColor);
            Renderer[] renderers = chest.ItemPrefab.transform.Find("attach_skin")?.GetComponentsInChildren<Renderer>();
            if (renderers != null && renderers.Length > 0)
                foreach (Renderer smr in renderers)
                    foreach (Material mat in smr.materials) ApplySetColor(mat, setColor);
            else Debug.LogWarning("No renderers found for " + chestPrefab);
            JotunnItemManager.Instance.AddItem(chest);
            customArmors.Add(chestName);
            Debug.Log("Added " + chestName);
        }

        void ParseLegs(ArmorConfig armor, GameObject legsPrefab, string setName, string verboseSetName, int setSize, Color setColor)
        {
            Debug.Log("Legs");
            string legsName = setName + "_Legs";
            Debug.Log("Adding " + legsName);
            CustomItem legs = new CustomItem(PrefabManager.Instance.CreateClonedPrefab(legsName, legsPrefab), true, new ItemConfig
            {
                Name = GetSetPieceName(verboseSetName, SetPieceType.Legs),
                Description = " ",
                Requirements = new RequirementConfig[]
                {
                                new RequirementConfig
                                {
                                    Item = "SwordCheat",
                                    Amount = 9999,
                                    AmountPerLevel = 9999,
                                }
                }
            });
            Debug.Log("Legs ItemDrop: " + legs.ItemDrop);
            ItemDrop.ItemData.SharedData shared = legs.ItemDrop.m_itemData.m_shared;
            shared.m_setName = setName;
            shared.m_setStatusEffect = ValkyrieStatusEffects.Main.statusEffects[("SE_" + setName).GetStableHashCode()];
            shared.m_armor = armor.armor;
            shared.m_armorPerLevel = armor.armorPerLevel;
            shared.m_setSize = setSize;
            shared.m_attackStaminaModifier = 0f;
            shared.m_blockStaminaModifier = 0f;
            shared.m_damageModifiers = new List<HitData.DamageModPair>();
            shared.m_dodgeStaminaModifier = 0f;
            shared.m_eitrRegenModifier = 0f;
            shared.m_equipStatusEffect = null;
            shared.m_homeItemsStaminaModifier = 0f;
            shared.m_jumpStaminaModifier = 0f;
            shared.m_movementModifier = 0f;
            shared.m_runStaminaModifier = 0f;
            shared.m_sneakStaminaModifier = 0f;
            shared.m_swimStaminaModifier = 0f;
            ApplySetColor(shared.m_armorMaterial, setColor);
            Renderer[] renderers = legs.ItemPrefab.transform.Find("attach_skin")?.GetComponentsInChildren<Renderer>();
            Debug.Log("Legs renderers: " + renderers);
            if (renderers != null && renderers.Length > 0)
                foreach (Renderer smr in renderers)
                    foreach (Material mat in smr.materials) ApplySetColor(mat, setColor);
            else Debug.LogWarning("No renderers found for " + legsPrefab);
            JotunnItemManager.Instance.AddItem(legs);
            customArmors.Add(legsName);
            if (BodypartSystem.bodypartSettingsAsBones.ContainsKey(legsPrefab.name)) CreateClonedBodypartConfig(legsPrefab.name, legsName);
            Debug.Log("Added " + legsName);
        }

        void CreateArmorESE(ArmorConfig armor, string setName, string verboseSetName, Sprite seIcon)
        {
            Dictionary<Skills.SkillType, float> weaponDamageModifiers = new Dictionary<Skills.SkillType, float>();
            float healingMultiplier = 1f;
            Debug.Log("WDM");
            Debug.Log(armor.weaponDamageMultipliers);
            if (armor.weaponDamageMultipliers != null)
            {
                foreach (KeyValuePair<string, long> wdm in armor.weaponDamageMultipliers)
                {
                    Debug.Log($"{wdm.Key} - {wdm.Value}");
                    if (wdm.Key == "Healing")
                    {
                        healingMultiplier = wdm.Value / 100;
                        continue;
                    };
                    Skills.SkillType weaponType = DeserializeEnum<Skills.SkillType>(wdm.Key);
                    weaponDamageModifiers.Add(weaponType, wdm.Value / 100);
                }
            }
            Debug.Log("PRE ESE");
            ExtendedStatusEffect ese = ScriptableObject.CreateInstance<ExtendedStatusEffect>();
            ese.name = "SE_" + setName;
            ese.m_name = verboseSetName;
            ese.m_startMessage = verboseSetName + " equipada";
            ese.m_startMessageType = MessageHud.MessageType.TopLeft;
            ese.m_icon = seIcon;
            ese.m_maxEitr = armor.eitr;
            ese.m_eitrRegenMultiplier = 1 + armor.eitrRegen;
            ese.m_maxHealth = armor.hp;
            ese.m_healthRegenMultiplier = 1 + armor.hpRegen;
            ese.m_maxStamina = armor.stamina;
            ese.m_staminaRegenMultiplier = 1 + armor.staminaRegen;
            ese.m_speedModifier = armor.movementSpeed;
            ese.m_addMaxCarryWeight = armor.carryWeight;
            ese.m_runStaminaDrainModifier = armor.staminaRunJumpReduction;
            ese.m_jumpStaminaUseModifier = armor.staminaRunJumpReduction;
            ese.m_damageReduction = armor.damageReduction;
            ese.m_skillLevel = Skills.SkillType.Sneak;
            ese.m_skillLevelModifier = armor.stealthLevels;
            ese.m_weaponDamageModifiers = weaponDamageModifiers;
            ese.m_healingMultiplier = healingMultiplier;
            CustomStatusEffect cse = new CustomStatusEffect(ese, true);
            JotunnItemManager.Instance.AddStatusEffect(cse);
            ValkyrieStatusEffects.Main.statusEffects.Add(ese.name.GetStableHashCode(), cse.StatusEffect);
            Debug.Log("Added ESE: " + ese.m_name);
        }

        bool ParseArmorConfig(string json)
        {
            ArmorConfig armor = JsonConvert.DeserializeObject<ArmorConfig>(json);
            string setName = armor.setName;
            string verboseSetName = armor.verboseSetName;
            Debug.Log("Creating armor: " + setName);
            GameObject helmetPrefab = armor.helmetPrefab != null ? PrefabManager.Instance.GetPrefab(armor.helmetPrefab) : null;
            GameObject chestPrefab = armor.chestPrefab != null ? PrefabManager.Instance.GetPrefab(armor.chestPrefab) : null;
            GameObject legsPrefab = armor.legsPrefab != null ? PrefabManager.Instance.GetPrefab(armor.legsPrefab) : null;
            GameObject capePrefab = armor.capePrefab != null ? PrefabManager.Instance.GetPrefab(armor.capePrefab) : null;
            int setSize = 0;
            Sprite seIcon = null;
            if (helmetPrefab != null)
            {
                if (seIcon == null) seIcon = helmetPrefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_icons[0];
                setSize += 1;
            }
            else Debug.LogWarning("Helmet not found: " + armor.helmetPrefab);
            if (chestPrefab != null)
            {
                if (seIcon == null) seIcon = chestPrefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_icons[0];
                setSize += 1;
            }
            else Debug.LogWarning("Chest not found: " + armor.chestPrefab);
            if (legsPrefab != null)
            {
                if (seIcon == null) seIcon = legsPrefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_icons[0];
                setSize += 1;
            }
            else Debug.LogWarning("Legs not found: " + armor.legsPrefab);
            if (capePrefab != null)
            {
                if (seIcon == null) seIcon = capePrefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_icons[0];
                setSize += 1;
            }
            if (setSize == 0)
            {
                Debug.LogWarning("No prefabs found for " + setName + ". Skipping armor set.");
                return false;
            }
            Color setColor = GetSetColor(setName);

            CreateArmorESE(armor, setName, verboseSetName, seIcon);

            if (helmetPrefab != null)
                ParseHelmet(armor, helmetPrefab, setName, verboseSetName, setSize, setColor);
            if (chestPrefab != null)
                ParseChest(armor, helmetPrefab, setName, verboseSetName, setSize, setColor);
            if (legsPrefab != null)
                ParseLegs(armor, helmetPrefab, setName, verboseSetName, setSize, setColor);

            armorSets.Add(setName);
            return true;
        }

        void AddArmorPrefabs()
        {
            if (addedPrefabs) return;
            foreach (GameObject prefab in armors)
            {
                if (prefab.GetComponent<ZNetView>() == null) prefab.AddComponent<ZNetView>();
                if (prefab.GetComponent<ZSyncTransform>() == null) prefab.AddComponent<ZSyncTransform>();
                CustomItem armor = new CustomItem(prefab, false, new ItemConfig
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
                if (prefab.name.Contains("Pecho"))
                {
                    armor.ItemDrop.m_itemData.m_shared.m_itemType = ItemDrop.ItemData.ItemType.Chest;
                    AddBodypartHiding(prefab.name);
                }
                if (prefab.name.Contains("Grebas"))
                {
                    armor.ItemDrop.m_itemData.m_shared.m_itemType = ItemDrop.ItemData.ItemType.Legs;
                    AddBodypartHiding(prefab.name);
                }
                if (prefab.name.Contains("Casco") || prefab.name.Contains("Corona"))
                    armor.ItemDrop.m_itemData.m_shared.m_itemType = ItemDrop.ItemData.ItemType.Helmet;
                JotunnItemManager.Instance.AddItem(armor);
            }
        }

        void OnPrefabsRegistered()
        {
            ReadArmorConfigs(false);
        }
        void OnVanillaPrefabsAvailable()
        {
            AddArmorPrefabs();
            ReadArmorConfigs(false);
        }
        void AddBodypartHiding(string prefabName, string originalPrefab = null)
        {
            string originalPrefabName = originalPrefab == null ? prefabName : originalPrefab;
            if (boneConfigs.ContainsKey(originalPrefabName) && !BodypartSystem.bodypartSettingsAsBones.ContainsKey(prefabName))
            {
                BodypartSystem.bodypartSettingsAsBones.Add(prefabName, boneConfigs[originalPrefabName]);
                if (unisexLegPrefabs.Contains(originalPrefabName) || unisexChestPrefabs.Contains(originalPrefabName))
                    BodypartSystem.bodypartSettingsAsBones.Add(prefabName + "_F", boneConfigs[originalPrefabName]);
            }
            if (bodypartConfigs.ContainsKey(originalPrefabName) && !BodypartSystem.bodypartSettings.ContainsKey(prefabName))
            {
                BodypartSystem.bodypartSettings.Add(prefabName, bodypartConfigs[originalPrefabName]);
                if (unisexLegPrefabs.Contains(originalPrefabName) || unisexChestPrefabs.Contains(originalPrefabName))
                    BodypartSystem.bodypartSettings.Add(prefabName + "_F", bodypartConfigs[originalPrefabName]);
            }
        }

        public static Sprite GetSlotBorder()
        {
            return Sprite.Create(borderTex, new Rect(0.0f, 0.0f, borderTex.width, borderTex.height), new Vector2(0.5f, 0.5f));
        }

        private void OnDestroy()
        {
            harm.UnpatchSelf();
            bundle.Unload(true);
        }

        [HarmonyPatch]
        class Patches
        {

            [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.UpdateGui))]
            class InventoryGui_UpdateGuiPatch
            {
                static void Prefix(InventoryGrid __instance)
                {
                    foreach (var b in addedBorders) Destroy(b);
                    addedBorders.Clear();
                }
                static void Postfix(InventoryGrid __instance)
                {
                    Inventory inventory = __instance.GetInventory();
                    List<ItemDrop.ItemData> items = inventory.GetAllItems();
                    foreach (var item in items)
                    {
                        if (!armorSets.Contains(item.m_shared.m_setName)) continue;
                        Type invType = __instance.GetType();
                        var slot = (InventoryGrid.Element)invType.GetMethod("GetElement", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, new object[] { item.m_gridPos.x, item.m_gridPos.y, inventory.GetWidth() });
                        var border = Instantiate(slot.m_equiped.gameObject, slot.m_go.transform);
                        slot.m_equiped.color = new Color(slot.m_equiped.color.r, slot.m_equiped.color.g, slot.m_equiped.color.b, 0.7f);
                        if (!border.activeSelf) border.SetActive(true);
                        border.name = "border";
                        var borderImg = border.GetComponent<Image>();
                        borderImg.enabled = true;
                        Color color = GetSetColor(item.m_shared.m_setName);
                        borderImg.color = color;
                        borderImg.sprite = GetSlotBorder();
                        border.transform.SetSiblingIndex(2);
                        addedBorders.Add(border);
                    }
                }
            }

            [HarmonyPatch(typeof(Piece), nameof(Piece.Awake))]
            class Piece_AwakePatch
            {
                static void Postfix(Piece __instance)
                {
                    if (!__instance.gameObject.name.ToLower().Contains("marketplacenpc")) return;
                    Debug.Log("Found NPC: " + __instance.gameObject.name);
                    string overrideModel = __instance.gameObject.GetComponent<ZNetView>().GetZDO().GetString("KGnpcModelOverride");
                    bool isPlayer = overrideModel == "Player" || overrideModel == "Player_Female";
                    if (isPlayer)
                    {
                        var boneReorder = __instance.gameObject.AddComponent<NPCBoneReorder>();
                        boneReorder.SetOverrideModel(overrideModel);
                    }
                }
            }

            [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.SetChestItem))]
            class VisEquipment_SetChestItemPatch
            {
                static void Prefix(VisEquipment __instance, ref string name)
                {
                    if (__instance.m_isPlayer)
                    {
                        if (__instance.GetModelIndex() == 1 && unisexChestPrefabs.Contains(name))
                        {
                            name += "_F";
                        }
                    }
                }
            }
            [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.SetLegItem))]
            class VisEquipment_SetLegItemPatch
            {
                static void Prefix(VisEquipment __instance, ref string name)
                {
                    if (__instance.m_isPlayer)
                    {
                        if (__instance.GetModelIndex() == 1 && unisexLegPrefabs.Contains(name))
                        {
                            name += "_F";
                        }
                    }
                }
            }
        }
    }

}
