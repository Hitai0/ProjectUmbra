namespace Umbra
{
    public enum DamageSourceKind { Contact, Projectile, Hazard, Legacy }
    public enum DamageRejectReason { None, Invalid, InactiveRun, Immune, StaleSource }

    public readonly struct DamageRequest
    {
        public readonly int RawDamage;
        public readonly DamageSourceKind Kind;
        public readonly ulong SourceId;
        // Contact only. Others use -1 / 0 and own an immutable event snapshot.
        public readonly int EnemyIndex;
        public readonly uint SpawnGeneration;

        public DamageRequest(int raw, DamageSourceKind kind, ulong id,
            int enemyIndex = -1, uint generation = 0)
        {
            RawDamage = raw;
            Kind = kind;
            SourceId = id;
            EnemyIndex = enemyIndex;
            SpawnGeneration = generation;
        }
    }

    public readonly struct DamageResult
    {
        public readonly int RequestedDamage, MitigatedDamage, AppliedDamage;
        public readonly DamageRejectReason Rejected;
        public bool Applied => AppliedDamage > 0;
        public DamageResult(int raw, int mitigated, int applied, DamageRejectReason rejected)
        {
            RequestedDamage = raw;
            MitigatedDamage = mitigated;
            AppliedDamage = applied;
            Rejected = rejected;
        }
    }
}
