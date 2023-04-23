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
        public static AssetBundle bundle;
        private static readonly string pluginPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        private static readonly string configPath = Path.GetFullPath(Path.Combine(pluginPath, @"..\", "config"));
        private static Harmony harm = new Harmony("ValkyrieCannons");
        public static bool usingCannon = false;
        public static bool aiming = false;
        public static CustomPiece cannonDrakkar;
        public static float originalFov;
        public static float zoomFov = 30f;
        public static Vector3 playerHitPoint;
        public static Transform cameraTransform;
        public static GameObject cannonPrefab;
        public static GameObject cannonballPrefab;
        public static GameObject cannonFireSfx;
        public static GameObject cannonLoadSfx;
        public static GameObject cannonballProjectile;
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
            LoadAssets();
            CommandManager.Instance.AddConsoleCommand(new ShowCoordsCommand());
            PrefabManager.OnVanillaPrefabsAvailable += AddCannonball;
            harm.PatchAll();

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
            cannonPrefab = bundle.LoadAsset<GameObject>("Cannon");
            cannonballPrefab = bundle.LoadAsset<GameObject>("Cannonball");
            cannonFireSfx = bundle.LoadAsset<GameObject>("sfx_cannon_fire");
            cannonLoadSfx = bundle.LoadAsset<GameObject>("sfx_cannon_load");
            cannonballProjectile = bundle.LoadAsset<GameObject>("cannonball_projectile");
            PrefabManager.Instance.AddPrefab(cannonballProjectile);
            PrefabManager.Instance.AddPrefab(cannonFireSfx);
            PrefabManager.Instance.AddPrefab(cannonLoadSfx);
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
        private void AddCannonball()
        {
            Sprite cannonballIcon = RenderManager.Instance.Render(cannonballPrefab, RenderManager.IsometricRotation);
            CustomItem cannonball = new CustomItem(cannonballPrefab, false, new ItemConfig
            {
                CraftingStation = "forge",
                Icons = new Sprite[] { cannonballIcon },
                Requirements = new RequirementConfig[]
                {
                    new RequirementConfig
                    {
                        Item = "Wood",
                        Amount = 3
                    }
                }
            });
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

        private static void AddCannonDrakkar(GameObject drakkarPrefab)
        {
            GameObject _cannonPrefab = cannonPrefab;
            DestroyImmediate(_cannonPrefab.GetComponent<Piece>());
            Cannon cannon = _cannonPrefab.AddComponent<Cannon>();
            Transform cannonTransform = _cannonPrefab.gameObject.transform.Find("cannon");
            cannon.barrel = cannonTransform.Find("barrel").gameObject;
            cannon.playerAttach = cannonTransform.Find("attach");
            cannon.projectilePrefab = cannonballPrefab;
            cannon.projectileSpawnPoint = cannon.barrel.transform.Find("BarrelTip");
            setPosition(_cannonPrefab, drakkarPrefab);
            cannonDrakkar = new CustomPiece(drakkarPrefab, false, new PieceConfig
            {
                Name = "CannonWarship",
                Icon = drakkarPrefab.GetComponent<Piece>().m_icon,
                Category = "Misc",
                PieceTable = "Hammer",
                Requirements = new RequirementConfig[]
                        {
                            new RequirementConfig
                            {
                                Item = "Wood",
                                Amount= 2,
                                Recover = true,
                            }
                        }
            }
                );

            cannonDrakkar.PiecePrefab.name = "CannonWarship";
            PieceManager.Instance.AddPiece(cannonDrakkar);
        }


    }
}
