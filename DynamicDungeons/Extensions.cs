using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DynamicDungeons
{
    public static class StringHashExtensions
    {
        /// <summary>
        /// <para>Produces a stable hash.</para>
        /// <para>Based on: https://stackoverflow.com/a/36846609 </para>
        /// </summary>
        /// <remarks>Seems like Valheim uses the very same stackoverflow answer. This method is used to detach SpawnThat further from the Valheim dlls.</remarks>
        internal static int HashInteger(this string str)
        {
            unchecked
            {
                int hash1 = 5381;
                int hash2 = hash1;

                for (int i = 0; i < str.Length && str[i] != '\0'; i += 2)
                {
                    hash1 = ((hash1 << 5) + hash1) ^ str[i];
                    if (i == str.Length - 1 || str[i + 1] == '\0')
                    {
                        break;
                    }
                    hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
                }

                return hash1 + (hash2 * 1566083941);
            }
        }

        /// <summary>
        /// <para>Produces a stable hash.</para>
        /// <para>Based on: https://stackoverflow.com/a/36846609 </para>
        /// </summary>
        /// <remarks>Seems like Valheim uses the very same stackoverflow answer.</remarks>
        public static long Hash(this string str)
        {
            unchecked
            {
                long hash1 = 5381;
                long hash2 = hash1;

                for (int i = 0; i < str.Length && str[i] != '\0'; i += 2)
                {
                    hash1 = ((hash1 << 5) + hash1) ^ str[i];
                    if (i == str.Length - 1 || str[i + 1] == '\0')
                    {
                        break;
                    }
                    hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
                }

                return hash1 + (hash2 * 1566083941);
            }
        }

        /// <summary>
        /// <para>Produces a stable hash, based on sum of <see cref="Hash"/> of each string in input.</para>
        /// <para>Deduplicates entries, and orders based on each items hash before summing.</para>
        /// </summary>
        public static long Hash(this IEnumerable<string> strs)
        {
            long hash = 0;

            var orderedHashes = strs
                .Select(x => x.Hash())
                .Distinct()
                .OrderBy(x => x);

            foreach (var hashedLocation in orderedHashes)
            {
                unchecked
                {
                    hash += hashedLocation;
                }
            }

            return hash;
        }
    }
    public static class ZdoExtensions
    {
        private static int NoiseHash = "noise".HashInteger();
        private static int TamedHash = "tamed".HashInteger();
        private static int EventCreatureHash = "EventCreature".HashInteger();
        private static int HuntPlayerHash = "huntplayer".HashInteger();

        // Custom ZDO entries
        private static int FactionHash = "faction".HashInteger();

        public static float GetNoise(this ZDO zdo)
        {
            return zdo.GetFloat(NoiseHash);
        }

        public static bool GetTamed(this ZDO zdo)
        {
            return zdo.GetBool(TamedHash);
        }

        public static bool GetEventCreature(this ZDO zdo)
        {
            return zdo.GetBool(EventCreatureHash);
        }

        /// <summary>
        /// Gets "faction" from zdo.
        /// </summary>
        /// <remarks>Spawn That setting.</remarks>
        public static Character.Faction? GetFaction(this ZDO zdo)
        {
            var faction = zdo.GetInt(FactionHash, -1);

            if (faction < 0)
            {
                return null;
            }

            return (Character.Faction)faction;
        }

        /// <summary>
        /// Sets "faction" in zdo.
        /// </summary>
        /// <remarks>Spawn That setting.</remarks>
        public static void SetFaction(this ZDO zdo, Character.Faction faction)
        {
            zdo.Set(FactionHash, (int)faction);
        }

        public static bool GetHuntPlayer(this ZDO zdo)
        {
            return zdo.GetBool(HuntPlayerHash);
        }

        public static void SetHuntPlayer(this ZDO zdo, bool huntPlayer)
        {
            zdo.Set(HuntPlayerHash, huntPlayer);
        }
    }
    public static class Vector3Extensions
    {
        public static bool WithinSquare(this Vector3 position, int centerX, int centerZ, int size = 10)
        {
            float posX = position.x;
            float posZ = position.z;

            if (posX < (centerX - size) || posX > (centerX + size))
            {
                return false;
            }

            if (posZ < (centerZ - size) || posZ > (centerZ + size))
            {
                return false;
            }

            return true;
        }

        public static Vector2i GetZoneId(this Vector3 position)
        {
            return ASPUtils.ZoneUtils.GetZone((int)position.x, (int)position.z);
        }

        public static float DistanceHorizontal(this Vector3 source, Vector3 destination)
        {
            float dx = source.x - destination.x;
            float dz = source.z - destination.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        public static bool WithinHorizontalDistance(this Vector3 pos1, Vector3 pos2, float distance)
        {
            float x = pos1.x - pos2.x;
            float z = pos1.z - pos2.z;
            return x * x + z * z < distance * distance;
        }
    }
}
