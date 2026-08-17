using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace CW.Bots
{
    internal static class Refl
    {
        internal static Type TPeer, TMain, TCVars, TConsole, TPhysicsUtility;
        internal static Type TBaseServerGame, TBaseNetPlayer, TBaseRpcNetPlayer, TBotNetPlayer;
        internal static Type TBaseMoveController, TMoveState, TPlayerInfo, TPlayerCmd, THostInfo;
        internal static Type TSpawnPoint, TUpperLeftPoint, TLowerRightPoint;
        internal static Type TBaseAmmunitions, TBaseWeapon, TWeaponState, TAmmoState, TBaseGame;

        private static MethodInfo _peerServerGame, _peerClientGame, _peerPeerType, _peerInfo;
        private static MethodInfo _mainIsGameLoaded, _hostMapName, _hostGameMode, _levelLayers;
        private static MethodInfo _serverNetPlayers, _consolePrint;

        private static FieldInfo _fGroup, _fPlayerInfo, _fController, _fBotCmd;
        private static FieldInfo _fDead, _fPlayerType;
        private static FieldInfo _fState, _fRootCamera, _fCharController;
        private static FieldInfo _fEuler, _fPos, _fIsGrounded, _fIsSeat;
        private static FieldInfo _fCmdButtons, _fCmdEuler;
        private static FieldInfo _fSpIsBear, _fSpIsTeam, _fSpIsDm, _fSpIsTe;
        private static FieldInfo _fBotType;
        private static MethodInfo _addBot;

        internal static Type TSkills, TWeaponInfo, TSkillInfo, TOverviewInfo;
        private static MethodInfo _loadingPlayers, _userInfo, _skillUnlockedGet, _skillUnlockedSet;
        private static FieldInfo _fAmmoOwner, _fHearRadius, _fGrenadeCount, _fGrenadeTimer, _fThrowPower;
        private static FieldInfo _fWeaponsStates, _fSkillsInfos, _fWpUnlocked, _fWpCurrent, _fWpUseType, _fPiSkills, _fSkillReqs;
        internal static int BotUserId = -999;

        private static FieldInfo _fAmmo, _fAmmoState, _fIsAim, _fWeaponState;
        private static FieldInfo _fClips, _fBagSize, _fSingleShot, _fNeedReload, _fWeaponNature, _fAuto;
        private static MethodInfo _currentWeapon, _weaponEquiped, _weaponEmpty, _curAccuracy, _dmgDistMax, _isTeamGame;

        internal static bool Ready { get; private set; }

        internal static void Init()
        {
            TPeer = AccessTools.TypeByName("Peer");
            TMain = AccessTools.TypeByName("Main");
            TCVars = AccessTools.TypeByName("CVars");
            TConsole = AccessTools.TypeByName("Console");
            TPhysicsUtility = AccessTools.TypeByName("PhysicsUtility");
            TBaseServerGame = AccessTools.TypeByName("BaseServerGame");
            TBaseNetPlayer = AccessTools.TypeByName("BaseNetPlayer");
            TBaseRpcNetPlayer = AccessTools.TypeByName("BaseRpcNetPlayer");
            TBotNetPlayer = AccessTools.TypeByName("BotNetPlayer");
            TBaseMoveController = AccessTools.TypeByName("BaseMoveController");
            TMoveState = AccessTools.TypeByName("MoveState");
            TPlayerInfo = AccessTools.TypeByName("PlayerInfo");
            TPlayerCmd = AccessTools.TypeByName("PlayerCmd");
            THostInfo = AccessTools.TypeByName("HostInfo");
            TSpawnPoint = AccessTools.TypeByName("SpawnPoint");
            TUpperLeftPoint = AccessTools.TypeByName("UpperLeftPoint");
            TLowerRightPoint = AccessTools.TypeByName("LowerRightPoint");

            _peerServerGame = AccessTools.PropertyGetter(TPeer, "ServerGame");
            _peerClientGame = AccessTools.PropertyGetter(TPeer, "ClientGame");
            _peerPeerType = AccessTools.PropertyGetter(TPeer, "PeerType");
            _peerInfo = AccessTools.PropertyGetter(TPeer, "Info");
            _mainIsGameLoaded = AccessTools.PropertyGetter(TMain, "IsGameLoaded");
            _hostMapName = AccessTools.PropertyGetter(THostInfo, "MapName");
            _hostGameMode = AccessTools.PropertyGetter(THostInfo, "GameMode");
            _levelLayers = AccessTools.PropertyGetter(TPhysicsUtility, "level_layers");
            _serverNetPlayers = AccessTools.PropertyGetter(TBaseServerGame, "ServerNetPlayers");
            _consolePrint = AccessTools.Method(TConsole, "print", new[] { typeof(string) });

            _fGroup = AccessTools.Field(TBaseRpcNetPlayer, "group");
            _fPlayerInfo = AccessTools.Field(TBaseNetPlayer, "playerInfo");
            _fController = AccessTools.Field(TBaseNetPlayer, "controller");
            _fBotCmd = AccessTools.Field(TBotNetPlayer, "cmd");

            _fDead = AccessTools.Field(TPlayerInfo, "dead");
            _fPlayerType = AccessTools.Field(TPlayerInfo, "playerType");

            _fState = AccessTools.Field(TBaseMoveController, "state");
            _fRootCamera = AccessTools.Field(TBaseMoveController, "rootCamera");
            _fCharController = AccessTools.Field(TBaseMoveController, "controller");

            _fEuler = AccessTools.Field(TMoveState, "euler");
            _fPos = AccessTools.Field(TMoveState, "pos");
            _fIsGrounded = AccessTools.Field(TMoveState, "isGrounded");
            _fIsSeat = AccessTools.Field(TMoveState, "isSeat");

            _fCmdButtons = AccessTools.Field(TPlayerCmd, "buttons");
            _fCmdEuler = AccessTools.Field(TPlayerCmd, "euler");

            _fSpIsBear = AccessTools.Field(TSpawnPoint, "isBear");
            _fSpIsTeam = AccessTools.Field(TSpawnPoint, "isTeam");
            _fSpIsDm = AccessTools.Field(TSpawnPoint, "isDeathMatch");
            _fSpIsTe = AccessTools.Field(TSpawnPoint, "isTeamEllimination");

            _fBotType = AccessTools.Field(TCVars, "g_botType");
            _addBot = AccessTools.Method(TCVars, "addbot");

            TBaseAmmunitions = AccessTools.TypeByName("BaseAmmunitions");
            TBaseWeapon = AccessTools.TypeByName("BaseWeapon");
            TWeaponState = AccessTools.TypeByName("WeaponState");
            TAmmoState = AccessTools.TypeByName("AmmoState");
            TBaseGame = AccessTools.TypeByName("BaseGame");

            _fAmmo = AccessTools.Field(TBaseNetPlayer, "ammo");
            _fAmmoState = AccessTools.Field(TBaseAmmunitions, "state");
            _fIsAim = AccessTools.Field(TAmmoState, "isAim");
            _fWeaponState = AccessTools.Field(TBaseWeapon, "state");
            _fWeaponNature = AccessTools.Field(TBaseWeapon, "weaponNature");
            _fAuto = AccessTools.Field(TBaseWeapon, "auto");

            _fClips = AccessTools.Field(TWeaponState, "clips");
            _fBagSize = AccessTools.Field(TWeaponState, "bagSize");
            _fSingleShot = AccessTools.Field(TWeaponState, "singleShot");
            _fNeedReload = AccessTools.Field(TWeaponState, "needReload");

            TSkills = AccessTools.TypeByName("Skills");
            TWeaponInfo = AccessTools.TypeByName("WeaponInfo");
            TSkillInfo = AccessTools.TypeByName("SkillInfo");
            TOverviewInfo = AccessTools.TypeByName("OverviewInfo");

            _loadingPlayers = AccessTools.PropertyGetter(TBaseServerGame, "LoadingNetPlayers");
            _userInfo = AccessTools.PropertyGetter(TBaseNetPlayer, "UserInfo");
            _skillUnlockedGet = AccessTools.PropertyGetter(TSkillInfo, "Unlocked");
            _skillUnlockedSet = AccessTools.PropertySetter(TSkillInfo, "Unlocked");

            _fWeaponsStates = AccessTools.Field(TOverviewInfo, "weaponsStates");
            _fSkillsInfos = AccessTools.Field(TOverviewInfo, "skillsInfos");
            _fWpUnlocked = AccessTools.Field(TWeaponInfo, "unlocked");
            _fWpCurrent = AccessTools.Field(TWeaponInfo, "currentWeapon");
            _fWpUseType = AccessTools.Field(TBaseWeapon, "weaponUseType");
            _fPiSkills = AccessTools.Field(TPlayerInfo, "skillsInfos");
            _fSkillReqs = AccessTools.Field(TSkillInfo, "requirements");
            _fAmmoOwner = AccessTools.Field(TBaseAmmunitions, "player");
            _fHearRadius = AccessTools.Field(TBaseWeapon, "hearRadius");
            _fGrenadeCount = AccessTools.Field(TAmmoState, "grenadeCount");
            _fGrenadeTimer = AccessTools.Field(TBaseAmmunitions, "GrenadeTimer");
            _fThrowPower = AccessTools.Field(TBaseAmmunitions, "throwPowerMult");

            var botIdField = AccessTools.Field(AccessTools.TypeByName("IDUtil"), "BotID");
            if (botIdField != null) BotUserId = (int)botIdField.GetValue(null);

            _currentWeapon = AccessTools.PropertyGetter(TBaseAmmunitions, "CurrentWeapon");
            _weaponEquiped = AccessTools.PropertyGetter(TBaseAmmunitions, "weaponEquiped");
            _weaponEmpty = AccessTools.PropertyGetter(TBaseWeapon, "Empty");
            _curAccuracy = AccessTools.PropertyGetter(TBaseWeapon, "CurrentAccuracy");
            _dmgDistMax = AccessTools.PropertyGetter(TBaseWeapon, "DamageReduceDistanceMax");
            _isTeamGame = AccessTools.PropertyGetter(TBaseGame, "IsTeamGame");

            Ready = _fGroup != null && _fPlayerInfo != null && _fController != null
                    && _fBotCmd != null && _fDead != null && _fPlayerType != null
                    && _fState != null && _fCmdButtons != null && _fCmdEuler != null;
        }

        internal static object ServerGame { get { return _peerServerGame.Invoke(null, null); } }
        internal static object ClientGame { get { return _peerClientGame.Invoke(null, null); } }
        internal static bool IsServer { get { return (int)_peerPeerType.Invoke(null, null) == (int)NetworkPeerType.Server; } }
        internal static bool IsGameLoaded { get { return (bool)_mainIsGameLoaded.Invoke(null, null); } }
        internal static int LevelLayers { get { return (int)_levelLayers.Invoke(null, null); } }

        internal static string MapName
        {
            get
            {
                var info = _peerInfo.Invoke(null, null);
                return info == null ? string.Empty : (string)_hostMapName.Invoke(info, null);
            }
        }

        internal static int GameMode
        {
            get
            {
                var info = _peerInfo.Invoke(null, null);
                return info == null ? -1 : (int)_hostGameMode.Invoke(info, null);
            }
        }

        internal static IList ServerPlayers(object serverGame)
        {
            return serverGame == null ? null : (IList)_serverNetPlayers.Invoke(serverGame, null);
        }

        internal static void Print(string s)
        {
            if (_consolePrint != null) _consolePrint.Invoke(null, new object[] { s });
        }

        internal static int Group(object netPlayer) { return (int)_fGroup.GetValue(netPlayer); }
        internal static object PlayerInfo(object netPlayer) { return _fPlayerInfo.GetValue(netPlayer); }
        internal static object Controller(object netPlayer) { return _fController.GetValue(netPlayer); }
        internal static object BotCmd(object bot) { return _fBotCmd.GetValue(bot); }

        internal static bool Dead(object playerInfo) { return (bool)_fDead.GetValue(playerInfo); }
        internal static int PlayerType(object playerInfo) { return (int)_fPlayerType.GetValue(playerInfo); }

        internal static object State(object moveController) { return _fState.GetValue(moveController); }
        internal static Transform RootCamera(object moveController) { return (Transform)_fRootCamera.GetValue(moveController); }
        internal static CharacterController CharController(object moveController) { return (CharacterController)_fCharController.GetValue(moveController); }

        internal static Vector3 Euler(object state) { return (Vector3)_fEuler.GetValue(state); }
        internal static Vector3 Pos(object state) { return (Vector3)_fPos.GetValue(state); }
        internal static bool IsGrounded(object state) { return (bool)_fIsGrounded.GetValue(state); }
        internal static bool IsSeat(object state) { return (bool)_fIsSeat.GetValue(state); }

        internal static void SetCmdEuler(object cmd, Vector3 euler) { _fCmdEuler.SetValue(cmd, euler); }
        internal static void SetCmdButtons(object cmd, int buttons) { _fCmdButtons.SetValue(cmd, buttons); }

        internal static void SpawnFlags(object sp, out bool isBear, out bool isTeam, out bool isDm, out bool isTe)
        {
            isBear = (bool)_fSpIsBear.GetValue(sp);
            isTeam = (bool)_fSpIsTeam.GetValue(sp);
            isDm = (bool)_fSpIsDm.GetValue(sp);
            isTe = (bool)_fSpIsTe.GetValue(sp);
        }

        internal static object Ammo(object netPlayer) { return _fAmmo == null ? null : _fAmmo.GetValue(netPlayer); }
        internal static object CurrentWeapon(object ammo) { return ammo == null || _currentWeapon == null ? null : _currentWeapon.Invoke(ammo, null); }
        internal static bool WeaponEquiped(object ammo) { return ammo != null && _weaponEquiped != null && (bool)_weaponEquiped.Invoke(ammo, null); }
        internal static object WeaponState(object weapon) { return weapon == null || _fWeaponState == null ? null : _fWeaponState.GetValue(weapon); }

        internal static int Clips(object ws) { return ws == null ? 0 : (int)_fClips.GetValue(ws); }
        internal static int BagSize(object ws) { return ws == null ? 0 : (int)_fBagSize.GetValue(ws); }
        internal static bool SingleShot(object ws) { return ws != null && (bool)_fSingleShot.GetValue(ws); }
        internal static bool NeedReload(object ws) { return ws != null && (bool)_fNeedReload.GetValue(ws); }

        internal static bool WeaponEmpty(object weapon) { return weapon != null && _weaponEmpty != null && (bool)_weaponEmpty.Invoke(weapon, null); }
        internal static float Accuracy(object weapon) { return weapon == null || _curAccuracy == null ? 3f : (float)_curAccuracy.Invoke(weapon, null); }
        internal static float RangeMax(object weapon) { return weapon == null || _dmgDistMax == null ? 60f : (float)_dmgDistMax.Invoke(weapon, null); }
        internal static int WeaponNature(object weapon) { return weapon == null || _fWeaponNature == null ? 0 : (int)_fWeaponNature.GetValue(weapon); }

        internal static bool IsTeamGame(object game) { return game != null && _isTeamGame != null && (bool)_isTeamGame.Invoke(game, null); }

        internal static IList LoadingPlayers(object game)
        {
            return game == null || _loadingPlayers == null ? null : (IList)_loadingPlayers.Invoke(game, null);
        }

        internal static object UserInfo(object netPlayer) { return _userInfo == null ? null : _userInfo.Invoke(netPlayer, null); }
        internal static Array WeaponsStates(object ui) { return ui == null || _fWeaponsStates == null ? null : (Array)_fWeaponsStates.GetValue(ui); }
        internal static Array SkillsInfos(object ui) { return ui == null || _fSkillsInfos == null ? null : (Array)_fSkillsInfos.GetValue(ui); }

        internal static bool WeaponUnlocked(object wi) { return wi != null && _fWpUnlocked != null && (bool)_fWpUnlocked.GetValue(wi); }
        internal static object WeaponOf(object wi) { return wi == null || _fWpCurrent == null ? null : _fWpCurrent.GetValue(wi); }
        internal static int WeaponUseType(object weapon) { return weapon == null || _fWpUseType == null ? -1 : (int)_fWpUseType.GetValue(weapon); }

        internal static bool SkillUnlocked(object si) { return si != null && _skillUnlockedGet != null && (bool)_skillUnlockedGet.Invoke(si, null); }
        internal static void SetSkillUnlocked(object si, bool v) { if (si != null && _skillUnlockedSet != null) _skillUnlockedSet.Invoke(si, new object[] { v }); }
        internal static void SetPlayerSkills(object playerInfo, bool[] arr) { if (_fPiSkills != null) _fPiSkills.SetValue(playerInfo, arr); }

        internal static int[] SkillRequirements(object si)
        {
            if (si == null || _fSkillReqs == null) return null;
            var arr = _fSkillReqs.GetValue(si) as Array;
            if (arr == null) return null;
            var outv = new int[arr.Length];
            for (int i = 0; i < arr.Length; i++) outv[i] = (int)arr.GetValue(i);
            return outv;
        }

        internal static object AmmoState(object ammo) { return ammo == null || _fAmmoState == null ? null : _fAmmoState.GetValue(ammo); }
        internal static int GrenadeCount(object ammoState) { return ammoState == null || _fGrenadeCount == null ? 0 : (int)_fGrenadeCount.GetValue(ammoState); }
        internal static float GrenadeTimer(object ammo) { return ammo == null || _fGrenadeTimer == null ? 99f : (float)_fGrenadeTimer.GetValue(ammo); }
        internal static float ThrowPower(object ammo) { return ammo == null || _fThrowPower == null ? 1f : (float)_fThrowPower.GetValue(ammo); }

        internal static object AmmoOwner(object ammo) { return ammo == null || _fAmmoOwner == null ? null : _fAmmoOwner.GetValue(ammo); }
        internal static float HearRadius(object weapon) { return weapon == null || _fHearRadius == null ? 0f : (float)_fHearRadius.GetValue(weapon); }

        internal static bool TryPosition(object netPlayer, out Vector3 pos)
        {
            pos = Vector3.zero;
            if (netPlayer == null) return false;
            var mc = Controller(netPlayer);
            if (mc == null) return false;
            var cc = CharController(mc);
            if (cc == null) return false;
            pos = cc.transform.position;
            return true;
        }

        internal static int SkillIndex(string name)
        {
            if (TSkills == null) return -1;
            try { return (int)Enum.Parse(TSkills, name); }
            catch { return -1; }
        }

        internal static void SetBotType(int type) { if (_fBotType != null) _fBotType.SetValue(null, type); }

        internal static void AddBot(int count)
        {
            if (_addBot == null) return;
            _addBot.Invoke(null, new object[] { new object[] { count.ToString() } });
        }
    }
}
