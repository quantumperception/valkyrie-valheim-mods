using BepInEx;
using BepInEx.Configuration;
using System.IO;

namespace ValkyrieArmors
{
    public partial class ValkyrieArmors
    {
        private static string ConfigFileName = PluginGUID + ".cfg";
        private static string ConfigFileFullPath = BepInEx.Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;

        public static ConfigEntry<string> LeatherSetColor;
        public static ConfigEntry<string> BronzeSetColor;
        public static ConfigEntry<string> IronSetColor;
        public static ConfigEntry<string> SilverSetColor;
        public static ConfigEntry<string> BlackmetalSetColor;
        public static ConfigEntry<string> CarapaceSetColor;
        public static ConfigEntry<string> FlametalSetColor;

        private void SetupWatcher()
        {
            FileSystemWatcher watcher = new FileSystemWatcher(BepInEx.Paths.ConfigPath, ConfigFileName);
            watcher.Changed += ReadConfigValues;
            watcher.Created += ReadConfigValues;
            watcher.Renamed += ReadConfigValues;
            watcher.IncludeSubdirectories = true;
            watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            watcher.EnableRaisingEvents = true;
        }

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

        private void CreateConfigValues()
        {
            Config.SaveOnConfigSet = true;
            ConfigurationManagerAttributes isAdminOnly = new ConfigurationManagerAttributes { IsAdminOnly = true };

            LeatherSetColor = Config.Bind("Set Color", "Leather Set Color", "753716", new ConfigDescription("Color of the Leather Armor Sets.", null, isAdminOnly));
            BronzeSetColor = Config.Bind("Set Color", "Bronze Set Color", "f5e614", new ConfigDescription("Color of the Bronze Armor Sets.", null, isAdminOnly));
            IronSetColor = Config.Bind("Set Color", "Iron Set Color", "0c18c4", new ConfigDescription("Color of the Iron Armor Sets.", null, isAdminOnly));
            SilverSetColor = Config.Bind("Set Color", "Silver Set Color", "1cc758", new ConfigDescription("Color of the Silver Armor Sets.", null, isAdminOnly));
            BlackmetalSetColor = Config.Bind("Set Color", "Blackmetal Set Color", "dba009", new ConfigDescription("Color of the Blackmetal Armor Sets.", null, isAdminOnly));
            CarapaceSetColor = Config.Bind("Set Color", "Carapace Set Color", "fc0f0f", new ConfigDescription("Color of the Carapace Armor Sets.", null, isAdminOnly));
            FlametalSetColor = Config.Bind("Set Color", "Flametal Set Color", "7c10a3", new ConfigDescription("Color of the Flametal Armor Sets.", null, isAdminOnly));
       
        }
    }
}