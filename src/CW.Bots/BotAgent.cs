using System.Collections.Generic;
using CW.Bots.Ai;
using CW.Bots.Nav;
using UnityEngine;

namespace CW.Bots
{
    internal sealed class BotAgent
    {
        internal int Group;
        internal object Bot;
        internal object Server;

        internal Vector3 Position;
        internal Vector3 Muzzle;
        internal Vector3 Forward = Vector3.forward;
        internal float Yaw;
        internal float Pitch;
        internal bool Alive;
        internal int Team = -1;

        internal readonly List<int> Path = new List<int>();
        internal int PathIndex;
        internal int Destination = -1;
        internal string State = "idle";

        internal readonly Perception Eyes = new Perception();
        internal readonly Combat Gun = new Combat();
        internal readonly Awareness Hearing = new Awareness();
        internal readonly Tactics Cover = new Tactics();
        internal readonly Grenades Nade = new Grenades();
        internal Doctrine Style = new Doctrine();

        private CharacterController _cc;
        private Transform _ccTransform;
        private Transform _eye;
        private object _moveState;
        private int _refsForGroup = -1;

        private float _desiredYaw;
        private float _repathAt;
        private int _repathFails;
        private Vector3 _investigate;
        private bool _hasInvestigate;

        private Vector3 _lastSample;
        private float _sampleAt;
        private float _stuckSince = -1f;
        private int _sidestep = 1;
        private float _jumpUntil;
        private float _strafeFlip;
        private int _strafeDir = 1;
        private readonly Vector3[] _footwork = new Vector3[4];

        private const float RetargetSlack = 4f;

        private int _doctrineNature = -1;
        private float _pushUntil;
        private float _nextPushRoll;

        private int _lastFrame = -1;
        private int _lastButtons;

        internal float LastTick { get; private set; }

        internal void BindServer(object serverPlayer)
        {
            if (!ReferenceEquals(Server, serverPlayer)) { Server = serverPlayer; Invalidate(); }
        }

        private void Invalidate()
        {
            _refsForGroup = -1;
            _cc = null;
            _ccTransform = null;
            _eye = null;
            _moveState = null;
            _stuckSince = -1f;
            Eyes.Forget();
            Hearing.Clear();
            Cover.Forget();
            Nade.Reset();
            _doctrineNature = -1;
            ClearPath();
        }

        internal void OnDamaged(object attacker, Vector3 from, float damage)
        {
            bool sawItComing = Eyes.Visible && ReferenceEquals(Eyes.Target, attacker);
            Hearing.OnDamaged(attacker, from, damage, sawItComing);
            if (!sawItComing) Eyes.PreferTarget(attacker);
        }

        internal int Think(BotDirector dir)
        {
            if (_lastFrame == Time.frameCount) return _lastButtons;
            _lastFrame = Time.frameCount;
            LastTick = Time.time;
            _lastButtons = Decide(dir);
            return _lastButtons;
        }

        private int Decide(BotDirector dir)
        {
            if (Server == null) { State = "no body"; return 0; }

            var info = Refl.PlayerInfo(Server);
            Alive = info != null && !Refl.Dead(info);
            Team = info == null ? -1 : Refl.PlayerType(info);

            if (!Alive) { State = "dead"; Invalidate(); return 0; }
            if (!ResolveRefs()) { State = "unspawned"; return 0; }

            Position = _ccTransform.position;
            Muzzle = _eye != null ? _eye.position : Position + Vector3.up * 1.4f;
            Yaw = Refl.Euler(_moveState).y;
            float rad = Yaw * Mathf.Deg2Rad;
            Forward = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));

            var d = dir.Skill;
            RefreshDoctrine();
            Eyes.Scan(dir, this, d, Style);

            bool suppressFire;
            int nadeButtons = Nade.Update(dir, this, d, out suppressFire);
            if (Nade.Throwing)
            {
                float ny = Mathf.MoveTowardsAngle(Yaw, Nade.AimEuler.y, d.TurnRate * Time.deltaTime);
                float np = Mathf.MoveTowardsAngle(Pitch, Nade.AimEuler.x, d.TurnRate * Time.deltaTime);
                Pitch = np;
                ApplyLook(ny, np - 90f);
                State = "grenade (" + Nade.Why + ")";
                return nadeButtons;
            }

            int buttons = Eyes.Visible ? Engage(dir, d) : Search(dir, d);
            if (suppressFire) buttons &= ~Btn.Fire;
            buttons |= nadeButtons;
            buttons |= StuckLogic(dir, buttons);
            return buttons;
        }

        private void RefreshDoctrine()
        {
            var weapon = Refl.CurrentWeapon(Refl.Ammo(Server));
            int nature = Refl.WeaponNature(weapon);
            if (nature == _doctrineNature) return;

            _doctrineNature = nature;
            Style = Doctrine.For(nature, Refl.RangeMax(weapon), Group);
        }

        private bool WantsToPush(float dist)
        {
            if (Time.time < _pushUntil) return true;

            if (Time.time >= _nextPushRoll)
            {
                _nextPushRoll = Time.time + Random.Range(1.5f, 4f);
                if (Random.value < Style.PushChance) _pushUntil = Time.time + Random.Range(1.5f, 4.5f);
            }
            return Time.time < _pushUntil || dist > Style.IdealMax;
        }

        private int Engage(BotDirector dir, Difficulty d)
        {
            float tracked = Eyes.VisibleSince < 0f ? 0f : Time.time - Eyes.VisibleSince;
            Gun.UpdateAimNoise(d, tracked, Hearing.Panicking);

            Vector3 dirToTarget = Eyes.AimPoint - Muzzle;
            Vector3 want = Quaternion.LookRotation(dirToTarget).eulerAngles;
            float wantYaw = want.y + Gun.AimOffset.x;
            float wantPitch = want.x + Gun.AimOffset.y;

            float yaw = Mathf.MoveTowardsAngle(Yaw, wantYaw, d.TurnRate * Time.deltaTime);
            float pitch = Mathf.MoveTowardsAngle(Pitch, wantPitch, d.TurnRate * Time.deltaTime);
            Pitch = pitch;
            ApplyLook(yaw, pitch - 90f);

            Gun.AimAngle = Vector3.Angle(
                Quaternion.Euler(pitch, yaw, 0f) * Vector3.forward,
                dirToTarget);

            var ammo = Refl.Ammo(Server);
            var weapon = Refl.CurrentWeapon(ammo);
            var ws = Refl.WeaponState(weapon);

            float range = Mathf.Min(Style.MaxEngage, d.Sight);
            if (range < 6f) range = 6f;

            int buttons = Gun.FireButtons(this, Eyes, d,
                Refl.Accuracy(weapon), range,
                Refl.SingleShot(ws), Refl.WeaponEmpty(weapon), Refl.NeedReload(ws),
                Refl.Clips(ws), Refl.BagSize(ws));

            State = (Hearing.Panicking ? "panic-" : "") + Style.Name + " d=" + Eyes.Distance.ToString("F0")
                    + " a=" + Gun.AimAngle.ToString("F1") + (Time.time < _pushUntil ? " PUSH" : "");

            return buttons | CoverFootwork(dir, d, range);
        }

        private int CoverFootwork(BotDirector dir, Difficulty d, float range)
        {
            var grid = dir.Grid;
            if (!Plugin.UseCover.Value || grid == null) return CombatFootwork(dir, range);

            bool wantsCover = Hearing.Panicking
                              || Gun.Reloading
                              || (Hearing.RecentlyHurt(2.5f) && Style.Aggression < 0.75f);

            if (!wantsCover)
            {
                if (Time.time - Cover.CoverPickedAt > 6f) Cover.Forget();
                return CombatFootwork(dir, range);
            }

            if (Cover.CoverNode < 0 || Time.time - Cover.CoverPickedAt > 5f)
                Cover.Take(grid, Position, Eyes.AimPoint, Style, 16f);

            Vector3 at;
            if (!Cover.Holding(grid, Position, out at)) return CombatFootwork(dir, range);

            if (!Cover.AtCover(grid, Position))
            {
                Vector3 toCover = at - Position;
                toCover.y = 0f;
                float dc = toCover.magnitude;
                State = "to cover " + dc.ToString("F0");
                if (dc > 0.2f) return MoveButtons(Yaw, toCover / dc);
                return 0;
            }

            State = "in cover" + (Cover.CoverKindHeld == CoverKind.Crouch ? " (low)" : "");
            return Cover.CoverKindHeld == CoverKind.Crouch && !Gun.Firing ? Btn.Sit : 0;
        }

        private int CombatFootwork(BotDirector dir, float range)
        {
            if (Time.time >= _strafeFlip)
            {
                _strafeFlip = Time.time + Random.Range(0.5f, 1.3f);
                _strafeDir = -_strafeDir;
            }

            Vector3 toTarget = Eyes.AimPoint - Position;
            toTarget.y = 0f;
            float dist = toTarget.magnitude;
            if (dist < 0.4f) return 0;
            toTarget /= dist;

            Vector3 side = new Vector3(-toTarget.z, 0f, toTarget.x) * _strafeDir;

            Vector3 primary;
            if (dist > Style.IdealMax || WantsToPush(dist)) primary = toTarget;
            else if (dist < Style.IdealMin) primary = -toTarget;
            else primary = (side + toTarget * Style.Aggression * 0.65f).normalized;

            if (Hearing.Panicking) primary = (primary + side * 0.8f).normalized;

            _footwork[0] = primary;
            _footwork[1] = (primary + side).normalized;
            _footwork[2] = -side;
            _footwork[3] = dist < Style.IdealMin ? side : -toTarget;

            for (int i = 0; i < _footwork.Length; i++)
            {
                Vector3 move = _footwork[i];
                if (move.sqrMagnitude < 0.01f) continue;
                if (!NavQuery.DirectWalk(Position, Position + move * 1.6f, dir.Radius, dir.Height, dir.Mask)) continue;
                if (i > 0) _strafeDir = -_strafeDir;
                return MoveButtons(Yaw, move);
            }

            return 0;
        }

        private int Search(BotDirector dir, Difficulty d)
        {
            var grid = dir.Grid;

            bool hasFocus = false;
            Vector3 heard;

            if (Hearing.RecentlyHurt(7f)) { _investigate = Hearing.AttackerAt; hasFocus = true; }
            else if (Eyes.HasMemory(6f)) { _investigate = Eyes.LastKnown; hasFocus = true; }
            else if (Hearing.HeardSomething(6f, out heard)) { _investigate = heard; hasFocus = true; }
            else
            {
                Vector3 contact;
                if (dir.Blackboard.Contact(Team, 8f, out contact)) { _investigate = contact; hasFocus = true; }
            }

            if (grid == null) { State = "no nav"; return Wander(dir, d); }

            _hasInvestigate = hasFocus;
            if (hasFocus) FocusOn(grid, _investigate);

            EnsureDestination(dir, grid);
            if (Destination < 0) { State = "no dest"; return 0; }

            if (Path.Count == 0 || PathIndex >= Path.Count)
            {
                if (!Repath(dir, grid)) { State = "repathing"; return 0; }
            }

            AdvanceWaypoint(dir, grid);

            if (PathIndex >= Path.Count)
            {
                Destination = -1;
                ClearPath();
                State = "arrived";
                return 0;
            }

            Vector3 target = grid.Pos[Path[PathIndex]];
            Vector3 flat = new Vector3(target.x - Position.x, 0f, target.z - Position.z);
            float dist = flat.magnitude;
            if (dist > 0.01f) _desiredYaw = Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg;

            if (Hearing.RecentlyHurt(1.2f))
            {
                Vector3 spin = Hearing.AttackerAt - Position;
                spin.y = 0f;
                if (spin.sqrMagnitude > 0.25f) _desiredYaw = Mathf.Atan2(spin.x, spin.z) * Mathf.Rad2Deg;
            }

            float turnRate = Hearing.Panicking ? d.TurnRate * 1.5f : d.TurnRate;
            float yaw = Mathf.MoveTowardsAngle(Yaw, _desiredYaw, turnRate * Time.deltaTime);
            Pitch = Mathf.MoveTowardsAngle(Pitch, 0f, d.TurnRate * Time.deltaTime);
            ApplyLook(yaw, Pitch - 90f);

            State = (Hearing.Panicking ? "panic " : "move ") + PathIndex + "/" + Path.Count;
            return dist > 0.01f ? MoveButtons(yaw, flat / dist) : 0;
        }

        private void ApplyLook(float yaw, float eulerX)
        {
            Yaw = yaw;
            var cmd = Refl.BotCmd(Bot);
            if (cmd != null) Refl.SetCmdEuler(cmd, new Vector3(Mathf.Repeat(eulerX, 360f), yaw, 0f));
        }

        private static int MoveButtons(float yaw, Vector3 worldDir)
        {
            float rad = yaw * Mathf.Deg2Rad;
            Vector3 fwd = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
            Vector3 rgt = new Vector3(Mathf.Cos(rad), 0f, -Mathf.Sin(rad));
            return Btn.Move(Vector3.Dot(worldDir, fwd), Vector3.Dot(worldDir, rgt));
        }

        private bool ResolveRefs()
        {
            if (_refsForGroup == Group && _cc != null && _ccTransform != null && _moveState != null) return true;

            var mc = Refl.Controller(Server);
            if (mc == null) { _cc = null; _ccTransform = null; _moveState = null; return false; }

            _cc = Refl.CharController(mc);
            if (_cc == null) return false;
            _ccTransform = _cc.transform;
            _eye = Refl.RootCamera(mc);
            _moveState = Refl.State(mc);
            _refsForGroup = Group;
            return _moveState != null;
        }

        private void FocusOn(NavGrid grid, Vector3 focus)
        {
            if (Destination >= 0 && Destination < grid.Count
                && (grid.Pos[Destination] - focus).sqrMagnitude < RetargetSlack * RetargetSlack) return;

            int node = grid.Nearest(focus);
            if (node < 0 || node == Destination) return;

            Destination = node;
            _repathFails = 0;
            ClearPath();
        }

        private void EnsureDestination(BotDirector dir, NavGrid grid)
        {
            if (Destination >= 0 && Destination < grid.Count) return;
            Destination = dir.PickRoamTarget(grid, Position);
            ClearPath();
        }

        private bool Repath(BotDirector dir, NavGrid grid)
        {
            if (Time.time < _repathAt) return false;

            int start = grid.Nearest(Position);
            if (start < 0) { State = "off mesh"; _repathAt = Time.time + 0.35f; return false; }
            if (Destination < 0) return false;

            if (!dir.Query.FindPath(start, Destination, Path))
            {
                _repathAt = Time.time + 0.35f;
                _repathFails++;
                ClearPath();
                if (_repathFails >= 3) { Destination = -1; _repathFails = 0; }
                return false;
            }

            _repathFails = 0;
            PathIndex = Mathf.Min(1, Path.Count - 1);
            return true;
        }

        private void AdvanceWaypoint(BotDirector dir, NavGrid grid)
        {
            while (PathIndex < Path.Count)
            {
                Vector3 wp = grid.Pos[Path[PathIndex]];
                Vector3 d = new Vector3(wp.x - Position.x, 0f, wp.z - Position.z);
                if (d.sqrMagnitude > 0.55f * 0.55f && Mathf.Abs(wp.y - Position.y) < 2.5f) break;
                PathIndex++;
            }

            if (PathIndex < Path.Count)
            {
                int pulled = dir.Query.StringPull(Path, PathIndex, Position, dir.Radius, dir.Height, dir.Mask);
                if (pulled > PathIndex) PathIndex = pulled;
            }
        }

        private int StuckLogic(BotDirector dir, int moveButtons)
        {
            int extra = 0;
            bool wantsMove = (moveButtons & (Btn.Up | Btn.Down | Btn.Left | Btn.Right)) != 0;

            if (Time.time - _sampleAt >= 0.25f)
            {
                float moved = (Position - _lastSample).magnitude;
                _lastSample = Position;
                _sampleAt = Time.time;

                if (wantsMove && moved < 0.08f)
                {
                    if (_stuckSince < 0f) _stuckSince = Time.time;
                }
                else _stuckSince = -1f;
            }

            if (_stuckSince < 0f) return 0;

            float stuckFor = Time.time - _stuckSince;

            if (stuckFor > 0.3f && Time.time > _jumpUntil)
            {
                extra |= Btn.Jump;
                _jumpUntil = Time.time + 0.6f;
            }

            if (stuckFor > 0.8f) extra |= _sidestep > 0 ? Btn.Right : Btn.Left;

            if (stuckFor > 1.8f)
            {
                _sidestep = -_sidestep;
                _strafeDir = -_strafeDir;
                _stuckSince = -1f;
                _repathAt = 0f;
                ClearPath();
                if (++_repathFails >= 3) { Destination = -1; _repathFails = 0; }
            }

            return extra;
        }

        private int Wander(BotDirector dir, Difficulty d)
        {
            float turn = Mathf.MoveTowardsAngle(Yaw, _desiredYaw, d.TurnRate * Time.deltaTime);
            ApplyLook(turn, -90f);
            if (Mathf.Abs(Mathf.DeltaAngle(turn, _desiredYaw)) < 2f) _desiredYaw = Random.Range(0f, 360f);
            return Btn.Up;
        }

        internal void ClearPath()
        {
            Path.Clear();
            PathIndex = 0;
        }
    }
}
