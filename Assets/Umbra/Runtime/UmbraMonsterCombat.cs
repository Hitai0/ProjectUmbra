using UnityEngine;

namespace Umbra
{
    public sealed partial class UmbraPrototype
    {
        public void ConfigureContactProfile(Enemy e, bool champion)
        {
            e.damageSourceId = AllocDamageSourceId();
            e.spawnGeneration++;
            e.isChampion = champion;
            e.contactRadius = champion ? DamageRules.ChampionRadius : (e.elite ? DamageRules.EliteRadius : DamageRules.CommonRadius);
            e.contactDamage = DamageRules.ContactDamage(champion, e.elite, SelectedMap, RunTimer);
            e.nextContactAt = Time.time;
            e.windupUntil = 0;
        }

        public bool TryContactAttack(Enemy e, int index, float now)
        {
            if (e.hp <= 0 || !e.root.gameObject.activeSelf || e.boss) return false;
            if (now < e.nextContactAt) return false;
            if (!DamageRules.Touching(Player.position, e.root.position, e.contactRadius)) return false;

            var req = new DamageRequest(e.contactDamage, DamageSourceKind.Contact, e.damageSourceId, index, e.spawnGeneration);
            return OfferPlayerDamage(req);
        }
    }
}
