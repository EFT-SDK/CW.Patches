using System.Reflection;
using HarmonyLib;

namespace CW.BotInput.Patches
{
    [HarmonyPatch]
    internal static class Patch_botstop
    {
        private static MethodBase TargetMethod() { return AccessTools.Method(AccessTools.TypeByName("CVars"), "botstop"); }
        private static void Postfix() { BotHooks.Mirror = false; }
    }
}
