using System.Reflection;
using HarmonyLib;

namespace CW.BotInput.Patches
{
    [HarmonyPatch]
    internal static class Patch_botstart
    {
        private static MethodBase TargetMethod() { return AccessTools.Method(AccessTools.TypeByName("CVars"), "botstart"); }
        private static void Postfix() { BotHooks.Mirror = true; }
    }
}
