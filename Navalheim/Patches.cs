using HarmonyLib;
using Jotunn.Managers;
using UnityEngine;
namespace Cannons
{
    public partial class Cannons
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
        
        public static class PlayerPatches
        {
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
