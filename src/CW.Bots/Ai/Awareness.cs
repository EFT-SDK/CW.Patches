using UnityEngine;

namespace CW.Bots.Ai
{
    internal sealed class Awareness
    {
        internal object Attacker;
        internal Vector3 AttackerAt;
        internal float HurtAt = -1f;
        internal float PanicUntil = -1f;
        internal float DamageBurst;

        internal Vector3 NoiseAt;
        internal float NoiseAt_Time = -1f;
        internal float NoiseLoudness;

        internal bool Panicking { get { return Time.time < PanicUntil; } }

        internal bool RecentlyHurt(float within)
        {
            return HurtAt > 0f && Time.time - HurtAt < within;
        }

        internal bool HeardSomething(float within, out Vector3 where)
        {
            where = NoiseAt;
            return NoiseAt_Time > 0f && Time.time - NoiseAt_Time < within;
        }

        internal void OnDamaged(object attacker, Vector3 from, float damage, bool sawItComing)
        {
            if (HurtAt < 0f || Time.time - HurtAt > 1.5f) DamageBurst = 0f;

            Attacker = attacker;
            AttackerAt = from;
            HurtAt = Time.time;
            DamageBurst += damage;

            float panic = sawItComing ? 0.35f : 1.1f;
            if (DamageBurst > 45f) panic += 0.6f;

            float until = Time.time + panic;
            if (until > PanicUntil) PanicUntil = until;

            NoiseAt = from;
            NoiseAt_Time = Time.time;
            NoiseLoudness = 1f;
        }

        internal void OnNoise(Vector3 where, float loudness)
        {
            if (NoiseAt_Time > 0f && Time.time - NoiseAt_Time < 0.4f && loudness < NoiseLoudness) return;
            NoiseAt = where;
            NoiseAt_Time = Time.time;
            NoiseLoudness = loudness;
        }

        internal void Clear()
        {
            Attacker = null;
            HurtAt = -1f;
            PanicUntil = -1f;
            DamageBurst = 0f;
            NoiseAt_Time = -1f;
        }
    }
}
