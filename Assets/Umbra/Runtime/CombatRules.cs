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
    }
}
