using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace CW.WeaponLock.Patches
{
    [HarmonyPatch]
    internal static class Patch_InterfaceGUI
    {
        private static MethodBase TargetMethod() { return AccessTools.Method(AccessTools.TypeByName("MainGUI"), "InterfaceGUI"); }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            int store = -1;
            for (int i = 1; i < codes.Count; i++)
                if (Gate.Store(codes[i], "disableSelection") && codes[i - 1].opcode == OpCodes.Ldc_I4_1) { store = i; break; }

            int hit = -1;
            if (store >= 0)
                for (int i = store - 2; i >= Math.Max(0, store - 14); i--)
                    if (Gate.Is(codes[i])) { Gate.Stub(codes[i]); hit = i; break; }

            return codes;
        }
    }
}
