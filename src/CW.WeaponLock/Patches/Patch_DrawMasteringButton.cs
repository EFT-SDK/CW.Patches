using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace CW.WeaponLock.Patches
{
    [HarmonyPatch]
    internal static class Patch_DrawMasteringButton
    {
        private static MethodBase TargetMethod() { return AccessTools.Method(AccessTools.TypeByName("MainGUI"), "DrawMasteringButton"); }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            bool done = false;
            for (int i = 0; i < codes.Count && !done; i++)
            {
                if (!Gate.Is(codes[i])) continue;
                for (int j = Math.Max(0, i - 4); j < i; j++)
                    if (Gate.Load(codes[j], "IsWeaponViewerClicked")) { Gate.Stub(codes[i]); done = true; break; }
            }
            return codes;
        }
    }
}
