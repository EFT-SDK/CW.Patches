using System.Reflection;
using HarmonyLib;

namespace CW.ClientEmu.Patches
{
    [HarmonyPatch]
    internal static class Patch_hidden
    {
        private static MethodInfo _getInfo;
        private static MethodInfo _setForceNat;

        private static MethodBase TargetMethod()
        {
            _getInfo = AccessTools.PropertyGetter(AccessTools.TypeByName("Peer"), "Info");
            _setForceNat = AccessTools.PropertySetter(AccessTools.TypeByName("HostInfo"), "ForceNAT");
            return AccessTools.Method(AccessTools.TypeByName("CVars"), "hidden");
        }

        private static void Postfix()
        {
            try
            {
                var info = _getInfo.Invoke(null, null);
                if (info != null) _setForceNat.Invoke(info, new object[] { false });
            }
            catch { }
        }
    }
}
