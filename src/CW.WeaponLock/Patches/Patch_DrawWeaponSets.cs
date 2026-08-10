using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace CW.WeaponLock.Patches
{
    [HarmonyPatch]
    internal static class Patch_DrawWeaponSets
    {
        private static MethodBase TargetMethod() { return AccessTools.Method(AccessTools.TypeByName("MainGUI"), "DrawWeaponSets"); }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            int n = 0;
            for (int i = 0; i < codes.Count; i++)
                if (Gate.Is(codes[i])) { Gate.Stub(codes[i]); n++; }
            return codes;
        }
    }
}
