using Jotunn.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DynamicDungeons
{
    public class ASPUtils
    {
        public abstract class BaseDungeonZdoQuery
        {
            protected List<Vector3> Corners { get; }

            protected List<Vector2i> ZoneIds { get; private set; }
            protected List<ZDO> Zdos { get; private set; }

            protected int MinX { get; private set; }
            protected int MaxX { get; private set; }
            protected int MinY { get; private set; }
            protected int MaxY { get; private set; }
            protected int MinZ { get; private set; }
            protected int MaxZ { get; private set; }

            private bool initialized;

            /// <summary>
            /// Prepares for querying zdo's within the zones 
            /// indicated by the center and range.
            /// Selects all ZDO's and ZoneId's using the square formed
            /// by the input, and caches them for subsequent queries.
            /// </summary>
            protected BaseDungeonZdoQuery(List<Vector3> corners)
            {
                Corners = corners;
                Initialize();
            }

            protected virtual void Initialize()
            {
                if (initialized) return;
                Util.GetMinMaxCoords(Corners[0], Corners[1], out Vector3 minCoords, out Vector3 maxCoords);
                (MinX, MaxX) = (Mathf.RoundToInt(minCoords.x), Mathf.RoundToInt(maxCoords.x));
                (MinY, MaxY) = (Mathf.RoundToInt(minCoords.y), Mathf.RoundToInt(maxCoords.y));
                (MinZ, MaxZ) = (Mathf.RoundToInt(minCoords.z), Mathf.RoundToInt(maxCoords.z));
                ZoneIds = ZoneUtils.GetZonesInSquare(MinX, MinZ, MaxX, MaxZ);
                Zdos = new List<ZDO>();

                foreach (var zone in ZoneIds) ReflectionHelper.InvokePrivate(ZDOMan.instance, "FindObjects", new object[] { zone, Zdos });
                initialized = true;
            }

            protected static (int min, int max) GetRange(int center, int range)
            {
                return (center - range, center + range);
            }

            protected bool IsWithinRange(ZDO zdo)
            {
                Vector3 zdoPosition = zdo.GetPosition();
                if (zdoPosition.x < MinX || zdoPosition.x > MaxX) return false;
                if (zdoPosition.z < MinZ || zdoPosition.z > MaxZ) return false;
                if (zdoPosition.y > MaxY || zdoPosition.y < MinY) return false;
                return true;
            }
        }
        public class DungeonZdoQuery : BaseDungeonZdoQuery
        {
            private Dictionary<int, int> CachedPrefabResults = new Dictionary<int, int>();

            public DungeonZdoQuery(List<Vector3> corners) : base(corners)
            {
            }

            public List<ZDO> GetZdosInDungeon(int prefabHash, Predicate<ZDO> condition = null)
            {
                List<ZDO> zdosInRange = new List<ZDO>();
                Util.GetMinMaxCoords(Corners[0], Corners[1], out Vector3 minCoords, out Vector3 maxCoords);
                if (condition is null) zdosInRange = Zdos.FindAll(z => IsWithinRange(z, prefabHash));
                else zdosInRange = Zdos.FindAll(zdo => IsWithinRange(zdo, prefabHash) && condition(zdo));
                return zdosInRange;
            }
            public int CountEntities(int prefabHash, Predicate<ZDO> condition = null)
            {
                Initialize();

                if (CachedPrefabResults.TryGetValue(prefabHash, out int cachedCount))
                {
                    return cachedCount;
                }

                int instances = 0;

                // Search zdo's with same prefab
                if (condition is null)
                {
                    instances = Zdos.Count(x =>
                        IsWithinRange(x, prefabHash));
                }
                else
                {
                    instances = Zdos.Count(x =>
                        IsWithinRange(x, prefabHash) &&
                        condition(x));
                }

                CachedPrefabResults[prefabHash] = instances;

                return instances;
            }

            public bool HasAny(int prefabHash)
            {
                Initialize();

                if (CachedPrefabResults.TryGetValue(prefabHash, out int cachedCount))
                {
                    return cachedCount > 0;
                }

                return Zdos.Any(x => IsWithinRange(x, prefabHash));
            }

            private bool IsWithinRange(ZDO zdo, int prefabId)
            {
                if (zdo.GetPrefab() != prefabId)
                {
                    return false;
                }

                return IsWithinRange(zdo);
            }
        }
        public static class ZoneUtils
        {
            public static List<Vector2i> GetZonesInSquare(int minX, int minZ, int maxX, int maxZ)
            {
                //IL_002f: Unknown result type (might be due to invalid IL or missing references)
                List<Vector2i> list = new List<Vector2i>();
                int num = Zonify(minX);
                int num2 = Zonify(maxX);
                int num3 = Zonify(minZ);
                int num4 = Zonify(maxZ);
                for (int i = num; i <= num2; i++)
                {
                    for (int j = num3; j <= num4; j++)
                    {
                        list.Add(new Vector2i(i, j));
                    }
                }
                return list;
            }

            public static Vector2i GetZone(Vector3 pos)
            {
                //IL_0006: Unknown result type (might be due to invalid IL or missing references)
                return ZoneSystem.instance.GetZone(pos);
            }

            public static Vector2i GetZone(int x, int z)
            {
                //IL_000c: Unknown result type (might be due to invalid IL or missing references)
                return new Vector2i(Zonify(x), Zonify(z));
            }

            public static int GetZoneIndex(Vector2i zone)
            {
                //IL_0005: Unknown result type (might be due to invalid IL or missing references)
                return ZDOMan.instance.SectorToIndex(zone);
            }

            public static int Zonify(int coordinate)
            {
                return Mathf.FloorToInt((float)(coordinate + 32) / 64f);
            }
        }
    }
}
