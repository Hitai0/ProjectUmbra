using UnityEngine;

namespace Umbra
{
    public static class DamageRules
    {
        public const float HitGrace = .35f;
        public const float ContactInterval = .60f;
        public const float PlayerRadius = .45f;
        public const float CommonRadius = .55f;
        public const float EliteRadius = .75f;
        public const float ChampionRadius = .90f;

        public const int CommonBaseContact = 10;
        public const int EliteBaseContact = 20;
        public const int ChampionBaseContact = 24;

        public static int ContactDamage(bool isChampion, bool isElite, int mapIndex, float runSeconds)
        {
            int baseDmg = isChampion ? ChampionBaseContact : isElite ? EliteBaseContact : CommonBaseContact;
            return Mathf.RoundToInt(baseDmg * (1f + mapIndex * .3f + runSeconds / 1200f));
        }

        public static int Incoming(int raw, int armor, float reduction)
        {
            if (raw <= 0) return 0;
            if (float.IsNaN(reduction) || float.IsInfinity(reduction)) reduction = 0;
            float afterArmor = Mathf.Max(1f, (float)raw - Mathf.Max(0, armor));
            return Mathf.Max(1, Mathf.CeilToInt(afterArmor * (1f - Mathf.Clamp(reduction, 0f, .8f))));
        }

        public static bool Touching(Vector3 a, Vector3 b, float enemyRadius)
        {
            float dx = a.x - b.x, dz = a.z - b.z;
            float radius = PlayerRadius + enemyRadius;
            return dx * dx + dz * dz <= radius * radius;
        }
    }
}
