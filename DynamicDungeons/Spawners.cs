using System.Collections.Generic;
using UnityEngine;

namespace DynamicDungeons
{
    public partial class DynamicDungeons
    {
        public class SimpleVector3
        {
            float x;
            float y;
            float z;

            public SimpleVector3(float _x, float _y, float _z)
            {
                x = _x;
                y = _y;
                z = _z;
            }
        }
        public enum MobTier
        {
            NONE,
            T1,
            T2,
            T3,
            T4,
            T5,
            BOSS
        }
        public class SpawnData
        {
            public GameObject prefab;
            public float weight;
            public int minLevel;
            public int maxLevel;
        }
        public class StoredSpawnData
        {
            public string prefab;
            public float weight;
            public int minLevel;
            public int maxLevel;
        }
        public class CustomSpawnerConfig
        {


            public string name = "DD_CustomSpawner";

            public List<SpawnData> prefabs = new List<SpawnData>();

            public float levelupChance = 15f;

            public float spawnIntervalSec = 30f;

            public float triggerDistance = 256f;

            public bool setPatrolSpawnPoint = true;

            public float spawnRadius = 2f;

            public float nearRadius = 10f;

            public float farRadius = 1000f;

            public int maxNear = 3;

            public int maxTotal = 20;

            public bool onGroundOnly;

            public List<string> spawnEffects = new List<string>();

            public int minSpawned = 1;

            public int maxSpawned = 3;

        }
        public class StoredSpawnerConfig : CustomSpawnerConfig
        {

            public List<StoredSpawnData> storedPrefabs { get; set; }
        }
        public class ChestItemData
        {
            public GameObject prefab;
            public float weight;
            public int minAmount;
            public int maxAmount;
        }
        public class StoredChestItemData
        {
            public string prefab;
            public float weight;
            public int minAmount;
            public int maxAmount;
        }
        public class ChestItemConfig
        {
            public int minItems = 1;
            public int maxItems = 3;
            public List<ChestItemData> items;
        }
        public class StoredChestItemConfig
        {
            public int minItems = 1;
            public int maxItems = 3;
            public List<StoredChestItemData> items;

        }
        public class MobConfig
        {
            public int minAmount = 1;
            public int maxAmount = 3;
            public int maxSpawned = 3;
            public int spawnCooldown = 750;
            public float spawnRadius = 3f;
            public float scanRadius = 5f;
            public List<DynamicDungeons.SpawnData> mobs;
        }
        public class StoredMobConfig
        {
            public int minAmount = 1;
            public int maxAmount = 3;
            public int maxSpawned = 3;
            public int spawnCooldown = 750;
            public float spawnRadius = 3f;
            public float scanRadius = 5f;
            public List<StoredSpawnData> mobs;
        }
        public class DynamicDungeon
        {
            public string name;
            public List<Vector3> corners;
            public Dictionary<MobTier, MobConfig> mobConfig;
            public ChestItemConfig normalChest;
            public ChestItemConfig specialChest;
        }
        public class StoredDungeonConfig
        {
            public string name;
            public List<Vector3> corners;
            public bool isActive = false;
            public string activeEvent;
            public List<string> playersInside;
            public Dictionary<string, StoredMobConfig> mobConfig;
            public StoredChestItemConfig normalChest;
            public StoredChestItemConfig specialChest;
        }
        public enum UpdateType
        {
            CurrentDungeon,
            EventState,
        }
    }
}
