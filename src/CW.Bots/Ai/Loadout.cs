using System;
using System.Collections.Generic;

namespace CW.Bots.Ai
{
    internal enum Archetype
    {
        Assault,
        Scout,
        Marksman,
        Breacher,
        Support
    }

    internal sealed class Kit
    {
        internal Archetype Type;
        internal int Primary = 127;
        internal int Secondary = 127;
        internal bool[] Skills;
        internal string Summary = "?";
    }

    internal static class Loadout
    {
        private const int NatureKnife = 2;
        private const int NaturePistol = 8;
        private const int NatureAutoPistol = 16;
        private const int NatureSmg = 32;
        private const int NatureRifle = 64;
        private const int NatureSniper = 128;
        private const int NatureShotgun = 256;
        private const int NatureMachinegun = 512;

        private static readonly string[] GrenadeLine =
        { "efd", "efd2", "efd_throw", "efd_dam", "efd_radius", "fragarmor" };

        private static int[] NaturesFor(Archetype a)
        {
            switch (a)
            {
                case Archetype.Assault: return new[] { NatureRifle };
                case Archetype.Scout: return new[] { NatureSmg, NatureAutoPistol };
                case Archetype.Marksman: return new[] { NatureSniper };
                case Archetype.Breacher: return new[] { NatureShotgun };
                default: return new[] { NatureMachinegun, NatureRifle };
            }
        }

        internal static Kit Roll(object userInfo, int seed, int skillCount, bool grenades)
        {
            var rng = new Random(seed);
            var kit = new Kit();
            kit.Type = (Archetype)rng.Next(5);

            var states = Refl.WeaponsStates(userInfo);
            if (states != null)
            {
                var wanted = NaturesFor(kit.Type);
                kit.Primary = PickWeapon(states, rng, 0, wanted);
                if (kit.Primary == 127) kit.Primary = PickWeapon(states, rng, 0, null);
                kit.Secondary = PickWeapon(states, rng, 1, new[] { NaturePistol, NatureAutoPistol });
                if (kit.Secondary == 127) kit.Secondary = PickWeapon(states, rng, 1, null);
            }

            kit.Skills = RollSkills(userInfo, rng, skillCount, grenades);
            kit.Summary = kit.Type + " p=" + kit.Primary + " s=" + kit.Secondary;
            return kit;
        }

        private static int PickWeapon(Array states, Random rng, int useType, int[] natures)
        {
            var pool = new List<int>();
            for (int i = 0; i < states.Length; i++)
            {
                var wi = states.GetValue(i);
                if (wi == null || !Refl.WeaponUnlocked(wi)) continue;

                var weapon = Refl.WeaponOf(wi);
                if (weapon == null) continue;
                if (Refl.WeaponUseType(weapon) != useType) continue;

                int nature = Refl.WeaponNature(weapon);
                if (nature == NatureKnife) continue;

                if (natures != null)
                {
                    bool ok = false;
                    for (int n = 0; n < natures.Length; n++) if (natures[n] == nature) { ok = true; break; }
                    if (!ok) continue;
                }
                pool.Add(i);
            }
            return pool.Count == 0 ? 127 : pool[rng.Next(pool.Count)];
        }

        private static bool[] RollSkills(object userInfo, Random rng, int count, bool grenades)
        {
            var infos = Refl.SkillsInfos(userInfo);
            if (infos == null) return null;

            var chosen = new bool[infos.Length];

            if (grenades)
            {
                for (int i = 0; i < GrenadeLine.Length; i++)
                {
                    int idx = Refl.SkillIndex(GrenadeLine[i]);
                    if (idx >= 0 && idx < chosen.Length) Unlock(infos, chosen, idx, 0);
                }
            }

            int guard = 0;
            int picked = 0;
            while (picked < count && guard++ < count * 12)
            {
                int idx = rng.Next(chosen.Length);
                if (chosen[idx]) continue;
                Unlock(infos, chosen, idx, 0);
                picked++;
            }

            return chosen;
        }

        private static void Unlock(Array infos, bool[] chosen, int index, int depth)
        {
            if (index < 0 || index >= chosen.Length || chosen[index] || depth > 8) return;
            chosen[index] = true;

            var si = infos.GetValue(index);
            var reqs = Refl.SkillRequirements(si);
            if (reqs == null) return;
            for (int i = 0; i < reqs.Length; i++) Unlock(infos, chosen, reqs[i], depth + 1);
        }

        internal static void Apply(object userInfo, object playerInfo, Kit kit)
        {
            if (kit.Skills == null) return;

            var infos = Refl.SkillsInfos(userInfo);
            if (infos == null) return;

            for (int i = 0; i < infos.Length && i < kit.Skills.Length; i++)
                Refl.SetSkillUnlocked(infos.GetValue(i), kit.Skills[i]);

            if (playerInfo != null) Refl.SetPlayerSkills(playerInfo, (bool[])kit.Skills.Clone());
        }
    }
}
