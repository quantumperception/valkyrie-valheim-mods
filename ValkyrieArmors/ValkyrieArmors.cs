using BepInEx;
using BlacksmithTools;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ValkyrieArmors
{

    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [BepInDependency(BlacksmithTools.Main.GUID)]
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

        public const string PluginGUID = "com.valkyrie.armors";
        public const string PluginName = "Valkyrie's Armors";
        public const string PluginVersion = "0.0.1";
        private bool addedPrefabs = false;
        public static CustomLocalization Localization;
        public static AssetBundle bundle;
        public static GameObject[] armors;
        private static readonly string pluginPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        private static readonly string configPath = BepInEx.Paths.ConfigPath;
        private static Harmony harm = new Harmony("ValkyrieArmors");
        private static List<string> unisexChestPrefabs = new List<string>()
        {
            "EspartaPecho",
            "MayaPecho",
        };
        private static List<string> unisexLegPrefabs = new List<string>()
        {
            "EspartaGrebas",
            "MayaGrebas",
        };

        private static Dictionary<string, List<int>> boneConfigs = new Dictionary<string, List<int>>()
        {
            { "EspartaPecho", new List<int>() { (int)PlayerBone.LeftShoulder, (int)PlayerBone.RightShoulder, (int)PlayerBone.Spine, (int)PlayerBone.Spine1  } },
            { "EspartaGrebas", new List<int>() { (int)PlayerBone.LeftFoot, (int)PlayerBone.LeftToeBase, (int)PlayerBone.RightFoot, (int)PlayerBone.RightToeBase }},
        };

        private static Dictionary<string, List<BodypartSystem.bodyPart>> bodypartConfigs = new Dictionary<string, List<BodypartSystem.bodyPart>>()
        {
             { "PersiaPecho", new List<BodypartSystem.bodyPart>() {
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
            { "ShogunPecho", new List<BodypartSystem.bodyPart>() {
                BodypartSystem.bodyPart.Torso,
                BodypartSystem.bodyPart.ArmUpperLeft,
                BodypartSystem.bodyPart.ArmLowerLeft,
                BodypartSystem.bodyPart.ArmUpperRight,
                BodypartSystem.bodyPart.ArmLowerRight
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
                BodypartSystem.bodyPart.FootLeft,
                BodypartSystem.bodyPart.FootRight,
            } }
        };

        void Awake()
        {
            bundle = AssetUtils.LoadAssetBundleFromResources("armor");
            armors = bundle.LoadAllAssets<GameObject>();
            PrefabManager.OnVanillaPrefabsAvailable += OnVanillaPrefabsAvailable;
            Jotunn.Utils.BoneReorder.ApplyOnEquipmentChanged();
            harm.PatchAll();
        }

        void OnVanillaPrefabsAvailable()
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
                    AddBodypartHiding(prefab);
                }
                if (prefab.name.Contains("Grebas"))
                {
                    armor.ItemDrop.m_itemData.m_shared.m_itemType = ItemDrop.ItemData.ItemType.Legs;
                    AddBodypartHiding(prefab);
                }
                if (prefab.name.Contains("Casco") || prefab.name.Contains("Corona"))
                    armor.ItemDrop.m_itemData.m_shared.m_itemType = ItemDrop.ItemData.ItemType.Helmet;
                ItemManager.Instance.AddItem(armor);
            }
            addedPrefabs = true;
        }
        void AddBodypartHiding(GameObject prefab)
        {
            if (boneConfigs.ContainsKey(prefab.name) && !BodypartSystem.bodypartSettingsAsBones.ContainsKey(prefab.name))
            {
                BodypartSystem.bodypartSettingsAsBones.Add(prefab.name, boneConfigs[prefab.name]);
                if (unisexLegPrefabs.Contains(prefab.name) || unisexChestPrefabs.Contains(prefab.name))
                    BodypartSystem.bodypartSettingsAsBones.Add(prefab.name + "_F", boneConfigs[prefab.name]);
            }
            if (bodypartConfigs.ContainsKey(prefab.name) && !BodypartSystem.bodypartSettings.ContainsKey(prefab.name))
            {
                BodypartSystem.bodypartSettings.Add(prefab.name, bodypartConfigs[prefab.name]);
                if (unisexLegPrefabs.Contains(prefab.name) || unisexChestPrefabs.Contains(prefab.name))
                    BodypartSystem.bodypartSettings.Add(prefab.name + "_F", bodypartConfigs[prefab.name]);
            }
        }
        [HarmonyPatch]
        class Patches
        {

            [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.SetChestItem))]
            class VisEquipment_SetChestItemPatch
            {
                static void Prefix(VisEquipment __instance, ref string name)
                {
                    Debug.Log("Equipping: " + name);
                    Debug.Log("Model idx: " + __instance.GetModelIndex());
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
                    Debug.Log("Equipping: " + name);
                    Debug.Log("Model idx: " + __instance.GetModelIndex());
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
