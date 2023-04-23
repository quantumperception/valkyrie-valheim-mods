using Jotunn.Managers;
using UnityEngine;
namespace Cannons
{
    class Cannonball : MonoBehaviour
    {
        public Collider collider = new SphereCollider
        {
            isTrigger = true,
            radius = 0.45f
        };
        private bool didHit = false;
        private void Start()
        {

        }
        private void FixedUpdate()
        {
            if (didHit)
            {
                GameObject slegdeAoe = Instantiate(PrefabManager.Instance.GetPrefab("sledge_aoe"));
                slegdeAoe.transform.rotation = new Quaternion { x = 0, y = 0, z = 90 };
                DestroyImmediate(base.gameObject);
                didHit = false;
            }
        }
        private void OnTriggerEnter(Collider other)
        {
            Jotunn.Logger.LogInfo("Cannonball hit: " + other.gameObject.name);
            if (!other.gameObject.name.Contains("WaterVolume")) didHit = true;
        }
    }
}
