using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace CW.ClientEmu.Patches
{
    [HarmonyPatch]
    internal static class Patch_ReplaceInit
    {
        private static MethodInfo _sunOnGlassInstance;
        private static int _skipped;

        private static MethodBase TargetMethod()
        {
            _sunOnGlassInstance = AccessTools.PropertyGetter(
                AccessTools.TypeByName("SunOnGlass"), "SunOnGlassInstance");
            return AccessTools.Method(
                AccessTools.TypeByName("StartData+WeaponShaders+Replace"), "Init");
        }

        private static bool Prefix()
        {
            string missing = null;

            if (_sunOnGlassInstance != null)
            {
                var sog = _sunOnGlassInstance.Invoke(null, null) as Object;
                if (sog == null) missing = "SunOnGlass.SunOnGlassInstance";
            }

            if (missing == null && GameObject.Find("Arms_root") == null) missing = "Arms_root";

            if (missing == null) return true;

            _skipped++;
            return false;
        }
    }
}
