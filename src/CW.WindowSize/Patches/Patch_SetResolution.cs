using System.Reflection;
using HarmonyLib;

namespace CW.WindowSize.Patches
{
    [HarmonyPatch]
    internal static class Patch_SetResolution
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(AccessTools.TypeByName("Utility"), "SetResolution",
                new[] { typeof(int), typeof(int), typeof(bool) });
        }

        private static void Prefix(ref int width, ref int height, bool fullScreen)
        {
            if (fullScreen) return;
            int w, h;
            if (Res.TrySettingsResolution(out w, out h)) { width = w; height = h; }
        }
    }
}
