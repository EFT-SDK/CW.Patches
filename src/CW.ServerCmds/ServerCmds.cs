using System;
using System.Collections;
using System.Reflection;
using CW.Shared;
using HarmonyLib;
using UnityEngine;

namespace CW.ServerCmds
{
    internal static class ServerCmds
    {
        private static Type _hostInfoType;
        private static MethodInfo _setInfo, _getHostInfo;
        private static MethodInfo _setIsHidden, _setIsHost, _createServer, _joinGame;
        private static MethodInfo _setMinLevel, _setMaxLevel, _setMapIndex, _setMapName, _setGameMode, _setName, _setForceNat;
        private static MethodInfo _getGlobalsI, _getMapName, _getMapModes;
        private static FieldInfo _mapsField, _fastmapField, _passwordField;
        private static object _teamElim, _targetDes, _tacConq;
        private static bool _ready;

        private static MethodInfo _joinGameHost, _getIp, _getPort;
        private static FieldInfo _gamesField, _collectedHostsField, _ipField, _portField;

        internal static void Init()
        {
            try
            {
                var peer = AccessTools.TypeByName("Peer");
                _hostInfoType = AccessTools.TypeByName("HostInfo");
                var main = AccessTools.TypeByName("Main");
                var globals = AccessTools.TypeByName("Globals");
                var map = AccessTools.TypeByName("Map");
                var gm = AccessTools.TypeByName("GameMode");

                _setInfo = AccessTools.PropertySetter(peer, "Info");
                _setIsHidden = AccessTools.PropertySetter(peer, "IsHidden");
                _setIsHost = AccessTools.PropertySetter(peer, "IsHost");
                _createServer = AccessTools.Method(peer, "CreateServer");
                _joinGame = AccessTools.Method(peer, "JoinGame", new[] { typeof(string), typeof(int), typeof(bool) });
                _getHostInfo = AccessTools.PropertyGetter(main, "HostInfo");

                _setMinLevel = AccessTools.PropertySetter(_hostInfoType, "MinLevel");
                _setMaxLevel = AccessTools.PropertySetter(_hostInfoType, "MaxLevel");
                _setMapIndex = AccessTools.PropertySetter(_hostInfoType, "MapIndex");
                _setMapName = AccessTools.PropertySetter(_hostInfoType, "MapName");
                _setGameMode = AccessTools.PropertySetter(_hostInfoType, "GameMode");
                _setName = AccessTools.PropertySetter(_hostInfoType, "Name");
                _setForceNat = AccessTools.PropertySetter(_hostInfoType, "ForceNAT");

                _getGlobalsI = AccessTools.PropertyGetter(globals, "I");
                _mapsField = AccessTools.Field(globals, "maps");
                _getMapName = AccessTools.PropertyGetter(map, "Name");
                _getMapModes = AccessTools.PropertyGetter(map, "Modes");

                _teamElim = Enum.ToObject(gm, 1);
                _targetDes = Enum.ToObject(gm, 2);
                _tacConq = Enum.ToObject(gm, 3);

                _fastmapField = AccessTools.Field(AccessTools.TypeByName("CVars"), "fastmap");
                _passwordField = AccessTools.Field(AccessTools.TypeByName("eNetwork"), "password");

                _joinGameHost = AccessTools.Method(peer, "JoinGame", new[] { _hostInfoType, typeof(bool) });
                _getIp = AccessTools.PropertyGetter(_hostInfoType, "Ip");
                _getPort = AccessTools.PropertyGetter(_hostInfoType, "Port");
                _ipField = AccessTools.Field(_hostInfoType, "ip");
                _portField = AccessTools.Field(_hostInfoType, "port");
                _gamesField = AccessTools.Field(peer, "games");
                _collectedHostsField = AccessTools.Field(
                    AccessTools.TypeByName("HttpMasterServer"), "collectedHosts");

                _ready = _setInfo != null && _createServer != null && _joinGame != null && _mapsField != null;
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("init failed: " + e.Message);
            }
            if (!_ready) Plugin.Log.LogWarning("some game members not resolved; commands may be inert");
        }

        private static Array Maps()
        {
            var inst = _getGlobalsI.Invoke(null, null);
            return (Array)_mapsField.GetValue(inst);
        }

        private static int MapIndex(string[] args)
        {
            var maps = Maps();
            int idx;
            if (args.Length == 0 || !int.TryParse(args[0], out idx))
                idx = Convert.ToInt32(_fastmapField.GetValue(null));
            if (idx < 0 || idx >= maps.Length)
                idx = Convert.ToInt32(_fastmapField.GetValue(null));
            if (idx < 0 || idx >= maps.Length) idx = 0;
            return idx;
        }

        private static bool ModesContain(object modes, object mode)
        {
            foreach (var e in (IEnumerable)modes)
                if (Equals(e, mode)) return true;
            return false;
        }

        internal static void StartServer(string[] args)
        {
            if (!_ready) { CwConsole.Print("startserver unavailable"); return; }
            try
            {
                int idx = MapIndex(args);
                var maps = Maps();
                var map = maps.GetValue(idx);

                var info = Activator.CreateInstance(_hostInfoType);
                _setInfo.Invoke(null, new[] { info });
                _setMinLevel.Invoke(info, new object[] { 0 });
                _setMaxLevel.Invoke(info, new object[] { 70 });

                var hostInfo = _getHostInfo.Invoke(null, null);
                _setMapIndex.Invoke(hostInfo, new object[] { idx });
                _setMapName.Invoke(hostInfo, new object[] { _getMapName.Invoke(map, null) });

                var modes = _getMapModes.Invoke(map, null);
                _setGameMode.Invoke(info, new object[] { ((IList)modes)[0] });
                if (Input.GetKey(KeyCode.LeftControl) && ModesContain(modes, _teamElim)) _setGameMode.Invoke(info, new[] { _teamElim });
                if (Input.GetKey(KeyCode.LeftShift) && ModesContain(modes, _targetDes)) _setGameMode.Invoke(info, new[] { _targetDes });
                if (Input.GetKey(KeyCode.RightControl) && ModesContain(modes, _tacConq)) _setGameMode.Invoke(info, new[] { _tacConq });

                _setName.Invoke(hostInfo, new object[] { "Server " });
                _setIsHidden.Invoke(null, new object[] { false });
                _setForceNat.Invoke(info, new object[] { false });
                _setIsHost.Invoke(null, new object[] { true });
                _createServer.Invoke(null, null);

                CwConsole.Print("startserver: hosting map " + idx + " (" + _getMapName.Invoke(map, null) + ")");
            }
            catch (Exception e)
            {
                CwConsole.Print("startserver failed: " + e.Message);
                Plugin.Log.LogError(e.ToString());
            }
        }

        private static bool Matches(object host, string ip, int port)
        {
            if (host == null || _getIp == null || _getPort == null) return false;
            try
            {
                var hIp = _getIp.Invoke(host, null) as string;
                if (hIp != ip) return false;
                return Convert.ToInt32(_getPort.Invoke(host, null)) == port;
            }
            catch { return false; }
        }

        private static object FindKnownHost(string ip, int port)
        {
            try
            {
                if (_gamesField != null)
                {
                    var games = _gamesField.GetValue(null) as IEnumerable;
                    if (games != null)
                        foreach (var h in games)
                            if (Matches(h, ip, port)) return h;
                }
                if (_collectedHostsField != null)
                {
                    var collected = _collectedHostsField.GetValue(null) as IEnumerable;
                    if (collected != null)
                        foreach (var h in collected)
                            if (Matches(h, ip, port)) return h;
                }
            }
            catch { }
            return null;
        }

        internal static void JoinServer(string[] args)
        {
            if (!_ready) { CwConsole.Print("joinserver unavailable"); return; }
            if (args.Length < 1) { CwConsole.Print("JoinServer <ip> [port] [password]"); return; }
            try
            {
                string ip = args[0];
                int port = 27015;
                if (args.Length > 1) int.TryParse(args[1], out port);
                if (args.Length > 2 && _passwordField != null) _passwordField.SetValue(null, args[2]);

                object host = FindKnownHost(ip, port);
                bool known = host != null;

                if (!known && _ipField != null && _portField != null)
                {
                    host = Activator.CreateInstance(_hostInfoType);
                    _ipField.SetValue(host, ip);
                    _portField.SetValue(host, port);
                }

                if (host != null && _joinGameHost != null)
                {
                    _joinGameHost.Invoke(null, new[] { host, (object)false });
                    CwConsole.Print("joinserver: connecting to " + ip + ":" + port
                                    + (known ? " (host known)" : " (host not in list)"));
                    return;
                }

                _setInfo.Invoke(null, new[] { Activator.CreateInstance(_hostInfoType) });
                _joinGame.Invoke(null, new object[] { ip, port, false });
                CwConsole.Print("joinserver: connecting to " + ip + ":" + port);
            }
            catch (Exception e)
            {
                CwConsole.Print("joinserver failed: " + e.Message);
                Plugin.Log.LogError(e.ToString());
            }
        }
    }
}
