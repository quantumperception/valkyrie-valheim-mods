using BepInEx;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace DynamicDungeons
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [BepInDependency("WackyMole.EpicMMOSystem", BepInDependency.DependencyFlags.SoftDependency)]
    [NetworkCompatibility(CompatibilityLevel.NotEnforced, VersionStrictness.Minor)]
    public partial class DynamicDungeons : BaseUnityPlugin
    {
        public const string PluginGUID = "com.valkyrie.dynamicdungeons";
        public const string PluginName = "DynamicDungeons";
        public const string PluginVersion = "0.0.1";
        private static AssetBundle bundle;
        private static GameObject dungeon;
        private static List<string> dungeonFiles;
        private static Dictionary<string, string> storedDungeons = new Dictionary<string, string>();
        public static List<DynamicDungeon> dungeons = new List<DynamicDungeon>();
        public static DungeonEventManager currentDungeon;
        private static List<GameObject> boundingBoxes = new List<GameObject>();
        public static readonly string pluginPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        public static readonly string configPath = Path.GetFullPath(Path.Combine(pluginPath, @"..\", "config"));
        private static readonly string jsonConfigsPath = Path.Combine(configPath, "DynamicDungeons");
        private static readonly Dictionary<string, string> configPaths = new Dictionary<string, string>() {
            {"main",  Path.Combine(jsonConfigsPath, "main.json") },
            {"dungeons", Path.Combine(jsonConfigsPath, "dungeons") },
        };
        public static readonly List<string> tiers = new List<string> { "T1", "T2", "T3", "T4", "T5", "BOSS", };
        public static readonly Dictionary<string, Color> tierColors = new Dictionary<string, Color>();
        public static readonly string[] normalChestPrefabs = { "piece_chest_wood", "stonechest" };
        public static GameObject workbenchMarker;
        public static int spawnerDataHash = StringExtensionMethods.GetStableHashCode("dd_spawnradius");
        public static int spawnerManagerHash = StringExtensionMethods.GetStableHashCode("dd_managername");
        private static Harmony harm = new Harmony("dynamicdungeons");

        public static bool IsServer
        {
            get
            {
                return SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;
            }
        }

        private void Awake()
        {
            FillTierColors();
            if (DynamicDungeons.IsServer)
            {
                TryCreateConfigs();
                FileSystemWatcher watcher = new FileSystemWatcher(configPaths["dungeons"]);
                watcher.Changed += new FileSystemEventHandler(OnDungeonConfigChange);
                PrefabManager.OnVanillaPrefabsAvailable += DungeonManager.ScanDungeonChests;
                PrefabManager.OnVanillaPrefabsAvailable += DungeonManager.ScanDungeonSpawners;
            }
            //AddPieceCategories();
            //bundle = AssetUtils.LoadAssetBundleFromResources("dungeonbundle");
            //AddDungeon();
            CreateSegments();
            AddCustomSpawners();
            CommandManager.Instance.AddConsoleCommand(new Commands.SavePrefabListCommand());
            CommandManager.Instance.AddConsoleCommand(new Commands.DynamicDungeonsCommand());
            harm.PatchAll();
        }
        private static void CreateSegments()
        {
            foreach (KeyValuePair<string, Color> tierColor in tierColors)
            {
                GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
                DestroyImmediate(segment.GetComponent<Collider>());
                segment.transform.localScale = new Vector3(0.15f, 0.1f, 1f);
                segment.GetComponent<Renderer>().material.color = tierColor.Value;
                segment.name = "spawnersegment_" + tierColor.Key;
                Jotunn.Logger.LogInfo("Added segment: " + segment.name);
                PrefabManager.Instance.AddPrefab(segment);
            }
            GameObject whiteSegment = GameObject.CreatePrimitive(PrimitiveType.Cube);
            whiteSegment.transform.localScale = new Vector3(0.15f, 0.1f, 1f);
            whiteSegment.GetComponent<Renderer>().material.color = Color.white;
            whiteSegment.name = "spawnersegment_white";
            PrefabManager.Instance.AddPrefab(whiteSegment);
        }
        private static void SetVanillaReferences()
        {
            workbenchMarker = ZNetScene.instance.m_prefabs.Find(p => p.name == "piece_workbench").GetComponent<CraftingStation>().m_areaMarker;
            Jotunn.Logger.LogInfo("Set workbench marker");
        }
        private static void FillTierColors()
        {
            Color cyanAlpha = Color.cyan; cyanAlpha.a = 0.6f;
            Color greenAlpha = Color.green; greenAlpha.a = 0.6f;
            Color yellowAlpha = Color.yellow; yellowAlpha.a = 0.6f;
            Color redAlpha = Color.red; redAlpha.a = 0.6f;
            Color blueAlpha = Color.blue; blueAlpha.a = 0.6f;
            Color magentaAlpha = Color.magenta; magentaAlpha.a = 0.6f;
            Color[] colors = new Color[] { cyanAlpha, greenAlpha, yellowAlpha, redAlpha, blueAlpha, magentaAlpha };
            for (int i = 0; i < tiers.Count; i++) { tierColors.Add(tiers[i], colors[i]); }
        }
        private static void OnDungeonConfigChange(object sender, FileSystemEventArgs e)
        {
            Jotunn.Logger.LogInfo("Got file change: " + e.Name);
            string dungeonJson = File.ReadAllText(e.FullPath);
            DynamicDungeon dungeon = DungeonFromJson(dungeonJson);
            UpdateDungeon(dungeon);
            SendDungeonToPeer(ZRoutedRpc.Everybody, dungeon);
        }
        private static void CreateDungeonBoundingBoxes()
        {
            if (DungeonManager.Instance.managers.Count != 0) DungeonManager.Instance.managers.Clear();
            foreach (DynamicDungeon dungeon in dungeons)
            {
                boundingBoxes.Add(CreateBoundingBox(dungeon));
            }
            Jotunn.Logger.LogInfo(DungeonManager.Instance.managers.Count + " dungeon managers found");
        }
        private static GameObject CreateBoundingBox(DynamicDungeon dungeon)
        {
            List<Vector3> corners = dungeon.corners;
            float h = Mathf.Abs(corners[0].y - corners[1].y);
            float w = Mathf.Abs(corners[0].x - corners[1].x);
            float d = Mathf.Abs(corners[0].z - corners[1].z);
            GameObject boundingBox = Instantiate(GameObject.CreatePrimitive(PrimitiveType.Cube), ZNetScene.instance.transform);
            boundingBox.layer = LayerMask.NameToLayer("character_trigger");
            Collider collider = boundingBox.GetComponent<Collider>();
            collider.isTrigger = true;
            DungeonEventManager manager = boundingBox.AddComponent<DungeonEventManager>();
            manager.dungeon = dungeon;
            DungeonManager.Instance.managers.Add(dungeon.name, manager);
            //GameObject corner1 = Instantiate(boundingBox, ZNetScene.instance.transform);
            //GameObject corner2 = Instantiate(boundingBox, ZNetScene.instance.transform);
            //corner1.transform.localScale = new Vector3(1, 1, 1);
            //corner1.transform.position = new Vector3(corners[0].x, corners[0].y, corners[0].z);
            //corner2.transform.localScale = new Vector3(1, 1, 1);
            //corner2.transform.position = new Vector3(corners[1].x, corners[1].y, corners[1].z);
            Renderer boundingBoxRenderer = boundingBox.GetComponent<Renderer>();
            Color redAlpha = Color.red;
            redAlpha.a = 0.5f;
            boundingBoxRenderer.material.color = redAlpha;
            boundingBoxRenderer.material = Util.SetRenderTransparent(boundingBoxRenderer.material);
            boundingBox.transform.localPosition = Vector3.Lerp(new Vector3(corners[0].x, corners[0].y, corners[0].z), new Vector3(corners[1].x, corners[1].y, corners[1].z), 0.5f);
            boundingBox.transform.localScale = new Vector3(w, h, d);
            boundingBox.name = dungeon.name + "_DungeonManager";
#if DEBUG
            Jotunn.Logger.LogInfo(dungeon.name + "'s center is: " + boundingBox.transform.localPosition.x + ", " + boundingBox.transform.localPosition.y + ", " + boundingBox.transform.localPosition.z);
            Jotunn.Logger.LogInfo(dungeon.name + "'s scale is: " + boundingBox.transform.localScale.x + ", " + boundingBox.transform.localScale.y + ", " + boundingBox.transform.localScale.z);
#endif
            return boundingBox;
        }

        private static void TryCreateConfigs()
        {
            if (!Directory.Exists(configPath)) { Jotunn.Logger.LogError("DynamicDungeons' DLL must be directly in the plugins folder."); return; }
            if (!Directory.Exists(jsonConfigsPath)) Directory.CreateDirectory(jsonConfigsPath);
            if (!Directory.Exists(configPaths["dungeons"])) Directory.CreateDirectory(configPaths["dungeons"]);
            foreach (string file in configPaths.Values)
            {
                if (!File.Exists(file) && !Directory.Exists(file))
                {
                    FileStream _file = File.Create(file);
                    File.WriteAllText(file, "{}");
                    _file.Close();
                };
            }
            return;
        }
        private static void AddCustomSpawners()
        {
            foreach (KeyValuePair<string, Color> tier in tierColors)
            {
                GameObject cube = PrefabManager.Instance.CreateEmptyPrefab("DungeonSpawner_" + tier.Key);
                cube.AddComponent<Piece>();
                cube.GetComponent<MeshRenderer>().material.color = tier.Value;
                cube.AddComponent<DungeonSpawnArea>();
                PieceConfig spawnerPieceConfig = new PieceConfig();
                spawnerPieceConfig.Name = tier.Key + " Dungeon Spawner";
                spawnerPieceConfig.PieceTable = "Hammer";
                spawnerPieceConfig.Icon = RenderManager.Instance.Render(cube, RenderManager.IsometricRotation);
                spawnerPieceConfig.Category = "Misc";
                spawnerPieceConfig.AddRequirement(new RequirementConfig { Item = "Wood", Amount = 1 });
                PieceManager.Instance.AddPiece(new CustomPiece(cube, false, spawnerPieceConfig));
            }
            Jotunn.Logger.LogInfo("Added dungeon spawners");
        }

        //private static void AddCustomSpawners()
        //{
        //    var deserializedSpawners = (List<StoredSpawnerConfig>)SimpleJson.SimpleJson.DeserializeObject(File.ReadAllText(configPaths["spawners"]), typeof(List<StoredSpawnerConfig>));
        //    foreach (StoredSpawnerConfig spawner in deserializedSpawners)
        //    {
        //        List<SpawnData> spawnDatas = new List<SpawnData>();
        //        foreach (StoredSpawnData spawnData in spawner.storedPrefabs)
        //        {
        //            Jotunn.Logger.LogInfo("Adding prefab | " + spawnData.prefab + " | to customSpawner");
        //            spawnDatas.Add(new SpawnData
        //            {
        //                prefab = PrefabManager.Instance.GetPrefab(spawnData.prefab),
        //                weight = spawnData.weight,
        //                minLevel = spawnData.minLevel,
        //                maxLevel = spawnData.maxLevel
        //            });
        //        }
        //        customSpawners.Add(new CustomSpawnerConfig
        //        {
        //            name = spawner.name,
        //            prefabs = spawnDatas,
        //            levelupChance = spawner.levelupChance,
        //            spawnIntervalSec = spawner.spawnIntervalSec,
        //            triggerDistance = spawner.triggerDistance,
        //            setPatrolSpawnPoint = spawner.setPatrolSpawnPoint,
        //            spawnRadius = spawner.spawnRadius,
        //            nearRadius = spawner.nearRadius,
        //            maxNear = spawner.maxNear,
        //            maxTotal = spawner.maxTotal,
        //            onGroundOnly = spawner.onGroundOnly,
        //            minSpawned = spawner.minSpawned,
        //            maxSpawned = spawner.maxSpawned,
        //            spawnEffects = new List<string>()
        //        }); ;
        //    }
        //}
        //private static void AddDungeon()
        //{
        //    dungeon = bundle.LoadAsset<GameObject>("Dungeon");
        //    Transform dungeonTransform = dungeon.transform.Find("DUNGEON1");
        //    Transform lightsTransform = dungeonTransform.transform.Find("Lights");
        //    Light[] lights = lightsTransform.GetComponentsInChildren<Light>();
        //    foreach (Light light in lights) light.intensity = 1.5f;
        //    LocationConfig dungeonConfig = new LocationConfig();
        //    dungeonConfig.Unique = true;
        //    dungeonConfig.Priotized = true;
        //    dungeonConfig.CenterFirst = true;
        //    dungeonConfig.MinAltitude = 6000f;
        //    dungeonConfig.MaxAltitude = 6000f;
        //    dungeonConfig.Biome = Heightmap.Biome.Meadows;
        //    dungeonConfig.Quantity = 1;
        //    dungeonConfig.MinDistance = 0;
        //    dungeonConfig.MaxDistance = 0;
        //    dungeonConfig.ClearArea = false;
        //    ZoneManager.Instance.AddCustomLocation(new CustomLocation(dungeon, false, dungeonConfig));
        //}
        private static DynamicDungeon DungeonFromJson(string dungeonJson)
        {
            var sdc = JsonConvert.DeserializeObject<StoredDungeonConfig>(dungeonJson);
            if (storedDungeons.ContainsKey(sdc.name)) storedDungeons.Remove(sdc.name);
            storedDungeons.Add(sdc.name, dungeonJson);
            Dictionary<MobTier, MobConfig> _mobConfig = new Dictionary<MobTier, MobConfig>();
            List<ChestItemData> normalChestItems = new List<ChestItemData>();
            List<ChestItemData> specialChestItems = new List<ChestItemData>();
            foreach (KeyValuePair<string, StoredMobConfig> smc in sdc.mobConfig)
            {
                MobTier tier = new MobTier();
                Dictionary<MobTier, List<SpawnData>> mobs = new Dictionary<MobTier, List<SpawnData>>
                    {
                        {MobTier.T1, new List<SpawnData>() },
                        {MobTier.T2, new List<SpawnData>() },
                        {MobTier.T3, new List<SpawnData>() },
                        {MobTier.T4, new List<SpawnData>() },
                        {MobTier.T5, new List<SpawnData>() },
                        {MobTier.BOSS, new List<SpawnData>() },
                    };
                switch (smc.Key)
                {
                    case "tier1":
                        tier = MobTier.T1;
                        break;
                    case "tier2":
                        tier = MobTier.T2;
                        break;
                    case "tier3":
                        tier = MobTier.T3;
                        break;
                    case "tier4":
                        tier = MobTier.T4;
                        break;
                    case "tier5":
                        tier = MobTier.T5;
                        break;
                    case "boss":
                        tier = MobTier.BOSS;
                        break;
                }
                List<SpawnData> currentTier = new List<SpawnData>();
                foreach (StoredSpawnData mob in smc.Value.mobs)
                {
                    if (IsServer || true) Jotunn.Logger.LogInfo("Adding prefab | " + mob.prefab + " | to dungeon's " + sdc.name + " " + tier + " mobs.");
                    mobs.TryGetValue(tier, out currentTier);
                    currentTier.Add(new SpawnData
                    {
                        prefab = PrefabManager.Instance.GetPrefab(mob.prefab),
                        weight = mob.weight,
                        minLevel = mob.minLevel,
                        maxLevel = mob.maxLevel
                    });

                }
                _mobConfig.Add(tier, new MobConfig
                {
                    minAmount = smc.Value.minAmount,
                    maxAmount = smc.Value.maxAmount,
                    maxSpawned = smc.Value.maxSpawned,
                    spawnCooldown = smc.Value.spawnCooldown,
                    spawnRadius = smc.Value.spawnRadius,
                    scanRadius = smc.Value.scanRadius,
                    mobs = currentTier,
                });

            }
            foreach (StoredChestItemData cid in sdc.normalChest.items)
            {
                if (IsServer || true) Jotunn.Logger.LogInfo("Adding prefab | " + cid.prefab + " | to dungeon's " + sdc.name + " NORMAL chest items.");
                normalChestItems.Add(new ChestItemData
                {
                    prefab = PrefabManager.Instance.GetPrefab(cid.prefab),
                    weight = cid.weight,
                    minAmount = cid.minAmount,
                    maxAmount = cid.maxAmount
                });
            }
            if (sdc.specialChest != null)
            {
                foreach (StoredChestItemData cid in sdc.specialChest.items)
                {
                    if (IsServer || true) Jotunn.Logger.LogInfo("Adding prefab | " + cid.prefab + " | to dungeon's " + sdc.name + " SPECIAL chest items.");
                    specialChestItems.Add(new ChestItemData
                    {
                        prefab = PrefabManager.Instance.GetPrefab(cid.prefab),
                        weight = cid.weight,
                        minAmount = cid.minAmount,
                        maxAmount = cid.maxAmount
                    });
                }
            }
            return new DynamicDungeon
            {
                name = sdc.name,
                corners = sdc.corners,
                mobConfig = _mobConfig,
                normalChest = new ChestItemConfig
                {
                    minItems = sdc.normalChest.minItems,
                    maxItems = sdc.normalChest.maxItems,
                    items = normalChestItems
                },
                specialChest = sdc.specialChest != null ? new ChestItemConfig
                {
                    minItems = sdc.specialChest.minItems,
                    maxItems = sdc.specialChest.maxItems,
                    items = specialChestItems
                } : null
            };
        }
        private static void LoadDungeons()
        {
            if (storedDungeons.Count != 0) storedDungeons.Clear();
            if (dungeons.Count != 0) dungeons.Clear();
            if (boundingBoxes.Count != 0) boundingBoxes.Clear();
            dungeonFiles = Directory.GetFiles(configPaths["dungeons"]).ToList();
            if (dungeonFiles.Count == 0) { Jotunn.Logger.LogInfo("No dungeon config files found"); return; }
            foreach (string file in dungeonFiles)
            {
                Jotunn.Logger.LogInfo("Parsing dungeon file: " + file);
                string dungeonJson = File.ReadAllText(file);
                //var sdc = (StoredDungeonConfig)SimpleJson.SimpleJson.DeserializeObject(File.ReadAllText(file), typeof(StoredDungeonConfig));
                DynamicDungeon dungeon = DungeonFromJson(dungeonJson);
                dungeons.Add(dungeon);
                Jotunn.Logger.LogInfo("Added dungeon " + dungeon.name);
            }
            CreateDungeonBoundingBoxes();
        }
        public static void RPC_OnReloadDungeons(long uid)
        {
            Jotunn.Logger.LogInfo("Reloading dungeons");
            LoadDungeons();
            SendDungeonsToPeer(ZRoutedRpc.Everybody);
        }
        public static void RPC_OnReloadDungeon(long uid, string dungeonName)
        {

        }
        private static void AddServerRPC()
        {
            ZRoutedRpc.instance.Register("DynamicDungeons AcceptPoll", new Action<long>(DungeonManager.RPC_AcceptPoll));
            ZRoutedRpc.instance.Register("DynamicDungeons DeclinePoll", new Action<long>(DungeonManager.RPC_DeclinePoll));
            ZRoutedRpc.instance.Register<string>("DynamicDungeons RequestInfo", new Action<long, string>(DungeonManager.RPC_RequestInfo));
            ZRoutedRpc.instance.Register<string>("DynamicDungeons ReloadDungeon", new Action<long, string>(RPC_OnReloadDungeon));
            ZRoutedRpc.instance.Register("DynamicDungeons ReloadAllDungeons", new Action<long>(RPC_OnReloadDungeons));
            ZRoutedRpc.instance.Register<string>("DynamicDungeons EnteredDungeon", new Action<long, string>(DungeonManager.RPC_OnEnteredDungeon));
            ZRoutedRpc.instance.Register<string>("DynamicDungeons ExitedDungeon", new Action<long, string>(DungeonManager.RPC_OnExitedDungeon));
            ZRoutedRpc.instance.Register<string>("DynamicDungeons DungeonDeath", new Action<long, string>(DungeonManager.RPC_OnDungeonDeath));
            ZRoutedRpc.instance.Register<string>("DynamicDungeons Damaged", new Action<long, string>(DungeonManager.RPC_OnDungeonDamaged));
            ZRoutedRpc.instance.Register<string>("DynamicDungeons DungeonKill", new Action<long, string>(DungeonManager.RPC_OnDungeonKill));
            ZRoutedRpc.instance.Register<string>("DynamicDungeons TakeItem", new Action<long, string>(DungeonManager.RPC_OnTakeChestItem));
            ZRoutedRpc.instance.Register<string>("DynamicDungeons LeaveItem", new Action<long, string>(DungeonManager.RPC_OnLeaveItem));
            ZRoutedRpc.instance.Register<string>("DynamicDungeons StartEvent", new Action<long, string>(DungeonManager.RPC_OnStartDungeonEvent));
            ZRoutedRpc.instance.Register<string>("DynamicDungeons StopEvent", new Action<long, string>(DungeonManager.RPC_OnStopDungeonEvent));
        }
        private static void AddClientRPC()
        {
            ZRoutedRpc.instance.Register("DynamicDungeons TeleportToCoords", new Action<long, ZPackage>(DungeonManager.TeleportToCoords));
            ZRoutedRpc.instance.Register("DynamicDungeons PollPlayer", new Action<long>(DungeonManager.PollPlayer));
            ZRoutedRpc.instance.Register("DynamicDungeons ReceivedDungeon", new Action<long, ZPackage>(DungeonManager.OnReceivedDungeon));
            ZRoutedRpc.instance.Register("DynamicDungeons DungeonUpdate", new Action<long, string, UpdateType, object>(DungeonManager.OnDungeonUpdate));
            ZRoutedRpc.instance.Register("DynamicDungeons DungeonCompleted", new Action<long, string>(DungeonManager.OnDungeonCompleted));
            ZRoutedRpc.instance.Register("DynamicDungeons DungeonFailed", new Action<long, string>(DungeonManager.OnDungeonFailed));
        }
        public static ZPackage SerializeDungeon(DynamicDungeon dungeon)
        {
            ZPackage pkg = new ZPackage();
            ZPackage zip = new ZPackage();
            pkg.Write(storedDungeons[dungeon.name]);
            zip.WriteCompressed(pkg);
            return zip;
        }
        public static void DeserializeDungeon(ZPackage zip)
        {
            DynamicDungeon dungeon = DungeonFromJson(zip.ReadCompressedPackage().ReadString());
            UpdateDungeon(dungeon);
            Jotunn.Logger.LogInfo("Got dungeon " + dungeon.name + " from server");
        }
        public static void UpdateDungeon(DynamicDungeon dungeon)
        {
            DynamicDungeon oldDungeon = dungeons.Find(d => d.name == dungeon.name);
            if (oldDungeon != null) dungeons.Remove(oldDungeon);
            dungeons.Add(dungeon);
            if (DungeonManager.Instance.managers.ContainsKey(dungeon.name)) DungeonManager.Instance.managers.Remove(dungeon.name);
            GameObject boundingBox = CreateBoundingBox(dungeon);
            GameObject oldBoundingBox = boundingBoxes.Find(bb => bb.name == boundingBox.name);
            if (oldBoundingBox != null) boundingBoxes.Remove(oldBoundingBox);
            boundingBoxes.Add(boundingBox);
        }
        public static void SendDungeonsToPeer(long peer)
        {
            foreach (DynamicDungeon dungeon in dungeons)
            {
                Jotunn.Logger.LogInfo("Sending compressed info for dungeon " + dungeon.name + " to " + peer);
                ZRoutedRpc.instance.InvokeRoutedRPC(peer, "DynamicDungeons ReceivedDungeon", SerializeDungeon(dungeon));
            }
        }
        public static void SendDungeonToPeer(long peer, DynamicDungeon dungeon)
        {
            ZRoutedRpc.instance.InvokeRoutedRPC(peer, "DynamicDungeons ReceivedDungeon", SerializeDungeon(dungeon));
        }
        [HarmonyPatch(typeof(ZNet), nameof(ZNet.Start))]
        private static class ZNetStartPatch
        {
            private static void Postfix()
            {
                if (DynamicDungeons.IsServer)
                {
                    //Game.instance.StartCoroutine(DungeonManager.dungeonPollCoroutine);
                    AddServerRPC();
                    LoadDungeons();
                }
                if (!DynamicDungeons.IsServer)
                {
                    AddClientRPC();
                    SetVanillaReferences();
                }
            }
        }
        [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.OnDestroy))]
        private static class ZNetSceneOnDestroyPatch
        {
            private static void Postfix()
            {
                foreach (GameObject boundingBox in boundingBoxes) DestroyImmediate(boundingBox);
                currentDungeon = null;
                boundingBoxes.Clear();
                storedDungeons.Clear();
                dungeons.Clear();
            }
        }
        [HarmonyPatch(typeof(ZNet), nameof(ZNet.RPC_PeerInfo))]
        private static class ZNetRPC_PeerInfoPatch
        {
            private static void Postfix(ZRpc rpc)
            {
                if (!IsServer) return;
                ZNetPeer peer = ZNet.instance.GetPeerByHostName(rpc.GetSocket().GetHostName());
                SendDungeonsToPeer(peer.m_uid);
            }
        }
    }
}
