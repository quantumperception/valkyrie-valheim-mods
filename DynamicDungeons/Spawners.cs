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
            public List<CharacterDrop.Drop> drops;
        }
        public class StoredSpawnData
        {
            public string prefab;
            public float weight;
            public int minLevel;
            public int maxLevel;
            public List<StoredDropConfig> drops;
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
            public List<CharacterDrop.Drop> tierDrops;
            public List<SpawnData> mobs;
        }
        public class StoredMobConfig
        {
            public int minAmount = 1;
            public int maxAmount = 3;
            public int maxSpawned = 3;
            public int spawnCooldown = 750;
            public float spawnRadius = 3f;
            public float scanRadius = 5f;
            public List<StoredDropConfig> tierDrops;
            public List<StoredSpawnData> mobs;
        }
        public class DynamicDungeon
        {
            public string name;
            public List<Vector3> corners;
            public Dictionary<string, MobConfig> mobConfig;
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
        public class StoredDropConfig
        {
            public string prefab;
            public int minAmount;
            public int maxAmount;
            public float dropChance;
            public bool onePerPlayer;
            public bool levelMultiplier;
        }
        public enum UpdateType
        {
            CurrentDungeon,
            EventState,
        }
        public class CustomCircleProjector : MonoBehaviour
        {
            public float m_radius = 5f;

            public int m_nrOfSegments = 20;

            public float m_speed = 0.1f;

            public float m_turns = 1f;

            public float m_start;

            public bool m_sliceLines;

            public float m_calcStart;

            public float m_calcTurns;

            public GameObject m_prefab;

            public LayerMask m_mask;

            public List<GameObject> m_segments = new List<GameObject>();

            public bool m_snap = true;

            public void Start()
            {
                CreateSegments();
            }

            public void Update()
            {
                CreateSegments();
                bool flag = m_turns == 1f;
                float num = (float)Mathf.PI * 2f * m_turns / (float)(m_nrOfSegments - ((!flag) ? 1 : 0));
                float num2 = ((flag && !m_sliceLines) ? (Time.time * m_speed) : 0f);
                for (int i = 0; i < m_nrOfSegments; i++)
                {
                    float f = (float)Mathf.PI / 180f * m_start + (float)i * num + num2;
                    Vector3 vector = base.transform.position + new Vector3(Mathf.Sin(f) * m_radius, 0f, Mathf.Cos(f) * m_radius);
                    GameObject obj = m_segments[i];
                    vector = new Vector3(vector.x, base.transform.position.y - 0.3f, vector.z);
                    obj.transform.position = vector;
                }
                for (int j = 0; j < m_nrOfSegments; j++)
                {
                    GameObject gameObject = m_segments[j];
                    GameObject gameObject2;
                    GameObject gameObject3;
                    if (flag)
                    {
                        gameObject2 = ((j == 0) ? m_segments[m_nrOfSegments - 1] : m_segments[j - 1]);
                        gameObject3 = ((j == m_nrOfSegments - 1) ? m_segments[0] : m_segments[j + 1]);
                    }
                    else
                    {
                        gameObject2 = ((j == 0) ? gameObject : m_segments[j - 1]);
                        gameObject3 = ((j == m_nrOfSegments - 1) ? gameObject : m_segments[j + 1]);
                    }
                    Vector3 normalized = (gameObject3.transform.position - gameObject2.transform.position).normalized;
                    gameObject.transform.rotation = Quaternion.LookRotation(normalized, Vector3.up);
                }
                for (int k = m_nrOfSegments; k < m_segments.Count; k++)
                {
                    Vector3 position = m_segments[k].transform.position;
                    position = new Vector3(position.x, base.transform.position.y - 0.3f, position.z);
                    m_segments[k].transform.position = position;
                }
            }

            public void CreateSegments()
            {
                if ((!m_sliceLines && m_segments.Count == m_nrOfSegments) || (m_sliceLines && m_calcStart == m_start && m_calcTurns == m_turns))
                {
                    return;
                }
                foreach (GameObject segment in m_segments)
                {
                    UnityEngine.Object.Destroy(segment);
                }
                m_segments.Clear();
                for (int i = 0; i < m_nrOfSegments; i++)
                {
                    GameObject item = UnityEngine.Object.Instantiate(m_prefab, base.transform.position, Quaternion.identity, base.transform);
                    m_segments.Add(item);
                }
                m_calcStart = m_start;
                m_calcTurns = m_turns;
                if (m_sliceLines)
                {
                    float start = m_start;
                    float angle2 = m_start + (float)Mathf.PI * 2f * m_turns * 57.29578f;
                    float num = 2f * m_radius * (float)Mathf.PI * m_turns / (float)m_nrOfSegments;
                    int count2 = (int)(m_radius / num) - 2;
                    placeSlices(start, count2);
                    placeSlices(angle2, count2);
                }
                void placeSlices(float angle, int count)
                {
                    for (int j = 0; j < count; j++)
                    {
                        GameObject gameObject = UnityEngine.Object.Instantiate(m_prefab, base.transform.position, Quaternion.Euler(0f, angle, 0f), base.transform);
                        gameObject.transform.position += gameObject.transform.forward * m_radius * ((float)(j + 1) / (float)(count + 1));
                        m_segments.Add(gameObject);
                    }
                }
            }
        }
    }
}
