using System.Collections.Generic;
using UnityEngine;

namespace DynamicDungeons
{
    public class DungeonSpawnArea : MonoBehaviour
    {
        public class SpawnData
        {
            public GameObject m_prefab;

            public float m_weight;

            public int m_maxLevel = 3;

            public int m_minLevel = 1;
        }

        public DungeonEventManager m_manager;
        public List<SpawnData> m_prefabs = new List<SpawnData>();
        public DynamicDungeons.MobTier m_tier;

        public GameObject m_areaMarker;
        public float m_levelupChance = 15f;

        public float m_spawnIntervalSec = 30f;

        public float m_triggerDistance = 256f;

        public bool m_setPatrolSpawnPoint = true;

        public float m_spawnRadius = 2f;

        public float m_nearRadius = 8f;

        public float m_farRadius = 15f;

        public int m_maxNear = 3;

        public int m_maxTotal = 20;

        public bool m_onGroundOnly;

        public EffectList m_spawnEffects = new EffectList();

        public ZNetView m_nview;

        public float m_spawnTimer;
        public int m_minSpawned = 1;
        public int m_maxSpawned = 3;
        public void Awake()
        {
            m_nview = GetComponent<ZNetView>();
            InvokeRepeating("UpdateSpawn", 2f, 2f);
        }
        public void Start()
        {
            if (!m_nview || m_nview.GetZDO() != null)
            {
                if (!(bool)m_areaMarker)
                {
                    m_areaMarker.SetActive(true);
                }
            }
        }
        public void UpdateSpawn()
        {
            if (m_nview.IsOwner() && !ZNetScene.instance.OutsideActiveArea(base.transform.position) && Player.IsPlayerInRange(base.transform.position, m_triggerDistance))
            {
                m_spawnTimer += 2f;
                if (m_spawnTimer > m_spawnIntervalSec)
                {
                    m_spawnTimer = 0f;
                    int randomSpawnAmount = Mathf.RoundToInt(Random.Range(m_minSpawned, m_maxSpawned));
                    SpawnMany(randomSpawnAmount);
                }
            }
        }

        public List<GameObject> SpawnMany(int amount)
        {
            GetInstances(out var near, out var total);
            if (total >= m_maxTotal)
            {
                return new List<GameObject>();
            }
            List<GameObject> attempts = new List<GameObject>();
            List<GameObject> successFulAttempts = new List<GameObject>();
            GameObject spawned = SpawnOne();
            do
            {
                attempts.Add(spawned);
                if (spawned) successFulAttempts.Add(spawned);
            } while (successFulAttempts.Count != amount);
            return successFulAttempts;
        }

        public GameObject SpawnOne()
        {
            SpawnData spawnData = SelectWeightedPrefab();
            if (spawnData == null)
            {
                return null;
            }
            if (!FindSpawnPoint(spawnData.m_prefab, out var point))
            {
                return null;
            }
            GameObject gameObject = Instantiate(spawnData.m_prefab, point, Quaternion.Euler(0f, Random.Range(0, 360), 0f));
            if (m_setPatrolSpawnPoint)
            {
                BaseAI ai = gameObject.GetComponent<BaseAI>();
                if (ai != null)
                {
                    ai.SetPatrolPoint();
                }
            }
            Character character = gameObject.GetComponent<Character>();
            if (spawnData.m_maxLevel > 1)
            {
                int i;
                for (i = spawnData.m_minLevel; i < spawnData.m_maxLevel; i++)
                {
                    if (!(Random.Range(0f, 100f) <= m_levelupChance))
                    {
                        break;
                    }
                }
                if (i > 1)
                {
                    character.SetLevel(i);
                }
            }
            Vector3 centerPoint = character.GetCenterPoint();
            m_spawnEffects.Create(centerPoint, Quaternion.identity);
            return gameObject;
        }

        public bool FindSpawnPoint(GameObject prefab, out Vector3 point)
        {
            prefab.GetComponent<BaseAI>();
            for (int i = 0; i < 10; i++)
            {
                Vector3 vector = base.transform.position + Quaternion.Euler(0f, UnityEngine.Random.Range(0, 360), 0f) * Vector3.forward * UnityEngine.Random.Range(0f, m_spawnRadius);
                if (ZoneSystem.instance.FindFloor(vector, out var height) && (!m_onGroundOnly || !ZoneSystem.instance.IsBlocked(vector)))
                {
                    vector.y = height + 0.1f;
                    point = vector;
                    return true;
                }
            }
            point = Vector3.zero;
            return false;
        }

        public SpawnData SelectWeightedPrefab()
        {
            if (m_prefabs.Count == 0)
            {
                return null;
            }
            float num = 0f;
            foreach (SpawnData prefab in m_prefabs)
            {
                num += prefab.m_weight;
            }
            float num2 = UnityEngine.Random.Range(0f, num);
            float num3 = 0f;
            foreach (SpawnData prefab2 in m_prefabs)
            {
                num3 += prefab2.m_weight;
                if (num2 <= num3)
                {
                    return prefab2;
                }
            }
            return m_prefabs[m_prefabs.Count - 1];
        }

        public void GetInstances(out int near, out int total)
        {
            near = 0;
            total = 0;
            Vector3 position = base.transform.position;
            foreach (BaseAI allInstance in BaseAI.GetAllInstances())
            {
                if (IsSpawnPrefab(allInstance.gameObject))
                {
                    float num = Utils.DistanceXZ(allInstance.transform.position, position);
                    if (num < m_nearRadius)
                    {
                        near++;
                    }
                    if (num < m_farRadius)
                    {
                        total++;
                    }
                }
            }
        }

        public bool IsSpawnPrefab(GameObject go)
        {
            string text = go.name;
            Character component = go.GetComponent<Character>();
            foreach (SpawnData prefab in m_prefabs)
            {
                if (text.StartsWith(prefab.m_prefab.name) && (!component || !component.IsTamed()))
                {
                    return true;
                }
            }
            return false;
        }

        public void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(base.transform.position, m_spawnRadius);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(base.transform.position, m_nearRadius);
        }
    }
}
