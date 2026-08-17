using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace CW.Bots.Patches
{
    [HarmonyPatch]
    internal static class Patch_InstallMods
    {
        private static float _nextReport;
        private static int _swallowed;

        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(AccessTools.TypeByName("ClientWeapon"), "InstallMods");
        }

        private static Exception Finalizer(Exception __exception, int prefabIndex)
        {
            if (__exception == null) return null;
            if (!Plugin.GuardEntities.Value) return __exception;

            _swallowed++;
            if (Time.time >= _nextReport)
            {
                _nextReport = Time.time + 10f;
                Plugin.Log.LogWarning("InstallMods failed for weapon " + prefabIndex + " (" + _swallowed
                                      + " so far); attachments skipped so the entity still spawns");
            }
            return null;
        }
    }
}
