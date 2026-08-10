using System.Reflection;
using HarmonyLib;

namespace CW.Snow.Patches
{
    [HarmonyPatch]
    internal static class Patch_HasValue
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(AccessTools.TypeByName("CommandLineArgs"), "HasValue", new[] { typeof(string) });
        }

        private static void Postfix(string key, ref bool __result)
        {
            if (Plugin.Enabled.Value && key == "--snow") __result = true;
        }
    }
}
