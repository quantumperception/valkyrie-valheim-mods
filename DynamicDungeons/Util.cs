using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace DynamicDungeons
{
    public class Util
    {
        public static void GetMinMaxCoords(Vector3 v1, Vector3 v2, out Vector3 minCoords, out Vector3 maxCoords)
        {
            float minX = v1.x < v2.x ? v1.x : v2.x;
            float maxX = v1.x > v2.x ? v1.x : v2.x;
            float minY = v1.y < v2.y ? v1.y : v2.y;
            float maxY = v1.y > v2.y ? v1.y : v2.y;
            float minZ = v1.z < v2.z ? v1.z : v2.z;
            float maxZ = v1.z > v2.z ? v1.z : v2.z;
            minCoords = new Vector3(minX, minY, minZ);
            maxCoords = new Vector3(maxX, maxY, maxZ);
        }
        public static void SavePrefabList()
        {
            List<string> items = new List<string>();
            List<string> pieces = new List<string>();
            List<string> mobs = new List<string>();
            List<string> others = new List<string>();

            foreach (GameObject prefab in ZNetScene.instance.m_prefabs)
            {
                if (prefab.GetComponent<ItemDrop>() != null) { items.Add(prefab.name); continue; }
                if (prefab.GetComponent<Piece>() != null) { pieces.Add(prefab.name); continue; }
                if (prefab.GetComponent<MonsterAI>() != null) { mobs.Add(prefab.name); continue; }
                else others.Add(prefab.name);
            }
            if (!Directory.Exists(Path.Combine(DynamicDungeons.configPath, "listed_prefabs"))) Directory.CreateDirectory(Path.Combine(DynamicDungeons.configPath, "listed_prefabs"));
            File.WriteAllLines(Path.Combine(DynamicDungeons.configPath, "listed_prefabs", "items.txt"), items);
            File.WriteAllLines(Path.Combine(DynamicDungeons.configPath, "listed_prefabs", "pieces.txt"), pieces);
            File.WriteAllLines(Path.Combine(DynamicDungeons.configPath, "listed_prefabs", "mobs.txt"), mobs);
            File.WriteAllLines(Path.Combine(DynamicDungeons.configPath, "listed_prefabs", "others.txt"), others);
        }
        public static bool FindSpawnPoint(Vector3 origin, float radius, out Vector3 point)
        {
            for (int i = 0; i < 10; i++)
            {
                Vector3 vector = origin/* + Quaternion.Euler(0f, Random.Range(0, 360), 0f) * Vector3.forward * Random.Range(0f, radius)*/;
                if (ZoneSystem.instance.FindFloor(vector, out var height))
                {
                    vector.y = height + 0.1f;
                    point = vector;
                    return true;
                }
            }
            point = Vector3.zero;
            return false;
        }
        public static bool IsAdmin()
        {
            if (DynamicDungeons.IsServer) return true;
            return Jotunn.Managers.SynchronizationManager.Instance.PlayerIsAdmin;
        }
        public static void KillLocalPlayer()
        {
            Player.m_localPlayer.Damage(new HitData { m_damage = new HitData.DamageTypes { m_damage = 9999 } });
        }
        public static void Broadcast(string text, string username = "DynamicDungeons")
        {
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "ChatMessage", new Vector3(0f, 100f, 0f), 2, username, text);
        }
        public static void SendPlayerMessage(long uid, MessageHud.MessageType type, string msg)
        {
            ZRoutedRpc.instance.InvokeRoutedRPC(uid, "Message", type, msg); return;
        }
        public static void SendPlayerChatMessage(long uid, string msg, string username = "DynamicDungeons")
        {
            ZRoutedRpc.instance.InvokeRoutedRPC(uid, "ChatMessage", new Vector3(0f, 100f, 0f), 2, username, msg); return;
        }
        public static Material SetRenderTransparent(Material mat)
        {
            Material material = new Material(mat);
            material.SetInt("_SrcBlend", 1);
            material.SetInt("_DstBlend", 10);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHABLEND_ON");
            material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;
            return material;
        }

        public static Material SetRenderOpaque(Material mat)
        {
            Material material = new Material(mat);
            material.SetInt("_SrcBlend", 1);
            material.SetInt("_DstBlend", 0);
            material.SetInt("_ZWrite", 1);
            material.DisableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = -1;
            return material;
        }
        public static Vector3 GetRandomXZPointInRadius(Vector3 center, float radius)
        {
            float randomX = UnityEngine.Random.Range(center.x - radius, center.x + radius);
            float randomZ = UnityEngine.Random.Range(center.z - radius, center.z + radius);
            return new Vector3(randomX, center.y, randomZ);
        }
        public static Vector3 GetRandomPointInPlane(Vector3 topLeft, Vector3 topRight, Vector3 bottomLeft, Vector3 bottomRight)
        {
            // Calculate the normal vector of the plane
            Vector3 planeNormal = Vector3.Cross(topRight - topLeft, bottomLeft - topLeft).normalized;

            // Calculate the center point of the plane
            Vector3 centerPoint = (topLeft + topRight + bottomLeft + bottomRight) / 4f;

            // Calculate the size of the plane
            float planeWidth = Vector3.Distance(topLeft, topRight);
            float planeHeight = Vector3.Distance(topLeft, bottomLeft);

            // Generate a random point within the bounds of the plane
            Vector2 randomPoint2D = new Vector2(Random.Range(-planeWidth / 2f, planeWidth / 2f), Random.Range(-planeHeight / 2f, planeHeight / 2f));

            // Calculate the 3D position of the random point within the plane
            Vector3 randomPoint3D = centerPoint + (topLeft - centerPoint) + (bottomLeft - centerPoint) * randomPoint2D.y / planeHeight + (topRight - centerPoint) * randomPoint2D.x / planeWidth;

            // Project the random point onto the plane
            float distanceToPlane = Vector3.Dot(randomPoint3D - centerPoint, planeNormal);
            Vector3 randomPointOnPlane = randomPoint3D - distanceToPlane * planeNormal;

            return randomPointOnPlane;
        }
    }
}
