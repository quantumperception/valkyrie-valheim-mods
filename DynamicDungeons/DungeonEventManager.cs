using Jotunn.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DynamicDungeons
{
    public class DungeonEventManager : MonoBehaviour
    {
        public HashSet<long> players = new HashSet<long>();
        public DynamicDungeons.DynamicDungeon dungeon;
        public bool isActive = false;
        public Dictionary<string, List<DungeonSpawnArea>> spawners;
        private List<Container> normalChests = new List<Container>();
        private List<Container> specialChests = new List<Container>();
        private Dictionary<string, List<GameObject>> spawnedMobs = new Dictionary<string, List<GameObject>>();
        public int ActiveMobCount { get; private set; }
        public int FoundNormalChestCount { get; private set; }
        public int FoundSpecialChestCount { get; private set; }
        public bool ActiveAlert { get; set; }
        public DateTime AlertTimerEnd { get; set; }
        public bool ActiveCooldown { get; set; }
        public DateTime CooldownEnd { get; set; }
        public DateTime EventEnd { get; set; }
        private void Awake()
        {
            spawners = new Dictionary<string, List<DungeonSpawnArea>>();
            spawnedMobs = new Dictionary<string, List<GameObject>>();
            foreach (string tier in DynamicDungeons.tiers)
            {
                spawners.Add("DungeonSpawner_" + tier, new List<DungeonSpawnArea>());
                spawnedMobs.Add("DungeonSpawner_" + tier, new List<GameObject>());
            }
            FoundNormalChestCount = 0;
            FoundSpecialChestCount = 0;
            ActiveMobCount = 0;
            DateTime now = new DateTime();
            AlertTimerEnd = now;
            CooldownEnd = now;
            EventEnd = now;
        }
        private void Update()
        {
            if (!isActive) return;
            if (ActiveAlert && new DateTime() > AlertTimerEnd) StopEvent();
            if (!Util.IsServer()) return;
            return;

        }
        private void OnTriggerEnter(Collider other)
        {
            Player player = other.gameObject.GetComponent<Player>();
            if (!Util.IsServer() && DynamicDungeons.currentDungeon == null && player != null && player.m_name == Player.m_localPlayer.m_name)
            {
                DynamicDungeons.lastDungeon = null;
                ZRoutedRpc.instance.InvokeRoutedRPC("DynamicDungeons EnteredDungeon", dungeon.name);
                return;
            };
        }
        private void OnTriggerExit(Collider other)
        {
            Player player = other.gameObject.GetComponent<Player>();
            if (!Util.IsServer() && DynamicDungeons.currentDungeon == this && player != null && player.m_name == Player.m_localPlayer.m_name)
            {
                DynamicDungeons.lastDungeon = dungeon.name;
                DDHUD.ResetHud();
                ZRoutedRpc.instance.InvokeRoutedRPC("DynamicDungeons ExitedDungeon", dungeon.name);
                return;
            };
        }
        public void AddPlayer(long uid)
        {
            players.Add(uid);
            Jotunn.Logger.LogInfo($"{uid} entered {dungeon.name}. Player count: {players.Count}");
            foreach (long player in players) Jotunn.Logger.LogInfo("Player: " + player);
            if (players.Count == 1)
            {
                DungeonManager.RPC_OnStartDungeonEvent(uid, dungeon.name);
                ActiveAlert = false;
                ZPackage pkg = new ZPackage();
                pkg.Write(false);
                ZRoutedRpc.instance.InvokeRoutedRPC(uid, "DynamicDungeons DungeonUpdate", dungeon.name, "AlertTimer", pkg);
            }
        }
        public void RemovePlayer(long uid)
        {
            players.Remove(uid);
            Jotunn.Logger.LogInfo($"{uid} exited {dungeon.name}. Player count: {players.Count}");
            foreach (long player in players) Jotunn.Logger.LogInfo("Player: " + player);
            if (players.Count == 0)
            {
                Jotunn.Logger.LogInfo("Starting timer for " + dungeon.name);
                DateTime timer = new DateTime().AddSeconds(15);
                ActiveAlert = true;
                AlertTimerEnd = timer;
                ZPackage pkg = new ZPackage();
                pkg.Write(true);
                pkg.Write(timer.ToString());
                ZRoutedRpc.instance.InvokeRoutedRPC(uid, "DynamicDungeons DungeonUpdate", dungeon.name, "AlertTimer", pkg);
            }
        }
        public void SpawnMobs()
        {
            if (Util.IsServer()) { /*ServerSpawnMobs()*/ }
            else ClientSpawnMobs();
        }
        public void DespawnMobs()
        {
            if (Util.IsServer()) { /*ServerDespawnMobs()*/ }
            else ClientDespawnMobs();
        }
        private void ClientDespawnMobs()
        {
            foreach (string tier in spawners.Keys)
                foreach (DungeonSpawnArea spawner in spawners[tier])
                    spawner.Despawn();

        }
        private void ClientSpawnMobs()
        {
            foreach (string tier in spawners.Keys)
                foreach (DungeonSpawnArea spawner in spawners[tier])
                    if (!spawner.m_hasSpawned) spawner.Spawn();
        }
        private void ServerSpawnMobs()
        {
            foreach (string tier in spawners.Keys)
                foreach (DungeonSpawnArea spawner in spawners[tier]) spawner.Spawn();
            //foreach (string tier in DynamicDungeons.tiers)
            //{
            //    string prefabName = "DungeonSpawner_" + tier;
            //    int hash = StringExtensionMethods.GetStableHashCode(prefabName);
            //    List<ZDO> spawnerZdos = new ASPUtils.DungeonZdoQuery(dungeon.corners).GetZdosInDungeon(hash);
            //    if (spawnerZdos.Count == 0) continue;
            //    foreach (ZDO zdo in spawnerZdos)
            //    {
            //        ZPackage pkg = new ZPackage();
            //        pkg.Write(dungeon.name);
            //        zdo.Set(DynamicDungeons.spawnerManagerHash, pkg.GetBase64());
            //        Jotunn.Logger.LogInfo("Set spawner manager to spawner ZDO id: " + zdo.m_uid);
            //    }
            //}
        }
        private void ServerDespawnMobs()
        {
            foreach (string tier in spawners.Keys)
                foreach (DungeonSpawnArea spawner in spawners[tier]) spawner.Despawn();
        }
        public void ClearChests()
        {
            ServerClearChests();
        }
        private void ServerClearChests()
        {
            foreach (string prefabName in DynamicDungeons.normalChestPrefabs)
            {
                normalChests.Clear();
                Jotunn.Logger.LogInfo("Getting chest prefab: " + prefabName);
                GameObject chestPrefab = ZNetScene.instance.m_prefabs.Find(p => p.name == prefabName);
                int hash = StringExtensionMethods.GetStableHashCode(prefabName);
                Jotunn.Logger.LogInfo("Getting normal chest ZDOS: " + prefabName);
                List<ZDO> normalChestZdos = new ASPUtils.DungeonZdoQuery(dungeon.corners).GetZdosInDungeon(hash);
                if (normalChestZdos.Count == 0) continue;
                Jotunn.Logger.LogInfo("Got normal chest ZDOS: " + chestPrefab.name);
                foreach (ZDO zdo in normalChestZdos)
                {
                    Jotunn.Logger.LogInfo("Creating container for ZDO " + zdo.m_uid);
                    Container container = chestPrefab.GetComponent<Container>();
                    Inventory inventory = container.GetInventory() ?? new Inventory("DungeonChest", container.m_bkg, container.m_width, container.m_height);
                    ReflectionHelper.SetPrivateField(container, "m_inventory", inventory);
                    Jotunn.Logger.LogInfo("Removing all inventory for normal chest ZDOID " + zdo.m_uid);
                    if (inventory.GetAllItems().Count != 0) inventory.RemoveAll();
                    ZPackage pkg = new ZPackage();
                    container.m_name = "Dungeon Chest";
                    Jotunn.Logger.LogInfo("Saving inventory for normal chest ZDOID " + zdo.m_uid);
                    container.GetInventory().Save(pkg);
                    Jotunn.Logger.LogInfo("Setting ZDO's items for normal chest ZDOID " + zdo.m_uid);
                    zdo.Set("items", pkg.GetBase64());
                    normalChests.Add(container);
                    Jotunn.Logger.LogInfo("Added chest container: " + prefabName);
                }
            }
        }
        public void RemoveDrops()
        {
            ServerRemoveDrops();
            foreach (long player in players)
                ZRoutedRpc.instance.InvokeRoutedRPC(player, "DynamicDungeons RemoveDrops");
        }
        private void ServerRemoveDrops()
        {

        }

        public void ServerScanChests()
        {
            foreach (string prefabName in DynamicDungeons.normalChestPrefabs)
            {
                Jotunn.Logger.LogInfo("Getting chest prefab: " + prefabName);
                GameObject chestPrefab = ZNetScene.instance.m_prefabs.Find(p => p.name == prefabName);
                int hash = StringExtensionMethods.GetStableHashCode(prefabName);
                Jotunn.Logger.LogInfo("Getting normal chest ZDOS: " + prefabName);
                List<ZDO> normalChestZdos = new ASPUtils.DungeonZdoQuery(dungeon.corners).GetZdosInDungeon(hash);
                if (normalChestZdos.Count == 0) continue;
                Jotunn.Logger.LogInfo("Got normal chest ZDOS: " + chestPrefab.name);
                foreach (ZDO zdo in normalChestZdos)
                {
                    Jotunn.Logger.LogInfo("Creating container for ZDO " + zdo.m_uid);
                    Container container = chestPrefab.GetComponent<Container>();
                    Inventory inventory = container.GetInventory() ?? new Inventory("DungeonChest", container.m_bkg, container.m_width, container.m_height);
                    ReflectionHelper.SetPrivateField(container, "m_inventory", inventory);
                    Jotunn.Logger.LogInfo("Removing all inventory for normal chest ZDOID " + zdo.m_uid);
                    if (inventory.GetAllItems().Count != 0) inventory.RemoveAll();
                    ZPackage pkg = new ZPackage();
                    container.m_name = "Dungeon Chest";
                    Jotunn.Logger.LogInfo("Setting default items for normal chest ZDOID " + zdo.m_uid);
                    List<DropTable.DropData> normalItems = new List<DropTable.DropData>();
                    foreach (DynamicDungeons.ChestItemData item in dungeon.normalChest.items)
                    {
                        if (item.prefab == null) { Jotunn.Logger.LogWarning($"Invalid prefab found in {dungeon.name}'s normal chest items."); continue; }
                        DropTable.DropData itemData = new DropTable.DropData
                        {
                            m_item = item.prefab,
                            m_weight = item.weight,
                            m_stackMin = item.minAmount,
                            m_stackMax = item.maxAmount
                        };
                        normalItems.Add(itemData);
                    }
                    container.m_defaultItems = new DropTable
                    {
                        m_dropChance = 1,
                        m_dropMin = dungeon.normalChest.minItems,
                        m_dropMax = dungeon.normalChest.maxItems,
                        m_oneOfEach = true,
                        m_drops = normalItems
                    };
                    Jotunn.Logger.LogInfo("Adding default items for normal chest ZDOID " + zdo.m_uid);
                    ReflectionHelper.InvokePrivate(container, "AddDefaultItems");
                    //container.AddDefaultItems();
                    Jotunn.Logger.LogInfo("Saving inventory for normal chest ZDOID " + zdo.m_uid);
                    container.GetInventory().Save(pkg);
                    Jotunn.Logger.LogInfo("Setting ZDO's items for normal chest ZDOID " + zdo.m_uid);
                    zdo.Set("items", pkg.GetBase64());
                    normalChests.Add(container);
                    Jotunn.Logger.LogInfo("Added chest container: " + prefabName);
                }
            }
        }
        public void ClientScanChests()
        {
            Collider[] colliders = Physics.OverlapBox(base.gameObject.transform.position, base.gameObject.transform.lossyScale / 2);
            if (colliders.Length == 0) { Jotunn.Logger.LogInfo("Didn't find anything inside dungeon area"); return; }
            if (normalChests.Count != 0) normalChests.Clear();
            if (specialChests.Count != 0) specialChests.Clear();
            foreach (Collider other in colliders)
            {
                //Jotunn.Logger.LogInfo(dungeon.name + ": Found " + other.gameObject.name);
                Player player = other.gameObject.GetComponent<Player>();
                if (!Util.IsServer() && player != null && player.m_name == Player.m_localPlayer.m_name)
                {
                    ZRoutedRpc.instance.InvokeRoutedRPC("DynamicDungeons EnteredDungeon", dungeon.name);
                    return;
                };
                if (player != null) players.Add(player.GetOwner());
                if (other.gameObject.name.ToLower().Contains("chest"))
                {
                    normalChests.Add(other.gameObject.GetComponent<Container>());
                };

            }
        }
        public void ClientScanSpawners()
        {
            Collider[] colliders = Physics.OverlapBox(base.gameObject.transform.position, base.gameObject.transform.lossyScale / 2);
            if (colliders.Length == 0) { Jotunn.Logger.LogInfo("Didn't find anything inside dungeon area"); return; }
            foreach (KeyValuePair<string, List<DungeonSpawnArea>> spawner in spawners) if (spawner.Value.Count != 0) spawner.Value.Clear();
            foreach (Collider other in colliders)
            {
                if (other.gameObject.name.Contains("DungeonSpawner_"))
                {
                    Jotunn.Logger.LogInfo("Found " + other.gameObject.name);
                    AddSpawnerToList(other.gameObject);
                };
            }
        }
        public void ServerScanSpawners()
        {
            foreach (string tier in DynamicDungeons.tiers)
            {
                string prefabName = "DungeonSpawner_" + tier;
                int hash = StringExtensionMethods.GetStableHashCode(prefabName);
                List<ZDO> spawnerZdos = new ASPUtils.DungeonZdoQuery(dungeon.corners).GetZdosInDungeon(hash);
                if (spawnerZdos.Count == 0) continue;
                foreach (ZDO zdo in spawnerZdos)
                {
                    ZPackage pkg = new ZPackage();
                    pkg.Write(dungeon.name);
                    zdo.Set(DynamicDungeons.spawnerManagerHash, pkg.GetBase64());
                    Jotunn.Logger.LogInfo("Set spawner manager to spawner ZDO id: " + zdo.m_uid);
                }
            }
        }
        public void ScanChests()
        {
            if (Util.IsServer()) ServerScanChests();
            else ClientScanChests();
        }
        public void ScanSpawners()
        {
            if (Util.IsServer()) ServerScanSpawners();
            else ClientScanSpawners();
        }
        public void LogDungeonInfo()
        {
            ScanChests();
            ScanSpawners();
            Jotunn.Logger.LogInfo("Found " + normalChests.Count + " normal chests");
            Jotunn.Logger.LogInfo("Found " + specialChests.Count + " special chests");
            Jotunn.Logger.LogInfo("Found " + spawners["DungeonSpawner_T1"].Count + " T1 spawners");
            Jotunn.Logger.LogInfo("Found " + spawners["DungeonSpawner_T2"].Count + " T2 spawners");
            Jotunn.Logger.LogInfo("Found " + spawners["DungeonSpawner_T3"].Count + " T3 spawners");
            Jotunn.Logger.LogInfo("Found " + spawners["DungeonSpawner_T4"].Count + " T4 spawners");
            Jotunn.Logger.LogInfo("Found " + spawners["DungeonSpawner_T5"].Count + " T5 spawners");
            Jotunn.Logger.LogInfo("Found " + spawners["DungeonSpawner_BOSS"].Count + " BOSS spawners");
        }
        public void StartEvent()
        {
            isActive = true;
            ScanChests();
            ScanSpawners();
            SpawnMobs();
        }
        public void StopEvent()
        {
            isActive = false;
            DespawnMobs();
            ClearChests();
            RemoveDrops();
        }
        private void AddSpawnerToList(GameObject prefab)
        {
            DungeonSpawnArea spawner = prefab.GetComponent<DungeonSpawnArea>();
            spawner.m_manager = this;
            Jotunn.Logger.LogInfo("Set spawner manager for " + prefab.name);
            if (prefab.name.Contains("DungeonSpawner_T1")) spawners["DungeonSpawner_T1"].Add(spawner);
            if (prefab.name.Contains("DungeonSpawner_T2")) spawners["DungeonSpawner_T2"].Add(spawner);
            if (prefab.name.Contains("DungeonSpawner_T3")) spawners["DungeonSpawner_T3"].Add(spawner);
            if (prefab.name.Contains("DungeonSpawner_T4")) spawners["DungeonSpawner_T4"].Add(spawner);
            if (prefab.name.Contains("DungeonSpawner_T5")) spawners["DungeonSpawner_T5"].Add(spawner);
            if (prefab.name.Contains("DungeonSpawner_BOSS")) spawners["DungeonSpawner_BOSS"].Add(spawner);
        }
        private void RemoveSpawnerFromList(GameObject prefab)
        {
            DungeonSpawnArea spawner = prefab.GetComponent<DungeonSpawnArea>();
            if (prefab.name.Contains("DungeonSpawner_T1")) spawners["DungeonSpawner_T1"].Remove(spawner);
            if (prefab.name.Contains("DungeonSpawner_T2")) spawners["DungeonSpawner_T2"].Remove(spawner);
            if (prefab.name.Contains("DungeonSpawner_T3")) spawners["DungeonSpawner_T3"].Remove(spawner);
            if (prefab.name.Contains("DungeonSpawner_T4")) spawners["DungeonSpawner_T4"].Remove(spawner);
            if (prefab.name.Contains("DungeonSpawner_T5")) spawners["DungeonSpawner_T5"].Remove(spawner);
            if (prefab.name.Contains("DungeonSpawner_BOSS")) spawners["DungeonSpawner_BOSS"].Remove(spawner);
        }
    }
}
