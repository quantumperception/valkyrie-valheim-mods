using HarmonyLib;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace ZonePermissions
{
    public partial class ZonePermissions
    {
        [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.AwakePlayFab))]
        private static class FejdStartupAwakePlayfabPatch
        {
            private static void Postfix(FejdStartup __instance)
            {
                if (Util.isServer()) return;
                MySteamID = Steamworks.SteamUser.GetSteamID().ToString();
                Debug.Log($"Caching our SteamID as {MySteamID}");
            }
        }


        [HarmonyPatch(typeof(ZNet), nameof(ZNet.Awake))]
        private static class ZNetAwakePatch
        {
            private static void Postfix(ZNet __instance)
            {
                if (Util.isServer())
                {
                    Debug.Log("Registering Server RPCs");
                    AddServerRPC();
                }
                else
                {
                    MySteamID = Steamworks.SteamUser.GetSteamID().ToString();
                    Debug.Log($"Caching our SteamID as {MySteamID}");
                    Debug.Log("Registering Client RPCs");
                    AddClientRPC();
                }
            }
        }

        // Patch Znet::OnDeath
        // We died! We need to reset variables to default
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player), "OnRespawn")]
        private static void Player__OnRespawn(Player __instance)
        {
            if (ZNet.instance.IsServer())
            {
                return;
            }
            ZoneHandler.CurrentZoneID = -2;
        }

        [HarmonyPatch(typeof(ZNet))]
        public class ZNetPatch
        {
            // Patch ZNet::OnNewConnection
            // This is where a client setup a connection to the server (vice versa)
            // Put any RPC register here to sync between server/client.
            //
            [HarmonyPrefix]
            [HarmonyPatch("OnNewConnection")]
            private static void ZNet_OnNewConnectionPrefix(ZNet __instance, ZNetPeer peer)
            {
                if (Util.isServer()) return;
                Debug.Log($"Server Zone Enforced: {Client.EnforceZones}");
                // Client special RPC calls
                peer.m_rpc.Register("ZP_ZoneHandler", new Action<ZRpc, ZPackage>(ZoneHandler.RPC));
                peer.m_rpc.Register("ZP_Client", new Action<ZRpc, ZPackage>(Client.RPC));
                // Reset zone ID
                ZoneHandler.CurrentZoneID = -2;
            }
            // Patch ZNet::SendPeerInfo
            // During connection, use to send info to the peer.
            // Great point to send to client.
            [HarmonyPrefix]
            [HarmonyPatch("SendPeerInfo")]
            private static void ZNet_SendPeerInfo(ZNet __instance, ZRpc rpc)
            {
                // Run away clients, we don't want you here!?!?ZoneHandler.Serialize
                if (!Util.isServer()) return;
                Debug.Log("SendPeerInfo");
                // Syncing Zone Handler Settings.
                Debug.Log("S2C ZoneHandler (SendPeerInfo)");
#if DEBUG
                ZoneHandler._debug();
#endif
                rpc.Invoke("ZP_ZoneHandler", ZoneHandler.Serialize());
                // Syncing the Client State with the server defaults.
                Debug.Log("S2C ClientState (SendPeerInfo)");
#if DEBUG
                Client._debug();
#endif
                rpc.Invoke("ZP_Client", Client.Serialize());
                Util.Connections.Add(new Util.ConnectionData
                {
                    rpc = rpc
                });
            }
        }

        [HarmonyPatch]
        public static class ZoneEnforcer
        {
            [HarmonyPostfix]
            [HarmonyPatch(typeof(Game), "Update")]
            private static void Game__Update()
            {
                if (!Player.m_localPlayer) return;
                // 
                // Goes through the zones and setup the necessary enforcements.
                //Debug.Log("Game__Update 1");
                ZoneHandler.Zone zone;
                ZoneHandler.ZoneTypes ztype;
                string Message = "";
                bool changed;
                bool zoneDetected = ZoneHandler.Detect(Player.m_localPlayer.transform.position, out changed, out zone, out ztype);
                //Debug.Log("Game__Update 2");
                if (changed)
                {
                    if (zoneDetected)
                    {
                        Client.IsZoneAdmin = Util.IsZoneAdmin(zone.ID);
                        var color = (ztype.PVPEnforce ? (ztype.PVP ? ZonePermissions.PVPColor.Value : ZonePermissions.PVEColor.Value) : ZonePermissions.NonEnforcedColor.Value);
                        string Name = zone.Name.Replace("_", " ");
                        Message = $"<color={color}>Now entering <b>{Name}</b>.</color>";
                        string BiomeMessage = (ztype.PVPEnforce ? ztype.PVP ? "PVP Enabled" : "PVP Disabled" : String.Empty);
                        // The message at the end is in the format of (PVP) (NOPVP) (NON-ENFORCED)
                        Player.m_localPlayer.Message(MessageHud.MessageType.Center, Message, 0, null);
                        //Debug.Log("Game__Update 3");

                        if (Client.EnforceZones && ztype.PVPEnforce && ztype.PVP != Player.m_localPlayer.IsPVPEnabled() && ZonePermissions.BiomePVPAnnouncement.Value)
                            MessageHud.instance.ShowBiomeFoundMsg(BiomeMessage, true);
                        //Debug.Log("Game__Update 4");
                    }
                    else
                    {
                        Client.IsZoneAdmin = Util.IsZoneAdmin(-1);
                        var color = (ztype.PVPEnforce ? (ztype.PVP ? ZonePermissions.PVPColor.Value : ZonePermissions.PVEColor.Value) : ZonePermissions.NonEnforcedColor.Value);
                        string Name = "The Wilderness";
                        Message = $"<color={color}>Now entering <b>{Name}</b>.</color>";
                        string BiomeMessage = (ztype.PVPEnforce ? ztype.PVP ? "PVP Enabled" : "PVP Disabled" : String.Empty);
                        // The message at the end is in the format of (PVP) (NOPVP) (NON-ENFORCED)
                        Player.m_localPlayer.Message(MessageHud.MessageType.Center, Message, 0, null);

                        //Debug.Log("Game__Update 5");
                        if (Client.EnforceZones && ztype.PVPEnforce && ztype.PVP != Player.m_localPlayer.IsPVPEnabled() && ZonePermissions.BiomePVPAnnouncement.Value)
                            MessageHud.instance.ShowBiomeFoundMsg(BiomeMessage, true);
                        //Debug.Log("Game__Update 6");
                    }
                    // Zones are now being enforced?
                    if (!Client.IsZoneAdmin && Client.EnforceZones)
                    {
                        //Debug.Log("Game__Update 7");
                        // Update the client settings based on zone type
                        // PVP settings:
                        Client.PVPEnforced = ztype.PVPEnforce;
                        if (ztype.PVPEnforce)
                            Client.PVPMode = ztype.PVP;
                        // Position settings:
                        Client.PositionEnforce = ztype.PositionEnforce;
                        if (ztype.PositionEnforce)
                            Client.ShowPosition = ztype.ShowPosition;
                        // Run the updated settings for the Clients
                        Player.m_localPlayer.SetPVP(Client.PVPMode);
                        InventoryGui.instance.m_pvp.isOn = Client.PVPMode;
                        InventoryGui.instance.m_pvp.interactable = !Client.PVPEnforced;
                        ZNet.instance.SetPublicReferencePosition(Client.ShowPosition);
                        //Util.InsertChatMessage(Message);
                        Player.m_localPlayer.Message(MessageHud.MessageType.Center, Message, 0, null);
                        // Other settings are scattered among the wind to other functions
                        // (Use Client class for the current state)
                        //Debug.Log("Game__Update 8");
                    }
#if DEBUG
                    ZoneHandler._debug(ztype);
                    Client._debug();
                    //Debug.Log("Game__Update 9");
#endif
                }
                else
                {
                    //Debug.Log("Game__Update 11");
                    if (!Client.IsZoneAdmin && Client.PVPEnforced && (Player.m_localPlayer.IsPVPEnabled() != Client.PVPMode))
                    {
                        //Debug.Log($"{ModInfo.Title}: ERROR: Your PVP Mode was changed by another plugin.  Resetting client PVP!");
                        //Debug.Log("Game__Update 12");
                        Player.m_localPlayer.SetPVP(Client.PVPMode);
                    }
                    //Debug.Log("Game__Update 13");
                    if (!Client.IsZoneAdmin && Client.PositionEnforce && (ZNet.instance?.IsReferencePositionPublic() != Client.ShowPosition))
                    {
                        //Debug.Log($"{ModInfo.Title}: ERROR: Your Position Sharing was changed by another plugin.  Resetting client Position Sharing!");
                        ZNet.instance.SetPublicReferencePosition(Client.ShowPosition);
                    }
                    //Debug.Log("Game__Update 14");
                }
            }
            [HarmonyPostfix]
            [HarmonyPatch(typeof(Minimap), "Update")]
            public static void Minimap_Start(Toggle ___m_publicPosition)
            {
                // PositionEnforce : True -> Disable intractable
                ___m_publicPosition.interactable = Client.IsZoneAdmin ? true : !Client.PositionEnforce;
            }
            [HarmonyPatch(typeof(InventoryGui), "UpdateCharacterStats")]
            public static class PVP_Patch
            {
                private static void Postfix(InventoryGui __instance)
                {
                    __instance.m_pvp.interactable = Client.IsZoneAdmin ? true : !Client.PVPEnforced;
                }
            }
        }
    }
}
