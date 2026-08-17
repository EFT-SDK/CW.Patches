using System.Collections;
using UnityEngine;

namespace CW.Bots.Ai
{
    internal sealed class Grenades
    {
        internal const float LaunchSpeed = 20f;
        internal const float BlastRadius = 7f;
        internal const float SafeSelf = 11f;

        internal bool Throwing { get { return _state != 0; } }
        internal Vector3 AimEuler;
        internal string Why = string.Empty;

        private int _state;
        private float _stateUntil;
        private float _nextTry;
        private Vector3 _mark;

        internal void Reset()
        {
            _state = 0;
            _nextTry = 0f;
        }

        internal int Update(BotDirector dir, BotAgent self, Difficulty d, out bool suppressFire)
        {
            suppressFire = false;

            if (_state == 1)
            {
                suppressFire = true;
                if (Time.time < _stateUntil) return Btn.Grenade;
                _state = 2;
                _stateUntil = Time.time + 0.25f;
                return 0;
            }

            if (_state == 2)
            {
                suppressFire = true;
                if (Time.time < _stateUntil) return 0;
                _state = 0;
                return 0;
            }

            if (!Plugin.UseGrenades.Value) return 0;
            if (Time.time < _nextTry) return 0;
            _nextTry = Time.time + 0.5f;

            if (!ShouldThrow(dir, self, d)) return 0;

            _state = 1;
            _stateUntil = Time.time + Mathf.Lerp(0.55f, 0.25f, d.HeadBias);
            suppressFire = true;
            return Btn.Grenade;
        }

        private bool ShouldThrow(BotDirector dir, BotAgent self, Difficulty d)
        {
            var ammo = Refl.Ammo(self.Server);
            if (ammo == null) return false;
            if (Refl.GrenadeCount(Refl.AmmoState(ammo)) <= 0) return false;
            if (Refl.GrenadeTimer(ammo) > 0f) return false;

            Vector3 target;
            if (!PickTarget(self, out target)) return false;

            float flat = new Vector2(target.x - self.Muzzle.x, target.z - self.Muzzle.z).magnitude;
            if (flat < 8f || flat > 32f) return false;
            if (Vector3.Distance(self.Muzzle, target) < SafeSelf) return false;

            if (TeammateNear(dir, self, target)) return false;

            float pitch;
            if (!Solve(self.Muzzle, target, Refl.ThrowPower(ammo), out pitch)) return false;
            if (!ArcClear(self.Muzzle, target, pitch, Refl.ThrowPower(ammo), dir.Mask)) return false;

            Vector3 to = target - self.Muzzle;
            float yaw = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
            AimEuler = new Vector3(pitch, yaw, 0f);
            _mark = target;
            return true;
        }

        private bool PickTarget(BotAgent self, out Vector3 target)
        {
            target = Vector3.zero;

            if (self.Eyes.Visible && self.Eyes.Target != null)
            {
                target = self.Eyes.AimPoint;
                target.y -= 1.2f;
                Why = "visible";
                return true;
            }

            if (self.Eyes.HasMemory(4f))
            {
                target = self.Eyes.LastKnown;
                target.y -= 1.2f;
                Why = "last known";
                return true;
            }

            if (self.Hearing.RecentlyHurt(4f))
            {
                target = self.Hearing.AttackerAt;
                Why = "return fire";
                return true;
            }

            return false;
        }

        private static bool TeammateNear(BotDirector dir, BotAgent self, Vector3 point)
        {
            var game = dir.CachedGame;
            if (game == null) return true;

            IList list = Refl.ServerPlayers(game);
            if (list == null) return true;

            float sq = (BlastRadius + 1.5f) * (BlastRadius + 1.5f);

            for (int i = 0; i < list.Count; i++)
            {
                var sp = list[i];
                if (sp == null || ReferenceEquals(sp, self.Server)) continue;

                var info = Refl.PlayerInfo(sp);
                if (info == null || Refl.Dead(info)) continue;

                int type = Refl.PlayerType(info);
                if (type == 2) continue;
                if (dir.IsTeamGame && type != self.Team) continue;
                if (!dir.IsTeamGame) continue;

                Vector3 at;
                if (!Refl.TryPosition(sp, out at)) continue;
                if ((at - point).sqrMagnitude < sq) return true;
            }
            return false;
        }

        internal static bool Solve(Vector3 from, Vector3 to, float throwPower, out float pitch)
        {
            pitch = 0f;

            float v = LaunchSpeed * (throwPower <= 0f ? 1f : throwPower);
            float g = Mathf.Abs(Physics.gravity.y);
            if (g < 0.01f) return false;

            Vector3 delta = to - from;
            float x = new Vector2(delta.x, delta.z).magnitude;
            float y = delta.y;
            if (x < 0.5f) return false;

            float v2 = v * v;
            float disc = v2 * v2 - g * (g * x * x + 2f * y * v2);
            if (disc < 0f) return false;

            float root = Mathf.Sqrt(disc);
            float lowAngle = Mathf.Atan((v2 - root) / (g * x));

            pitch = -lowAngle * Mathf.Rad2Deg;
            return true;
        }

        internal static bool ArcClear(Vector3 from, Vector3 to, float pitchDeg, float throwPower, int mask)
        {
            float v = LaunchSpeed * (throwPower <= 0f ? 1f : throwPower);
            float g = Mathf.Abs(Physics.gravity.y);

            Vector3 delta = to - from;
            Vector3 flat = new Vector3(delta.x, 0f, delta.z);
            float dist = flat.magnitude;
            if (dist < 0.5f) return false;
            flat /= dist;

            float theta = -pitchDeg * Mathf.Deg2Rad;
            float vx = v * Mathf.Cos(theta);
            float vy = v * Mathf.Sin(theta);
            if (vx < 0.1f) return false;

            float flight = dist / vx;
            const int steps = 7;
            Vector3 prev = from;

            for (int i = 1; i <= steps; i++)
            {
                float t = flight * i / steps;
                Vector3 p = from + flat * (vx * t) + Vector3.up * (vy * t - 0.5f * g * t * t);
                if (i < steps && Physics.Linecast(prev, p, mask)) return false;
                prev = p;
            }
            return true;
        }
    }
}
