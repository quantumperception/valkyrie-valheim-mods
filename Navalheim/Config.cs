using BepInEx;
using BepInEx.Configuration;
using Jotunn.Managers;
using Jotunn.Utils;
using System;
using System.IO;

namespace Cannons
{
    public partial class Cannons
    {
        private static string ConfigFileName = PluginGUID + ".cfg";
        private static string ConfigFileFullPath = BepInEx.Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;

        public static ConfigEntry<int> CannonShipIronCost;
        public static ConfigEntry<int> CannonShipFinewoodCost;
        public static ConfigEntry<int> CannonShipAncientBarkCost;
        public static ConfigEntry<int> CannonballSalitreCost;
        public static ConfigEntry<float> CannonballWeight;
        public static ConfigEntry<float> CannonballProjectileBluntDamage;
        public static ConfigEntry<float> CannonballProjectileChopDamage;
        public static ConfigEntry<float> CannonballSplashDamage;
        public static ConfigEntry<int> CannonballIronCost;
        public static ConfigEntry<int> CannonballCoalCost;
        public static ConfigEntry<int> SalitreMinYield;
        public static ConfigEntry<int> SalitreMaxYield;

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
            AcceptableValueRange<float> floatRange = new AcceptableValueRange<float>(0f, 1000f);

            CannonballWeight = Config.Bind("Cannonball", "Item Weight", 10f, new ConfigDescription("Weight of the cannonball item.", floatRange, isAdminOnly));
            CannonballWeight.SettingChanged += delegate (object obj, EventArgs args)
            {
                ItemManager.Instance.GetItem("Cannonball").ItemDrop.m_itemData.m_shared.m_weight = CannonballWeight.Value;
            };
            CannonballProjectileBluntDamage = Config.Bind("Cannonball", "Projectile Blunt Damage", 130f, new ConfigDescription("Blunt damage for the cannonball projectile impact.", floatRange, isAdminOnly));
            CannonballProjectileBluntDamage.SettingChanged += delegate (object obj, EventArgs args)
            {
                ItemManager.Instance.GetItem("Cannonball").ItemDrop.m_itemData.m_shared.m_damages.m_blunt = CannonballProjectileBluntDamage.Value;
            };
            CannonballProjectileChopDamage = Config.Bind("Cannonball", "Projectile Chop Damage", 40f, new ConfigDescription("Chop damage for the cannonball projectile impact.", floatRange, isAdminOnly));
            CannonballProjectileChopDamage.SettingChanged += delegate (object obj, EventArgs args)
            {
                ItemManager.Instance.GetItem("Cannonball").ItemDrop.m_itemData.m_shared.m_damages.m_chop = CannonballProjectileChopDamage.Value;
            };
            CannonballSplashDamage = Config.Bind("Cannonball", "Splash Blunt Damage", 70f, new ConfigDescription("Blunt damage for the cannonball splash damage.", floatRange, isAdminOnly));
            CannonballSplashDamage.SettingChanged += delegate (object obj, EventArgs args)
            {
                cannonballHitAoe.GetComponent<Aoe>().m_damage = new HitData.DamageTypes { m_blunt = CannonballSplashDamage.Value };
            };


            CannonShipIronCost = Config.Bind("Cannon Drakkar", "Iron Cost", 40, new ConfigDescription("Iron cost for the Cannon Drakkar.", null, isAdminOnly));
            CannonShipIronCost.SettingChanged += delegate (object obj, EventArgs args)
            {
                foreach (Piece.Requirement req in PieceManager.Instance.GetPiece("CannonShip").Piece.m_resources)
                {
                    if (req.m_resItem.gameObject.name != "Iron") continue;
                    req.m_amount = CannonShipIronCost.Value;
                }
            };

            CannonShipFinewoodCost = Config.Bind("Cannon Drakkar", "Finewood Cost", 50, new ConfigDescription("Finewood cost for the Cannon Drakkar.", null, isAdminOnly));
            CannonShipFinewoodCost.SettingChanged += delegate (object obj, EventArgs args)
            {
                foreach (Piece.Requirement req in PieceManager.Instance.GetPiece("CannonShip").Piece.m_resources)
                {
                    if (req.m_resItem.gameObject.name != "FineWood") continue;
                    req.m_amount = CannonShipFinewoodCost.Value;
                }
            };

            CannonShipAncientBarkCost = Config.Bind("Cannon Drakkar", "Ancient Bark Cost", 50, new ConfigDescription("Ancient Bark cost for the Cannon Drakkar.", null, isAdminOnly));
            CannonShipAncientBarkCost.SettingChanged += delegate (object obj, EventArgs args)
            {
                foreach (Piece.Requirement req in PieceManager.Instance.GetPiece("CannonShip").Piece.m_resources)
                {
                    if (req.m_resItem.gameObject.name != "ElderBark") continue;
                    req.m_amount = CannonShipAncientBarkCost.Value;
                }
            };

            CannonballSalitreCost = Config.Bind("Cannonball", "Salitre Cost", 1, new ConfigDescription("Salitre cost for the cannonball item.", null, isAdminOnly));
            CannonballSalitreCost.SettingChanged += delegate (object obj, EventArgs args)
            {
                foreach (Piece.Requirement req in ItemManager.Instance.GetRecipe("Cannonball").Recipe.m_resources)
                {
                    if (req.m_resItem.gameObject.name != "Salitre") continue;
                    req.m_amount = CannonballSalitreCost.Value;
                }
                foreach (Piece.Requirement req in ItemManager.Instance.GetRecipe("Cannonballx5").Recipe.m_resources)
                {
                    if (req.m_resItem.gameObject.name != "Salitre") continue;
                    req.m_amount = CannonballSalitreCost.Value * 5;
                }
            };

            CannonballIronCost = Config.Bind("Cannonball", "Iron Cost", 1, new ConfigDescription("Iron cost for the cannonball item.", null, isAdminOnly));
            CannonballIronCost.SettingChanged += delegate (object obj, EventArgs args)
            {
                foreach (Piece.Requirement req in ItemManager.Instance.GetRecipe("Cannonball").Recipe.m_resources)
                {
                    if (req.m_resItem.gameObject.name != "Iron") continue;
                    req.m_amount = CannonballIronCost.Value;
                }
                foreach (Piece.Requirement req in ItemManager.Instance.GetRecipe("Cannonballx5").Recipe.m_resources)
                {
                    if (req.m_resItem.gameObject.name != "Iron") continue;
                    req.m_amount = CannonballIronCost.Value * 5;
                }
            };

            CannonballCoalCost = Config.Bind("Cannonball", "Coal Cost", 1, new ConfigDescription("Coal cost for the cannonball item.", null, isAdminOnly));
            CannonballCoalCost.SettingChanged += delegate (object obj, EventArgs args)
            {
                foreach (Piece.Requirement req in ItemManager.Instance.GetRecipe("Cannonball").Recipe.m_resources)
                {
                    if (req.m_resItem.gameObject.name != "Coal") continue;
                    req.m_amount = CannonballCoalCost.Value;
                }
                foreach (Piece.Requirement req in ItemManager.Instance.GetRecipe("Cannonballx5").Recipe.m_resources)
                {
                    if (req.m_resItem.gameObject.name != "Coal") continue;
                    req.m_amount = CannonballCoalCost.Value * 5;
                }
            };
           
            SynchronizationManager.OnConfigurationSynchronized += delegate (object obj, ConfigurationSynchronizationEventArgs attr)
            {
                if (attr.InitialSynchronization)
                {
                    Logger.LogMessage((object)"Initial Config sync event received for Cannons");
                }
                else
                {
                    Logger.LogMessage((object)"Config sync event received for Cannons");
                }
            };
        }
    }
}
