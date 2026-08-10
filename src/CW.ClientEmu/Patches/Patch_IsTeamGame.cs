using System.Reflection;
using HarmonyLib;

namespace CW.ClientEmu.Patches
{
    [HarmonyPatch]
    internal static class Patch_IsTeamGame
    {
        private static MethodInfo _getIsHost, _getServerGame, _getClientGame;

        private static MethodBase TargetMethod()
        {
            var peer = AccessTools.TypeByName("Peer");
            _getIsHost = AccessTools.PropertyGetter(peer, "IsHost");
            _getServerGame = AccessTools.PropertyGetter(peer, "ServerGame");
            _getClientGame = AccessTools.PropertyGetter(peer, "ClientGame");
            return AccessTools.PropertyGetter(AccessTools.TypeByName("Main"), "IsTeamGame");
        }

        private static bool Prefix(ref bool __result)
        {
            try
            {
                bool isHost = (bool)_getIsHost.Invoke(null, null);
                var game = isHost ? _getServerGame.Invoke(null, null) : _getClientGame.Invoke(null, null);
                if (game != null) return true;
            }
            catch { return true; }

            __result = false;
            return false;
        }
    }
}
