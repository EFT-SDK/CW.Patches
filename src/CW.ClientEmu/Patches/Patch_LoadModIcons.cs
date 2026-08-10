using System.Reflection;
using HarmonyLib;

namespace CW.ClientEmu.Patches
{
    [HarmonyPatch]
    internal static class Patch_LoadModIcons
    {
        private static PropertyInfo _completed;
        private static object _owner;
        private static int _skipped;

        private static MethodBase TargetMethod()
        {
            var t = AccessTools.TypeByName("ModIconsDownloader");
            _completed = AccessTools.Property(t, "DataLoadingCompleted");
            return AccessTools.Method(t, "LoadModIcons");
        }

        private static bool Prefix(object __instance)
        {
            if (!ReferenceEquals(_owner, __instance))
            {
                _owner = __instance;
                _skipped = 0;
                return true;
            }

            if (_completed == null) return true;

            bool done;
            try { done = (bool)_completed.GetValue(__instance, null); }
            catch { return true; }

            if (done) return true;

            _skipped++;
            return false;
        }
    }
}
