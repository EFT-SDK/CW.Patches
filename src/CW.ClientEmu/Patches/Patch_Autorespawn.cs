using System.Reflection;
using HarmonyLib;

namespace CW.ClientEmu.Patches
{
    [HarmonyPatch]
    internal static class Patch_Autorespawn
    {
        private static FieldInfo _field;

        private static MethodBase TargetMethod()
        {
            var t = AccessTools.TypeByName("UserGraphics");
            _field = AccessTools.Field(t, "autorespawn");
            return AccessTools.PropertyGetter(t, "Autorespawn");
        }

        private static void Postfix(object __instance, ref bool __result)
        {
            if (_field == null) return;
            try { __result = (bool)_field.GetValue(__instance); }
            catch { }
        }
    }
}
