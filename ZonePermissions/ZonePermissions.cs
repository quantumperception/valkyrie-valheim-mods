using BepInEx;
using HarmonyLib;
using Jotunn.Utils;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace ZonePermissions
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.ClientMustHaveMod, VersionStrictness.Minor)]
    public partial class ZonePermissions : BaseUnityPlugin
    {
        private readonly static ZonePermissions instance;
        public const string PluginGUID = "com.valkyrie.zonepermissions";
        public const string PluginName = "Zone Permissions";
        public const string PluginVersion = "0.1.0";
        public static string ZoneDir = Path.Combine(BepInEx.Paths.ConfigPath, "ZonePermissions");
        public static string DefaultZonePath = Path.Combine(ZoneDir, "Zones.txt");
        public const int AreaRange = 100;
        public static int HealTick = 0;
        public static int DamageTick = 0;
        public static int EffectTick = 0;
        public static Harmony harm = new Harmony("ZonePermissions");
        public static string MySteamID = "";

        private void Awake()
        {
            CreateConfigValues();
            if (Util.isServer())
            {
                CreateFiles();
                SetupZoneWatcher();
                Client.Ward.Damage = WardProtectDamage.Value;
                Client.Ward.Pickup = WardProtectItemPickup.Value;
                Client.Ward.Drop = WardProtectItemDrop.Value;
                Client.NoItemLoss = NoItemLoss.Value;
                Client.RespawnTimer = RespawnTimer.Value;
                ZoneHandler.LoadZoneData(DefaultZonePath);
            }
            else
            {
            }
            Client.EnforceZones = true;
            harm.PatchAll();
        }

        private void FixedUpdate()
        {
            ZonePatches.FixedUpdate();
        }

        private static void SetupZoneWatcher()
        {
            Debug.Log("Creating file watcher for Zones");
            FileSystemWatcher watcher = new FileSystemWatcher(ZoneDir);
            watcher.Changed += OnZoneConfigChange;
            watcher.Created += OnZoneConfigChange;
            watcher.Renamed += OnZoneConfigChange;
            watcher.IncludeSubdirectories = true;
            watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            watcher.EnableRaisingEvents = true;
        }

        private static void OnZoneConfigChange(object sender, FileSystemEventArgs e)
        {
            Debug.Log("ZONES FILE CHANGED!");
            ZoneHandler.LoadZoneData(DefaultZonePath);
            SendAllUpdate();
        }

        private static void AddServerRPC()
        {
            ZRoutedRpc.instance.Register("ZonePermissions_AddZone", new Action<long, ZPackage>(AddZone)); // Adding Zone
            ZRoutedRpc.instance.Register("ZonePermissions_ReloadZones", new Action<long, ZPackage>(ReloadZones)); // Adding ReloadZones
            ZRoutedRpc.instance.Register("ZonePermissions_ZoneHandler", new Action<long, ZPackage>(ZoneHandler.RPC)); // Adding ZoneHandler
            //ZRoutedRpc.instance.Register("DynamicDungeons AcceptPoll", new Action<long>(DungeonManager.RPC_AcceptPoll));
            //ZRoutedRpc.instance.Register("DynamicDungeons DeclinePoll", new Action<long>(DungeonManager.RPC_DeclinePoll));
            //ZRoutedRpc.instance.Register<string>("DynamicDungeons RequestInfo", new Action<long, string>(DungeonManager.RPC_RequestInfo));
            //ZRoutedRpc.instance.Register<string>("DynamicDungeons ReloadDungeon", new Action<long, string>(RPC_OnReloadDungeon));
            //ZRoutedRpc.instance.Register("DynamicDungeons ReloadAllDungeons", new Action<long>(RPC_OnReloadDungeons));
            //ZRoutedRpc.instance.Register<string>("DynamicDungeons EnteredDungeon", new Action<long, string>(DungeonManager.RPC_OnEnteredDungeon));
            //ZRoutedRpc.instance.Register<string>("DynamicDungeons ExitedDungeon", new Action<long, string>(DungeonManager.RPC_OnExitedDungeon));
            //ZRoutedRpc.instance.Register<string>("DynamicDungeons DungeonDeath", new Action<long, string>(DungeonManager.RPC_OnDungeonDeath));
            //ZRoutedRpc.instance.Register<string>("DynamicDungeons Damaged", new Action<long, string>(DungeonManager.RPC_OnDungeonDamaged));
            //ZRoutedRpc.instance.Register<string>("DynamicDungeons DungeonKill", new Action<long, string>(DungeonManager.RPC_OnDungeonKill));
            //ZRoutedRpc.instance.Register<string>("DynamicDungeons TakeItem", new Action<long, string>(DungeonManager.RPC_OnTakeChestItem));
            //ZRoutedRpc.instance.Register<string>("DynamicDungeons LeaveItem", new Action<long, string>(DungeonManager.RPC_OnLeaveItem));
            //ZRoutedRpc.instance.Register<string>("DynamicDungeons StartEvent", new Action<long, string>(DungeonManager.RPC_OnStartDungeonEvent));
            //ZRoutedRpc.instance.Register<string>("DynamicDungeons StopEvent", new Action<long, string>(DungeonManager.RPC_OnStopDungeonEvent));
        }
        private static void AddClientRPC()
        {
            ZRoutedRpc.instance.Register("ZonePermissions_AddZone", new Action<long, ZPackage>(AddZone)); // Adding Zone
            ZRoutedRpc.instance.Register("ZonePermissions_ReloadZones", new Action<long, ZPackage>(ReloadZones)); // Adding ReloadZones
            ZRoutedRpc.instance.Register("ZonePermissions_ZoneHandler", new Action<long, ZPackage>(ZoneHandler.RPC)); // Adding ZoneHandler
            ZRoutedRpc.instance.Register("ZonePermissions_ChatMessage", new Action<long, ZPackage>(Util.ShowChatMessage));
            ZRoutedRpc.instance.Register("ZonePermissions_ShowMessage", new Action<long, int, string>(RPC_ShowMessage));
            ZRoutedRpc.instance.Register("ZonePermissions_Client", new Action<long, ZPackage>(Client.RPC)); // Adding ZoneHandler
            //ZRoutedRpc.instance.Register("DynamicDungeons TeleportToCoords", new Action<long, ZPackage>(DungeonManager.TeleportToCoords));
            //ZRoutedRpc.instance.Register("DynamicDungeons PollPlayer", new Action<long>(DungeonManager.PollPlayer));
            //ZRoutedRpc.instance.Register("DynamicDungeons ReceivedDungeon", new Action<long, ZPackage>(DungeonManager.OnReceivedDungeon));
            //ZRoutedRpc.instance.Register("DynamicDungeons DungeonUpdate", new Action<long, string, string, ZPackage>(DungeonManager.OnDungeonUpdate));
            //ZRoutedRpc.instance.Register("DynamicDungeons DungeonCompleted", new Action<long, string>(DungeonManager.OnDungeonCompleted));
            //ZRoutedRpc.instance.Register("DynamicDungeons DungeonFailed", new Action<long, string>(DungeonManager.OnDungeonFailed));
            //ZRoutedRpc.instance.Register("DynamicDungeons Message", new Action<long, string, string>(DungeonManager.ShowMessage));
            //ZRoutedRpc.instance.Register("DynamicDungeons RemoveDrops", new Action<long>(DungeonManager.ClientRemoveDrops));
        }

        private void CreateFiles()
        {
            if (!Directory.Exists(ZoneDir))
            {
                Directory.CreateDirectory(ZoneDir);
            }
            if (!File.Exists(DefaultZonePath))
            {
                File.Create(DefaultZonePath);
            }
        }
        public static void ReloadZones(long sender, ZPackage pkg)
        {
            ZNetPeer peer = ZNet.instance.GetPeer(sender);
            if (peer != null)
            {
                string permissionnode = "HackShardGaming.WoV-Zones.Reload";
                string peerSteamID = peer.m_uid.ToString(); // Get the SteamID from peer.
                bool PlayerPermission = true /*ValheimPermissions.ValheimDB.CheckUserPermission(peerSteamID, permissionnode)*/;
                if (PlayerPermission)
                {
                    ZoneHandler.LoadZoneData(ZonePermissions.DefaultZonePath);
                    Util.Broadcast("Reloading Zone");
                    Debug.Log("S2C ZoneHandler (SendPeerInfo)");
                    SendAllUpdate();
                }
                else
                {
                    Util.RoutedBroadcast(sender, $"Sorry! You do not have the permission to use !ReloadZones (Required Permission: {permissionnode})");
                }
            }
        }
        public static void SendAllUpdate()
        {
            Debug.Log("SendAllUpdate");
            foreach (ZNetPeer peer in ZNet.instance.GetPeers())
            {
                ZRoutedRpc.instance.InvokeRoutedRPC(peer.m_uid, "ZonePermissions_ZoneHandler", ZoneHandler.Serialize(peer.m_uid));
                Debug.Log("Sent update to " + peer.m_uid);
            }
            Debug.Log("Broadcast Reloading Zone");
            Util.Broadcast("Reloading Zones");
        }

        public static void RPC_ShowMessage(long uid, int type, string msg)
        {
            Util.ShowMessage(type, msg);
        }

        public static void AddZone(long sender, ZPackage pkg)
        {
            if (pkg != null && pkg.Size() > 0)
            { // Check that our Package is not null, and if it isn't check that it isn't empty.
                ZNetPeer peer = ZNet.instance.GetPeer(sender); // Get the Peer from the sender, to later check the SteamID against our Adminlist.
                if (peer != null)
                { // Confirm the peer exists
                    string permissionnode = "HackShardGaming.WoV-Zones.Add";
                    string peerSteamID = peer.m_uid.ToString(); // Get the SteamID from peer.
                    bool PlayerPermission = false /*ValheimPermissions.ValheimDB.CheckUserPermission(peerSteamID, permissionnode)*/;
                    if (PlayerPermission)
                    {
                        string msg = pkg.ReadString();
                        string[] results = msg.Split(' ');
                        string Name = results[0];
                        Debug.Log($"C-<S AddZone (RPC Call)");
                        string Type = results[1];
                        ZoneHandler.ZoneTypes zt = ZoneHandler.FindZoneType(results[1]);
                        if (zt.Name != Type)
                        {
                            msg = $"ERROR: The requested Zone Type {Type} does not exist!";
                            Util.RoutedBroadcast(sender, msg);
                            return;
                        }
                        int Priority = Int32.Parse(results[2]);
                        if (Priority < 1 || Priority > 5)
                        {
                            msg = $"ERROR: The requested Priority {Priority} is out of bounds! (Priorities are ranged from 1-5)!";
                            Util.RoutedBroadcast(sender, msg);
                            return;
                        }
                        string Shape = results[3];
                        if (Shape.ToLower() != "circle" && Shape.ToLower() != "square")
                        {
                            msg = $"ERROR: The requested Shape: {Shape} is incorrectly formated! (Shapes can either be circle or square only)";
                            Util.RoutedBroadcast(sender, msg);
                            return;
                        }
                        Single i = new float();
                        string X = results[4];
                        if (!Single.TryParse(X, out i))
                        {
                            msg = $"ERROR: The requested X {X} is incorrectly formated! (Correct Format is 0.0)!";
                            Util.RoutedBroadcast(sender, msg);
                            return;
                        }
                        string Y = results[5];
                        if (!Single.TryParse(Y, out i))
                        {
                            msg = $"ERROR: The requested Y {Y} is incorrectly formated! (Correct Format is 0.0)!";
                            Util.RoutedBroadcast(sender, msg);
                            return;
                        }
                        string R = results[6];
                        if (!Single.TryParse(R, out i))
                        {
                            msg = $"ERROR: The requested Radius {R} is incorrectly formated! (Correct Format is 0.0)!";
                            Util.RoutedBroadcast(sender, msg);
                            return;
                        }
                        string addline = Name + " " + Type + " " + Priority + " " + Shape + " " + X + " " + Y + " " + R;
                        File.AppendAllText(ZonePermissions.DefaultZonePath, addline + Environment.NewLine);
                    }
                    else
                    {
                        Util.RoutedBroadcast(sender, $"Sorry! You do not have the permission to use !AddZone (Required Permission: {permissionnode})");
                        Debug.Log($"An unauthorized user {peerSteamID} attempted to use the AddZone RPC!");
                        string msg = pkg.ReadString();
                        Debug.Log($"Here is a log of the attempted AddZone {msg}");
                    }
                }
            }
        }
    }


}
