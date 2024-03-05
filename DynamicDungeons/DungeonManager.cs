using Jotunn.Managers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace DynamicDungeons
{
    public class DungeonManager
    {
        public static DungeonManager Instance = new DungeonManager();
        public Dictionary<string, DungeonEventManager> managers = new Dictionary<string, DungeonEventManager>();
        private static GameObject pollPanel;
        public static Vector3 dungeonLobbyPos = new Vector3(0, 6000, 0);
        public static float dungeonLobbyRadius = 2f;
        public static Vector3 lobbyTLCorner = new Vector3(0, 6000, 0);
        public static Vector3 lobbyTRCorner = new Vector3(0, 6000, 0);
        public static Vector3 lobbyBLCorner = new Vector3(0, 6000, 0);
        public static Vector3 lobbyBRCorner = new Vector3(0, 6000, 0);
        public static IEnumerator dungeonPollCoroutine = WaitAndPollPlayers(15);
        public static List<long> accepteduids = new List<long>();
        public static List<long> declineduids = new List<long>();
        private void Awake()
        {
            if (!Util.IsServer()) return;
            if (!File.Exists(DynamicDungeons.configPaths["cooldowns"]))
                File.WriteAllText(DynamicDungeons.configPaths["cooldowns"], "");
            //InvokeRepeating("SaveCooldowns", 5, 300);
        }
        private void SaveCooldowns()
        {
            foreach (DynamicDungeons.DynamicDungeon dungeon in DynamicDungeons.dungeons)
            {
                Jotunn.Logger.LogInfo("Serializing dungeon cooldowns json: " + dungeon.name);
                //string cooldownJson = JsonConvert.SerializeObject(cooldown);
                Jotunn.Logger.LogInfo("Serialized dungeon cooldowns json: " + dungeon.name);
                //File.WriteAllText(Path.Combine(DynamicDungeons.configPaths["cooldowns"], dungeon.name + ".json"), cooldownJson);
                Jotunn.Logger.LogInfo("Saved dungeon cooldowns json: " + dungeon.name);
            }
        }
        public static void ScanDungeonChests()
        {
            if (!Util.IsServer()) return;
            foreach (DungeonEventManager manager in Instance.managers.Values) manager.ScanChests();
        }
        public static void ScanDungeonSpawners()
        {
            if (!Util.IsServer()) return;
            foreach (DungeonEventManager manager in Instance.managers.Values) manager.ScanSpawners();
        }
        public static IEnumerator WaitAndPollPlayers(int waitTime)
        {
            while (true)
            {
                yield return new WaitForSeconds(waitTime);
                PollPlayers();
            }
        }
        private static void PollPlayers()
        {
            if (!(ZNet.GetConnectionStatus() == ZNet.ConnectionStatus.Connected)) return;
            List<ZNetPeer> peers = ZNet.instance.GetPeers();
            if (peers.Count > 0 && accepteduids.Count + declineduids.Count != peers.Count)
            {
                Jotunn.Logger.LogInfo("Polling " + peers.Count + " players");
                foreach (ZNetPeer peer in peers)
                {
                    ZPackage pkg = new ZPackage();
                    pkg.Write(dungeonLobbyPos);
                    ZRoutedRpc.instance.InvokeRoutedRPC(peer.m_uid, "DynamicDungeons PollPlayer", pkg);
                }

            }
            return;
        }
        private static void SendCurrentDungeonUpdate(long uid, string dungeonName, bool entering)
        {
            ZPackage pkg = new ZPackage();
            pkg.Write(entering);
            ZRoutedRpc.instance.InvokeRoutedRPC(uid, "DynamicDungeons DungeonUpdate", dungeonName, "CurrentDungeon", pkg);
        }

        public static void RPC_RequestInfo(long uid, string dungeonName)
        {
            if (!Instance.managers.ContainsKey(dungeonName)) { Jotunn.Logger.LogWarning("Log Info Failed - Didn't find dungeon " + dungeonName); return; }
            Instance.managers[dungeonName].LogDungeonInfo();
            return;
        }
        public static void RPC_OnEnteredDungeon(long uid, string dungeonName)
        {
            DungeonEventManager manager = Instance.managers[dungeonName];
            manager.AddPlayer(uid);
            SendCurrentDungeonUpdate(uid, dungeonName, true);
            Util.SendPlayerMessage(uid, "center", "Entrando a " + dungeonName);

        }
        public static void RPC_OnExitedDungeon(long uid, string dungeonName)
        {
            DungeonEventManager manager = Instance.managers[dungeonName];
            manager.RemovePlayer(uid);
            SendCurrentDungeonUpdate(uid, dungeonName, false);
            Util.SendPlayerMessage(uid, "center", "Saliendo de " + dungeonName);
        }
        public static void RPC_OnDungeonDeath(long uid, string dungeonName)
        {
            Jotunn.Logger.LogInfo(uid + " died in dungeon " + dungeonName);

        }
        public static void RPC_OnDungeonKill(long uid, string dungeonName)
        {
            Jotunn.Logger.LogInfo(uid + " killed in dungeon " + dungeonName);

        }
        public static void RPC_OnDungeonDamaged(long uid, string dungeonName)
        {
            Jotunn.Logger.LogInfo(uid + " damaged in dungeon " + dungeonName);

        }
        public static void RPC_OnTakeChestItem(long uid, string dungeonName)
        {
            Jotunn.Logger.LogInfo(uid + " took item in dungeon " + dungeonName);

        }
        public static void RPC_OnLeaveItem(long uid, string dungeonName)
        {
            Jotunn.Logger.LogInfo(uid + " left item in dungeon " + dungeonName);

        }
        public static void RPC_OnStartDungeonEvent(long uid, string dungeonName)
        {
            if (!Instance.managers.ContainsKey(dungeonName)) { Jotunn.Logger.LogWarning("Start Event Failed - Didn't find dungeon " + dungeonName); return; }
            if (Instance.managers[dungeonName].isActive) { Jotunn.Logger.LogWarning("Event already active in " + dungeonName); return; }
            Jotunn.Logger.LogInfo("Starting dungeon event at " + dungeonName);
            Instance.managers[dungeonName].StartEvent();
            ZPackage pkg = new ZPackage();
            pkg.Write(true);
            ZRoutedRpc.instance.InvokeRoutedRPC(uid, "DynamicDungeons DungeonUpdate", dungeonName, "EventState", pkg);
            Jotunn.Logger.LogInfo("Started dungeon event at " + dungeonName);
            return;
        }
        public static void RPC_OnStopDungeonEvent(long uid, string dungeonName)
        {
            if (!Instance.managers.ContainsKey(dungeonName)) { Jotunn.Logger.LogWarning("Stop Event Failed - Didn't find dungeon " + dungeonName); return; }
            if (!Instance.managers[dungeonName].isActive) { Jotunn.Logger.LogWarning("No active event in " + dungeonName); return; }
            Jotunn.Logger.LogInfo("Stopping dungeon event at " + dungeonName);
            Instance.managers[dungeonName].StopEvent();
            ZPackage pkg = new ZPackage();
            pkg.Write(false);
            ZRoutedRpc.instance.InvokeRoutedRPC(uid, "DynamicDungeons DungeonUpdate", dungeonName, "EventState", pkg);
            Jotunn.Logger.LogInfo("Stopped dungeon event at " + dungeonName);
            return;
        }
        public static void OnDungeonUpdate(long uid, string dungeonName, string type, ZPackage pkg)
        {
            Jotunn.Logger.LogInfo("Got dungeon update from server: " + dungeonName);
            DungeonEventManager manager = Instance.managers[dungeonName];
            switch (type)
            {
                case "CurrentDungeon":
                    bool setDungeon = pkg.ReadBool();
                    if (setDungeon)
                    {
                        Jotunn.Logger.LogInfo("Current dungeon: " + dungeonName);
                        DynamicDungeons.currentDungeon = manager;
                        break;
                    }
                    Jotunn.Logger.LogInfo("Exited dungeon: " + dungeonName);
                    DynamicDungeons.currentDungeon = null;
                    break;
                case "EventState":
                    bool eventActive = pkg.ReadBool();
                    if (eventActive)
                    {
                        manager.StartEvent();
                        Jotunn.Logger.LogInfo("Started event at: " + dungeonName);
                        break;
                    }
                    manager.StopEvent();
                    Jotunn.Logger.LogInfo("Stopped event at: " + dungeonName);
                    break;
                case "AlertTimer":
                    if (pkg.ReadBool()) { manager.AlertTimerEnd = DateTime.Parse(pkg.ReadString()); manager.ActiveAlert = true; DynamicDungeons.lastDungeon = dungeonName; }
                    else manager.ActiveAlert = false;
                    break;
                case "CooldownTimer":
                    manager.CooldownEnd = DateTime.Parse(pkg.ReadString());
                    break;
                case "EventTimer":
                    manager.EventEnd = DateTime.Parse(pkg.ReadString());
                    break;
            }
            return;
        }
        public static void OnDungeonCompleted(long uid, string dungeonName)
        {
            Jotunn.Logger.LogInfo(uid + " finished dungeon " + dungeonName);

        }
        public static void OnDungeonFailed(long uid, string dungeonName)
        {
            Jotunn.Logger.LogInfo(uid + " failed dungeon " + dungeonName);
        }
        public static void OnReceivedDungeon(long uid, ZPackage zip)
        {
            Jotunn.Logger.LogInfo("Got dungeon from server");
            DynamicDungeons.DeserializeDungeon(zip);
        }
        public static void RPC_AcceptPoll(long uid)
        {
            accepteduids.Add(uid);
            Jotunn.Logger.LogInfo("Raid accepted:  " + uid);
            Vector3 randomLobbyPoint = Util.GetRandomPointInPlane(lobbyTLCorner, lobbyTRCorner, lobbyBRCorner, lobbyBLCorner);
            ZPackage pkg = new ZPackage();
            pkg.Write(randomLobbyPoint);
            ZRoutedRpc.instance.InvokeRoutedRPC(uid, "DynamicDungeons TeleportToCoords", pkg);
        }

        public static void RPC_DeclinePoll(long uid)
        {
            declineduids.Add(uid);
            Jotunn.Logger.LogInfo("Raid declined:  " + uid);
        }
        public static void ShowMessage(long uid, MessageHud.MessageType typeString, string msg)
        {
            Jotunn.Logger.LogInfo("Showing message");
            Player.m_localPlayer.Message(typeString, msg);
        }
        public static void PollPlayer(long sender)
        {
            Jotunn.Logger.LogInfo("Got dungeon poll");
            CreatePollWindow();
            return;
        }
        public static void AcceptPoll()
        {
            ZRoutedRpc.instance.InvokeRoutedRPC("DynamicDungeons AcceptPoll");
            ClosePollWindow();
        }
        public static void DeclinePoll()
        {
            ZRoutedRpc.instance.InvokeRoutedRPC("DynamicDungeons DeclinePoll");
            ClosePollWindow();
        }
        public static void TeleportToCoords(long sender, ZPackage pkg)
        {
            Vector3 pos = pkg.ReadVector3();
            Player.m_localPlayer.TeleportTo(pos, Quaternion.identity, true);
        }
        public static void ClosePollWindow()
        {
            pollPanel.SetActive(false);
            GUIManager.BlockInput(false);
        }
        private static void CreatePollWindow()
        {
            if (!pollPanel)
            {
                if (GUIManager.Instance == null)
                {
                    Jotunn.Logger.LogError("GUIManager instance is null");
                    return;
                }

                if (!GUIManager.CustomGUIFront)
                {
                    Jotunn.Logger.LogError("GUIManager CustomGUI is null");
                    return;
                }
                pollPanel = GUIManager.Instance.CreateWoodpanel(
                    parent: GUIManager.CustomGUIFront.transform,
                    anchorMin: new Vector2(0.5f, 0.5f),
                    anchorMax: new Vector2(0.5f, 0.5f),
                    position: new Vector2(0f, 0f),
                    width: 400f,
                    height: 300f,
                    draggable: true);
                pollPanel.SetActive(false);
                GUIManager.Instance.CreateText(
                    text: "El Calabozo de las Almas",
                    parent: pollPanel.transform,
                    anchorMin: new Vector2(0.5f, 1f),
                    anchorMax: new Vector2(0.5f, 1f),
                    position: new Vector2(0f, -100f),
                    font: GUIManager.Instance.AveriaSerifBold,
                    fontSize: 24,
                    color: GUIManager.Instance.ValheimOrange,
                    outline: true,
                    outlineColor: Color.black,
                    width: 400f,
                    height: 40f,
                    addContentSizeFitter: false);

                GUIManager.Instance.CreateText(
                     text: "Deseas participar de la raid?",
                     parent: pollPanel.transform,
                     anchorMin: new Vector2(0.5f, 1f),
                     anchorMax: new Vector2(0.5f, 1f),
                     position: new Vector2(0f, -50f),
                     font: GUIManager.Instance.AveriaSerifBold,
                     fontSize: 20,
                     color: GUIManager.Instance.ValheimOrange,
                     outline: true,
                     outlineColor: Color.black,
                     width: 450f,
                     height: 40f,
                     addContentSizeFitter: false);

                GameObject acceptButton = GUIManager.Instance.CreateButton(
                    text: "Aceptar",
                    parent: pollPanel.transform,
                    anchorMin: new Vector2(0.5f, 0.5f),
                    anchorMax: new Vector2(0.5f, 0.5f),
                    position: new Vector2(-50f, -0f),
                    width: 75f,
                    height: 60f);

                GameObject declineButton = GUIManager.Instance.CreateButton(
                   text: "Rechazar",
                   parent: pollPanel.transform,
                   anchorMin: new Vector2(0.5f, 0.5f),
                   anchorMax: new Vector2(0.5f, 0.5f),
                   position: new Vector2(50f, -0f),
                   width: 75f,
                   height: 60f);

                Button accButton = acceptButton.GetComponent<Button>();
                Button decButton = declineButton.GetComponent<Button>();
                accButton.onClick.AddListener(AcceptPoll);
                decButton.onClick.AddListener(DeclinePoll);
            }
            pollPanel.SetActive(true);
            GUIManager.BlockInput(true);
        }
        public static void ClientRemoveDrops(long uid)
        {
            ItemDrop[] items = UnityEngine.Object.FindObjectsOfType<ItemDrop>();
            foreach (ItemDrop item in items) GameObject.DestroyImmediate(item.gameObject);
        }
    }
}
