using Jotunn.Managers;
using System.Collections.Generic;
using UnityEngine;

namespace DynamicDungeons
{
    public class DungeonEventManager : MonoBehaviour
    {
        public List<Player> players = new List<Player>();
        public DynamicDungeons.DynamicDungeon dungeon;
        public bool isActive = false;
        public Dictionary<string, List<DungeonSpawnArea>> spawners;
        private List<Container> normalChests = new List<Container>();
        private List<Container> specialChests = new List<Container>();
        private Dictionary<string, List<GameObject>> spawnedMobs = new Dictionary<string, List<GameObject>>();
        private float testTimer = 0f;
        private readonly string[] tiers =
        {
            "Spawners_T1",
            "Spawners_T2",
            "Spawners_T3",
            "Spawners_T4",
            "Spawners_T5",
            "Spawners_BOSS"
        };
        private void Start()
        {
            spawners = new Dictionary<string, List<DungeonSpawnArea>>();
            spawnedMobs = new Dictionary<string, List<GameObject>>();
            foreach (string tier in tiers)
            {
                spawners.Add(tier, new List<DungeonSpawnArea>());
                spawnedMobs.Add(tier, new List<GameObject>());
            }
            ScanObjects();
        }
        private void Update()
        {
            if (!DynamicDungeons.IsServer) return;
            if (!isActive) return;
            if (testTimer < 5) { testTimer += Time.deltaTime; return; }
            if (!Util.FindSpawnPoint(base.gameObject.transform.position, 5, out Vector3 point))
            {
                Jotunn.Logger.LogInfo("Couldn't find spawn point for Greydwarf");
                return;
            }
            Instantiate(PrefabManager.Instance.GetPrefab("Greydwarf"), point, Quaternion.Euler(0f, Random.Range(0, 360), 0f));
            testTimer = 0;
            Jotunn.Logger.LogInfo("Spawned Greydwarf");
            return;

        }
        private void OnTriggerEnter(Collider other)
        {
            Player player = other.gameObject.GetComponent<Player>();
            if (!DynamicDungeons.IsServer && player != null && player.m_name == Player.m_localPlayer.m_name)
            {
                ZRoutedRpc.instance.InvokeRoutedRPC("DynamicDungeons EnteredDungeon", dungeon.name);
                DynamicDungeons.currentDungeon = this;
                Player.m_localPlayer.Message(MessageHud.MessageType.Center, "Entrando a " + dungeon.name);
                return;
            };
        }
        private void OnTriggerExit(Collider other)
        {
            Player player = other.gameObject.GetComponent<Player>();
            if (!DynamicDungeons.IsServer && player != null && player.m_name == Player.m_localPlayer.m_name)
            {
                ZRoutedRpc.instance.InvokeRoutedRPC("DynamicDungeons ExitedDungeon", dungeon.name);
                DynamicDungeons.currentDungeon = null;
                Player.m_localPlayer.Message(MessageHud.MessageType.Center, "Saliendo de " + dungeon.name);
                return;
            };
        }
        public void ClientScanChests()
        {
            Collider[] colliders = Physics.OverlapBox(base.gameObject.transform.position, base.gameObject.transform.lossyScale / 2);
            if (colliders.Length == 0) { Jotunn.Logger.LogInfo("Didn't find anything inside dungeon area"); return; }
            if (normalChests.Count != 0) normalChests.Clear();
            if (specialChests.Count != 0) specialChests.Clear();
            foreach (KeyValuePair<string, List<DungeonSpawnArea>> spawner in spawners) if (spawner.Value.Count != 0) spawner.Value.Clear();
            foreach (Collider other in colliders)
            {
                Jotunn.Logger.LogInfo(dungeon.name + ": Found " + other.gameObject.name);
                Player player = other.gameObject.GetComponent<Player>();
                if (!DynamicDungeons.IsServer && player != null && player.m_name == Player.m_localPlayer.m_name)
                {
                    ZRoutedRpc.instance.InvokeRoutedRPC("DynamicDungeons EnteredDungeon", dungeon.name);
                    return;
                };
                if (player != null) players.Add(player);
                if (other.gameObject.name.ToLower().Contains("chest"))
                {
                    Jotunn.Logger.LogInfo("Found normal chest");
                    normalChests.Add(other.gameObject.GetComponent<Container>());
                };
                if (other.gameObject.name.Contains("DungeonSpawner_"))
                {
                    Jotunn.Logger.LogInfo("Found " + other.gameObject.name);
                    AddSpawnerToList(other.gameObject);
                };
            }
        }
        public void ServerScanChests()
        {
            Collider[] colliders = Physics.OverlapBox(base.gameObject.transform.position, base.gameObject.transform.lossyScale / 2);
            if (colliders.Length == 0) { Jotunn.Logger.LogInfo("Didn't find anything inside dungeon area"); return; }
            if (normalChests.Count != 0) normalChests.Clear();
            if (specialChests.Count != 0) specialChests.Clear();
            foreach (KeyValuePair<string, List<DungeonSpawnArea>> spawner in spawners) if (spawner.Value.Count != 0) spawner.Value.Clear();
            foreach (Collider other in colliders)
            {
                Jotunn.Logger.LogInfo(dungeon.name + ": Found " + other.gameObject.name);
                Player player = other.gameObject.GetComponent<Player>();
                if (!DynamicDungeons.IsServer && player != null && player.m_name == Player.m_localPlayer.m_name)
                {
                    ZRoutedRpc.instance.InvokeRoutedRPC("DynamicDungeons EnteredDungeon", dungeon.name);
                    return;
                };
                if (player != null) players.Add(player);
                if (other.gameObject.name.ToLower().Contains("chest"))
                {
                    Jotunn.Logger.LogInfo("Found normal chest");
                    normalChests.Add(other.gameObject.GetComponent<Container>());
                };
                if (other.gameObject.name.Contains("DungeonSpawner_"))
                {
                    Jotunn.Logger.LogInfo("Found " + other.gameObject.name);
                    AddSpawnerToList(other.gameObject);
                };
            }
        }
        public void LogDungeonInfo()
        {
            if (DynamicDungeons.IsServer) ServerScanChests();
            else ClientScanChests();
            Jotunn.Logger.LogInfo("Found " + normalChests.Count + " normal chests");
            Jotunn.Logger.LogInfo("Found " + specialChests.Count + " special chests");
            Jotunn.Logger.LogInfo("Found " + spawners["Spawners_T1"].Count + " T1 spawners");
            Jotunn.Logger.LogInfo("Found " + spawners["Spawners_T2"].Count + " T2 spawners");
            Jotunn.Logger.LogInfo("Found " + spawners["Spawners_T3"].Count + " T3 spawners");
            Jotunn.Logger.LogInfo("Found " + spawners["Spawners_T4"].Count + " T4 spawners");
            Jotunn.Logger.LogInfo("Found " + spawners["Spawners_T5"].Count + " T5 spawners");
            Jotunn.Logger.LogInfo("Found " + spawners["Spawners_BOSS"].Count + " BOSS spawners");
        }
        public void StartEvent()
        {
            Jotunn.Logger.LogInfo("StartEvent RPC");
            isActive = true;
        }
        public void StopEvent()
        {
            Jotunn.Logger.LogInfo("StopEvent RPC");
            isActive = false;
        }
        private void AddSpawnerToList(GameObject prefab)
        {
            DungeonSpawnArea spawner = prefab.GetComponent<DungeonSpawnArea>();
            if (prefab.name.Contains("DungeonSpawner_T1")) spawners["T1_spawners"].Add(spawner);
            if (prefab.name.Contains("DungeonSpawner_T2")) spawners["T2_spawners"].Add(spawner);
            if (prefab.name.Contains("DungeonSpawner_T3")) spawners["T3_spawners"].Add(spawner);
            if (prefab.name.Contains("DungeonSpawner_T4")) spawners["T4_spawners"].Add(spawner);
            if (prefab.name.Contains("DungeonSpawner_T5")) spawners["T5_spawners"].Add(spawner);
            if (prefab.name.Contains("DungeonSpawner_BOSS")) spawners["BOSS_spawners"].Add(spawner);
        }
        private void RemoveSpawnerFromList(GameObject prefab)
        {
            DungeonSpawnArea spawner = prefab.GetComponent<DungeonSpawnArea>();
            if (prefab.name.Contains("DungeonSpawner_T1")) spawners["T1_spawners"].Remove(spawner);
            if (prefab.name.Contains("DungeonSpawner_T2")) spawners["T2_spawners"].Remove(spawner);
            if (prefab.name.Contains("DungeonSpawner_T3")) spawners["T3_spawners"].Remove(spawner);
            if (prefab.name.Contains("DungeonSpawner_T4")) spawners["T4_spawners"].Remove(spawner);
            if (prefab.name.Contains("DungeonSpawner_T5")) spawners["T5_spawners"].Remove(spawner);
            if (prefab.name.Contains("DungeonSpawner_BOSS")) spawners["BOSS_spawners"].Remove(spawner);
        }
    }
}
