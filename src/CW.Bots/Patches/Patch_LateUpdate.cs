using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace CW.Bots.Patches
{
    [HarmonyPatch]
    internal static class Patch_LateUpdate
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(AccessTools.TypeByName("BotNetPlayer"), "LateUpdate");
        }

        private static void Prefix()
        {
            var dir = BotDirector.Instance;
            if (dir == null || !Refl.Ready) return;

            try
            {
                var game = Refl.ServerGame;
                if (game != null) Refl.SetBotType(dir.ClaimTeam(game));
            }
            catch { }
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var think = typeof(BotHook).GetMethod("Think", BindingFlags.Public | BindingFlags.Static);
            var codes = new List<CodeInstruction>(instructions);

            for (int i = 0; i < codes.Count; i++)
            {
                var c = codes[i];
                var m = c.operand as MethodInfo;
                if (m == null) continue;
                if (m.Name != "Save" || m.DeclaringType == null || m.DeclaringType.Name != "CWInput") continue;
                if (c.opcode != OpCodes.Callvirt && c.opcode != OpCodes.Call) continue;

                codes.Insert(i + 1, new CodeInstruction(OpCodes.Ldarg_0));
                codes.Insert(i + 2, new CodeInstruction(OpCodes.Call, think));
                i += 2;
            }

            return codes;
        }
    }
}
