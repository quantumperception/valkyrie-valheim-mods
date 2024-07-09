using BepInEx;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ValkyrieAssetLoader
{

    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.ClientMustHaveMod, VersionStrictness.Minor)]
    public partial class ValkyrieAssetLoader : BaseUnityPlugin
    {

        public const string PluginGUID = "com.valkyrie.assetloader";
        public const string PluginName = "Valkyrie's Asset Loader";
        public const string PluginVersion = "0.0.1";
        private bool addedPrefabs = false;
        public static CustomLocalization Localization;
        public static List<AssetBundle> bundles = new List<AssetBundle>();
        public static Dictionary<string, List<GameObject>> loadedAssets = new Dictionary<string, List<GameObject>>();
        public static List<StatusEffect> statusEffects = new List<StatusEffect>();
        private static readonly string pluginPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        private static readonly string configPath = BepInEx.Paths.ConfigPath;
        private static readonly string bundlesPath = Path.Combine(pluginPath, "AssetBundles");
        private static Harmony harm = new Harmony("ValkyrieAssetLoader");


        void Awake()
        {
            CreateFolders();
            LoadBundles();
            LoadAssets();
            PrefabManager.OnVanillaPrefabsAvailable += OnVanillaPrefabsAvailable;
            harm.PatchAll();
            BoneReorder.ApplyOnEquipmentChanged();
        }

        void OnVanillaPrefabsAvailable()
        {
            if (addedPrefabs) return;
            foreach (GameObject prefab in loadedAssets["items"])
            {
                CustomItem item = new CustomItem(prefab, false, new ItemConfig
                {
                    Enabled = true,
                    Icon = RenderManager.Instance.Render(prefab, RenderManager.IsometricRotation),
                    CraftingStation = CraftingStations.None,
                    Requirements = new RequirementConfig[]
                    {
                        new RequirementConfig()
                        {
                            Item = "Wood",
                            Amount = 1,
                        }
                    }
                });
                ItemManager.Instance.AddItem(item);
                Debug.Log("Added Item: " + prefab.name);
            }
            addedPrefabs = true;
        }

        void LoadAssets()
        {
            foreach (AssetBundle bundle in bundles)
            {
                GameObject[] prefabs = bundle.LoadAllAssets<GameObject>();
                foreach (GameObject prefab in prefabs)
                {
                    if (prefab.GetComponent<ItemDrop>() != null)
                    {
                        if (loadedAssets.ContainsKey("items")) loadedAssets["items"].Add(prefab);
                        else loadedAssets.Add("items", new List<GameObject>() { prefab });

                    }
                }
            }
        }

        void UnloadAssets()
        {
            foreach (GameObject prefab in loadedAssets["items"])
            {
                ItemManager.Instance.RemoveItem(prefab.name);
                PrefabManager.Instance.RemovePrefab(prefab.name);
            }
        }


        void CreateFolders()
        {
            if (!Directory.Exists(bundlesPath)) Directory.CreateDirectory(bundlesPath);
        }

        void LoadBundles()
        {
            foreach (string file in Directory.GetFiles(bundlesPath))
            {
                AssetBundle bundle = AssetBundle.LoadFromFile(file);
                bundles.Add(bundle);
                Logger.LogInfo("Added bundle: " + file);
            }
        }

        void UnloadBundles()
        {
            foreach (AssetBundle bundle in bundles) bundle.Unload(true);
        }

        private void SetupWatcher()
        {
            FileSystemWatcher watcher = new FileSystemWatcher(BepInEx.Paths.ConfigPath, bundlesPath);
            watcher.Changed += OnBundlesPathChanged;
            watcher.Created += OnBundlesPathChanged;
            watcher.IncludeSubdirectories = true;
            watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            watcher.EnableRaisingEvents = true;
        }

        private void OnBundlesPathChanged(object sender, FileSystemEventArgs e)
        {
            try
            {
                Jotunn.Logger.LogDebug("Attempting to reload AssetBundles...");
                UnloadAssets();
                UnloadBundles();
                LoadBundles();
                LoadAssets();
            }
            catch
            {
                Jotunn.Logger.LogError($"There was an issue loading bundles in the plugins/AssetBundles folder");
            }
        }
        class Patches
        {

        }

    }
}