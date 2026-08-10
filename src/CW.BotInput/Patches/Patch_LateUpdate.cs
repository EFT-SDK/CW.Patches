using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace CW.BotInput.Patches
{
    [HarmonyPatch]
    internal static class Patch_LateUpdate
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(AccessTools.TypeByName("BotNetPlayer"), "LateUpdate");
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var wrap = typeof(BotHooks).GetMethod("BotButtons", BindingFlags.Public | BindingFlags.Static);
            var codes = new List<CodeInstruction>(instructions);
            int wrapped = 0;
            for (int i = 0; i < codes.Count; i++)
            {
                var c = codes[i];
                var m = c.operand as MethodInfo;
                if (m != null && m.Name == "Save" && m.DeclaringType != null && m.DeclaringType.Name == "CWInput"
                    && (c.opcode == OpCodes.Callvirt || c.opcode == OpCodes.Call))
                {
                    codes.Insert(i + 1, new CodeInstruction(OpCodes.Call, wrap));
                    i++;
                    wrapped++;
                }
            }
            return codes;
        }
    }
}
