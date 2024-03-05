using BepInEx;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace Cannons
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.ClientMustHaveMod, VersionStrictness.Minor)]
    public partial class Cannons : BaseUnityPlugin
    {
        private readonly static Cannons instance;
        public const string PluginGUID = "com.valkyrie.cannons";
        public const string PluginName = "Cannons!";
        public const string PluginVersion = "0.0.1";
        public static CustomLocalization Localization;
        public static AssetBundle bundle;
        private static readonly string pluginPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        private static readonly string configPath = BepInEx.Paths.ConfigPath;
        private static Harmony harm = new Harmony("ValkyrieCannons");
        private static AudioSource sfxGroup;
        public static bool usingCannon = false;
        public static bool aiming = false;
        public static CustomPiece cannonDrakkar;
        public static float originalFov;
        public static float zoomFov = 30f;
        public static Vector3 playerHitPoint;
        public static Transform cameraTransform;
        public static GameObject salitre;
        public static GameObject cannonPrefab;
        public static GameObject cannonballPrefab;
        public static GameObject cannonFireSfx;
        public static GameObject cannonLoadSfx;
        public static GameObject cannonballHitSfx;
        public static GameObject cannonballProjectile;
        public static GameObject cannonballHitAoe;
        public static GameObject coordsText;
        public static bool showCoords;
        public static Vector3 playerPosition;
        private static bool IsServer
        {
            get
            {
                return SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;
            }
        }
        private void Awake()
        {
            bundle = AssetUtils.LoadAssetBundleFromResources("cannons");
            CreateConfigValues();
            SetupWatcher();
            LoadAssets();
            AddLocalization();
            //CommandManager.Instance.AddConsoleCommand(new ShowCoordsCommand());
            PrefabManager.OnVanillaPrefabsAvailable += OnVanillaPrefabsAvailable;
            //AddCannonPiece();
            harm.PatchAll();

        }

        private static void AddLocalization()
        {
            Localization = LocalizationManager.Instance.GetLocalization();
            Localization.AddTranslation("English", new Dictionary<string, string>
            {
                {"cannons_load_ammo", "Load cannonball" },
                {"item_cannonball", "Cannonball" },
                {"item_cannonball_desc", "A solid iron ball." },
                {"item_salitre", "Sulfur" },
                {"item_salitre_desc", "A component of gunpowder. Used to create Cannonballs." },
                {"piece_cannon", "Cannon" },
                {"piece_cannonship", "Cannon Drakkar" },
                {"$piece_cannonship_desc", "A drakkar equipped with 4 cannons." }
            });
            Localization.AddTranslation("Spanish", new Dictionary<string, string>
            {
                {"cannons_load_ammo", "Cargar munición" },
                {"item_cannonball", "Bola de Cañón" },
                {"item_cannonball_desc", "Una bola de hierro sólido." },
                {"item_salitre", "Salitre" },
                {"item_salitre_desc", "Uno de los ingredientes de la pólvora. Sirve para crear Bolas de Cañón." },
                {"piece_cannon", "Cañón" },
                {"piece_cannonship", "Drakkar de Cañón" },
                {"$piece_cannonship_desc", "Un Drakkar equipado con 4 cañones." }
            });
        }
        private static string GetPositionString()
        {
            return "Coords: " + playerPosition.x.ToString("F1") + "/" + playerPosition.y.ToString("F1") + "/" + playerPosition.z.ToString("F1");
        }
        public class ShowCoordsCommand : ConsoleCommand
        {
            public override string Name => "coords";

            public override string Help => "Toggles showing coordinates con top left corner";

            public override void Run(string[] args)
            {
                if (!showCoords)
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

                    // Create the text object
                    showCoords = true;
                    return;
                }
                showCoords = false;
                return;
            }
        }

        private static void setPosition(GameObject extensions, GameObject ship, bool rotate = false)
        {
            //extensions.transform.position = new Vector3(ship.transform.position.x - 1, ship.transform.position.y, ship.transform.position.z + 0.5f);
            //if (rotate) extensions.transform.rotation = ship.transform.rotation;
            extensions.transform.position = ship.transform.position;
            extensions.transform.parent = ship.transform;
            extensions.transform.localPosition = new Vector3(2f, 1, 0f);
            extensions.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            //if (rotate) extensions.transform.rotation = ship.transform.rotation;
        }

        private void LoadAssets()
        {
            salitre = bundle.LoadAsset<GameObject>("Salitre");
            cannonPrefab = bundle.LoadAsset<GameObject>("Cannon");
            cannonballPrefab = bundle.LoadAsset<GameObject>("Cannonball");
            cannonballHitAoe = bundle.LoadAsset<GameObject>("cannonball_aoe");
            cannonFireSfx = bundle.LoadAsset<GameObject>("sfx_cannon_fire");
            cannonLoadSfx = bundle.LoadAsset<GameObject>("sfx_cannon_load");
            cannonballHitSfx = bundle.LoadAsset<GameObject>("sfx_cannonball_hit");
            cannonballProjectile = bundle.LoadAsset<GameObject>("cannonball_projectile");
            PrefabManager.Instance.AddPrefab(cannonballProjectile);
            PrefabManager.Instance.AddPrefab(cannonFireSfx);
            PrefabManager.Instance.AddPrefab(cannonLoadSfx);
            PrefabManager.Instance.AddPrefab(cannonballHitSfx);
            cannonballHitAoe.GetComponent<Aoe>().m_damage = new HitData.DamageTypes { m_blunt = CannonballSplashDamage.Value };
            ZNetView cannonView = cannonPrefab.GetComponent<ZNetView>();
            if (cannonView == null) cannonView = cannonPrefab.AddComponent<ZNetView>();
            cannonView.m_persistent = true;
            cannonPrefab.AddComponent<ZSyncTransform>();
            Cannon cannon = cannonPrefab.AddComponent<Cannon>();
            Transform cannonTransform = cannonPrefab.transform.Find("cannon");
            cannon.barrel = cannonTransform.Find("barrel").gameObject;
            cannon.playerAttach = cannonTransform.Find("attach");
            cannon.projectilePrefab = cannonballProjectile;
            cannon.projectileSpawnPoint = cannon.barrel.transform.Find("BarrelTip");
        }
        private void OnVanillaPrefabsAvailable()
        {
            SetupSFX();
            AddSalitre();
            AddCannonball();
            AddCannonDrakkar();
            SetupDrops();
        }

        private void AddSalitre()
        {
            Sprite icon = RenderManager.Instance.Render(salitre, RenderManager.IsometricRotation);
            CustomItem salitreItem = new CustomItem(salitre, false, new ItemConfig
            {
                Enabled = false,
                Name = "$item_salitre",
                Description = "$item_salitre_desc",
                Icons = new Sprite[] { icon },
            });
            ItemManager.Instance.AddItem(salitreItem);
        }

        private void SetupDrops()
        {
            GameObject leviathan = PrefabManager.Cache.GetPrefab<GameObject>("Leviathan");
            MineRock mr = leviathan.GetComponent<MineRock>();
            DropTable.DropData newDrop = new DropTable.DropData();
            newDrop.m_item = PrefabManager.Instance.GetPrefab("Salitre");
            newDrop.m_stackMax = 1;
            newDrop.m_stackMin = 1;
            newDrop.m_weight = 1;
            newDrop.m_dontScale = false;
            mr.m_dropItems.m_drops.Add(newDrop);
        }

        private void SetupSFX()
        {
            sfxGroup = PrefabManager.Cache.GetPrefab<AudioSource>("sfx_arrow_hit");
            cannonFireSfx.GetComponentInChildren<AudioSource>().outputAudioMixerGroup = sfxGroup.outputAudioMixerGroup;
            cannonLoadSfx.GetComponentInChildren<AudioSource>().outputAudioMixerGroup = sfxGroup.outputAudioMixerGroup;
            cannonballHitSfx.GetComponentInChildren<AudioSource>().outputAudioMixerGroup = sfxGroup.outputAudioMixerGroup;
        }

        private void AddCannonball()
        {
            Sprite cannonballIcon = RenderManager.Instance.Render(cannonballPrefab, RenderManager.IsometricRotation);
            CustomItem cannonball = new CustomItem(cannonballPrefab, false, new ItemConfig
            {
                Name = "$item_cannonball",
                Description = "$item_cannonball_desc",
                CraftingStation = "forge",
                Icons = new Sprite[] { cannonballIcon },
                Weight = CannonballWeight.Value,
                StackSize = 20,
                Requirements = new RequirementConfig[]
                {
                    new RequirementConfig
                    {
                        Item = "Salitre",
                        Amount = CannonballSalitreCost.Value
                    },
                    new RequirementConfig
                    {
                        Item = "Coal",
                        Amount = CannonballCoalCost.Value
                    },
                    new RequirementConfig
                    {
                        Item = "Iron",
                        Amount = CannonballIronCost.Value
                    }
                }
            });
            cannonball.ItemDrop.m_itemData.m_shared.m_ammoType = "Cannon";
            CustomRecipe r5 = new CustomRecipe(new RecipeConfig()
            {
                Item = "Cannonball",
                Name = "Cannonballx5",
                Amount = 5,
                Requirements = new RequirementConfig[]
                {
                    new RequirementConfig
                    {
                        Item = "Salitre",
                        Amount = CannonballSalitreCost.Value * 5
                    },
                    new RequirementConfig
                    {
                        Item = "Coal",
                        Amount = CannonballCoalCost.Value * 5
                    },
                    new RequirementConfig
                    {
                        Item = "Iron",
                        Amount = CannonballIronCost.Value * 5
                    }
                }
            });
            ItemManager.Instance.AddRecipe(r5);
            ItemManager.Instance.AddItem(cannonball);
        }
        private static void AddCannonPiece()
        {
            Sprite cannonIcon = RenderManager.Instance.Render(cannonPrefab, RenderManager.IsometricRotation);
            GameObject _cannonPrefab = cannonPrefab;
            _cannonPrefab.AddComponent<Piece>();
            CustomPiece cannon = new CustomPiece(_cannonPrefab, false, new PieceConfig
            {
                Name = "Cañón",
                PieceTable = "Hammer",
                Category = "Misc",
                Icon = cannonIcon,
                Requirements = new RequirementConfig[]
                {
                    new RequirementConfig
                    {
                        Item = "Wood",
                        Amount= 2,
                        Recover = true,
                    }
                }
            });
            cannon.PiecePrefab.name = "piece_cannon";
            PieceManager.Instance.AddPiece(cannon);

        }

        private static void AddCannonDrakkar()
        {
            GameObject vikingShip = PrefabManager.Cache.GetPrefab<GameObject>("VikingShip");
            GameObject drakkarPrefab = bundle.LoadAsset<GameObject>("CannonShip");
            Transform watermask = drakkarPrefab.transform.Find("ship").Find("visual").Find("watermask");
            Transform originalWatermask = vikingShip.transform.Find("ship").Find("visual").Find("watermask");
            Transform watereffects = drakkarPrefab.transform.Find("watereffects");
            Transform originalWatereffects = vikingShip.transform.Find("watereffects");
            Transform splashEffects = watereffects.Find("splash_effects");
            Transform originalSplashEffects = originalWatereffects.Find("splash_effects");
            Transform speedWake = watereffects.Find("SpeedWake");
            Transform originalSpeedWake = originalWatereffects.Find("SpeedWake");
            foreach (ParticleSystemRenderer renderer in speedWake.GetComponentsInChildren<ParticleSystemRenderer>())
            {
                if (renderer.transform.name == "Trail")
                {
                    renderer.material = originalSpeedWake.Find("Trail").GetComponentInChildren<ParticleSystemRenderer>().material;
                    continue;
                }
                renderer.material = originalSpeedWake.Find("rudder").GetComponentInChildren<ParticleSystemRenderer>().material;
            }
            foreach (WaterTrigger trigger in splashEffects.GetComponentsInChildren<WaterTrigger>())
            {
                trigger.m_effects.m_effectPrefabs = originalSplashEffects.GetComponentInChildren<WaterTrigger>().m_effects.m_effectPrefabs;
            }
            watereffects.Find("WaterSurface").GetComponentInChildren<ParticleSystemRenderer>().material = originalWatereffects.Find("WaterSurface").GetComponentInChildren<ParticleSystemRenderer>().material;
            vikingShip.transform.Find("watereffects").localPosition = new Vector3(0f, -0.7999992f, 0f);
            watermask.GetComponent<MeshRenderer>().materials = originalWatermask.GetComponent<MeshRenderer>().materials;
            Transform interactive = drakkarPrefab.transform.Find("interactive");
            Transform cannons = interactive.Find("cannons");
            for (int i = 0; i < cannons.childCount; i++)
            {
                Transform prefab = cannons.GetChild(i);
                Cannon cannon = prefab.gameObject.AddComponent<Cannon>();
                Transform cannonTransform = prefab.Find("cannon");
                cannon.barrel = cannonTransform.Find("barrel").gameObject;
                cannon.playerAttach = prefab.Find("attach");
                cannon.projectilePrefab = cannonballProjectile;
                cannon.projectileSpawnPoint = cannon.barrel.transform.Find("BarrelTip");
            }
            cannonDrakkar = new CustomPiece(drakkarPrefab, true, new PieceConfig
            {
                Name = "$piece_cannonship",
                Description = "$piece_cannonship_desc",
                Icon = RenderManager.Instance.Render(drakkarPrefab, RenderManager.IsometricRotation),
                Category = "Misc",
                PieceTable = "Hammer",
                Requirements = new RequirementConfig[]
                        {
                            new RequirementConfig
                            {
                                Item = "IronNails",
                                Amount= 100,
                                Recover = true,
                            },
                            new RequirementConfig
                            {
                                Item = "DeerHide",
                                Amount= 10,
                                Recover = true,
                            },
                            new RequirementConfig
                            {
                                Item = "FineWood",
                                Amount= CannonShipFinewoodCost.Value,
                                Recover = true,
                            },
                            new RequirementConfig
                            {
                                Item = "ElderBark",
                                Amount= CannonShipAncientBarkCost.Value,
                                Recover = true,
                            },
                            new RequirementConfig
                            {
                                Item = "Iron",
                                Amount= CannonShipIronCost.Value,
                                Recover = true,
                            },
                        }
            }
                );
            PieceManager.Instance.AddPiece(cannonDrakkar);
        }


    }
}
