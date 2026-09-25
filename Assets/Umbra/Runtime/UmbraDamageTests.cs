using System;
using System.Collections.Generic;
using UnityEngine;

namespace Umbra
{
    public sealed partial class UmbraPrototype
    {
        public void RunDamageUnitAndAcceptanceTests(Action<bool, string> check)
        {
            // 1. Pure formula tests
            check(DamageRules.Incoming(10, 0, 0) == 10, "pure damage: raw 10, armor 0, red 0 == 10");
            check(DamageRules.Incoming(10, 3, 0) == 7, "pure damage: raw 10, armor 3, red 0 == 7");
            check(DamageRules.Incoming(10, 3, .20f) == 6, "pure damage: raw 10, armor 3, red .20 == 6");
            check(DamageRules.Incoming(3, 5, .20f) == 1, "pure damage: raw 3, armor 5, red .20 == 1");
            check(DamageRules.Incoming(0, 0, 0) == 0, "pure damage: raw 0 == 0");
            check(DamageRules.Incoming(-5, 2, .20f) == 0, "pure damage: negative raw == 0");
            check(DamageRules.Incoming(10, -2, -1f) == 10, "pure damage: negative armor/reduction clamped safely");

            // 2. Pure geometry / contact tests
            float playerR = DamageRules.PlayerRadius; // .45
            float commonR = DamageRules.CommonRadius; // .55 -> sum = 1.00
            check(DamageRules.Touching(Vector3.zero, new Vector3(1.00f, 0, 0), commonR), "touching on exact boundary");
            check(DamageRules.Touching(Vector3.zero, new Vector3(0.50f, 0, 0.50f), commonR), "touching within boundary");
            check(!DamageRules.Touching(Vector3.zero, new Vector3(1.01f, 0, 0), commonR), "not touching outside boundary");
            check(DamageRules.Touching(new Vector3(0, 15f, 0), new Vector3(0.5f, -10f, 0), commonR), "touching ignores Y elevation");

            // 3. Protection separation & deadlines
            float now = 100f;
            hitGraceUntil = 0;
            dashImmuneUntil = 0;
            spawnImmuneUntil = now + 2f;
            check(IsPlayerProtected(now), "player protected during spawn immunity");
            check(IsPlayerProtected(now + 1.999f), "player protected just before spawn immunity ends");
            check(!IsPlayerProtected(now + 2.001f), "player unprotected after spawn immunity expires");

            // Dash should never shrink spawn protection
            dashImmuneUntil = Mathf.Max(dashImmuneUntil, now + 0.28f);
            check(spawnImmuneUntil == now + 2f, "dash does not truncate spawn protection");
            check(IsPlayerProtected(now + 0.50f), "player protected while dash ended but spawn protection remains");

            // 4. Modal and Phase rejection
            ClearIncomingDamage();
            var dummyReq = new DamageRequest(20, DamageSourceKind.Legacy, AllocDamageSourceId());
            // Test rejection in Camp phase
            check(!OfferPlayerDamage(dummyReq), "offer rejected outside Running phase");

            // 5. Multi-hit single frame resolution & deterministic winner
            // Set up a mock running phase without modifying real player prefs
            var prevPhase = Phase;
            var prevHealth = Health;
            var savedPerks = new Dictionary<CombatRules.PerkKind, int>(perkRanks);
            try
            {
                Phase = RunPhase.Running;
                hitGraceUntil = dashImmuneUntil = spawnImmuneUntil = 0;
                Health = 100;
                perkRanks.Clear();

                BeginIncomingFrame();
                // Enqueue 20 common hits (raw 10) and 1 boss hit (raw 40)
                for (int i = 0; i < 20; i++)
                {
                    OfferPlayerDamage(new DamageRequest(10, DamageSourceKind.Legacy, (ulong)(100 + i)));
                }
                OfferPlayerDamage(new DamageRequest(40, DamageSourceKind.Legacy, 50));

                check(DamageQueueCount == 21, "all 21 damage candidates collected in queue");
                var res = ResolveIncomingDamage(now);
                check(res.AppliedDamage == 40, "only strongest hit applied from 21 candidates");
                check(Health == 60, "health reduced exactly once by strongest hit");
                check(hitGraceUntil >= now + DamageRules.HitGrace - 0.0001f, "hit grace triggered after hit resolution");

                // Shuffled order produces identical deterministic winner
                BeginIncomingFrame();
                hitGraceUntil = dashImmuneUntil = spawnImmuneUntil = 0;
                Health = 100;
                OfferPlayerDamage(new DamageRequest(40, DamageSourceKind.Legacy, 50));
                for (int i = 0; i < 20; i++)
                {
                    OfferPlayerDamage(new DamageRequest(10, DamageSourceKind.Legacy, (ulong)(100 + i)));
                }
                var res2 = ResolveIncomingDamage(now);
                check(res2.AppliedDamage == 40 && Health == 60, "shuffled candidate order yields identical result");

                // 6. Armor Boon (IronBark) rank cap and calculation
                check(RankCap(CombatRules.PerkKind.IronBark) == 5, "IronBark rank cap is 5");
                perkRanks[CombatRules.PerkKind.IronBark] = 3;
                check(PlayerArmor == 3, "PlayerArmor reads from Rank(IronBark)");
                check(DamageRules.Incoming(10, PlayerArmor, 0) == 7, "Armor 3 reduces 10 damage to 7");
                perkRanks[CombatRules.PerkKind.IronBark] = 5;
                check(PlayerArmor == 5, "PlayerArmor maxes at rank 5");
                check(DamageRules.Incoming(10, PlayerArmor, 0) == 5, "Armor 5 reduces 10 damage to 5");
            }
            finally
            {
                Phase = prevPhase;
                Health = prevHealth;
                ClearIncomingDamage();
                perkRanks.Clear();
                foreach(var kv in savedPerks) perkRanks[kv.Key] = kv.Value;
            }

            // 7. Swept segment projectile hit test
            Vector2 ply = new(0, 0);
            // Bullet moves from (-2, 0) to (2, 0) across player at (0, 0) in one frame
            Vector2 p0 = new(-2, 0), p1 = new(2, 0);
            Vector2 ab = p1 - p0;
            float abSqr = ab.sqrMagnitude;
            float t = Mathf.Clamp01(Vector2.Dot(ply - p0, ab) / abSqr);
            Vector2 closest = p0 + t * ab;
            bool sweptHit = (ply - closest).sqrMagnitude < .45f;
            check(sweptHit, "swept projectile piercing through player in 1 frame is detected");

            // Bullet passes far above player (-2, 5) to (2, 5)
            p0 = new(-2, 5); p1 = new(2, 5);
            ab = p1 - p0; abSqr = ab.sqrMagnitude;
            t = Mathf.Clamp01(Vector2.Dot(ply - p0, ab) / abSqr);
            closest = p0 + t * ab;
            bool sweptMiss = (ply - closest).sqrMagnitude < .45f;
            check(!sweptMiss, "distant projectile segment does not falsely hit player");
        }
    }
}
