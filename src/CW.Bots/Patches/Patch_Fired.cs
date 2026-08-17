using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace CW.Bots.Patches
{
    [HarmonyPatch]
    internal static class Patch_Fired
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(AccessTools.TypeByName("ServerAmmunitions"), "SingleAttack");
        }

        private static void Postfix(object __instance)
        {
            var dir = BotDirector.Instance;
            if (dir == null || !Refl.Ready || dir.AgentCount == 0) return;

            try
            {
                var shooter = Refl.AmmoOwner(__instance);
                if (shooter == null) return;

                Vector3 at;
                if (!Refl.TryPosition(shooter, out at)) return;

                float radius = Refl.HearRadius(Refl.CurrentWeapon(__instance));
                if (radius < 5f) radius = 40f;

                dir.ReportGunshot(shooter, at, radius);
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogError("gunshot hook failed: " + e.Message);
            }
        }
    }
}
