using System.Reflection;
using HarmonyLib;

namespace CW.ClientEmu.Patches
{
    [HarmonyPatch]
    internal static class Patch_NetEmulationUpdate
    {
        private static bool _logged;

        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(AccessTools.TypeByName("NetEmulation"), "Update");
        }

        private static bool Prefix()
        {
            if (!_logged)
            {
                _logged = true;
            }
            return false;
        }
    }
}
