using BepInEx;
using HarmonyLib;
using Jotunn.Utils;
using System.IO;

namespace ServerDevTools
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.ClientMustHaveMod, VersionStrictness.Minor)]
    public partial class ServerDevTools : BaseUnityPlugin
    {
        private readonly static ServerDevTools instance;
        public const string PluginGUID = "com.valkyrie.serverdevtools";
        public const string PluginName = "ServerDevTools";
        public const string PluginVersion = "0.1.0";
        public static string ConfigPath = Path.Combine(BepInEx.Paths.ConfigPath, PluginName);
        public static Harmony harm = new Harmony(PluginName);


        private void Awake()
        {
            SetupFiles();
            SetupConfigWatcher();
            PlayerTracker.Initialize();
            harm.PatchAll();
        }

        private static void AddServerRPC()
        {

        }

        private static void AddClientRPC()
        {

        }

        private void SetupFiles()
        {
            if (!Directory.Exists(ConfigPath)) Directory.CreateDirectory(ConfigPath);
            if (!Directory.Exists(PlayerTracker.PlayerTrackerDir)) Directory.CreateDirectory(PlayerTracker.PlayerTrackerDir);
        }

        [HarmonyPatch(typeof(ZNet), nameof(ZNet.Awake))]
        private static class ZNetAwakePatch
        {
            private static void Postfix(ZNet __instance)
            {
                if (Util.isServer())
                {
                    AddServerRPC();
                }
                else
                {
                    AddClientRPC();
                }
            }
        }
    }
}
