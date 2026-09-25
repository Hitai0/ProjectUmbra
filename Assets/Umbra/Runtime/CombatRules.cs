namespace Umbra
{
    // Shared single-player balance rules. All stat formulas use 1 as the baseline.
    public enum RuneKind { Pierce, Scatter, Ember }

    public static class CombatRules
    {
        public enum StatKind { STR, AGI, VIT, INT, DEX, LUK }

        public const float AttackCooldown = .36f;
        public const float VolleyCooldown = 7f;
        public const float DashCooldown = 1.8f;
        public const float HealCooldown = 22f;
        public const int MaxHealth = 120;
        public const int StatPointsPerLevel = 3;
        public const float CritMultiplier = 1.75f;

        public static float DamageMultiplier(int str, int dex) => 1f + (str - 1) * 0.02f + (dex - 1) * 0.015f;
        public static float AttackSpeedBonus(int agi) => (agi - 1) * 0.015f;
        public static float MoveSpeedBonus(int agi) => (agi - 1) * 0.004f;
        public static int BonusHealthFromVit(int vit) => (vit - 1) * 8;
        public static float HealthRegenPerSecond(int vit) => (vit - 1) * 0.08f;
        public static float CooldownReduction(int intel) => UnityEngine.Mathf.Min(0.50f, (intel - 1) * 0.012f);
        public static float ProjectileSpeedBonus(int dex) => (dex - 1) * 0.02f;
        public static float CritChance(int luk) => UnityEngine.Mathf.Min(.65f, .05f + (luk - 1) * 0.005f);

        public static int Damage(RuneKind rune, int level, int str = 1, int dex = 1)
        {
            int baseDmg = (rune == RuneKind.Scatter ? 12 : 23) + UnityEngine.Mathf.Min(30, level - 1);
            return UnityEngine.Mathf.RoundToInt(baseDmg * DamageMultiplier(str, dex));
        }

        public static int ProjectileCount(RuneKind rune) => rune == RuneKind.Scatter ? 3 : 1;
        public static int ExperienceToLevel(int level) => 60 + (level - 1) * 35;
        public static int RunExperienceToLevel(int level) => 140 + (level - 1) * 60 + (level - 1) * (level - 1) * 8;
        // Fixed time-based pressure: progression never silently scales enemies to the player's build.
        public const int EnemyPoolSize = 72;
        public const float EliteArrival = 45f;
        public const float ChampionArrival = 120f;
        public static int HordeCap(float seconds) => UnityEngine.Mathf.Min(64, 18 + (int)(seconds / 15f));
        public static int HordeBatch(float seconds) => seconds < 60 ? 4 : seconds < 180 ? 5 : seconds < 420 ? 6 : 8;
        public static float HordeInterval(float seconds) => UnityEngine.Mathf.Max(.65f, 1.25f - seconds / 1200f);
        public static int EnemyHealth(bool elite, int map, float seconds) => UnityEngine.Mathf.RoundToInt((elite ? 240 : 65) * (1 + map * .5f + seconds / 480f));
        public static int EnemyDamage(bool elite, int map, float seconds) => UnityEngine.Mathf.RoundToInt((elite ? 30 : 16) * (1 + map * .3f + seconds / 1200f));
        public static int GuardianHealth(int map) => 3200 + map * 1400;
        public const float BossTime = 840f;
        public const float RunDuration = 900f;
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
            WindNovaPulse,
            Chain,
            Ignite,
            SproutCard,
            MushroomCard,
            BattleFocus
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
            new(PerkKind.RapidFire, "Quickdraw", "STAT BOON", "+15% attack rate. Up to 4 ranks.", "COMMON"),
            new(PerkKind.SwiftBoots, "Forest Stride", "STAT BOON", "+8% movement speed. Up to 3 ranks.", "COMMON"),
            new(PerkKind.Multishot, "Twin Flight", "COMBAT BOON", "+1 projectile. Swordsman gains +20% slash damage instead. Up to 2 ranks.", "EPIC"),
            new(PerkKind.Vitality, "Roots of Life", "SURVIVAL BOON", "+20 Max Health and restore 30 HP. Up to 4 ranks.", "COMMON"),
            new(PerkKind.Magnetism, "Amber Attraction", "UTILITY BOON", "+2m shard pickup radius. Up to 2 ranks.", "COMMON"),
            new(PerkKind.WindNovaPulse, "Gale Ward", "PASSIVE BOON", "Unleash a radial nova every 10 seconds, independently of Q.", "EPIC"),
            new(PerkKind.Chain, "Branching Spirit", "SHARED SUPPORT", "Each projectile's first hit chains to one nearby foe for 40% damage.", "RARE"),
            new(PerkKind.Ignite, "Kindling", "SHARED SUPPORT", "Hits burn for 30% damage over 3 seconds. Refreshes; does not stack.", "RARE"),
            new(PerkKind.SproutCard, "Sprout Card", "MONSTER CARD", "+1 projectile pierce. Swordsman gains +0.4m slash reach instead.", "RARE"),
            new(PerkKind.MushroomCard, "Mushroom Card", "MONSTER CARD", "Defeating an enemy restores 1 HP.", "RARE"),
            new(PerkKind.BattleFocus, "Battle Focus", "STAT BOON", "+8% damage this run. Always available.", "COMMON")
        };

        public static System.Collections.Generic.List<Perk> RollPerks(int count = 3, int luk = 1)
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
