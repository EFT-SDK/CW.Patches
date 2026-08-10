using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace CW.WeaponLock
{
    internal static class Gate
    {
        internal static bool Is(CodeInstruction c)
        {
            var m = c.operand as MethodInfo;
            return c.opcode == OpCodes.Call && m != null && m.Name == "get_IsGameLoaded";
        }
        internal static bool Store(CodeInstruction c, string field)
        {
            var f = c.operand as FieldInfo;
            return c.opcode == OpCodes.Stfld && f != null && f.Name == field;
        }
        internal static bool Load(CodeInstruction c, string field)
        {
            var f = c.operand as FieldInfo;
            return c.opcode == OpCodes.Ldfld && f != null && f.Name == field;
        }
        internal static void Stub(CodeInstruction c) { c.opcode = OpCodes.Ldc_I4_0; c.operand = null; }
    }
}
