using System;
using System.Reflection;
using HarmonyLib;

namespace CW.Bots.Patches
{
    [HarmonyPatch]
    internal static class Patch_ParseLine
    {
        private static readonly char[] Sep = { ' ', '=' };

        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(AccessTools.TypeByName("Console"), "ParseLine", new[] { typeof(string) });
        }

        private static bool Prefix(string line)
        {
            if (string.IsNullOrEmpty(line)) return true;

            var parts = line.Split(Sep);
            var cmd = parts[0].ToLowerInvariant();

            if (cmd != "bots" && cmd != "navbake" && cmd != "navinfo" && cmd != "navdraw"
                && cmd != "botdebug" && cmd != "botskill" && cmd != "botkits")
                return true;

            var args = new string[parts.Length - 1];
            Array.Copy(parts, 1, args, 0, args.Length);

            try
            {
                switch (cmd)
                {
                    case "bots": Commands.Bots(args); break;
                    case "navbake": Commands.NavBake(args); break;
                    case "navinfo": Commands.NavInfo(args); break;
                    case "navdraw": Commands.NavDraw(args); break;
                    case "botdebug": Commands.BotDebug(args); break;
                    case "botskill": Commands.BotSkill(args); break;
                    case "botkits": Commands.BotKits(args); break;
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogError(cmd + " failed: " + e);
                Refl.Print(cmd + " failed: " + e.Message);
            }

            return false;
        }
    }
}
