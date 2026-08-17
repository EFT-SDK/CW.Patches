using System.Reflection;
using HarmonyLib;

namespace CW.Bots.Patches
{
    [HarmonyPatch]
    internal static class Patch_ChooseAmmunition
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(AccessTools.TypeByName("BaseRpcNetPlayer"), "ChooseAmmunitionFromClient");
        }

        private static void Prefix(object __instance, ref int secondaryIndex, ref int primaryIndex,
                                   ref bool secondaryMod, ref bool primaryMod,
                                   ref string secondaryMods, ref string primaryMods)
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

                if (kit.Primary != 127)
                {
                    primaryIndex = kit.Primary;
                    primaryMod = false;
                    primaryMods = string.Empty;
                }

                if (kit.Secondary != 127)
                {
                    secondaryIndex = kit.Secondary;
                    secondaryMod = false;
                    secondaryMods = string.Empty;
                }
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogError("bot kit roll failed: " + e.Message);
            }
        }
    }
}
