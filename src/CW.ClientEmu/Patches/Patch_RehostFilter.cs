using System.Collections;
using System.Reflection;
using HarmonyLib;

namespace CW.ClientEmu.Patches
{
    [HarmonyPatch]
    internal static class Patch_RehostFilter
    {
        private static FieldInfo _filter;

        private static MethodBase TargetMethod()
        {
            var peer = AccessTools.TypeByName("Peer");
            _filter = AccessTools.Field(peer, "_filterDoubleConnection");
            return AccessTools.Method(peer, "OnDisconnect");
        }

        private static void Postfix(object __instance)
        {
            if (_filter == null || __instance == null) return;
            try
            {
                var list = _filter.GetValue(__instance) as IList;
                if (list == null || list.Count == 0) return;
                int n = list.Count;
                list.Clear();
                Plugin.Log.LogInfo("rehost filter cleared (" + n + " stale entr"
                                   + (n == 1 ? "y" : "ies") + ")");
            }
            catch { }
        }
    }
}
