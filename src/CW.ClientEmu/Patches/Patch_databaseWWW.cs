using System.Reflection;
using HarmonyLib;

namespace CW.ClientEmu.Patches
{
    [HarmonyPatch]
    internal static class Patch_databaseWWW
    {
        private static FieldInfo _rootField;
        private static MethodInfo _globalsInstance;

        private static MethodBase TargetMethod()
        {
            return AccessTools.PropertyGetter(AccessTools.TypeByName("WWWUtil"), "databaseWWW");
        }

        private static void Postfix(ref string __result)
        {
            string root = string.Empty;
            try
            {
                var g = AccessTools.TypeByName("Globals");
                if (_globalsInstance == null) _globalsInstance = AccessTools.PropertyGetter(g, "I");
                if (_rootField == null) _rootField = AccessTools.Field(g, "databaseRoot");
                var inst = _globalsInstance.Invoke(null, null);
                var r = _rootField.GetValue(inst) as string;
                if (r != null) root = r;
            }
            catch { }
            __result = Plugin.Host.Value + root;
        }
    }
}
