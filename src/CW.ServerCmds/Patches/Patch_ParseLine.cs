using System;
using System.Reflection;
using HarmonyLib;

namespace CW.ServerCmds.Patches
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
            var cmd = parts[0];
            if (cmd != "startserver" && cmd != "joinserver") return true;

            var args = new string[parts.Length - 1];
            Array.Copy(parts, 1, args, 0, args.Length);
            if (cmd == "startserver") ServerCmds.StartServer(args);
            else ServerCmds.JoinServer(args);
            return false;
        }
    }
}
