using BepInEx;
using HarmonyLib;
using Jotunn.Utils;
using System;

namespace ValkyriePVEP
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.NotEnforced, VersionStrictness.Minor)]
    public partial class PVEP : BaseUnityPlugin
    {
        public const string PluginGUID = "com.valkyrie.pvep";
        public const string PluginName = "ValkyriePVEP";
        public const string PluginVersion = "0.0.1";
        private Harmony harm = new Harmony("ValkyriePVEP");
        private DateTime pvpCooldown;

        private void Awake()
        {
            harm.PatchAll();
        }

        [HarmonyPatch(typeof(ZNet), nameof(ZNet.Start))]
        private static class ZNetStartPatch
        {
            private static void Postfix(ZNet __instance)
            {
                if (Util.IsServer())
                {
                    Jotunn.Logger.LogInfo("Registering Server RPCs");
                }
                if (!Util.IsServer())
                {
                    Jotunn.Logger.LogInfo("Registering Client RPCs");
                }
            }
        }
        [HarmonyPatch(typeof(Player), nameof(Player.OnDeath))]
        public static class PlayerOnDeathPatch
        {
            private static bool Prefix(Player __instance)
            {

                if (!InventoryGui.instance.m_pvp.isOn)
                {
                    __instance.m_nview.GetZDO().Set("dead", value: true);
                    __instance.m_nview.InvokeRPC(ZNetView.Everybody, "OnDeath");
                    Game.instance.GetPlayerProfile().m_playerStats.m_deaths++;
                    Game.instance.RequestRespawn(8); // Respawn timer
                    __instance.m_timeSinceDeath = 0f;
                    __instance.Message(MessageHud.MessageType.TopLeft, "PVEP: Your items have been preserved!");
                    return false;
                }

                return true;
            }
        }
    }
}
