using Jotunn.Managers;
using UnityEngine;
namespace Cannons
{


    public class Cannon : MonoBehaviour, Interactable, Hoverable
    {
        public string m_name = "Cañón";
        public GameObject barrel; // The barrel of the cannon
        private Collider barrelCollider;
        public float maxPushForce = 100f; // The maximum force that can be applied to the cannon
        public float pushMultiplier = 1f; // A multiplier for the amount of force applied when pushing
        public Transform playerAttach; // The mount point for the player to be fixed to when pushing the cannon
        public Projectile projectile;
        public GameObject projectilePrefab; // The prefab for the projectile to be loaded and shot from the cannon
        public Transform projectileSpawnPoint;
        public float projectileSpeed = 50f; // The speed at which the projectile is launched from the cannon
        public float projectileLifeTime = 10f; // The lifetime of the projectile before it is destroyed
        private bool reloading = false; // Whether the cannon is currently aiming at a target
        private float fireTimer = 0f; // The time since the cannon was last fired
        private Quaternion originalBarrelRotation;
        private Player player; // The player object that is pushing the cannon

        private bool loadedProjectile;
        private GameObject currentProjectile; // The current projectile loaded in the cannon
        private static ItemDrop.ItemData arbalestItem;
        private static ItemDrop.ItemData blackmetalBolt;
        private static GameObject aoeSpawn;

        private void Awake()
        {

        }

        private void Start()
        {
            playerAttach = this.barrel.transform.parent.parent.Find("attach");
            barrelCollider = barrel.GetComponentInChildren<Collider>();
            originalBarrelRotation = barrel.transform.rotation;
            arbalestItem = PrefabManager.Instance.GetPrefab("DvergerArbalest").GetComponent<ItemDrop>().m_itemData;
            blackmetalBolt = PrefabManager.Instance.GetPrefab("BoltBlackmetal").GetComponent<ItemDrop>().m_itemData;
            aoeSpawn = PrefabManager.Instance.GetPrefab("sledge_aoe");
            aoeSpawn.AddComponent<Aoe>().m_damage = new HitData.DamageTypes { m_blunt = 80 };
        }
        private void Update()
        {
            if (player != null && Cannons.usingCannon)
            {
                if (!player.IsAttached()) SetActive(false);
                if (fireTimer >= 1f) reloading = false;
                if (reloading) fireTimer += Time.deltaTime;
                if (barrel != null && Cannons.cameraTransform != null) barrel.transform.LookAt(Cannons.cameraTransform.position + Cannons.cameraTransform.forward * 100f);
                if (Input.GetKeyDown(KeyCode.F) && loadedProjectile && !reloading) ShootProjectile();
            }
        }
        public string GetHoverText()
        {
            if (!Cannons.usingCannon)
            {
                if (!loadedProjectile) return (m_name + "\n[<color=yellow><b>$KEY_Use</b></color>] ") + "Cargar munición "; ;
                return ("\n[<color=yellow><b>$KEY_Use</b></color>] ") + "Usar " + m_name;
            };
            return "";
        }
        public string GetHoverName()
        {
            return m_name;
        }
        public bool Interact(Humanoid _player, bool hold, bool alt)
        {
            if (loadedProjectile)
            {
                SetActive(true);
                player = Player.m_localPlayer;
                player.AttachStart(playerAttach, null, true, false, true, "attach_chair", new Vector3(0, 0.5f, 0), null);
                if (player.IsAttached()) Jotunn.Logger.LogInfo(player.GetPlayerName() + " is using the cannon");
                player.GetCollider().enabled = false;
                barrelCollider.enabled = false;
                return true;
            }
            LoadProjectile();
            return true;

        }
        public bool UseItem(Humanoid player, ItemDrop.ItemData item)
        {
            return false;
        }
        private void SetActive(bool value)
        {
            Cannons.usingCannon = value;
            Cannons.aiming = value;
            if (!value)
            {
                player.GetCollider().enabled = true;
                if (player.IsAttached()) player.AttachStop();
                barrel.transform.rotation = originalBarrelRotation;
                barrelCollider.enabled = true;
                player = null;
            }
        }
        //private void FixedUpdate()
        //{
        //    if (player != null)
        //    {
        //        if (Input.GetKey(KeyCode.E))
        //        {
        //            var angle = Vector3.Angle(player.transform.forward, playerMountPoint.position - player.transform.position);
        //            if (angle <= 60)
        //            {
        //                // Fix the player's position to the mount point
        //                transform.position = player.transform.position + player.transform.forward;
        //                transform.rotation = player.transform.rotation;
        //                // Apply a force to the cannon in the direction the player is pushing
        //                Vector3 force = player.transform.forward * pushMultiplier;
        //                rigidbody.AddForce(force, ForceMode.Force);
        //            }
        //            if (currentProjectile != null && currentProjectile.GetComponent<Rigidbody>() == null)
        //            {
        //                // If the current projectile doesn't have a Rigidbody component, add one
        //                Rigidbody projectileRigidbody = currentProjectile.AddComponent<Rigidbody>();
        //                projectileRigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
        //            }
        //        }
        //    }
        //}

        //private void OnTriggerEnter(Collider other)
        //{
        //    // If a player enters the trigger, save their object reference and offset from the mount point
        //    Player _player = other.GetComponent<Player>();
        //    if (_player)
        //    {
        //        Jotunn.Logger.LogInfo("Player entered cannon area");
        //        player = _player;
        //    }
        //}

        //private void OnTriggerExit(Collider other)
        //{
        //    // If the player exits the trigger, reset the player object reference and offset
        //    Player _player = other.GetComponent<Player>();
        //    if (_player)
        //    {
        //        Jotunn.Logger.LogInfo("Player exited cannon area");
        //        player = null;
        //        if (Cannons.usingCannon) SetActive(false);
        //    }
        //}
        //private void DrawProjectileProjection()
        //{
        //    LineRenderer.enabled = true;
        //    LineRenderer.positionCount = Mathf.CeilToInt(projectionPoints / timeBetweenPoints) + 1;
        //    Vector3 startVelocity = projectileSpeed * barrel.transform.forward / projectileRigidbody.mass;
        //    int i = 0;
        //    LineRenderer.SetPosition(i, projectileSpawnPoint.position);
        //    for (float time = 0; time < projectionPoints; time += timeBetweenPoints)
        //    {
        //        i++;
        //        Vector3 point = projectileSpawnPoint.position + time * startVelocity;
        //        point.y = projectileSpawnPoint.position.y + startVelocity.y * time + (Physics.gravity.y / 2f * time * time);
        //        LineRenderer.SetPosition(i, point);
        //    }

        //}
        public void LoadProjectile()
        {
            if (!loadedProjectile)
            {
                // Spawn a new projectile at the spawn point
                string cannonballName = PrefabManager.Instance.GetPrefab("Cannonball").GetComponent<ItemDrop>().m_itemData.m_shared.m_name;
                bool hasCannonball = Player.m_localPlayer.GetInventory().HaveItem(cannonballName);
                if (!hasCannonball)
                {
                    Player.m_localPlayer.Message(MessageHud.MessageType.Center, "No tienes " + cannonballName);
                    return;
                }
                Player.m_localPlayer.GetInventory().RemoveItem(cannonballName, 1);
                Instantiate(Cannons.cannonLoadSfx, base.transform.position, base.transform.rotation);
                loadedProjectile = true;
                return;
            }
        }

        public void ShootProjectile()
        {
            currentProjectile = Instantiate(projectilePrefab, projectileSpawnPoint.position + projectileSpawnPoint.forward * 0.5f, projectileSpawnPoint.rotation);
            projectile = currentProjectile.GetComponent<Projectile>();
            projectile.m_damage = new HitData.DamageTypes { m_blunt = 130, m_chop = 50 };
            projectile.m_spawnOnHit = PrefabManager.Instance.GetPrefab("sledge_aoe");
            projectile.m_spawnOnHit.GetComponent<Aoe>().m_damage = new HitData.DamageTypes { m_blunt = 100 };
            projectile.m_canHitWater = false;
            currentProjectile.SetActive(false);
            HitData hitData = new HitData();
            hitData.m_pushForce = projectileSpeed;
            hitData.m_backstabBonus = arbalestItem.m_shared.m_backstabBonus;
            hitData.m_staggerMultiplier = 10;
            hitData.m_blockable = false;
            hitData.m_dodgeable = true;
            hitData.SetAttacker(player);
            currentProjectile.SetActive(true);
            Instantiate(Cannons.cannonFireSfx, projectileSpawnPoint.position, projectileSpawnPoint.rotation);
            currentProjectile.GetComponent<IProjectile>()?.Setup(player, currentProjectile.transform.forward * projectileSpeed, 50f, hitData, arbalestItem, blackmetalBolt);
            Destroy(currentProjectile, projectileLifeTime);
            arbalestItem.m_lastProjectile = currentProjectile;
            reloading = true;
            fireTimer = 0f;
            loadedProjectile = false;
            currentProjectile = null;
            return;

        }
    }

}
