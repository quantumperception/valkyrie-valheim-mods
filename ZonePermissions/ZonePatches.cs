using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace ZonePermissions
{
    class ZonePatches
    {
        /// <summary>
        /// FixedUpdate<> Patch
        /// Includes the following:
        ///     PushAway (Do not enter zone)
        ///     PeriodicDamage
        ///     PeriodicHeal
        ///     Last Updated: 4/24/2021
        ///     Status: 100% Working
        /// </summary>
        public static void FixedUpdate()
        {
            if (ZonePermissions.EffectTick > 0) ZonePermissions.EffectTick--;
            if (ZonePermissions.HealTick > 0) ZonePermissions.HealTick--;
            if (ZonePermissions.DamageTick > 0) ZonePermissions.DamageTick--;
            Player p = Player.m_localPlayer;
            if (p != null)
            {
                // Are we in a zone? if so select that zone.
                ZoneHandler.Zone z = new ZoneHandler.Zone();
                ZoneHandler.ZoneTypes zt = new ZoneHandler.ZoneTypes();
                List<ZoneHandler.Zone> zlist = ZoneHandler.ListOccupiedZones(p.transform.position);
                if (zlist.Count == 0)
                {
                    zt = ZoneHandler.FindZoneType("wilderness");
                }
                else
                {
                    z = ZoneHandler.TopZone(zlist);
                    zt = ZoneHandler.FindZoneType(z.Type);

                }
                // Lets set our strings...
                string admins = zt.Admins;
                string key = zt.Configurations;
                // Lets see if the user is considered a Zone Admin!
                if (admins.Contains(ZonePermissions.MySteamID))
                {
                    return;
                }
                // Keep player out of an area.
                if (key.ToLower().Contains("pushaway"))
                {
                    Vector3 zposition = new Vector3(z.Position.x, p.transform.position.y, z.Position.y);
                    Vector3 newVector3 = p.transform.position +
                                         (p.transform.position - zposition).normalized * 0.15f;
                    p.transform.position = new Vector3(newVector3.x, p.transform.position.y, newVector3.z);
                }
                // Deal damage to the player while they are in the area.
                if (key.ToLower().Contains("periodicdamage") && ZonePermissions.DamageTick <= 0)
                {
                    ZonePermissions.DamageTick = 100;
                    string s = key.ToLower();
                    int indexStart = s.IndexOf("periodicdamage(") + "periodicdamage(".Length;
                    string test = "";
                    for (int i = indexStart; i < indexStart + 30; i++)
                    {
                        if (s[i] == ')') break;
                        test += s[i];
                    }
                    int damage = 0;
                    HitData hit = new HitData();
                    string[] array = test.Split(',');
                    if (array.Count() == 2)
                    {
                        int.TryParse(array[1], out damage);
                        if (array[0].ToLower() == "fire")
                        {
                            hit.m_damage.m_fire = (float)damage;
                        }
                        else if (array[0].ToLower() == "frost")
                        {
                            hit.m_damage.m_frost = (float)damage;
                        }
                        else if (array[0].ToLower() == "poison")
                        {
                            hit.m_damage.m_poison = (float)damage;
                        }
                        else if (array[0].ToLower() == "lightning")
                        {
                            hit.m_damage.m_lightning = (float)damage;
                        }
                        else if (array[0].ToLower() == "pierce")
                        {
                            hit.m_damage.m_pierce = (float)damage;
                        }
                        else if (array[0].ToLower() == "blunt")
                        {
                            hit.m_damage.m_blunt = (float)damage;
                        }
                        else if (array[0].ToLower() == "slash")
                        {
                            hit.m_damage.m_slash = (float)damage;
                        }
                        else if (array[0].ToLower() == "damage")
                        {
                            hit.m_damage.m_damage = (float)damage;
                        }
                        else
                        {
                            hit.m_damage.m_fire = (float)damage;
                        }
                    }
                    else
                    {
                        int.TryParse(test, out damage);
                        hit.m_damage.m_fire = (float)damage;
                    }
                    p.Damage(hit);
                }
                // Heal the player while they are in the area
                if (key.ToLower().Contains("periodicheal") && ZonePermissions.HealTick <= 0)
                {
                    ZonePermissions.HealTick = 50;
                    string s = key.ToLower();
                    int indexStart = s.IndexOf("periodicheal(") + "periodicheal(".Length;
                    string test = "";
                    for (int i = indexStart; i < indexStart + 20; i++)
                    {
                        if (s[i] == ')') break;
                        test += s[i];
                    }
                    int damage = 0;
                    int.TryParse(test, out damage);
                    p.Heal(damage, true);
                }
            }
        }
        /// <summary>
        /// Harmony Patch Type: Player Method: OnDeath
        /// Includes the following:
        /// Checks the player against NoItemLoss.
        /// If they have a "Restriction" to it prevent there death.
        /// Also configurable Global option server side..
        ///     Last Updated: 4/29/2021
        ///     Status: 100% Working
        /// </summary>
        /// 
        [HarmonyPatch(typeof(Player), "OnDeath")]
        public static class Death_Patch
        {
            private static bool Prefix(Player __instance)
            {
                if (Util.RestrictionCheck("noitemloss") || Client.NoItemLoss)
                {
                    Type player = __instance.GetType();
                    bool hardDeath = (bool)player.GetMethod("HardDeath", Util.NonPublicFlags).Invoke(__instance, new object[] { });
                    ZNetView m_nview = (ZNetView)player.GetField("m_nview", Util.NonPublicFlags).GetValue(__instance);
                    HitData m_lastHit = (HitData)player.GetField("m_lastHit", Util.NonPublicFlags).GetValue(__instance);
                    List<Player.Food> m_foods = (List<Player.Food>)player.GetField("m_foods", Util.NonPublicFlags).GetValue(__instance);
                    Skills m_skills = (Skills)player.GetField("m_skills", Util.NonPublicFlags).GetValue(__instance);
                    SEMan m_seman = (SEMan)player.GetField("m_seman", Util.NonPublicFlags).GetValue(__instance);
                    m_nview.GetZDO().Set(ZDOVars.s_dead, value: true);
                    Game.instance.IncrementPlayerStat(PlayerStatType.Deaths);
                    switch (m_lastHit.m_hitType)
                    {
                        case HitData.HitType.Undefined:
                            Game.instance.IncrementPlayerStat(PlayerStatType.DeathByUndefined);
                            break;
                        case HitData.HitType.EnemyHit:
                            Game.instance.IncrementPlayerStat(PlayerStatType.DeathByEnemyHit);
                            break;
                        case HitData.HitType.PlayerHit:
                            Game.instance.IncrementPlayerStat(PlayerStatType.DeathByPlayerHit);
                            break;
                        case HitData.HitType.Fall:
                            Game.instance.IncrementPlayerStat(PlayerStatType.DeathByFall);
                            break;
                        case HitData.HitType.Drowning:
                            Game.instance.IncrementPlayerStat(PlayerStatType.DeathByDrowning);
                            break;
                        case HitData.HitType.Burning:
                            Game.instance.IncrementPlayerStat(PlayerStatType.DeathByBurning);
                            break;
                        case HitData.HitType.Freezing:
                            Game.instance.IncrementPlayerStat(PlayerStatType.DeathByFreezing);
                            break;
                        case HitData.HitType.Poisoned:
                            Game.instance.IncrementPlayerStat(PlayerStatType.DeathByPoisoned);
                            break;
                        case HitData.HitType.Water:
                            Game.instance.IncrementPlayerStat(PlayerStatType.DeathByWater);
                            break;
                        case HitData.HitType.Smoke:
                            Game.instance.IncrementPlayerStat(PlayerStatType.DeathBySmoke);
                            break;
                        case HitData.HitType.EdgeOfWorld:
                            Game.instance.IncrementPlayerStat(PlayerStatType.DeathByEdgeOfWorld);
                            break;
                        case HitData.HitType.Impact:
                            Game.instance.IncrementPlayerStat(PlayerStatType.DeathByImpact);
                            break;
                        case HitData.HitType.Cart:
                            Game.instance.IncrementPlayerStat(PlayerStatType.DeathByCart);
                            break;
                        case HitData.HitType.Tree:
                            Game.instance.IncrementPlayerStat(PlayerStatType.DeathByTree);
                            break;
                        case HitData.HitType.Self:
                            Game.instance.IncrementPlayerStat(PlayerStatType.DeathBySelf);
                            break;
                        case HitData.HitType.Structural:
                            Game.instance.IncrementPlayerStat(PlayerStatType.DeathByStructural);
                            break;
                        case HitData.HitType.Turret:
                            Game.instance.IncrementPlayerStat(PlayerStatType.DeathByTurret);
                            break;
                        case HitData.HitType.Boat:
                            Game.instance.IncrementPlayerStat(PlayerStatType.DeathByBoat);
                            break;
                        case HitData.HitType.Stalagtite:
                            Game.instance.IncrementPlayerStat(PlayerStatType.DeathByStalagtite);
                            break;
                        default:
                            ZLog.LogWarning("Not implemented death type " + m_lastHit.m_hitType);
                            break;
                    }
                    Game.instance.GetPlayerProfile().SetDeathPoint(__instance.transform.position);
                    player.GetMethod("CreateDeathEffects", Util.NonPublicFlags).Invoke(__instance, new object[] { });
                    m_foods.Clear();
                    if (ZoneSystem.instance.GetGlobalKey(GlobalKeys.DeathSkillsReset))
                    {
                        m_skills.Clear();
                    }
                    else if (hardDeath)
                    {
                        m_skills.OnDeath();
                    }
                    m_seman.RemoveAllStatusEffects();
                    Game.instance.RequestRespawn(10f, afterDeath: true);
                    player.GetField("m_timeSinceDeath", Util.NonPublicFlags).SetValue(__instance, 0f);
                    if (!hardDeath)
                    {
                        Util.ShowMessage((int)MessageHud.MessageType.TopLeft, "$msg_softdeath");
                    }
                    __instance.Message(MessageHud.MessageType.Center, "$msg_youdied");
                    Minimap.instance.AddPin(__instance.transform.position, Minimap.PinType.Death, $"$hud_mapday {EnvMan.instance.GetDay(ZNet.instance.GetTimeSeconds())}", save: true, isChecked: false, 0L);
                    if (__instance.m_onDeath != null)
                    {
                        __instance.m_onDeath();
                    }
                    string eventLabel = "biome:" + __instance.GetCurrentBiome();
                    Gogan.LogEvent("Game", "Death", eventLabel, 0L);
                    return false;
                }
                else
                {
                    ZoneHandler.CurrentZoneID = -2;
                    return true;
                }
            }
        }

        /// <summary>
        /// This is a list of our "Zone Configurations"
        /// </summary>

        /// <summary>
        /// Harmony Patch Type: Attack Method: SpawnOnHitTerrain
        /// Includes the following:
        /// if the zone includes "NoPickaxe" prevent the use of Pickaxes in this area.
        /// If the zone includes "NoTerrain" prevent the use of PickAxes in this area.
        ///     Last Updated: 4/24/2021
        ///     Status: 100% Working
        /// </summary>
        [HarmonyPatch(typeof(Attack), "SpawnOnHitTerrain")]
        public static class Attack_Patch
        {
            private static bool Prefix(Vector3 hitPoint)
            {
                bool isInArea = false;
                if (Util.isServer()) return true;
                if (Util.RestrictionCheck("nopickaxe") || Util.RestrictionCheck("noterrain"))
                {
                    isInArea = true;
                    Util.DoPrivateAreaEffect(hitPoint + Player.m_localPlayer.transform.forward * 1f);
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, "This is a Private Area", 0, null);
                }
                return !isInArea;
            }
        }
        /// <summary>
        /// Harmony Patch Type: TerrainOp Method: Awake
        /// Includes the following:
        /// if the zone includes "NoTerrain" prevent modifying the terrain in the area.
        ///     Last Updated: 4/26/2021
        ///     Status: 100% Working
        /// </summary>
        [HarmonyPatch(typeof(TerrainOp), "Awake")]
        public static class TerrainComp_Patch
        {
            private static bool Prefix(TerrainOp __instance)
            {
                if (Player.m_localPlayer)
                {
                    if (Util.RestrictionCheck("noterrain"))
                    {
                        Util.DoPrivateAreaEffect(__instance.transform.position);
                        MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, "This is a Private Area", 0, null);
                        return false;
                    }
                }
                else
                {
                    /*
                    if (Util.RestrictionCheckTerrain(__instance, "noterrain"))
                    {
                        Util.DoAreaEffect(__instance.transform.position);
                        MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, "This is a Private Area", 0, null);
                        return false;
                    }
                    */
                }
                return true;
            }
        }


        /// <summary>
        /// Harmony Patch Type: Container Method: Interact
        /// Includes the following:
        /// if the zone includes "NoChest" prevent accessing Chests in this area.
        ///     Last Updated: 4/24/2021
        ///     Status: 100% Working
        /// </summary>
        [HarmonyPatch(typeof(Container), "Interact")]
        public static class Container_Patch
        {
            private static bool Prefix(Container __instance)
            {
                bool isInArea = false;
                if (Util.isServer()) return true;
                if (Util.RestrictionCheck("nochest"))
                {
                    isInArea = true;
                    Util.DoPrivateAreaEffect(__instance.transform.position);
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, "This is a Private Area", 0, null);
                }
                return !isInArea;
            }
        }

        /// <summary>
        /// Harmony Patch Type: Door Method: Interact
        /// Includes the following:
        /// if the zone includes "NoDoors" prevent the use of Doors in this area.
        ///     Last Updated: 4/24/2021
        ///     Status: 100% Working
        /// </summary>
        [HarmonyPatch(typeof(Door), "Interact")]
        public static class Door_Patch
        {
            private static bool Prefix(Door __instance)
            {
                if (Util.isServer()) return true;
                bool isInArea = false;
                if (Util.RestrictionCheck("nodoors"))
                {
                    isInArea = true;
                    Util.DoPrivateAreaEffect(__instance.transform.position);
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, "This is a Private Area", 0, null);
                }
                return !isInArea;
            }
        }

        [HarmonyPatch(typeof(Sign), "Interact")]
        public static class Sign_Patch
        {
            private static bool Prefix(Door __instance)
            {
                if (Util.isServer()) return true;
                bool isInArea = false;
                if (Util.RestrictionCheck("nosigns"))
                {
                    isInArea = true;
                    Util.DoPrivateAreaEffect(__instance.transform.position);
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, "This is a Private Area", 0, null);
                }
                return !isInArea;
            }
        }

        /// <summary>
        /// Harmony Patch Type: Player Method: PlacePiece
        /// Includes the following:
        /// if the zone includes "NoBuilding" prevent building (with hammer) in this area.
        ///     Last Updated: 4/24/2021
        ///     Status: 100% Working
        /// </summary>
        [HarmonyPatch(typeof(Player), "PlacePiece")]
        public static class NoBuild_Patch
        {
            private static bool Prefix(Player __instance)
            {
                if (Util.isServer()) return true;
                bool isInArea = false;
                if (Util.RestrictionCheck("nobuilding"))
                {
                    isInArea = true;
                    Type player = __instance.GetType();
                    GameObject placementGhost = (GameObject)player.GetField("m_placementGhost", Util.NonPublicFlags).GetValue(__instance);
                    Util.DoPrivateAreaEffect(placementGhost.transform.position);
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, "This is a Private Area", 0, null);
                }
                return !isInArea;
            }
        }

        /// <summary>
        /// Harmony Patch Type: Player Method: RemovePiece
        /// Includes the following:
        /// if the zone includes "NoBuilding" prevent destroying buildings (with hammer) in this area.
        ///     Last Updated: 4/24/2021
        ///     Status: 100% Working
        /// </summary>
        [HarmonyPatch(typeof(Player), "RemovePiece")]
        public static class NoBuild_Patch2
        {
            private static bool Prefix(Player __instance)
            {
                if (Util.isServer()) return true;
                bool isInArea = false;
                if (Util.RestrictionCheck("nobuilding"))
                {
                    isInArea = true;
                    Util.DoPrivateAreaEffect(__instance.transform.position);
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, "This is a Private Area", 0, null);

                }
                return !isInArea;
            }
        }

        /// <summary>
        /// Harmony Patch Type: WearNTear Method: RPC_Damage
        /// Includes the following:
        /// If the area is in a Ward Do the following checks:
        ///     Is the attacker a player?
        ///         Check player against the ward for permission.
        ///     Is the attacker a non player?
        ///         Deny permissions to the area.
        /// if the zone includes "NoBuildDamage" prevent damaging buildings in this area.
        ///     Last Updated: 4/24/2021
        ///     Status: 100% Working
        /// </summary>

        /// <summary>
        /// Harmony Patch Type: WearNTear Method: Damage
        /// Includes the following:
        /// If the area is in a Ward Do the following checks:
        ///     Is the attacker a player?
        ///         Check player against the ward for permission.
        ///     Is the attacker a non player?
        ///         Deny permissions to the area.
        /// if the zone includes "NoBuildDamage" prevent damaging buildings in this area.
        ///     Last Updated: 4/24/2021
        ///     Status: 100% Working
        /// </summary>
        [HarmonyPatch(typeof(WearNTear), "Damage")]
        public static class Building_Wear_N_Tear_Patch
        {
            private static bool Prefix(WearNTear __instance, HitData hit)
            {
                if (Util.isServer()) return true;

                bool isWardedArea = false;
                bool isInArea = false;

                // Is the area we are searching in a Warded area.
                Debug.Log("pre allPrivateAreas");
                List<PrivateArea> allPrivateAreas = (List<PrivateArea>)typeof(PrivateArea).GetField("m_allAreas", BindingFlags.NonPublic | BindingFlags.Static).GetValue(new PrivateArea());
                Debug.Log("post allPrivateAreas");
                foreach (PrivateArea privateArea in allPrivateAreas)
                {
                    Debug.Log("privateArea");
                    Type pa = privateArea.GetType();
                    Debug.Log("privateArea type");
                    bool isEnabled = (bool)pa.GetMethod("IsEnabled", Util.NonPublicFlags).Invoke(privateArea, new object[] { });
                    Debug.Log("privateArea reflection 1");
                    bool isInside = (bool)pa.GetMethod("IsInside", Util.NonPublicFlags).Invoke(privateArea, new object[] { __instance.transform.position, 0 });
                    Debug.Log("privateArea reflection 2");
                    if (isEnabled && isInside)
                    {
                        isWardedArea = true;
                        break;
                    }
                }
                Debug.Log("wnt check iswarded");
                if (Client.Ward.Damage && isWardedArea)
                {
                    ZDOID attacker = hit.m_attacker;
                    bool isplayer = false;
                    List<Character> list = new List<Character>();
                    foreach (var character in Character.GetAllCharacters())
                    {
                        if (character.GetZDOID() == attacker)
                        {
                            if (character.GetComponent<Player>())
                            {
                                isplayer = true;
                            }
                        }
                    }
                    // It's a player so lets see if it has access.
                    if (isplayer)
                    {
                        if (!PrivateArea.CheckAccess(Player.m_localPlayer.transform.position, 0f, true, false))
                        {
                            Util.DoWardedAreaEffect(__instance.transform.position);
                            MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, "This is a Warded Area", 0, null);
                            isInArea = true;
                            return !isInArea;
                        }
                    }
                    else
                    {
                        // It's not a player so lets send out a Ward notification and block the damage.
                        PrivateArea.CheckAccess(__instance.transform.position);
                        isInArea = true;
                        return !isInArea;
                    }
                }
                Debug.Log("wnt check nobuilddamage");
                // Is the user restricted by NoBuildDamage?
                if (Util.RestrictionCheck("nobuilddamage"))
                {
                    isInArea = true;
                    Util.DoPrivateAreaEffect(__instance.transform.position + Vector3.up * 0.5f);
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, "This is a Private Area", 0, null);
                }
                return !isInArea;
            }
        }
        /// <summary>
        /// Harmony Patch Type: ItemDrop Method: Interact
        /// Includes the following:
        /// if the zone includes "NoItemPickup" prevent picking up items in this area.
        /// If the area is in a ward prevent picking up items in this area.
        ///     Last Updated: 4/24/2021
        ///     Status: 100% Working
        /// </summary>
        [HarmonyPatch(typeof(ItemDrop), "Interact")]
        public static class NoDropInteraction_Patch
        {
            private static bool Prefix(ItemDrop __instance)
            {
                if (Util.isServer()) return true;

                bool isInArea = false;
                // Ward Check
                if (Client.Ward.Drop && PrivateArea.CheckAccess(Player.m_localPlayer.transform.position))
                {
                    if (!PrivateArea.CheckAccess(Player.m_localPlayer.transform.position, 0f, true, false))
                    {
                        Util.DoWardedAreaEffect(__instance.transform.position);
                        MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, "This is a Warded Area", 0, null);
                        isInArea = true;
                        return !isInArea;
                    }
                }
                if (Util.RestrictionCheck("noitempickup"))
                {
                    isInArea = true;
                    Util.DoPrivateAreaEffect(__instance.transform.position + Vector3.up * 0.5f);
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, "This is a Private Area", 0, null);
                }
                return !isInArea;
            }
        }

        /// <summary>
        /// Harmony Patch Type: Player Method: AutoPickup
        /// Includes the following:
        /// if the zone includes "NoItemPickup" prevent auto picking up items in this area.
        /// If the area is in a ward prevent picking up items in this area.
        ///     Last Updated: 4/24/2021
        ///     Status: 100% Working
        /// </summary>
        [HarmonyPatch(typeof(Player), "AutoPickup")]
        public static class NoAutoPickup_Patch
        {
            private static bool Prefix(Player __instance)
            {
                if (Util.isServer()) return true;

                bool isInArea = false;
                Vector3 point = __instance.transform.position;
                // Ward Check
                if (Client.Ward.Pickup && PrivateArea.CheckAccess(Player.m_localPlayer.transform.position))
                {
                    if (!PrivateArea.CheckAccess(Player.m_localPlayer.transform.position, 0f, false, false))
                    {
                        isInArea = true;
                        return !isInArea;
                    }
                }
                if (Util.RestrictionCheck("noitempickup"))
                {
                    isInArea = true;
                }
                return !isInArea;
            }
        }
        /// <summary>
        /// HarmonyPatch Type: InventoryGrid Method: OnLeftClick
        /// This method includes:
        ///     If in a warded area do not drop items.
        ///     If in a zone with "NoItemDrop" configuration do not drop items.
        ///     Last Updated: 4/24/2021
        ///     Status: 100% Working
        /// </summary>

        [HarmonyPatch(typeof(InventoryGrid), "OnLeftClick")]
        public static class No_Inventory_Ctrl_Left_Click
        {
            private static bool Prefix(InventoryGrid __instance)
            {
                if (Util.isServer()) return true;

                bool isInArea = false;
                // Ward Check
                if (Player.m_localPlayer)
                {
                    if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                    {
                        if (Client.Ward.Drop && PrivateArea.CheckAccess(Player.m_localPlayer.transform.position))
                        {
                            if (!PrivateArea.CheckAccess(Player.m_localPlayer.transform.position, 0f, true, false))
                            {
                                Util.DoWardedAreaEffect(Player.m_localPlayer.transform.position);
                                MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, "This is a Warded Area", 0, null);
                                isInArea = true;
                                return !isInArea;
                            }
                        }
                        if (Util.RestrictionCheck("noitemdrop"))
                        {
                            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                            {
                                isInArea = true;
                                Util.DoPrivateAreaEffect(Player.m_localPlayer.transform.position + Vector3.up * 0.5f);
                                MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, "This is a Private Area", 0, null);
                            }
                        }
                    }
                }
                return !isInArea;
            }
        }

        /// <summary>
        /// HarmonyPatch Type: InventoryGui Method: OnDropOutside
        /// This method includes:
        ///     If in a warded area do not drop items.
        ///     If in a zone with "NoItemDrop" configuration do not drop items.
        ///     Last Updated: 4/24/2021
        ///     Status: 100% Working
        /// </summary>
        [HarmonyPatch(typeof(InventoryGui), "OnDropOutside")]
        public static class InventoryGui_No_Drop_Patch
        {
            private static bool Prefix(InventoryGui __instance)
            {
                if (Util.isServer()) return true;

                bool isInArea = false;
                // Ward Check
                if (Client.Ward.Drop && PrivateArea.CheckAccess(Player.m_localPlayer.transform.position))
                {
                    if (!PrivateArea.CheckAccess(Player.m_localPlayer.transform.position, 0f, true, false))
                    {
                        bool test = PrivateArea.CheckAccess(Player.m_localPlayer.transform.position);
                        long PlayerID = Player.m_localPlayer.GetPlayerID();
                        Util.DoWardedAreaEffect(Player.m_localPlayer.transform.position);
                        MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, "This is a Warded Area", 0, null);
                        isInArea = true;
                        return !isInArea;
                    }
                }
                if (Util.RestrictionCheck("noitemdrop"))
                {
                    isInArea = true;
                    Util.DoPrivateAreaEffect(Player.m_localPlayer.transform.position + Vector3.up * 0.5f);
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, "This is a Private Area", 0, null);
                }
                return !isInArea;
            }
        }
        /// <summary>
        /// HarmonyPatch Type: Character Method: Damage
        /// This method includes:
        ///     If in a warded area do not drop items.
        ///     If attacking a Monster
        ///         If damagemultipliertomobs is enabled do (X)% damage to mobs
        ///     if attacking a Player
        ///         If damagemultipliertoplayers is enabled do (X)% damage to players
        ///     Last Updated: 4/24/2021
        ///     Status: 100% Working
        /// </summary>
        [HarmonyPatch(typeof(Character), "Damage")]
        public static class Damage_Modifier
        {
            public static void Prefix(Character __instance, HitData hit, ZNetView ___m_nview)
            {
                if (__instance.m_faction != Character.Faction.Players)
                {
#if DEBUG
                    Debug.Log($"A monster is being attacked!");
#endif
                    if (Util.RestrictionCheckNone(__instance, "damagemultipliertomobs") && (__instance.m_faction != Character.Faction.Players))
                    {
                        float multiplier = Util.RestrictionCheckFloatReturnNone(__instance, "damagemultipliertomobs");
#if DEBUG
                        Debug.Log($"Multiplier: {multiplier}");
#endif
                        hit.m_damage.m_damage *= multiplier;
                        hit.m_damage.m_blunt *= multiplier;
                        hit.m_damage.m_slash *= multiplier;
                        hit.m_damage.m_pierce *= multiplier;
                        hit.m_damage.m_chop *= multiplier;
                        hit.m_damage.m_pickaxe *= multiplier;
                        hit.m_damage.m_fire *= multiplier;
                        hit.m_damage.m_frost *= multiplier;
                        hit.m_damage.m_lightning *= multiplier;
                        hit.m_damage.m_poison *= multiplier;
                        hit.m_damage.m_spirit *= multiplier;
                        return;
                    }
                }
                else if (__instance.m_faction == Character.Faction.Players)
                {
#if DEBUG
                    Debug.Log("A player is being Attacked!");
#endif
                    if (Util.RestrictionCheckNone(__instance, "damagemultipliertoplayers"))
                    {
                        float multiplier = Util.RestrictionCheckFloatReturnNone(__instance, "damagemultipliertoplayers");
#if DEBUG
                        Debug.Log($"Mutliplier: {multiplier}");
#endif
                        hit.m_damage.m_damage *= multiplier;
                        hit.m_damage.m_blunt *= multiplier;
                        hit.m_damage.m_slash *= multiplier;
                        hit.m_damage.m_pierce *= multiplier;
                        hit.m_damage.m_chop *= multiplier;
                        hit.m_damage.m_pickaxe *= multiplier;
                        hit.m_damage.m_fire *= multiplier;
                        hit.m_damage.m_frost *= multiplier;
                        hit.m_damage.m_lightning *= multiplier;
                        hit.m_damage.m_poison *= multiplier;
                        hit.m_damage.m_spirit *= multiplier;
                        return;
                    }
                }

            }
        }
        /// <summary>
        /// HarmonyPatch Type: TreeBase Method: Damage
        /// This method includes:
        ///     If in a zone with "damagemultipliertotrees" configuration do (X) percent damage to trees.
        ///     Last Updated: 4/24/2021
        ///     Status: 100% Working
        /// </summary>
        [HarmonyPatch(typeof(Destructible), "Damage")]
        public static class Destructible_Modifier
        {
            public static void Prefix(Destructible __instance, HitData hit)
            {
                if (Util.isServer()) return;

                if (Util.RestrictionCheck("nodamagetotrees"))
                {
                    float multiplier = 0f;
                    hit.m_damage.m_damage *= multiplier;
                    hit.m_damage.m_blunt *= multiplier;
                    hit.m_damage.m_slash *= multiplier;
                    hit.m_damage.m_pierce *= multiplier;
                    hit.m_damage.m_chop *= multiplier;
                    hit.m_damage.m_pickaxe *= multiplier;
                    hit.m_damage.m_fire *= multiplier;
                    hit.m_damage.m_frost *= multiplier;
                    hit.m_damage.m_lightning *= multiplier;
                    hit.m_damage.m_poison *= multiplier;
                    hit.m_damage.m_spirit *= multiplier;
                    Util.DoPrivateAreaEffect(Player.m_localPlayer.transform.position + Vector3.up * 0.5f);
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, "This is a Private Area", 0, null);
                }
                else if (Util.RestrictionCheck("damagemultipliertotrees"))
                {
                    float multiplier = Util.RestrictionCheckFloatReturn("damagemultipliertotrees");
                    hit.m_damage.m_damage *= multiplier;
                    hit.m_damage.m_blunt *= multiplier;
                    hit.m_damage.m_slash *= multiplier;
                    hit.m_damage.m_pierce *= multiplier;
                    hit.m_damage.m_chop *= multiplier;
                    hit.m_damage.m_pickaxe *= multiplier;
                    hit.m_damage.m_fire *= multiplier;
                    hit.m_damage.m_frost *= multiplier;
                    hit.m_damage.m_lightning *= multiplier;
                    hit.m_damage.m_poison *= multiplier;
                    hit.m_damage.m_spirit *= multiplier;
                }
            }
        }

        /// <summary>
        /// HarmonyPatch Type: TreeBase Method: Damage
        /// This method includes:
        ///     If in a zone with "damagemultipliertotrees" configuration do (X) percent damage to trees.
        ///     Last Updated: 4/24/2021
        ///     Status: 100% Working
        /// </summary>
        [HarmonyPatch(typeof(TreeBase), "Damage")]
        public static class TreeBase_Modifier
        {
            public static void Prefix(TreeBase __instance, HitData hit)
            {
                if (Util.isServer()) return;

                if (Util.RestrictionCheck("nodamagetotrees"))
                {
                    float multiplier = 0f;
                    hit.m_damage.m_damage *= multiplier;
                    hit.m_damage.m_blunt *= multiplier;
                    hit.m_damage.m_slash *= multiplier;
                    hit.m_damage.m_pierce *= multiplier;
                    hit.m_damage.m_chop *= multiplier;
                    hit.m_damage.m_pickaxe *= multiplier;
                    hit.m_damage.m_fire *= multiplier;
                    hit.m_damage.m_frost *= multiplier;
                    hit.m_damage.m_lightning *= multiplier;
                    hit.m_damage.m_poison *= multiplier;
                    hit.m_damage.m_spirit *= multiplier;
                    Util.DoPrivateAreaEffect(Player.m_localPlayer.transform.position + Vector3.up * 0.5f);
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, "This is a Private Area", 0, null);
                }
                else if (Util.RestrictionCheck("damagemultipliertotrees"))
                {
                    float multiplier = Util.RestrictionCheckFloatReturn("damagemultipliertotrees");
                    hit.m_damage.m_damage *= multiplier;
                    hit.m_damage.m_blunt *= multiplier;
                    hit.m_damage.m_slash *= multiplier;
                    hit.m_damage.m_pierce *= multiplier;
                    hit.m_damage.m_chop *= multiplier;
                    hit.m_damage.m_pickaxe *= multiplier;
                    hit.m_damage.m_fire *= multiplier;
                    hit.m_damage.m_frost *= multiplier;
                    hit.m_damage.m_lightning *= multiplier;
                    hit.m_damage.m_poison *= multiplier;
                    hit.m_damage.m_spirit *= multiplier;
                }
            }
        }
        /// <summary>
        /// HarmonyPatch Type: TreeLog Method: Damage
        /// This method includes:
        ///     If in a zone with "damagemultipliertotrees" configuration do (X) percent damage to trees.
        ///     Last Updated: 4/24/2021
        ///     Status: 100% Working
        /// </summary>
        [HarmonyPatch(typeof(TreeLog), "Damage")]
        public static class TreeLog_Modifier
        {
            public static void Prefix(TreeLog __instance, HitData hit)
            {
                if (Util.isServer()) return;

                if (Util.RestrictionCheck("nodamagetotrees"))
                {
                    float multiplier = 0f;
                    hit.m_damage.m_damage *= multiplier;
                    hit.m_damage.m_blunt *= multiplier;
                    hit.m_damage.m_slash *= multiplier;
                    hit.m_damage.m_pierce *= multiplier;
                    hit.m_damage.m_chop *= multiplier;
                    hit.m_damage.m_pickaxe *= multiplier;
                    hit.m_damage.m_fire *= multiplier;
                    hit.m_damage.m_frost *= multiplier;
                    hit.m_damage.m_lightning *= multiplier;
                    hit.m_damage.m_poison *= multiplier;
                    hit.m_damage.m_spirit *= multiplier;
                    Util.DoPrivateAreaEffect(Player.m_localPlayer.transform.position + Vector3.up * 0.5f);
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, "This is a Private Area", 0, null);
                }
                else if (Util.RestrictionCheck("damagemultipliertotrees"))
                {
                    float multiplier = Util.RestrictionCheckFloatReturn("damagemultipliertotrees");
                    hit.m_damage.m_damage *= multiplier;
                    hit.m_damage.m_blunt *= multiplier;
                    hit.m_damage.m_slash *= multiplier;
                    hit.m_damage.m_pierce *= multiplier;
                    hit.m_damage.m_chop *= multiplier;
                    hit.m_damage.m_pickaxe *= multiplier;
                    hit.m_damage.m_fire *= multiplier;
                    hit.m_damage.m_frost *= multiplier;
                    hit.m_damage.m_lightning *= multiplier;
                    hit.m_damage.m_poison *= multiplier;
                    hit.m_damage.m_spirit *= multiplier;
                }
            }
        }
    }
}