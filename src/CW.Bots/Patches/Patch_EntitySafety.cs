using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace CW.Bots.Patches
{
    [HarmonyPatch]
    internal static class Patch_EntitySafety
    {
        private static float _nextReport;
        private static int _swallowed;

        private static IEnumerable<MethodBase> TargetMethods()
        {
            var t = AccessTools.TypeByName("EntityNetPlayer");
            yield return AccessTools.Method(t, "CallLateUpdate");
            yield return AccessTools.Method(t, "CallFixedUpdate");
        }

        private static Exception Finalizer(Exception __exception)
        {
            if (__exception == null) return null;
            if (!Plugin.GuardEntities.Value) return __exception;

            _swallowed++;
            if (Time.time >= _nextReport)
            {
                _nextReport = Time.time + 10f;
                Plugin.Log.LogWarning("entity update threw (" + _swallowed + " so far, suppressed so the "
                                      + "render loop keeps going): " + __exception.Message);
            }
            return null;
        }
    }
}
