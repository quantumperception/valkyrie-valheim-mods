using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace ServerDevTools
{
    class PlayerTracker
    {
        public static string PlayerTrackerDir = Path.Combine(ServerDevTools.ConfigPath, "PlayerTracker");
        public static DateTime LastSave;
        public static List<string> SessionLogs = new List<string>();
        private static IEnumerator SavePlayerLogs()
        {
            while (true)
            {
                Debug.Log("Saving Player Logs");
                DateTime dt = DateTime.UtcNow;
                if (LastSave.Year != dt.Year || LastSave.Month != dt.Month || LastSave.Day != dt.Day) SessionLogs.Clear();
                foreach (ZNetPeer peer in ZNet.instance.GetPeers())
                {
                    string log = "";
                    ZDO zdo = ZDOMan.instance.GetZDO(peer.m_characterID);
                    if (zdo != null)
                    {
                        log += string.Format("{0}|{1}|{2}|{3}|{4}|{5}|{6}|{7}|{8}",
                            dt,
                            peer.m_playerName,
                            (int)peer.m_refPos.x,
                            (int)peer.m_refPos.y,
                            (int)peer.m_refPos.z,
                            (int)zdo.GetFloat("health", 0f),
                            (int)zdo.GetFloat("max_health", 0f),
                            peer.m_socket.GetHostName(),
                            zdo.GetLong("playerID", 0L));
                    }
                    Debug.Log("Saved player log:\n" + log);
                    SessionLogs.Add(log);
                }
                string logFilePath = GetLogFilePath();
                File.WriteAllLines(logFilePath, SessionLogs, System.Text.Encoding.UTF8);
                LastSave = DateTime.UtcNow;
                yield return new WaitForSeconds(120);
            }
        }

        public static void Initialize()
        {
            Debug.Log("Initializing Player Tracker");
            LastSave = DateTime.UtcNow;
            string logFilePath = GetLogFilePath();
            if (!File.Exists(logFilePath)) { FileStream createFs = File.Create(logFilePath); createFs.Close(); }
            SessionLogs = File.ReadAllLines(logFilePath).ToList();
        }
        private static string GetLogFilePath()
        {
            DateTime dt = DateTime.UtcNow;
            string month = dt.Month < 10 ? $"0{dt.Month}" : dt.Month.ToString();
            string day = dt.Day < 10 ? $"0{dt.Day}" : dt.Day.ToString();
            return Path.Combine(PlayerTrackerDir, $"{dt.Year}-{month}-{day}.log");
        }

        class Patches
        {
            [HarmonyPatch(typeof(ZNet), nameof(ZNet.Awake))]
            private static class ZNetAwakePatch
            {
                private static void Postfix(ZNet __instance)
                {
                    if (!Util.isServer()) return;
                    Game.instance.StartCoroutine(SavePlayerLogs());
                }
            }


        }
    }
}
