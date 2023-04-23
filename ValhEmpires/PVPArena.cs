using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System.Collections.Generic;
using UnityEngine;

namespace ValkyrieUtils
{
    internal class PVPArena
    {
        static PVPArena instance;
        static string arenaName;
        static ArenaMode arenaType;
        static CombatRules arenaRules;
        static List<ArenaPlayer> players;
        static List<long> lobbyPlayers;
        static bool active;
        enum CombatRules
        {
            NAKED,
            MELEE_ONLY,
            RANGE_ONLY,
            MAGIC_ONLY,
            NO_RULES
        }
        enum ArenaMode
        {
            ONE_ON_ONE,
            TWO_ON_TWO,
            THREE_ON_THREE,
            FOUR_ON_FOUR,
            FIVE_ON_FIVE,
            CAPTURE_THE_FLAG,
            BATTLE_ROYALE,
            DEATHMATCH,
        }
        class ArenaPlayer
        {
            string name;
            string steamId;
            int kills;
            int deaths;
            int assists;
            KeyValuePair<string, int> killList;
            public ArenaPlayer(string _name, string _steamId, int _kills, int _deaths, int _assists)
            {
                name = _name;
                steamId = _steamId;
                kills = _kills;
                deaths = _deaths;
                assists = _assists;
                killList = new KeyValuePair<string, int>();
            }

        }

        class LobbyPlatform : MonoBehaviour
        {
            public void OnTriggerEnter(Collider other)
            {
                if (other.CompareTag("Player")) ZRoutedRpc.instance.InvokeRoutedRPC("ValkyrieUtils EnterArenaLobby", null);
            }
            public void OnTriggerExit(Collider other)
            {
                if (other.CompareTag("Player")) ZRoutedRpc.instance.InvokeRoutedRPC("ValkyrieUtils EnterArenaLobby", null);
            }
        }


        public static void LoadArenaPrefabs()
        {
            GameObject arenaPrefab = ValkyrieUtils.bundle.LoadAsset<GameObject>("Arena");
            GameObject lobbyPlatform = ValkyrieUtils.bundle.LoadAsset<GameObject>("LobbyPlatform");
            lobbyPlatform.AddComponent<LobbyPlatform>();
            PieceConfig platform = new PieceConfig();
            platform.Name = "PVP Platform";
            platform.PieceTable = "Hammer";
            platform.Category = "Misc";
            platform.AddRequirement(new RequirementConfig("Wood", 2, 0, true));
            CustomPiece platformPiece = new CustomPiece(lobbyPlatform, false, platform);
            PieceManager.Instance.AddPiece(platformPiece);
        }

        public static void OnEnterArenaLobby(long steamId, string cat, string msg)
        {
            Jotunn.Logger.LogInfo("Added " + steamId + " to lobby");
            lobbyPlayers.Add(steamId);
        }
        public static void OnLeaveArenaLobby(long steamId, string cat, string msg)
        {
            Jotunn.Logger.LogInfo("Removed " + steamId + " from lobby");
            lobbyPlayers.RemoveAll(item => item == steamId);
        }

        void GenerateArena()
        {
        }
    }


}
