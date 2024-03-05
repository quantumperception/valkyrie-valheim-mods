using Jotunn.Entities;
using System.Collections;
using UnityEngine;

namespace ZonePermissions
{
    partial class ZonePermissions
    {
        public static readonly WaitForSeconds OneSecondWait = new WaitForSeconds(1f);
        public static readonly WaitForSeconds HalfSecondWait = new WaitForSeconds(0.5f);

        public static CustomRPC RPC_ZoneHandler;

        // React to the RPC call on a server
        private static IEnumerator SRPC_Null(long sender, ZPackage package)
        {
            yield return null;
        }


        // React to the RPC call on a client
        private static IEnumerator CRPC_ZoneHandler(long sender, ZPackage package)
        {
            Debug.Log("CRPC ZoneHandler");
            ZoneHandler.RPC(sender, package);
            yield return null;
        }
    }
}
