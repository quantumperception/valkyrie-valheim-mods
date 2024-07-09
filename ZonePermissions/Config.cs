using BepInEx;
using BepInEx.Configuration;
using System;
using System.IO;
using UnityEngine;

namespace ZonePermissions
{
    public partial class ZonePermissions
    {
        private static string ConfigFileName = PluginGUID + ".cfg";
        private static string ConfigFileFullPath = BepInEx.Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;

        public static ConfigEntry<string> ZonePath;
        public static ConfigEntry<int> NexusID;
        public static ConfigEntry<bool> BiomePVPAnnouncement;
        public static ConfigEntry<bool> NoItemLoss;
        public static ConfigEntry<Single> RespawnTimer;
        public static ConfigEntry<string> PVPColor;
        public static ConfigEntry<string> PVEColor;
        public static ConfigEntry<string> NonEnforcedColor;
        public static ConfigEntry<bool> WardProtectItemDrop;
        public static ConfigEntry<bool> WardProtectItemPickup;
        public static ConfigEntry<bool> WardProtectDamage;
        public static ConfigEntry<bool> ReloadDetection;

        private void ReadConfigValues(object sender, FileSystemEventArgs e)
        {
            if (!File.Exists(ConfigFileFullPath)) return;
            try
            {
                Jotunn.Logger.LogDebug("Attempting to reload configuration...");
                Config.Reload();
            }
            catch
            {
                Jotunn.Logger.LogError($"There was an issue loading {ConfigFileName}");
            }
        }
        private void SetupConfigWatcher()
        {
            FileSystemWatcher watcher = new FileSystemWatcher(BepInEx.Paths.ConfigPath, ConfigFileName);
            watcher.Changed += ReadConfigValues;
            watcher.Created += ReadConfigValues;
            watcher.Renamed += ReadConfigValues;
            watcher.IncludeSubdirectories = true;
            watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            watcher.EnableRaisingEvents = true;
        }

        private void CreateConfigValues()
        {
            Config.SaveOnConfigSet = true;
            ConfigurationManagerAttributes isAdminOnly = new ConfigurationManagerAttributes { IsAdminOnly = true };
            if (Util.isServer())
            {
                ReloadDetection = base.Config.Bind<bool>("Config", "ReloadDetection", false, "SERVER ONLY: Should the server auto reload if the config file is changed? (May cause DeSync)");
                NoItemLoss = base.Config.Bind<bool>("Death", "NoItemLoss", false, "SERVER ONLY: Should we prevent a user from losing items/skills on death globally?");
                RespawnTimer = base.Config.Bind<Single>("Death", "RespawnTimer", 10, "SERVER ONLY: How fast should the clients respawn?");
                ZonePath = base.Config.Bind<string>("WorldofValheimZones", "ZonePath", DefaultZonePath, "SERVER ONLY: The file path to the zone file. If it does not exist, it will be created with a default zone.");
                WardProtectDamage = base.Config.Bind<bool>("Ward", "Building_ProtectDamage", false, "SERVER ONLY: Protect buildings from being damaged inside Warded Areas?");
                WardProtectItemPickup = base.Config.Bind<bool>("Ward", "Item_Pickup", false, "SERVER ONLY: Protect Picking up items in Warded Areas?");
                WardProtectItemDrop = base.Config.Bind<bool>("Ward", "Item_Drop", false, "SERVER ONLY: Protect Dropping items in Warded Areas?");
            }
            else
            {
                Debug.Log("[ZonePermissions - Client Mode]");
                BiomePVPAnnouncement = Config.Bind<bool>("Biome", "BiomePVPAnnouncement", true, "Should we announce changing PVP in a Biome Announcement? true or false");
                PVPColor = Config.Bind<string>("Colors", "PVPColor", "Red", "What color should our 'Now Entering' message be if the zone type has PVP on");
                PVEColor = Config.Bind<string>("Colors", "PVEColor", "White", "What color should our 'Now Entering' message be if the zone type has PVE off");
                NonEnforcedColor = Config.Bind<string>("Colors", "NonEnforcedColor", "Yellow", "What color should our 'Now Entering' message be if the zone type has No PVP Enforcement");
            }
        }
    }
}
