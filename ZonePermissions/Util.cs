using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using System.Reflection;

namespace ZonePermissions
{

    public class Util
    {
        public static BindingFlags NonPublicFlags = BindingFlags.NonPublic | BindingFlags.Instance;
        public static List<Util.ConnectionData> Connections = new List<Util.ConnectionData>();

        public static IEnumerator ZoneHandler2(ZRpc rpc)
        {
            Debug.Log("ZoneHandler2");
            rpc.Invoke("ZP_ZoneHandler", ZoneHandler.Serialize());
            yield return new WaitForSeconds(1);
        }
        public static IEnumerator Client2(ZRpc rpc)
        {
            Debug.Log("Client2");
            rpc.Invoke("ZP_Client", Client.Serialize());
            yield return new WaitForSeconds(1);
        }
        public static void InsertChatMessage(string Message)
        {
            Chat.instance.AddString($"<color=grey><b>[{ZonePermissions.PluginName}]</b></color> {Message}");
        }
        public class ConnectionData
        {
            public ZRpc rpc;
        }
        public static float RestrictionCheckFloatReturn(string restriction)
        {
            string PlayerSteamID = SteamUser.GetSteamID().ToString();
            Player p = Player.m_localPlayer;
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
            string key = "";
            string admins = "";
            // Lets set our admins and keys..
            admins = zt.Admins;
            key = zt.Configurations;
            // Lets see if the user is actually an admin in the zone first..
            if (admins.Contains(ZonePermissions.MySteamID))
            {
                // Ok they are an admin. Therefore, do not initialize the change...
                return 1;
            }
            if (key.ToLower().Contains(restriction))
            {
                string s = key.ToLower();
                string restrictioncheck = restriction + "(";
                int indexStart = s.IndexOf(restrictioncheck) + restrictioncheck.Length;
                string test = "";
                for (int i = indexStart; i < indexStart + 20; i++)
                {
                    if (s[i] == ')') break;
                    test += s[i];
                }
                float multiplier = 1;
                multiplier = Convert.ToSingle(test, new CultureInfo("en-US"));
                return multiplier;
            }
            else
                return 1;
        }
        public static float RestrictionCheckFloatReturnCharacter(Character __instance, string restriction)
        {
            Character p = __instance;
            string CharacterSteamID = (ZNet.instance.GetPeer(__instance.GetZDOID().UserID)).m_socket.GetHostName();
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
            string key = "";
            string admins = "";
            // Lets set our admins and keys..
            admins = zt.Admins;
            key = zt.Configurations;
            // Lets see if the user is actually an admin in the zone first..
            if (admins.Contains(CharacterSteamID))
            {
                // Ok they are an admin. Therefore, do not initialize the change...
                return 1;
            }
            if (key.ToLower().Contains(restriction))
            {
                string s = key.ToLower();
                string restrictioncheck = restriction + "(";
                int indexStart = s.IndexOf(restrictioncheck) + restrictioncheck.Length;
                string test = "";
                for (int i = indexStart; i < indexStart + 20; i++)
                {
                    if (s[i] == ')') break;
                    test += s[i];
                }
                float multiplier = 1;
                multiplier = Convert.ToSingle(test, new CultureInfo("en-US"));
                return multiplier;
            }
            else
                return 1;
        }
        public static float RestrictionCheckFloatReturnNone(Character __instance, string restriction)
        {
            Character p = __instance;
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
            string key = "";
            string admins = "";
            // Lets set our admins and keys..
            admins = zt.Admins;
            key = zt.Configurations;
            // Lets see if the user is actually an admin in the zone first..
            if (key.ToLower().Contains(restriction))
            {
                string s = key.ToLower();
                string restrictioncheck = restriction + "(";
                int indexStart = s.IndexOf(restrictioncheck) + restrictioncheck.Length;
                string test = "";
                for (int i = indexStart; i < indexStart + 20; i++)
                {
                    if (s[i] == ')') break;
                    test += s[i];
                }
                float multiplier = 1;
                multiplier = Convert.ToSingle(test, new CultureInfo("en-US"));
                return multiplier;
            }
            else
                return 1;
        }
        public static bool RestrictionCheckCharacter(Character __instance, string restriction)
        {
            Character p = __instance;
            string CharacterSteamID = (ZNet.instance.GetPeer(__instance.GetZDOID().UserID)).m_socket.GetHostName();
            // Are we in a zone? if so select that zone.
            if (ZoneHandler.Zones.Count() == 0)
            {
                return false;
            }
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
            string key = "";
            string admins = "";
            // Lets set our admin list and keys...
            admins = zt.Admins;
            key = zt.Configurations;
            // Lets check and see if the user is actually an admin in the zone.
            if (admins.Contains(CharacterSteamID))
            {
                return false;
            }
            if (key.ToLower().Contains(restriction))
                return true;
            else
                return false;
        }

        public static bool RestrictionCheckTerrain(TerrainComp __instance, string restriction)
        {
            TerrainComp p = __instance;
            // Are we in a zone? if so select that zone.
            if (ZoneHandler.Zones.Count() == 0)
            {
                return false;
            }
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
            string key = "";
            string admins = "";
            // Lets set our admin list and keys...
            admins = zt.Admins;
            key = zt.Configurations;
            if (key.ToLower().Contains(restriction))
                return true;
            else
                return false;
        }
        public static bool RestrictionCheckNone(Character __instance, string restriction)
        {
            Character p = __instance;
            // Are we in a zone? if so select that zone.
            if (ZoneHandler.Zones.Count() == 0)
            {
                return false;
            }
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
            string key = "";
            // Lets set our admin list and keys...
            key = zt.Configurations;
            if (key.ToLower().Contains(restriction))
                return true;
            else
                return false;
        }
        public static bool RestrictionCheck(string restriction)
        {
            Player p = Player.m_localPlayer;
            // Are we in a zone? if so select that zone.
            if (ZoneHandler.Zones.Count() == 0)
            {
                return false;
            }
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
            string key = "";
            string admins = "";
            // Lets set our admin list and keys...
            admins = zt.Admins;
            key = zt.Configurations;
            // Lets check and see if the user is actually an admin in the zone.
            if (admins.Contains(ZonePermissions.MySteamID))
            {
                return false;
            }
            if (key.ToLower().Contains(restriction))
                return true;
            else
                return false;
        }
        public static void DoPrivateAreaEffect(Vector3 pos)
        {
            //if (ZonePermissions.EffectTick <= 0)
            //{
            //    ZonePermissions.EffectTick = 120;
                GameObject znet = ZNetScene.instance.GetPrefab("vfx_lootspawn");
                GameObject obj = UnityEngine.Object.Instantiate(znet, pos, Quaternion.identity);
                if (Player.m_localPlayer == null) return;
                ShowChatMessage(Player.m_localPlayer.GetPlayerID(), ZonePermissions.PluginName, "PRIVATE AREA", true, pos);
                //DamageText.WorldTextInstance worldTextInstance = new DamageText.WorldTextInstance();
                //worldTextInstance.m_worldPos = pos;
                //worldTextInstance.m_gui = UnityEngine.Object.Instantiate(DamageText.instance.m_worldTextBase, DamageText.instance.transform);
                //worldTextInstance.m_textField = worldTextInstance.m_gui.GetComponent<TMPro.TMP_Text>();
                //Type dmgTxt = DamageText.instance.GetType();
                //DamageText.instance.AddInworldText(DamageText.TextType.Normal, pos, 0, 1, true);
                //dmgTxt.GetMethod("Add", Util.NonPublicFlags).Invoke(DamageText.instance, new object[] { worldTextInstance });
                //worldTextInstance.m_textField.color = Color.cyan;
                //worldTextInstance.m_textField.fontSize = 24;
                //worldTextInstance.m_textField.text = "PRIVATE AREA";
                //worldTextInstance.m_timer = -2f;
            //}
        }
        public static void DoWardedAreaEffect(Vector3 pos)
        {
            //if (ZonePermissions.EffectTick <= 0)
            //{
                //ZonePermissions.EffectTick = 120;
                GameObject znet = ZNetScene.instance.GetPrefab("vfx_lootspawn");
                GameObject obj = UnityEngine.Object.Instantiate(znet, pos, Quaternion.identity);
                if (Player.m_localPlayer == null) return;
                ShowChatMessage(Player.m_localPlayer.GetPlayerID(), ZonePermissions.PluginName, "WARDED AREA", true, pos);
                //DamageText.WorldTextInstance worldTextInstance = new DamageText.WorldTextInstance();
                //worldTextInstance.m_worldPos = pos;
                //worldTextInstance.m_gui = UnityEngine.Object.Instantiate(DamageText.instance.m_worldTextBase, DamageText.instance.transform);
                //worldTextInstance.m_textField = worldTextInstance.m_gui.GetComponent<TMPro.TMP_Text>();
                //Type dmgTxt = DamageText.instance.GetType();
                //dmgTxt.GetMethod("Add", Util.NonPublicFlags).Invoke(DamageText.instance, new object[] { worldTextInstance });
                //worldTextInstance.m_textField.color = Color.cyan;
                //worldTextInstance.m_textField.fontSize = 24;
                //worldTextInstance.m_textField.text = "WARDED AREA";
                //worldTextInstance.m_timer = -2f;
            //}
        }
        public static void ShowMessage(int type, string msg)
        {
            Player.m_localPlayer.Message((MessageHud.MessageType)type, msg);
        }

        public static void ShowChatMessage(long uid, ZPackage pkg)
        {
            UserInfo userInfo = new UserInfo()
            {
                Name = pkg.ReadString(),
                Gamertag = null,
                NetworkUserId = uid.ToString()
            };
            string msg = pkg.ReadString();
            bool inWorld = pkg.ReadBool();
            Vector3 pos = pkg.ReadVector3();
            Type chat = Chat.instance.GetType();
            if (inWorld) chat.GetMethod("AddInworldText", NonPublicFlags).Invoke(Chat.instance,
                new object[] {
                    null,
                    uid,
                    pos,
                    Talker.Type.Shout,
                    userInfo,
                    msg
                });
            chat.GetField("m_hideTimer", NonPublicFlags).SetValue(Chat.instance, 0f);
            Chat.instance.AddString(ZonePermissions.PluginName, msg, Talker.Type.Shout);
        }
        public static void ShowChatMessage(long uid, string username, string msg, bool inWorld = false, Vector3 pos = new Vector3(), Talker.Type type = Talker.Type.Normal)
        {
            UserInfo userInfo = new UserInfo()
            {
                Name = username,
                Gamertag = null,
                NetworkUserId = uid.ToString()
            };
            Type chat = Chat.instance.GetType();
            if (inWorld) chat.GetMethod("AddInworldText", NonPublicFlags).Invoke(Chat.instance,
                new object[] {
                    null,
                    uid,
                    pos,
                    type,
                    userInfo,
                    msg
                });
            chat.GetField("m_hideTimer", NonPublicFlags).SetValue(Chat.instance, 0f);
            Chat.instance.AddString(ZonePermissions.PluginName, msg, Talker.Type.Shout);
        }
        public static void Broadcast(string text, string username = ZonePermissions.PluginName, bool inWorld = false)
        {
            ZPackage pkg = new ZPackage();
            pkg.Write(username);
            pkg.Write(text);
            pkg.Write(inWorld);
            pkg.Write(new Vector3(0f, 100f, 0f));
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "ZonePermissions_ChatMessage", pkg);
        }
        public static void RoutedBroadcast(long peer, string text, string username = ZonePermissions.PluginName)
        {
            var cwti = Chat.instance.FindExistingWorldText(peer);
            ZRoutedRpc.instance.InvokeRoutedRPC(peer, "ChatMessage", new object[]
            {
                new Vector3(0,100,0),
                Talker.Type.Shout,
                username,
                cwti.m_userInfo,
                text,
                cwti.m_userInfo.NetworkUserId
            });
        }
        public static bool isServer()
        {
            return SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;
        }
        public static bool isAdmin(long sender)
        {
            ZNetPeer peer = ZNet.instance.GetPeer(sender);
            string SteamID = sender.ToString();
            if (
                ZNet.instance.m_adminList != null &&
                ZNet.instance.m_adminList.Contains(SteamID)
            )
                return true;
            else
            {
                return false;
            }
        }
        public static bool IsZoneAdmin(int zoneID)
        {
            ZoneHandler.ZoneTypes ztype;
            if (zoneID < 0)
            {
                ztype = ZoneHandler.FindZoneType("wilderness");
            }
            else
            {
                Debug.Log($"Checking admin for zone id {zoneID}");
                ZoneHandler.Zone zone = ZoneHandler.Zones.Find(z => z.ID == zoneID);
                Debug.Log($"Got Zone: {zone.Name}");
                ztype = ZoneHandler.FindZoneType(zone.Type);
            }
            return ztype.Admins.Contains(ZonePermissions.MySteamID);

        }
        public static Util.ConnectionData GetServer()
        {
            Debug.Assert(!ZNet.instance.IsServer());
            Debug.Assert(Util.Connections.Count == 1);
            return Util.Connections[0];
        }


    }
}