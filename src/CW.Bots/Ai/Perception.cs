using System.Collections;
using UnityEngine;

namespace CW.Bots.Ai
{
    internal sealed class Perception
    {
        internal object Target;
        internal Transform TargetEye;
        internal Vector3 AimPoint;
        internal bool Visible;
        internal float VisibleSince = -1f;
        internal float LastSeen = -1f;
        internal Vector3 LastKnown;
        internal float Distance;

        private float _nextScan;
        private object _lastTarget;
        private object _preferred;
        private float _preferredUntil;

        internal bool HasMemory(float within)
        {
            return LastSeen > 0f && Time.time - LastSeen < within;
        }

        internal void Forget()
        {
            Target = null;
            _preferred = null;
            _preferredUntil = -1f;
            TargetEye = null;
            Visible = false;
            VisibleSince = -1f;
        }

        internal void PreferTarget(object who)
        {
            _preferred = who;
            _preferredUntil = Time.time + 4f;
            _nextScan = 0f;
        }

        internal void Scan(BotDirector dir, BotAgent self, Difficulty d, Doctrine style)
        {
            if (Time.time < _nextScan)
            {
                if (Target != null) Refresh(dir, self, d, style);
                return;
            }
            _nextScan = Time.time + 0.1f;

            var game = dir.CachedGame;
            if (game == null) { Forget(); return; }

            IList list = Refl.ServerPlayers(game);
            if (list == null) { Forget(); return; }

            bool teamGame = dir.IsTeamGame;
            Vector3 muzzle = self.Muzzle;
            float bestScore = float.MaxValue;
            object best = null;
            Transform bestEye = null;
            Vector3 bestPoint = Vector3.zero;
            float bestDist = 0f;

            for (int i = 0; i < list.Count; i++)
            {
                var sp = list[i];
                if (sp == null || ReferenceEquals(sp, self.Server)) continue;

                var info = Refl.PlayerInfo(sp);
                if (info == null || Refl.Dead(info)) continue;

                int type = Refl.PlayerType(info);
                if (type == 2) continue;
                if (teamGame && type == self.Team) continue;

                var mc = Refl.Controller(sp);
                if (mc == null) continue;
                var eye = Refl.RootCamera(mc);
                if (eye == null) continue;

                Vector3 point = eye.position - Vector3.up * (0.35f * (1f - d.HeadBias));
                Vector3 delta = point - muzzle;
                float dist = delta.magnitude;
                if (dist > d.Sight || dist > style.Notice) continue;

                bool wasHitBy = Time.time < _preferredUntil && ReferenceEquals(sp, _preferred);
                float angle = Vector3.Angle(self.Forward, delta);
                if (angle > d.Fov * 0.5f && !wasHitBy) continue;

                if (Physics.Linecast(muzzle, point, dir.Mask)) continue;

                float score = dist * (1f + angle / 180f);
                if (ReferenceEquals(sp, Target)) score *= 0.65f;
                if (wasHitBy) score *= 0.2f;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = sp;
                    bestEye = eye;
                    bestPoint = point;
                    bestDist = dist;
                }
            }

            if (best == null)
            {
                if (Visible) LastSeen = Time.time;
                Visible = false;
                return;
            }

            if (!ReferenceEquals(best, _lastTarget) || VisibleSince < 0f)
            {
                VisibleSince = Time.time;
                _lastTarget = best;
            }

            Target = best;
            TargetEye = bestEye;
            AimPoint = bestPoint;
            Distance = bestDist;
            Visible = true;
            LastSeen = Time.time;
            LastKnown = bestPoint;
            dir.Blackboard.Report(self.Team, bestPoint);
        }

        private void Refresh(BotDirector dir, BotAgent self, Difficulty d, Doctrine style)
        {
            if (TargetEye == null) { Visible = false; return; }

            var info = Refl.PlayerInfo(Target);
            if (info == null || Refl.Dead(info)) { Forget(); return; }

            Vector3 point = TargetEye.position - Vector3.up * (0.35f * (1f - d.HeadBias));
            Vector3 muzzle = self.Muzzle;
            Vector3 delta = point - muzzle;

            Distance = delta.magnitude;
            AimPoint = point;

            bool blocked = Physics.Linecast(muzzle, point, dir.Mask);
            bool inRange = Distance <= d.Sight && Distance <= style.Notice;
            bool inFov = Vector3.Angle(self.Forward, delta) <= d.Fov * 0.5f;

            if (!blocked && inRange && inFov)
            {
                if (!Visible) VisibleSince = Time.time;
                Visible = true;
                LastSeen = Time.time;
                LastKnown = point;
            }
            else
            {
                if (Visible) LastSeen = Time.time;
                Visible = false;
                VisibleSince = -1f;
            }
        }
    }
}
