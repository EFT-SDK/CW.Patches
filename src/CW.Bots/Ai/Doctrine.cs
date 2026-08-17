using UnityEngine;

namespace CW.Bots.Ai
{
    internal sealed class Doctrine
    {
        internal string Name = "default";
        internal float IdealMin = 8f;
        internal float IdealMax = 35f;
        internal float MaxEngage = 55f;
        internal float Notice = 90f;
        internal float Aggression = 0.5f;
        internal float PushChance = 0.35f;
        internal bool Wildcard;

        private const int Knife = 2;
        private const int Pistol = 8;
        private const int AutoPistol = 16;
        private const int Smg = 32;
        private const int Rifle = 64;
        private const int Sniper = 128;
        private const int Shotgun = 256;
        private const int Machinegun = 512;

        internal static Doctrine For(int nature, float weaponRange, int seed)
        {
            switch (nature)
            {
                case Shotgun:
                    return new Doctrine
                    {
                        Name = "shotgun",
                        IdealMin = 0f, IdealMax = 7f, MaxEngage = 16f, Notice = 70f,
                        Aggression = 1f, PushChance = 0.95f
                    };

                case Knife:
                    return new Doctrine
                    {
                        Name = "knife",
                        IdealMin = 0f, IdealMax = 2f, MaxEngage = 3f, Notice = 60f,
                        Aggression = 1f, PushChance = 1f
                    };

                case Sniper:
                    return new Doctrine
                    {
                        Name = "sniper",
                        IdealMin = 35f, IdealMax = Mathf.Max(70f, weaponRange * 1.4f),
                        MaxEngage = Mathf.Max(90f, weaponRange * 1.8f), Notice = 140f,
                        Aggression = 0.1f, PushChance = 0.05f
                    };

                case Smg:
                case AutoPistol:
                    return new Doctrine
                    {
                        Name = "smg",
                        IdealMin = 4f, IdealMax = 18f, MaxEngage = 34f, Notice = 80f,
                        Aggression = 0.8f, PushChance = 0.7f
                    };

                case Rifle:
                    return new Doctrine
                    {
                        Name = "rifle",
                        IdealMin = 9f, IdealMax = 42f, MaxEngage = Mathf.Max(60f, weaponRange * 1.5f), Notice = 110f,
                        Aggression = 0.55f, PushChance = 0.45f
                    };

                case Machinegun:
                    return new Doctrine
                    {
                        Name = "mg",
                        IdealMin = 14f, IdealMax = 48f, MaxEngage = Mathf.Max(65f, weaponRange * 1.5f), Notice = 110f,
                        Aggression = 0.35f, PushChance = 0.25f
                    };

                case Pistol:
                    return Wild(seed);

                default:
                    return new Doctrine();
            }
        }

        private static Doctrine Wild(int seed)
        {
            var rng = new System.Random(seed ^ 0x5EED);
            float aggro = 0.15f + (float)rng.NextDouble() * 0.85f;
            float near = 2f + (float)rng.NextDouble() * 10f;
            float far = near + 8f + (float)rng.NextDouble() * 30f;

            return new Doctrine
            {
                Name = "wildcard",
                IdealMin = near,
                IdealMax = far,
                MaxEngage = far + 10f + (float)rng.NextDouble() * 20f,
                Notice = 85f,
                Aggression = aggro,
                PushChance = aggro,
                Wildcard = true
            };
        }
    }
}
