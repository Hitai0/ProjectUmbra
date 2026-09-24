namespace Umbra
{
    // Engine-independent rules: suitable for a future authoritative server.
    public enum RuneKind { Pierce, Scatter, Ember }

    public static class CombatRules
    {
        public const float AttackCooldown = .36f;
        public const float VolleyCooldown = 4.5f;
        public const float DashCooldown = 1.25f;
        public const float HealCooldown = 12f;
        public const int MaxHealth = 120;
        public static int Damage(RuneKind rune, int level) => (rune == RuneKind.Scatter ? 13 : 23) + (level - 1) * 3;
        public static int ProjectileCount(RuneKind rune) => rune == RuneKind.Scatter ? 3 : 1;
        public static int ExperienceToLevel(int level) => 60 + (level - 1) * 35;
        public static string RuneName(RuneKind rune) => rune switch
        {
            RuneKind.Pierce => "PIERCING WIND",
            RuneKind.Scatter => "SPLIT SHOT",
            _ => "EMBER SEED"
        };
        public static string RuneDescription(RuneKind rune) => rune switch
        {
            RuneKind.Pierce => "One arrow. Passes through up to three enemies.",
            RuneKind.Scatter => "Three arrows in a fan. Lower damage per arrow.",
            _ => "Arrows burst on impact, damaging nearby enemies."
        };

        public enum PerkKind
        {
            RunePierce,
            RuneScatter,
            RuneEmber,
            RapidFire,
            SwiftBoots,
            Multishot,
            Vitality,
            Magnetism,
            WindNovaPulse
        }

        public sealed class Perk
        {
            public PerkKind Kind;
            public string Title;
            public string Category;
            public string Description;
            public string Rarity;
            public Perk(PerkKind kind, string title, string category, string desc, string rarity = "COMMON")
            {
                Kind = kind; Title = title; Category = category; Description = desc; Rarity = rarity;
            }
        }

        public static readonly Perk[] AllPerks = new Perk[]
        {
            new(PerkKind.RunePierce, "Piercing Wind", "SUPPORT RUNE", "Arrows pierce up to 3 enemies in a straight line.", "RARE"),
            new(PerkKind.RuneScatter, "Split Shot", "SUPPORT RUNE", "Fires 3 arrows in a wide spread fan.", "RARE"),
            new(PerkKind.RuneEmber, "Ember Seed", "SUPPORT RUNE", "Arrows explode on impact, damaging nearby foes.", "RARE"),
            new(PerkKind.RapidFire, "Quickdraw", "STAT BOON", "+25% faster attack rate. Loose arrows like the wind.", "COMMON"),
            new(PerkKind.SwiftBoots, "Forest Stride", "STAT BOON", "+18% movement speed. Glide smoothly through the grove.", "COMMON"),
            new(PerkKind.Multishot, "Twin Flight", "COMBAT BOON", "+1 additional arrow to all attacks.", "EPIC"),
            new(PerkKind.Vitality, "Roots of Life", "SURVIVAL BOON", "+35 Max Health and instantly heals 50 HP.", "COMMON"),
            new(PerkKind.Magnetism, "Amber Attraction", "UTILITY BOON", "+60% Amber shard collection pull radius.", "COMMON"),
            new(PerkKind.WindNovaPulse, "Gale Ward", "PASSIVE BOON", "Automatically unleashes a radial Wind Nova every 8 seconds.", "EPIC")
        };

        public static System.Collections.Generic.List<Perk> RollPerks(int count = 3)
        {
            var list = new System.Collections.Generic.List<Perk>(AllPerks);
            var result = new System.Collections.Generic.List<Perk>();
            for (int i = 0; i < count && list.Count > 0; i++)
            {
                int idx = UnityEngine.Random.Range(0, list.Count);
                result.Add(list[idx]);
                list.RemoveAt(idx);
            }
            return result;
        }
    }
}
