using Jotunn.Utils;
using System.Collections.Generic;
using UnityEngine;
namespace DynamicDungeons
{
    public class DungeonSpawnArea : MonoBehaviour, Hoverable, Interactable
    {
        public DungeonEventManager m_manager;

        public List<DynamicDungeons.SpawnData> m_prefabs = new List<DynamicDungeons.SpawnData>();

        public string m_tier;

        public GameObject m_spawnRadiusMarker;

        public GameObject m_triggerDistanceMarker;

        public float m_levelupChance = 15f;

        public float m_spawnIntervalSec = 30f;

        public float m_triggerDistance = 10f;

        public bool m_setPatrolSpawnPoint = true;

        public float m_spawnRadius = 2f;

        public float m_nearRadius = 8f;

        public float m_farRadius = 8f;

        public int m_maxNear = 3;

        public int m_maxTotal = 20;

        public bool m_onGroundOnly;

        public EffectList m_spawnEffects = new EffectList();

        public ZNetView m_nview;

        public bool m_editing = false;

        public float m_spawnTimer;

        public int m_minSpawned = 1;

        public int m_maxSpawned = 3;

        private Player m_player;

        private CircleProjector spawnProjector;

        private CircleProjector triggerProjector;
        public float m_resizeTimer = 0f;


        public void Awake()
        {
            Initialize();
            InvokeRepeating("UpdateSpawn", 2f, 2f);
        }
        public void Start()
        {
            GameObject segment = DynamicDungeons.workbenchMarker.GetComponent<CircleProjector>().m_prefab;
            GameObject triggerSegment = Instantiate(segment);
            triggerSegment.GetComponent<Renderer>().material.color = DynamicDungeons.tierColors[m_tier];
            triggerSegment.name = "spawnersegment_" + m_tier;
            m_spawnRadiusMarker = Instantiate(DynamicDungeons.workbenchMarker, base.transform);
            spawnProjector = m_spawnRadiusMarker.GetComponent<CircleProjector>();
            spawnProjector.m_radius = m_spawnRadius;
            spawnProjector.m_nrOfSegments = Mathf.RoundToInt(m_spawnRadius * 4);
            m_triggerDistanceMarker = Instantiate(DynamicDungeons.workbenchMarker, base.transform);
            triggerProjector = m_triggerDistanceMarker.GetComponent<CircleProjector>();
            triggerProjector.m_prefab = triggerSegment;
            triggerProjector.m_radius = m_triggerDistance;
            triggerProjector.m_nrOfSegments = Mathf.RoundToInt(m_triggerDistance * 4);
            if (!m_nview || m_nview.GetZDO() != null)
            {
                if (!(bool)m_spawnRadiusMarker) m_spawnRadiusMarker.SetActive(true);
                if (!(bool)m_triggerDistanceMarker) m_triggerDistanceMarker.SetActive(true);
            }
        }
        public void Update()
        {
            if (!m_editing) return;
            if (m_resizeTimer <= 0.25f) { m_resizeTimer += Time.deltaTime; return; }
            if (m_resizeTimer > 0.25f) m_resizeTimer = 0f;
            if (Input.GetKey(KeyCode.KeypadPlus))
            {
                if (Input.GetKey(KeyCode.LeftShift))
                {
                    IncreaseSpawnRadius();
                    return;
                }
                IncreaseTriggerRadius();
                return;
            }
            if (Input.GetKey(KeyCode.KeypadMinus))
            {
                if (Input.GetKey(KeyCode.LeftShift))
                {
                    DecreaseSpawnRadius();
                    return;
                }
                DecreaseTriggerRadius();
                return;
            }
        }
        private void Initialize()
        {
            m_nview = GetComponent<ZNetView>();
            m_tier = base.gameObject.name.Split('_')[1].Split('(')[0];
            if (!m_nview || m_nview.GetZDO() != null)
            {
                InitializeManager();
                DynamicDungeons.MobConfig mobConfig = DynamicDungeons.dungeons.Find(d => d.name == m_manager.dungeon.name).mobConfig[Util.StringToTier(m_tier)];
                m_spawnRadius = mobConfig.spawnRadius;
                m_triggerDistance = mobConfig.scanRadius;
                InitializeRanges();
                m_spawnIntervalSec = mobConfig.spawnCooldown;
                m_maxNear = mobConfig.maxSpawned;
                m_maxTotal = mobConfig.maxSpawned;
                m_minSpawned = mobConfig.minAmount;
                m_maxSpawned = mobConfig.maxAmount;
                m_prefabs = mobConfig.mobs;
                Jotunn.Logger.LogInfo("Loaded data for " + m_tier + " spawner");
            }
        }
        private void InitializeManager()
        {
            string managerName64 = m_nview.GetZDO().GetString(DynamicDungeons.spawnerManagerHash);
            ZPackage manPkg = new ZPackage(managerName64);
            if (manPkg.Size() == 0) return;
            string managerName = manPkg.ReadString();
            m_manager = DungeonManager.Instance.managers[managerName];
            Jotunn.Logger.LogInfo("Loaded manager " + m_manager.dungeon.name + " for " + m_tier + " spawner");
        }
        private void InitializeRanges()
        {
            string rangeData64 = m_nview.GetZDO().GetString(DynamicDungeons.spawnerDataHash);
            ZPackage rangesPkg = new ZPackage(rangeData64);
            if (rangesPkg.Size() == 0) return;
            string newSpawnRadius = rangesPkg.ReadString();
            string newTriggerDistance = rangesPkg.ReadString();
            m_spawnRadius = float.Parse(newSpawnRadius);
            m_triggerDistance = float.Parse(newTriggerDistance);
            Jotunn.Logger.LogInfo("Loaded ranges for " + m_tier + " spawner");
        }
        private void IncreaseSpawnRadius()
        {
            m_spawnRadius += 0.5f;
            spawnProjector.m_radius += 0.5f;
            spawnProjector.m_nrOfSegments = Mathf.RoundToInt(m_spawnRadius * 4);
        }
        private void DecreaseSpawnRadius()
        {
            m_spawnRadius -= 0.5f;
            spawnProjector.m_radius -= 0.5f;
            spawnProjector.m_nrOfSegments = Mathf.RoundToInt(m_spawnRadius * 4);
        }
        private void IncreaseTriggerRadius()
        {
            m_triggerDistance += 0.5f;
            triggerProjector.m_radius += 0.5f;
            triggerProjector.m_nrOfSegments = Mathf.RoundToInt(m_triggerDistance * 4);
        }
        private void DecreaseTriggerRadius()
        {
            m_triggerDistance -= 0.5f;
            triggerProjector.m_radius -= 0.5f;
            triggerProjector.m_nrOfSegments = Mathf.RoundToInt(m_triggerDistance * 4);
        }
        public string GetHoverName()
        {
            return m_tier + " Spawner";
        }
        public string GetHoverText()
        {
            if (m_editing) return Localization.instance.Localize("\n[<color=yellow><b>$KEY_Use</b></color>] ") + "Guardar " + m_tier + " Spawner";
            return Localization.instance.Localize("\n[<color=yellow><b>$KEY_Use</b></color>] ") + "Editar " + m_tier + " Spawner";
        }
        public bool Interact(Humanoid other, bool hold, bool alt)
        {
            Player player = other.gameObject.GetComponent<Player>();
            if (player == null) return false;
            m_nview.GetZDO().SetOwner((long)ReflectionHelper.InvokePrivate(ZRoutedRpc.instance, "GetServerPeerID"));
            m_player = player;
            m_editing = !m_editing;
            Jotunn.Logger.LogInfo("Editing " + m_tier + " spawner: " + m_editing);
            if (!m_editing)
            {
                ZPackage pkg = new ZPackage();
                pkg.Write(m_spawnRadius.ToString());
                pkg.Write(m_triggerDistance.ToString());
                m_nview.GetZDO().Set(DynamicDungeons.spawnerDataHash, pkg.GetBase64());
                Jotunn.Logger.LogInfo("Saved ranges to ZDO: " + m_nview.GetZDO().m_uid);
                Jotunn.Logger.LogInfo("Spawn radius: " + m_spawnRadius);
                Jotunn.Logger.LogInfo("Trigger radius: " + m_triggerDistance);
            }
            return true;
        }
        public bool UseItem(Humanoid other, ItemDrop.ItemData item) { return false; }
        public void UpdateSpawn()
        {
            if (m_manager != null && m_manager.isActive && m_nview.IsOwner() && !ZNetScene.instance.OutsideActiveArea(base.transform.position) && Player.IsPlayerInRange(base.transform.position, m_triggerDistance))
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
            DynamicDungeons.SpawnData spawnData = SelectWeightedPrefab();
            if (spawnData == null)
            {
                return null;
            }
            if (!FindSpawnPoint(spawnData.prefab, out var point))
            {
                return null;
            }
            GameObject gameObject = Instantiate(spawnData.prefab, point, Quaternion.Euler(0f, Random.Range(0, 360), 0f));
            if (m_setPatrolSpawnPoint)
            {
                BaseAI ai = gameObject.GetComponent<BaseAI>();
                if (ai != null)
                {
                    ai.SetPatrolPoint();
                }
            }
            Character character = gameObject.GetComponent<Character>();
            if (spawnData.maxLevel > 1)
            {
                int i;
                for (i = spawnData.minLevel; i < spawnData.maxLevel; i++)
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
            Jotunn.Logger.LogInfo(base.gameObject.name + " spawned " + gameObject.name);
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

        public DynamicDungeons.SpawnData SelectWeightedPrefab()
        {
            if (m_prefabs.Count == 0)
            {
                return null;
            }
            float num = 0f;
            foreach (DynamicDungeons.SpawnData prefab in m_prefabs)
            {
                num += prefab.weight;
            }
            float num2 = UnityEngine.Random.Range(0f, num);
            float num3 = 0f;
            foreach (DynamicDungeons.SpawnData prefab2 in m_prefabs)
            {
                num3 += prefab2.weight;
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
            foreach (DynamicDungeons.SpawnData prefab in m_prefabs)
            {
                if (text.StartsWith(prefab.prefab.name) && (!component || !component.IsTamed()))
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
