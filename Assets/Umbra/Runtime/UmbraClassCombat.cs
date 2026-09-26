using System.Collections.Generic;
using UnityEngine;

namespace Umbra
{
    public sealed partial class UmbraPrototype
    {
        readonly Dictionary<HeroClass, Sprite[]> classFrames = new();
        readonly List<Enemy> lightningTargets = new();
        readonly List<LightningFlash> lightningFlashes = new();
        sealed class LightningFlash { public LineRenderer line; public float until; }
        const float LightningRange = 12f;
        const float LightningCooldown = 1.1f;
        public Sprite ClassPortrait => classFrames.TryGetValue(Class, out var frames) ? frames[0] : null;

        Sprite[] LoadClassSheet(string name)
        {
            var texture = Resources.Load<Texture2D>("Umbra/" + name);
            if (!texture) throw new System.InvalidOperationException("Missing class sprite sheet: " + name);
            var frames = new Sprite[3];
            int width = texture.width / 3;
            var pixels = texture.GetPixels32();
            int bottom = texture.height, top = 0;
            for (int y = 0; y < texture.height; y++)
                for (int x = 0; x < texture.width; x++)
                    if (pixels[y * texture.width + x].a > 32) { bottom = Mathf.Min(bottom, y); top = Mathf.Max(top, y); }
            int height = top - bottom + 1;
            for (int i = 0; i < frames.Length; i++)
            {
                frames[i] = Sprite.Create(texture, new Rect(i * width, bottom, width, height),
                    new Vector2(.5f, 0), height / 1.85f);
                owned.Add(frames[i]);
            }
            return frames;
        }

        void RefreshClassSprite(bool moving = false)
        {
            if (!playerSprite || !classFrames.TryGetValue(Class, out var frames)) return;
            playerSprite.sprite = frames[moving ? 1 + (int)(elapsed * 9) % 2 : 0];
            playerSprite.color = Color.white;
        }

        bool CastLightning()
        {
            lightningTargets.Clear();
            foreach (var enemy in enemies)
                if (enemy.hp > 0 && enemy.root.gameObject.activeSelf &&
                    (enemy.root.position - Player.position).sqrMagnitude <= LightningRange * LightningRange)
                    lightningTargets.Add(enemy);
            if (lightningTargets.Count == 0) return false;

            // Every cast takes the nearest living foes, independent of cursor position or aim mode.
            lightningTargets.Sort((a, b) =>
                (a.root.position - Player.position).sqrMagnitude.CompareTo((b.root.position - Player.position).sqrMagnitude));

            int count = Mathf.Min(lightningTargets.Count, CombatRules.ProjectileCount(Rune) + BonusProjectiles);
            float modifier = 2.5f * (1 + (Rune == RuneKind.Pierce ? .2f : 0) +
                (HasSproutCard ? .2f : 0) + (Rank(CombatRules.PerkKind.Chain) > 0 ? .4f : 0));
            for (int i = 0; i < count && Phase == RunPhase.Running; i++)
            {
                var target = lightningTargets[i];
                bool crit = Random.value < CombatRules.CritChance(LUK);
                int damage = Mathf.RoundToInt(AttackDamage * modifier * (crit ? CombatRules.CritMultiplier : 1));
                ShowLightning(target.root.position);
                DamageEnemy(target, damage, crit);
                // Lightning never splashes or chains to a second victim. Supports affect this target only.
                ApplyHitSupports(target, damage, false);
                if (Rune == RuneKind.Ember && target.hp > 0)
                {
                    target.burnUntil = Time.time + 3.1f;
                    target.burnDamage = Mathf.Max(1, damage / 10);
                    if (target.burnTick < Time.time) target.burnTick = Time.time + 1;
                }
            }
            attackAt = Time.time + LightningCooldown / (AttackSpeedMultiplier * (1 + CombatRules.AttackSpeedBonus(AGI)));
            PlayCue(0);
            return true;
        }

        void ShowLightning(Vector3 position)
        {
            var go = new GameObject("Targeted lightning", typeof(LineRenderer));
            go.transform.SetParent(transform);
            var line = go.GetComponent<LineRenderer>();
            line.sharedMaterial = RingMaterial();
            line.positionCount = 7;
            line.widthMultiplier = .10f;
            line.startColor = new Color(.55f, .8f, 1f);
            line.endColor = Color.white;
            for (int i = 0; i < 7; i++)
                line.SetPosition(i, position + new Vector3(i == 6 ? 0 : (i % 2 == 0 ? -.22f : .22f), 6f - i * .9f, 0));
            lightningFlashes.Add(new LightningFlash { line = line, until = Time.time + .18f });
        }

        void UpdateLightning()
        {
            for (int i = lightningFlashes.Count - 1; i >= 0; i--)
                if (Time.time >= lightningFlashes[i].until)
                {
                    Destroy(lightningFlashes[i].line.gameObject);
                    lightningFlashes.RemoveAt(i);
                }
        }

        void ClearLightning()
        {
            foreach (var flash in lightningFlashes) if (flash.line) Destroy(flash.line.gameObject);
            lightningFlashes.Clear();
        }

        public string ClassRuneDescription(RuneKind rune) => Class != HeroClass.Mage ? Loc.RuneDescription(rune) : rune switch
        {
            RuneKind.Pierce => Loc.T("One targeted bolt. +20% lightning damage.", "สายฟ้ารายตัว 1 เส้น ดาเมจสายฟ้า +20%"),
            RuneKind.Scatter => Loc.T("Strike up to 3 distinct foes. Lower damage per bolt.", "ผ่าได้สูงสุด 3 เป้าหมาย ดาเมจต่อเส้นลดลง"),
            _ => Loc.T("Lightning burns its target. No area explosion.", "สายฟ้าเผาเฉพาะเป้าหมาย ไม่มีระเบิดวงกว้าง")
        };

        public string ClassPerkDescription(CombatRules.PerkKind kind)
        {
            if (Class != HeroClass.Mage) return Loc.PerkDescription(kind);
            return kind switch
            {
                CombatRules.PerkKind.RunePierce => ClassRuneDescription(RuneKind.Pierce),
                CombatRules.PerkKind.RuneScatter => ClassRuneDescription(RuneKind.Scatter),
                CombatRules.PerkKind.RuneEmber => ClassRuneDescription(RuneKind.Ember),
                CombatRules.PerkKind.Multishot => Loc.T("+1 distinct lightning target per cast. Up to 2 ranks.", "+1 เป้าหมายสายฟ้าต่อครั้ง สูงสุด 2 ขั้น"),
                CombatRules.PerkKind.Chain => Loc.T("+40% lightning damage instead of bouncing to nearby foes.", "+40% ดาเมจสายฟ้าแทนการชิ่งใส่ศัตรูข้างเคียง"),
                CombatRules.PerkKind.SproutCard => Loc.T("+20% lightning damage instead of piercing.", "+20% ดาเมจสายฟ้าแทนการทะลวง"),
                _ => Loc.PerkDescription(kind)
            };
        }

        void RunLightningTests(System.Action<bool, string> check)
        {
            bool previousAuto = AutoAim;
            try
            {
                EnterCamp(); SelectClass(HeroClass.Mage); StartRun(); ApplyPerk(ActiveDraft[0]);
                AutoAim = false; perkRanks.Clear(); BonusProjectiles = 0;
                foreach (bool auto in new[] { false, true })
                {
                    ClearCombat(); AutoAim = auto; Rune = RuneKind.Pierce;
                    SpawnEnemy(enemies[0], Player.position + Vector3.forward * 8, 10000);
                    SpawnEnemy(enemies[1], Player.position + Vector3.forward * 2, 10000);
                    SpawnEnemy(enemies[2], Player.position + Vector3.forward * 5, 10000);
                    SpawnEnemy(enemies[3], Player.position + Vector3.forward, 10000);
                    enemies[3].root.gameObject.SetActive(false);
                    attackAt = 0; TryAttack(enemies[0].root.position);
                    check(enemies[1].hp < 10000 && enemies[0].hp == 10000 && enemies[2].hp == 10000 && enemies[3].hp == 10000,
                        "lightning chooses nearest active foe regardless of aim " + auto);
                    enemies[1].hp = 10000; BonusProjectiles = 1;
                    attackAt = 0; TryAttack(enemies[0].root.position);
                    check(enemies[1].hp < 10000 && enemies[2].hp < 10000 && enemies[0].hp == 10000,
                        "extra lightning chooses next nearest foe " + auto);
                    BonusProjectiles = 0;
                }
                foreach (RuneKind rune in System.Enum.GetValues(typeof(RuneKind)))
                {
                    ClearCombat(); Rune = rune;
                    for (int i = 0; i < 4; i++) SpawnEnemy(enemies[i], Player.position + new Vector3(i * .1f, 0, 3), 10000);
                    SpawnEnemy(enemies[4], Player.position + Vector3.forward * 20, 10000);
                    attackAt = 0;
                    check(TryAttack(enemies[0].root.position), "lightning cast " + rune);
                    int damaged = 0;
                    for (int i = 0; i < 4; i++) if (enemies[i].hp < 10000) damaged++;
                    check(damaged == (rune == RuneKind.Scatter ? 3 : 1) && enemies[3].hp == 10000,
                        "lightning distinct victims without splash " + rune);
                    check(enemies[4].hp == 10000 && shots.Count == 0, "lightning range and no projectiles " + rune);
                    check(!TryAttack(enemies[0].root.position), "lightning cooldown " + rune);
                    if (rune == RuneKind.Ember)
                        check(enemies[0].burnUntil > Time.time && enemies[1].burnUntil == 0, "lightning targeted burn");
                }
                ClearCombat(); Rune = RuneKind.Pierce; BonusProjectiles = 2;
                SpawnEnemy(enemies[0], Player.position + Vector3.forward * 3, 10000);
                attackAt = 0; TryAttack(enemies[0].root.position);
                check(lightningFlashes.Count == 1, "extra bolts never repeat the same victim");
                ClearCombat(); attackAt = 0;
                check(!TryAttack(Player.position) && attackAt == 0, "no target does not consume lightning cooldown");
                SpawnEnemy(enemies[0], Player.position + Vector3.forward * 3, 10000);
                Paused = true;
                check(!TryAttack(enemies[0].root.position) && enemies[0].hp == 10000, "paused lightning cannot deal damage");
                Paused = false; BonusProjectiles = 0;
                SpawnEnemy(enemies[1], Player.position + Vector3.forward * 3.1f, 10000);
                perkRanks[CombatRules.PerkKind.Chain] = 1;
                attackAt = 0; TryAttack(enemies[0].root.position);
                check(enemies[0].hp < 10000 && enemies[1].hp == 10000, "chain support keeps lightning single target");
                ClearLightning();
                check(lightningFlashes.Count == 0, "lightning effects cleaned up");
            }
            finally { AutoAim = previousAuto; EnterCamp(); }
        }

#if UNITY_EDITOR
        void CaptureClassTestFrame(HeroClass kind)
        {
            System.IO.Directory.CreateDirectory("Recordings");
            var previous = WorldCamera.targetTexture;
            var previousActive = RenderTexture.active;
            var target = new RenderTexture(960, 540, 24);
            var texture = new Texture2D(960, 540, TextureFormat.RGB24, false);
            try
            {
                WorldCamera.targetTexture = target;
                WorldCamera.Render();
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, 960, 540), 0, 0);
                texture.Apply();
                System.IO.File.WriteAllBytes("Recordings/Class-" + kind + ".png", texture.EncodeToPNG());
            }
            finally
            {
                WorldCamera.targetTexture = previous;
                RenderTexture.active = previousActive;
                DestroyImmediate(texture); DestroyImmediate(target);
            }
        }
#endif
    }
}
