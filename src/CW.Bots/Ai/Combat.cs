using UnityEngine;

namespace CW.Bots.Ai
{
    internal sealed class Combat
    {
        internal Vector2 AimOffset;
        internal float AimAngle = 180f;
        internal bool Firing;
        internal bool Reloading;

        private float _nextOffsetChange;
        private Vector2 _offsetTarget;
        private float _burstUntil;
        private float _gapUntil;
        private bool _fireHeld;
        private float _reloadHeldUntil;
        private float _nextReloadPress;

        internal void UpdateAimNoise(Difficulty d, float trackedFor, bool panicking)
        {
            float panicMult = panicking ? 2.2f : 1f;
            if (Time.time >= _nextOffsetChange)
            {
                _nextOffsetChange = Time.time + Random.Range(0.12f, 0.3f);
                _offsetTarget = Random.insideUnitCircle * d.AimError * panicMult;
            }

            float settle = d.SettleTime <= 0f ? 1f : Mathf.Clamp01(trackedFor / d.SettleTime);
            float scale = Mathf.Lerp(1.8f, 0.35f, settle) * panicMult;
            AimOffset = Vector2.Lerp(AimOffset, _offsetTarget * scale, Time.deltaTime * 6f);
        }

        internal int FireButtons(BotAgent self, Perception p, Difficulty d, float weaponAccuracy, float weaponRange, bool singleShot, bool empty, bool needReload, int clips, int bag)
        {
            int buttons = 0;
            Firing = false;

            Reloading = clips <= 0 && bag > 0;
            if (Reloading) return ReloadPulse();
            if (empty) return 0;

            if (!p.Visible || p.Target == null)
            {
                if (needReload && bag > 0 && !p.HasMemory(2.5f)) return ReloadPulse();
                return 0;
            }

            if (p.VisibleSince < 0f || Time.time - p.VisibleSince < d.Reaction) return 0;
            if (p.Distance > weaponRange) return 0;

            float gate = Mathf.Max(1.2f, weaponAccuracy * 0.55f);
            if (AimAngle > gate) return 0;

            if (p.Distance > 18f && weaponRange > 30f) buttons |= Btn.Aim;

            if (Time.time < _gapUntil) { _fireHeld = false; return buttons; }

            if (singleShot)
            {
                _fireHeld = !_fireHeld;
                if (_fireHeld) { Firing = true; buttons |= Btn.Fire; _gapUntil = Time.time + 0.12f; }
                return buttons;
            }

            if (Time.time >= _burstUntil)
            {
                if (_fireHeld)
                {
                    _fireHeld = false;
                    _gapUntil = Time.time + d.BurstOff * Mathf.Lerp(0.6f, 1.6f, Mathf.Clamp01(p.Distance / weaponRange));
                    return buttons;
                }
                _fireHeld = true;
                _burstUntil = Time.time + d.BurstOn;
            }

            Firing = true;
            buttons |= Btn.Fire;
            return buttons;
        }

        private int ReloadPulse()
        {
            if (Time.time < _reloadHeldUntil) return 0;
            if (Time.time < _nextReloadPress) return 0;
            _reloadHeldUntil = Time.time + 0.05f;
            _nextReloadPress = Time.time + 2.5f;
            return Btn.Reload;
        }
    }
}
