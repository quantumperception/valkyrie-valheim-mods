using HarmonyLib;
using Jotunn.Managers;
using UnityEngine;
namespace Cannons
{
    partial class Cannons
    {
        [HarmonyPatch]
        public static class GameCameraPatch
        {
            [HarmonyPatch(typeof(GameCamera), "GetCameraPosition")]
            [HarmonyPriority(100)]
            [HarmonyPostfix]
            private static void GetCameraPosition_Postfix(GameCamera __instance, float dt, Vector3 pos, Quaternion rot)
            {
                if (__instance != null && originalFov == 0f)
                {
                    originalFov = __instance.m_fov;
                }
            }

            [HarmonyPriority(0)]
            [HarmonyPrefix]
            [HarmonyPatch(typeof(GameCamera), "UpdateCamera")]
            public static void UpdateCamera_Prefix(GameCamera __instance)
            {
                if (usingCannon)
                {
                    cameraTransform = __instance.transform;
                    if (Input.GetKey(KeyCode.LeftShift)) __instance.m_fov = zoomFov;
                    if (Input.GetKeyUp(KeyCode.LeftShift)) __instance.m_fov = originalFov;
                }
            }
        }
        [HarmonyPatch]
        public static class ZNetScenePatches 
        {
            [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
            private static class ZNetSceneAwakePatch
            {
                private static void Prefix(ZNetScene __instance)
                {
                    AddCannonPiece();
                    Jotunn.Logger.LogInfo("Added cannon");
                    GameObject drakkarPrefab = __instance.m_prefabs.Find((GameObject x) => x.name == "VikingShip");

                    Jotunn.Logger.LogInfo("Got drakkar prefab");
                    AddCannonDrakkar(drakkarPrefab);
                    Jotunn.Logger.LogInfo("Added cannon drakkar");
                }
            }
        }
        public static class PlayerPatches
        {
            [HarmonyPatch(typeof(Player), "UpdatePlacementGhost")]
            [HarmonyPostfix]
            private static void UpdatePlacementGhostPatch(Player __instance)
            {
                bool localPlayer = !Player.m_localPlayer || Player.m_localPlayer != __instance;
                if (!localPlayer) return;
                GameObject placementGhost = __instance.m_placementGhost;
                Piece piece = (placementGhost != null) ? placementGhost.GetComponent<Piece>() : null;
                if (!piece) return;
                bool isCannon = Utils.GetPrefabName(piece.gameObject) == "piece_cannon";
                if (!isCannon) return;
                bool isAboveWater = piece.transform.position.y < ZoneSystem.instance.m_waterLevel;
                if (!isAboveWater)
                {
                    __instance.m_placementStatus = Player.PlacementStatus.Invalid;
                    __instance.SetPlacementGhostValid(false);
                }
                 else
                {
                    __instance.m_placementStatus = Player.PlacementStatus.Valid;
                    __instance.SetPlacementGhostValid(true);
                }
            }
            [HarmonyPatch(typeof(Player), nameof(Player.Update))]
            private static class PlayerUpdatePatch
            {
                private static void Postfix()
                {
                    if (Player.m_localPlayer && showCoords)
                    {
                        if (!coordsText)
                        {
                            playerPosition = Player.m_localPlayer.transform.position;
                            DestroyImmediate(coordsText);
                            coordsText = GUIManager.Instance.CreateText(
                            text: GetPositionString(),
                            parent: GUIManager.CustomGUIFront.transform,
                            anchorMin: new Vector2(0.5f, 1f),
                            anchorMax: new Vector2(0.5f, 1f),
                            position: new Vector2(0f, -50f),
                            font: GUIManager.Instance.AveriaSerifBold,
                            fontSize: 18,
                            color: Color.white,
                            outline: false,
                            outlineColor: Color.white,
                            width: 350f,
                            height: 40f,
                            addContentSizeFitter: false);
                            return;
                        }
                    }
                    coordsText = null;
                    return;
                }
            }
            [HarmonyPatch(typeof(Player), nameof(Player.PlayerAttackInput))]
            private static class PlayerAttackInputPatch
            {
                private static bool Prefix()
                {
                    if (usingCannon) return false;
                    return true;
                }
            }
            [HarmonyPatch(typeof(Character), nameof(Character.Damage))]
            private static class CharacterDamagePatch
            {
                private static bool Prefix()
                {
                    bool badConnection = (ZNet.instance != null && ZNet.instance.HasBadConnection() && Mathf.Sin(Time.time * 10f) > 0f);
                    if (badConnection) return false;
                    return true;
                }
            }
        }
    }
}
