using System.Collections.Generic;
using UnityEngine;

namespace DynamicDungeons
{
    public class ASPUtils
    {
        public abstract class BaseZdoQuery
        {
            protected Vector3 Center { get; }
            protected int Range { get; }

            protected List<Vector2i> ZoneIds { get; private set; }
            protected List<ZDO> Zdos { get; private set; }

            protected int MinX { get; private set; }
            protected int MinZ { get; private set; }
            protected int MaxX { get; private set; }
            protected int MaxZ { get; private set; }

            private bool initialized;

            /// <summary>
            /// Prepares for querying zdo's within the zones 
            /// indicated by the center and range.
            /// Selects all ZDO's and ZoneId's using the square formed
            /// by the input, and caches them for subsequent queries.
            /// </summary>
            protected BaseZdoQuery(Vector3 center, int range)
            {
                Center = center;
                Range = range;

                Initialize();
            }

            protected virtual void Initialize()
            {
                if (initialized)
                {
                    return;
                }

                (MinX, MaxX) = GetRange((int)Center.x, Range);
                (MinZ, MaxZ) = GetRange((int)Center.z, Range);

                // Get zones to check
                ZoneIds = ZoneUtils.GetZonesInSquare(MinX, MinZ, MaxX, MaxZ);

                // Get zdo's
                Zdos = new List<ZDO>();

                foreach (var zone in ZoneIds)
                {
                    ZDOMan.instance.FindObjects(zone, Zdos);
                }

                initialized = true;
            }

            protected static (int min, int max) GetRange(int center, int range)
            {
                return (center - range, center + range);
            }

            protected bool IsWithinRange(ZDO zdo)
            {
                // Check if within manhattan distance.
                if (zdo.m_position.x < MinX || zdo.m_position.x > MaxX)
                {
                    return false;
                }

                if (zdo.m_position.z < MinZ || zdo.m_position.z > MaxZ)
                {
                    return false;
                }

                // Check if within circle distance
                return zdo.m_position.WithinHorizontalDistance(Center, Range);
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
