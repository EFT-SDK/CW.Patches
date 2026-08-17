namespace CW.Bots.Ai
{
    internal sealed class Difficulty
    {
        internal string Name;
        internal float Reaction;
        internal float AimError;
        internal float SettleTime;
        internal float TurnRate;
        internal float Fov;
        internal float Sight;
        internal float BurstOn;
        internal float BurstOff;
        internal float HeadBias;

        private static readonly Difficulty[] Tiers =
        {
            new Difficulty { Name = "recruit", Reaction = 0.70f, AimError = 6.5f, SettleTime = 1.6f, TurnRate = 200f, Fov = 105f, Sight = 55f,  BurstOn = 0.20f, BurstOff = 0.55f, HeadBias = 0.00f },
            new Difficulty { Name = "regular", Reaction = 0.45f, AimError = 3.5f, SettleTime = 1.1f, TurnRate = 320f, Fov = 125f, Sight = 85f,  BurstOn = 0.30f, BurstOff = 0.35f, HeadBias = 0.15f },
            new Difficulty { Name = "veteran", Reaction = 0.28f, AimError = 2.0f, SettleTime = 0.8f, TurnRate = 460f, Fov = 145f, Sight = 115f, BurstOn = 0.42f, BurstOff = 0.24f, HeadBias = 0.40f },
            new Difficulty { Name = "elite",   Reaction = 0.16f, AimError = 1.0f, SettleTime = 0.5f, TurnRate = 620f, Fov = 165f, Sight = 145f, BurstOn = 0.60f, BurstOff = 0.15f, HeadBias = 0.70f }
        };

        internal static int Count { get { return Tiers.Length; } }

        internal static Difficulty Get(int tier)
        {
            if (tier < 0) tier = 0;
            if (tier >= Tiers.Length) tier = Tiers.Length - 1;
            return Tiers[tier];
        }

        internal static string NameOf(int tier) { return Get(tier).Name; }
    }
}
