using BepInEx;
using System.IO;

namespace ServerDevTools
{
    public partial class ServerDevTools
    {
        private static string ConfigFileName = PluginGUID + ".cfg";
        private static string ConfigFileFullPath = BepInEx.Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;

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
        }
    }
}
