using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace CW.Bots.Patches
{
    [HarmonyPatch]
    internal static class Patch_AddPlayer
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(AccessTools.TypeByName("BaseServerGame"), "AddPlayer",
                new[] { typeof(int), typeof(NetworkViewID), typeof(string), typeof(string) });
        }

        private static void Postfix(object __instance, int userID)
        {
            if (userID != Refl.BotUserId) return;

            var dir = BotDirector.Instance;
            if (dir == null || !Refl.Ready) return;

            try
            {
                var loading = Refl.LoadingPlayers(__instance);
                if (loading == null || loading.Count == 0) return;

                var added = loading[loading.Count - 1];
                if (added == null) return;

                dir.RegisterBotBody(Refl.Group(added));
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogError("AddPlayer hook failed: " + e.Message);
            }
        }
    }
}
