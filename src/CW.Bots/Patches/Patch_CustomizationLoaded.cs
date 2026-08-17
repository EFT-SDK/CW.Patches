using System.Reflection;
using HarmonyLib;

namespace CW.Bots.Patches
{
    [HarmonyPatch]
    internal static class Patch_CustomizationLoaded
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(AccessTools.TypeByName("ServerNetPlayer"), "OnCustomizationLoaded");
        }

        private static void Postfix(object __instance)
        {
            var dir = BotDirector.Instance;
            if (dir == null || !Refl.Ready || !Plugin.RandomKits.Value) return;

            try
            {
                int group = Refl.Group(__instance);
                if (!dir.IsBotBody(group)) return;

                var userInfo = Refl.UserInfo(__instance);
                if (userInfo == null) return;

                var kit = dir.KitFor(group, userInfo);
                if (kit == null) return;

                Ai.Loadout.Apply(userInfo, Refl.PlayerInfo(__instance), kit);
                Plugin.Log.LogInfo("bot g" + group + " loadout: " + kit.Summary);
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogError("bot skill roll failed: " + e.Message);
            }
        }
    }
}
