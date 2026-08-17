using System;
using System.Reflection;
using HarmonyLib;

namespace CW.ClientEmu.Patches
{
    [HarmonyPatch]
    internal static class Patch_BotProgressLeak
    {
        private const int BotId = -999;

        private static PropertyInfo _userInfo;
        private static FieldInfo _userId;
        private static MethodInfo _intToInt;

        private static MethodBase TargetMethod()
        {
            var rpcPlayer = AccessTools.TypeByName("BaseRpcNetPlayer");
            var netPlayer = AccessTools.TypeByName("BaseNetPlayer");
            _userInfo = AccessTools.Property(netPlayer, "UserInfo")
                        ?? AccessTools.Property(rpcPlayer, "UserInfo");
            var overview = AccessTools.TypeByName("OverviewInfo");
            _userId = AccessTools.Field(overview, "userID");
            if (_userId != null && _userId.FieldType != typeof(int))
            {
                foreach (var m in _userId.FieldType.GetMethods(BindingFlags.Public | BindingFlags.Static))
                {
                    if (m.Name != "op_Implicit" || m.ReturnType != typeof(int)) continue;
                    _intToInt = m;
                    break;
                }
            }
            return AccessTools.Method(rpcPlayer, "ToClient",
                                      new[] { typeof(string), typeof(object[]) });
        }

        private static bool IsBot(object player)
        {
            if (player == null || _userInfo == null || _userId == null) return false;
            var info = _userInfo.GetValue(player, null);
            if (info == null) return false;
            var raw = _userId.GetValue(info);
            if (raw == null) return false;
            int id = _intToInt != null ? (int)_intToInt.Invoke(null, new[] { raw })
                                       : Convert.ToInt32(raw);
            return id == BotId;
        }

        private static int _suppressed;

        private static bool Prefix(object __instance, string name)
        {
            try
            {
                if (!IsBot(__instance)) return true;
            }
            catch
            {
                return true;
            }

            _suppressed++;
            if (_suppressed == 1 || _suppressed % 50 == 0)
                Plugin.Log.LogInfo("bot RPC suppressed: " + name + " (total " + _suppressed + ")");
            return false;
        }
    }
}
