using UnityEngine;
using UnityEngine.Rendering;
using System.Reflection;

namespace ServerDevTools
{
    class Util
    {
        public static BindingFlags NonPublicFlags = BindingFlags.NonPublic | BindingFlags.Instance;
        public static bool isServer()
        {
            return SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;
        }
        public static bool isAdmin(long sender)
        {
            ZNetPeer peer = ZNet.instance.GetPeer(sender);
            string SteamID = sender.ToString();
            if (
                ZNet.instance.m_adminList != null &&
                ZNet.instance.m_adminList.Contains(SteamID)
            )
                return true;
            else
            {
                return false;
            }
        }
    }
}
