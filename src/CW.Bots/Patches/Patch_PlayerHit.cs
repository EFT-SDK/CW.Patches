using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace CW.Bots.Patches
{
    [HarmonyPatch]
    internal static class Patch_PlayerHit
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(AccessTools.TypeByName("ServerNetPlayer"), "PlayerHit");
        }

        private static void Postfix(object __instance, float damage, object player)
        {
            var dir = BotDirector.Instance;
            if (dir == null || !Refl.Ready || player == null) return;
            if (ReferenceEquals(__instance, player)) return;

            try
            {
                var victim = dir.AgentByGroup(Refl.Group(__instance));
                if (victim == null) return;

                Vector3 from;
                if (!Refl.TryPosition(player, out from)) return;

                victim.OnDamaged(player, from, damage);
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogError("PlayerHit hook failed: " + e.Message);
            }
        }
    }
}
