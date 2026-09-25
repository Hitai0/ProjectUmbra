using System.Collections.Generic;
using UnityEngine;

namespace Umbra
{
    public sealed partial class UmbraPrototype
    {
        public int PlayerArmor => Rank(CombatRules.PerkKind.IronBark);
        public float PlayerReduction => Class == HeroClass.Swordsman ? .20f : 0f;

        public float hitGraceUntil;
        public float dashImmuneUntil;
        public float spawnImmuneUntil;
        public float invulnerableUntil
        {
            get => Mathf.Max(hitGraceUntil, Mathf.Max(dashImmuneUntil, spawnImmuneUntil));
            set { hitGraceUntil = value; dashImmuneUntil = value; spawnImmuneUntil = value; }
        }

        ulong nextDamageSourceId = 1;
        public ulong AllocDamageSourceId() => nextDamageSourceId++;

        readonly List<DamageRequest> incomingQueue = new(256);
        bool isCollectingDamage;
        public int DamageQueueCount => incomingQueue.Count;

        public bool IsPlayerProtected(float now) =>
            now < Mathf.Max(hitGraceUntil, Mathf.Max(dashImmuneUntil, spawnImmuneUntil));

        public void BeginIncomingFrame()
        {
            incomingQueue.Clear();
            isCollectingDamage = true;
        }

        public void ClearIncomingDamage()
        {
            incomingQueue.Clear();
            isCollectingDamage = false;
        }

        public bool OfferPlayerDamage(in DamageRequest request)
        {
            if (Phase != RunPhase.Running || IsModal || request.RawDamage <= 0 || Health <= 0)
                return false;

            if (incomingQueue.Count < 256)
            {
                incomingQueue.Add(request);
                return true;
            }

            int incomingMitigated = DamageRules.Incoming(request.RawDamage, PlayerArmor, PlayerReduction);
            int worstIndex = -1;
            int worstDamage = int.MaxValue;
            ulong worstSourceId = 0;

            for (int i = 0; i < incomingQueue.Count; i++)
            {
                var cur = incomingQueue[i];
                int curMitigated = DamageRules.Incoming(cur.RawDamage, PlayerArmor, PlayerReduction);
                if (curMitigated < worstDamage || (curMitigated == worstDamage && cur.SourceId > worstSourceId))
                {
                    worstDamage = curMitigated;
                    worstSourceId = cur.SourceId;
                    worstIndex = i;
                }
            }

            if (incomingMitigated > worstDamage || (incomingMitigated == worstDamage && request.SourceId < worstSourceId))
            {
                incomingQueue[worstIndex] = request;
                return true;
            }

            return false;
        }

        public DamageResult ResolveIncomingDamage(float now)
        {
            try
            {
                if (Phase != RunPhase.Running || IsModal || Health <= 0)
                    return new DamageResult(0, 0, 0, DamageRejectReason.InactiveRun);

                if (IsPlayerProtected(now))
                    return new DamageResult(0, 0, 0, DamageRejectReason.Immune);

                int bestMitigated = 0;
                int bestRaw = 0;
                int bestCandidateIndex = -1;
                ulong bestSourceId = ulong.MaxValue;

                for (int i = 0; i < incomingQueue.Count; i++)
                {
                    var req = incomingQueue[i];
                    if (req.RawDamage <= 0) continue;

                    if (req.Kind == DamageSourceKind.Contact)
                    {
                        if (req.EnemyIndex < 0 || req.EnemyIndex >= enemies.Count) continue;
                        var e = enemies[req.EnemyIndex];
                        if (e.hp <= 0 || !e.root.gameObject.activeSelf || e.spawnGeneration != req.SpawnGeneration || e.damageSourceId != req.SourceId)
                            continue;
                        if (now < e.nextContactAt)
                            continue;
                    }

                    int mitigated = DamageRules.Incoming(req.RawDamage, PlayerArmor, PlayerReduction);
                    if (mitigated <= 0) continue;

                    if (mitigated > bestMitigated || (mitigated == bestMitigated && req.SourceId < bestSourceId))
                    {
                        bestMitigated = mitigated;
                        bestRaw = req.RawDamage;
                        bestCandidateIndex = i;
                        bestSourceId = req.SourceId;
                    }
                }

                if (bestCandidateIndex < 0)
                    return new DamageResult(0, 0, 0, DamageRejectReason.None);

                var winner = incomingQueue[bestCandidateIndex];
                int applied = Mathf.Min(Health, bestMitigated);
                Health -= applied;
                hitGraceUntil = Mathf.Max(hitGraceUntil, now + DamageRules.HitGrace);
                lastHitAt = now;

                if (winner.Kind == DamageSourceKind.Contact && winner.EnemyIndex >= 0 && winner.EnemyIndex < enemies.Count)
                {
                    var winnerEnemy = enemies[winner.EnemyIndex];
                    winnerEnemy.nextContactAt = now + DamageRules.ContactInterval;
                }

                PlayCue(1);
                Popup(Player.position + Vector3.up * 1.6f, "-" + applied, new Color(1, .4f, .3f));

                if (Health == 0)
                    FinishRun(false, "Defeated in the field");

                return new DamageResult(bestRaw, bestMitigated, applied, DamageRejectReason.None);
            }
            finally
            {
                incomingQueue.Clear();
                isCollectingDamage = false;
            }
        }

        public void HurtPlayer(int damage)
        {
            if (Phase != RunPhase.Running || IsModal) return;
            var req = new DamageRequest(damage, DamageSourceKind.Legacy, AllocDamageSourceId());
            if (isCollectingDamage)
            {
                OfferPlayerDamage(req);
            }
            else
            {
                BeginIncomingFrame();
                OfferPlayerDamage(req);
                ResolveIncomingDamage(Time.time);
            }
        }
    }
}
