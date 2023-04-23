using BepInEx;
using HarmonyLib;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
namespace ValkyrieUtils
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [BepInDependency("WackyMole.EpicMMOSystem", BepInDependency.DependencyFlags.SoftDependency)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    internal class ValkyrieUtils : BaseUnityPlugin
    {
        private readonly static ValkyrieUtils instance;
        public const string PluginGUID = "com.valkyrie.utils";
        public const string PluginName = "ValkyrieUtils";
        public const string PluginVersion = "0.0.1";
        public static AssetBundle bundle;
        private static readonly string ValkyrieUtilsPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        private static Harmony harm = new Harmony("ValkyrieUtilsServer");

        private static bool IsServer
        {
            get
            {
                return SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;
            }
        }

        private void Awake()
        {
            bundle = Jotunn.Utils.AssetUtils.LoadAssetBundleFromResources("valkyrie");
            PVPArena.LoadArenaPrefabs();
            CommandManager.Instance.AddConsoleCommand(new ClearInventoryCommand());
            harm.PatchAll();
        }

        public static void BroadcastDeath(string[] text)
        {
            Jotunn.Logger.LogInfo("Broadcasting");
            foreach (string line in text)
            {
                Jotunn.Logger.LogInfo(line);
            }
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "DarwinAwards IDied", new object[]
            {
                text
            });
        }

        private static IEnumerator WaitAndLogKDAs(float waitTime)
        {
            while (true)
            {
                yield return new WaitForSeconds(waitTime);
                LogKDAs();
            }
        }

        private static IEnumerator WaitAndLogPositions(float waitTime)
        {
            while (true)
            {
                yield return new WaitForSeconds(waitTime);
                SavePlayerPositions();
            }
        }
        private static void LogKDAs()
        {

        }

        private static void onReceivedDeath(long senderId, string a)
        {
            ZNetPeer peer = ZNet.instance.GetPeer(senderId);
            Jotunn.Logger.LogInfo(peer.m_playerName + "died");
        }
        private static void onReceivedHit(long senderId, ZPackage pkg)
        {

        }
        private static string[] SavePlayerPositions()
        {
            if (!(ZNet.GetConnectionStatus() == ZNet.ConnectionStatus.Connected)) return new string[1] { "Not connected" };
            if (ZNet.instance.m_peers.Count == 0)
            {
                Jotunn.Logger.LogInfo("No players connected, skipping position save.");
                return new string[1] { "No players connected, skipping position save." };
            }
            string today = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
            Jotunn.Logger.LogInfo(today);
            string positionsPath = ValkyrieUtilsPath + "\\" + "player_positions";
            if (!Directory.Exists(positionsPath)) Directory.CreateDirectory(positionsPath);
            string todayLocationsPath = positionsPath + "\\" + today.Split('T')[0] + ".log";
            if (!File.Exists(todayLocationsPath)) File.Create(todayLocationsPath).Close();
            List<string> todayLocations = File.ReadAllLines(todayLocationsPath).ToList();
            List<string> positions = new List<string>();
            foreach (ZNetPeer znetPeer in ZNet.instance.m_peers)
            {
                ZDO zdo8 = ZDOMan.instance.GetZDO(znetPeer.m_characterID);
                if (zdo8 != null)
                {
                    positions.Add(
                                today.Split('T')[1] + "|" +
                                znetPeer.m_playerName + "|" +
                                (int)znetPeer.m_refPos.x + "|" +
                                (int)znetPeer.m_refPos.y + "|" +
                                (int)znetPeer.m_refPos.z + "|" +
                                (int)zdo8.GetFloat("health", 0f) + "|" +
                                (int)zdo8.GetFloat("max_health", 0f) + "|" +
                                znetPeer.m_socket.GetHostName() + "|" +
                                zdo8.GetLong("playerID", 0L)
                    );
                }
            }
            todayLocations.AddRange(positions);
            File.WriteAllLines(todayLocationsPath, todayLocations.ToArray());
            Jotunn.Logger.LogInfo("Saved positions from " + positions.Count + " players.");
            return positions.ToArray();

        }

        public class ClearInventoryCommand : ConsoleCommand
        {
            public override string Name => "clearinventory";

            public override string Help => "Clears player inventory";

            public override void Run(string[] args)
            {
                Player.m_localPlayer.m_inventory.RemoveAll();
            }
        }

        [HarmonyPatch(typeof(ZNet), "Start")]
        private static class ZNETSTARTPATCH
        {
            private static void Postfix()
            {
                if (ValkyrieUtils.IsServer)
                {
                    Game.instance.StartCoroutine(WaitAndLogPositions(120));
                    ZRoutedRpc.instance.Register<string>("ValkyrieUtils IDied", new Action<long, string>(ValkyrieUtils.onReceivedDeath));
                    ZRoutedRpc.instance.Register<ZPackage>("ValkyrieUtils IDied", new Action<long, ZPackage>(ValkyrieUtils.onReceivedHit));
                    ZRoutedRpc.instance.Register<string, string>("ValkyrieUtils EnterArenaLobby", new Action<long, string, string>(PVPArena.OnEnterArenaLobby));
                    ZRoutedRpc.instance.Register<string, string>("ValkyrieUtils LeaveArenaLobby", new Action<long, string, string>(PVPArena.OnLeaveArenaLobby));
                }
            }
        }
    }



}

