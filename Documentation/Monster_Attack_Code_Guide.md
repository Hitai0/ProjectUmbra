# Monster Attack — Code and Structure Guide

สถานะ: **design contract สำหรับ implementation ถัดไป ไม่ใช่ระบบที่สร้างเสร็จแล้ว**
อ่าน [Implementation Plan](Monster_Attack_Implementation_Plan.md) ก่อน ค่าทั้งหมดเป็น Umbra tuning

## 1. โครงสร้างไฟล์

```text
Assets/Umbra/Runtime/
  CombatRules.cs                 existing stats, outgoing damage, wave rules
  DamageTypes.cs                 NEW: immutable request/result + source enums
  DamageRules.cs                 NEW: pure mitigation/contact tuning
  UmbraDamage.cs                 NEW: UmbraPrototype partial, player damage gateway
  UmbraMonsterCombat.cs          NEW: UmbraPrototype partial, contact helpers
  UmbraPrototype.cs              keep component identity, Enemy, Update and outgoing combat
  UmbraCampaign.cs               spawn snapshots, boss/hazard producers, lifecycle
  UmbraCampaignTests.cs          extend existing smoke integration
  UmbraDamageTests.cs            NEW optional partial for focused helper tests
  UmbraHud.cs / UmbraCampHud.cs   only relevant Armor display/help text
```

สร้าง Unity `.meta` ผ่าน Editor และเก็บใน Git สำหรับไฟล์ใหม่ ไม่สร้าง MonoBehaviour ตัวที่สอง ไม่เปลี่ยนชื่อ/namespace/component ใน scene ไม่ทำ ScriptableObject migration ทั้งเกม งานนี้ใช้ small immutable profile ใน code ก่อน

## 2. Contracts

`DamageTypes.cs` ไม่มีการเรียก PlayerPrefs, GameObject หรือ Time:

```csharp
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
            RawDamage=raw; Kind=kind; SourceId=id;
            EnemyIndex=enemyIndex; SpawnGeneration=generation;
        }
    }

    public readonly struct DamageResult
    {
        public readonly int RequestedDamage, MitigatedDamage, AppliedDamage;
        public readonly DamageRejectReason Rejected;
        public bool Applied => AppliedDamage > 0;
        public DamageResult(int raw, int mitigated, int applied, DamageRejectReason rejected)
        { RequestedDamage=raw; MitigatedDamage=mitigated; AppliedDamage=applied; Rejected=rejected; }
    }
}
```

ใช้ monotonic `ulong nextDamageSourceId` ต่อ run; assign ID เมื่อ spawn enemy/projectile/hazard ไม่ใช่ทุก frame Enemy เพิ่ม `uint spawnGeneration` ทุก reuse; `SourceId` ใหม่ทุก spawn ต้อง clear pending queue ก่อน reset ID ใน StartRun ห้ามใช้ `GetHashCode` เป็น identity ที่ขึ้นกับ runtime

`DamageRules.cs` ตัวอย่าง pure code ที่นำไปใช้ได้:

```csharp
using UnityEngine;
namespace Umbra
{
    public static class DamageRules
    {
        public const float HitGrace = .35f;
        public const float ContactInterval = .60f;
        public const float PlayerRadius = .45f;

        public static int Incoming(int raw, int armor, float reduction)
        {
            if (raw <= 0) return 0;
            if (float.IsNaN(reduction) || float.IsInfinity(reduction)) reduction = 0;
            float afterArmor = Mathf.Max(1f, (float)raw - Mathf.Max(0, armor));
            return Mathf.Max(1, Mathf.CeilToInt(afterArmor * (1f - Mathf.Clamp(reduction,0f,.8f))));
        }

        public static bool Touching(Vector3 a, Vector3 b, float enemyRadius)
        {
            float dx=a.x-b.x, dz=a.z-b.z;
            float radius=PlayerRadius+enemyRadius;
            return dx*dx+dz*dz <= radius*radius;
        }
    }
}
```

Expected tests: `Incoming(10,3,.2f)==6`, `Incoming(3,5,.2f)==1`, `Incoming(0,0,0)==0` คำนวณ once ต่อ candidate, ห้าม ceil/class reduction อีกครั้งใน `HurtPlayer`

## 3. Runtime ownership และ API ที่ต้องสร้าง

| API/field | เจ้าของ | ความหมาย |
|---|---|---|
| `PlayerArmor => Rank(PerkKind.IronBark)` | UmbraDamage partial | source of truth ของ Armor |
| `PlayerReduction` | UmbraDamage partial | .2 เมื่อ Swordsman, 0 สำหรับ class อื่น |
| `BeginIncomingFrame()` | UmbraDamage | clear reusable queue; mark collecting |
| `OfferPlayerDamage(in DamageRequest)` | UmbraDamage | validate raw/phase/modal; enqueue without mutating HP |
| `ResolveIncomingDamage(float now)` | UmbraDamage | eligibility, strongest hit, immunity, HP, cooldown commit, death |
| `ClearIncomingDamage()` | UmbraDamage | clear queue/collecting flags on all lifecycle exits |
| `IsPlayerProtected(float now)` | UmbraDamage | compare three protection deadlines |
| `TryContactAttack(Enemy e, int index, float now)` | UmbraMonsterCombat | source alive, ready, overlap; then offer |
| `ConfigureContactProfile(Enemy e, bool champion)` | UmbraMonsterCombat | spawn snapshot, radius, raw damage, cooldown reset |
| `nextContactAt`, `contactRadius`, `damageSourceId`, `spawnGeneration` | Enemy | pooled per-actor state |
| `damage`, `damageSourceId` | HostileShot/Hazard | immutable on creation |

Use `Enemy` ที่มีอยู่ใน partial; ห้ามสร้าง Enemy type ใหม่ชื่อซ้ำ Startup/CreateActors ยังสร้าง pool เดิม `SpawnEnemy` เรียก ConfigureContactProfile สำหรับ common/elite, champion เรียกด้วย champion=true หลัง spawn จาก reserved slot, boss ปิด contact โดย explicit profile/flag

### Queue และ allocation

ใช้ reusable `List<DamageRequest>` ที่ preallocate 256 entries (64 contact + bounded hazards/projectiles โดยทั่วไป) หรือ fixed buffer เทียบเท่า ไม่มี LINQ/closure/new list ใน Update ถ้าเต็มให้คง 256 candidate ที่ดีที่สุดตาม finalDamage/SourceId เดียวกับ resolver แทนการทิ้งตามลำดับ list; นับ overflow ใน diagnostics สำหรับทดสอบ ห้ามเพิ่ม allocation ต่อเฟรมเพื่อแก้ overflow

Queue เก็บ contact handle (index+generation) เพื่อ validate ตอน resolve ส่วน projectile/hazard เป็น **event snapshot**: เมื่อ collision/tick ผ่าน eligibility แล้ว visual/source อาจถูกลบได้ การลบ projectile visual ห้าม invalidate hit ที่เสนอไปแล้ว หากรันจบหรือเริ่มใหม่ต้องล้าง snapshot ทั้งหมด

## 4. Frame ordering ที่บังคับใช้

```text
Update
  update UI/input capture; SyncPause
  if not CanAct: clear incoming queue; return
  now = Time.time; dt = Time.deltaTime
  BeginIncomingFrame
  advance RunTimer
  UpdateRunDirector      // schedule boss/champion/timeout only
  if run ended: ClearIncomingDamage; return
  input + player movement
  UpdateHorde
  UpdateEnemies          // movement/contact offers; boss telegraphs/shots
  if run ended: ClearIncomingDamage; return
  UpdateHazards          // call exactly ONCE here, not also in director
  ResolveIncomingDamage(now)
  if run ended: return
  UpdateShots            // keep outgoing hit/support/kill logic
  if run ended: return
  UpdateLoot / rings / regeneration
```

Incoming hits have priority over player's outgoing projectile kills in the same frame, matching the existing general ordering. Burn kills evaluated in UpdateEnemies can finish a boss encounter before pending incoming hits resolve; after any phase transition discard pending hits. Document this tie rule rather than relying on accidental method order.

`ResolveIncomingDamage` algorithm:

```text
try:
  reject/clear if !CanAct or player already dead
  reject/clear if now < max(hitGraceUntil, dashImmuneUntil, spawnImmuneUntil)
  for each candidate:
    reject invalid raw
    contact only: confirm index/generation/alive/active and nextContactAt <= now
    compute final incoming damage
    choose largest final damage; tie lower SourceId
  if no candidate: return no-hit result
  applied = min(Health, finalDamage)
  Health -= applied
  hitGraceUntil = max(hitGraceUntil, now + HitGrace)
  winning contact only: nextContactAt = now + ContactInterval
  update lastHitAt, popup, cue once using applied
  if Health == 0: FinishRun(false, reason) exactly once
  return DamageResult
finally:
  clear queue; collecting = false
```

การเลือกผู้ชนะต้อง revalidate contact source ก่อน ranking ถ้า stale source แรงที่สุดให้พิจารณาตัวที่เหลือ ไม่ใช่ทิ้งทั้งเฟรม

`HurtPlayer(int)` ยังต้องอยู่สำหรับ call sites/tests เดิม แต่เป็น adapter เท่านั้น: ถ้ากำลัง collecting ให้ Offer ด้วย Legacy ID; ถ้าเรียกนอก collection ให้ Begin/Offer/Resolve แบบ transaction หนึ่งครั้ง โดยยังตรวจ CanAct และ protection ห้ามมี HP mutation สองเส้นทาง หลัง migrate producers แล้วค้น `HurtPlayer(` อีกครั้งเพื่อยืนยันว่าไม่หลงเหลือ live source ที่เสีย identity

## 5. Contact integration

- ใช้ horizontal overlap หลัง movement เพื่อไม่เสียหนึ่งเฟรมจากตำแหน่งเก่า
- กัน overshoot: จำกัด movement step ไม่ให้ผ่านเป้าหมายในเฟรม dt สูง และ/หรือใช้ relative swept XZ segment ของ player/enemy เมื่อทั้งคู่เคลื่อนที่เร็ว เกณฑ์ swept ใช้ระยะจุด origin ถึง relative segment <= summed radius
- ห้ามให้ collider ของ mouse picking มาเป็น contact hitbox; layer system ที่เพิ่งทำมีหน้าที่คนละส่วน
- `nextContactAt` ค่าเริ่มต้น `now` ตอนเกิด (spawn ยังอยู่ห่าง >=8m) ไม่ให้มอนที่ถูก pool reuse ติด cooldown เก่า
- Remove เฉพาะ ordinary windup branch/ordinary warning pulse; อย่าลบ boss telegraphs
- เพิ่ม explicit `isChampion` หรือ `ContactProfileKind` ไม่อนุมานจาก visual scale และ reset ทุก reuse

## 6. Projectile / Hazard

Hostile projectile ใช้ closest point บน segment ระหว่าง `before` กับ `after` ต่อ player position ใน XZ; segment length zero ต้องไม่ divide by zero ใช้รัศมีชนเดิม `sqrt(.45)` ใน pass แรก เพื่อลดการเปลี่ยน balance หลายอย่างพร้อมกัน หาก player เคลื่อนที่เร็วให้ใช้ relative previous/current positions ด้วย

Collision แล้ว Offer damage snapshot และ consume projectile เสมอ รวมถึงระหว่าง grace; keep expired cleanup once ค่าดาเมจ snapshot = 26 + map × 7 ตามเดิม

Hazard active ตาม activates/expires; เมื่อ due ตั้ง nextTick=now+.7 ไม่ใช้ while catch-up และ Offer เฉพาะผู้เล่นอยู่ในรัศมี ข้อจำกัดคือที่ frame rate ต่ำมากจำนวน tick จริงอาจลดลง: บันทึกเป็นข้อจำกัด ไม่ชดเชยโดย burst damage Boss body/aimed raw ยังคง 36/32 + map × 7; map death hazards 8/24 คงเดิม

## 7. Outgoing damage: preserve compatibility

ระบบใหม่นี้แยก incoming กับ outgoing ชัดเจน:

```text
CombatRules.Damage(rune, BaseLevel, STR, DEX)
  -> AttackDamage (BattleFocus + class)
  -> existing one crit roll per volley / CritMultiplier 1.75
  -> projectile/slash/support calculation
  -> DamageEnemy(final integer, isCrit) -> HP/death/loot
```

ห้าม roll crit ซ้ำที่ DamageEnemy หรือใช้ PlayerArmor ลดดาเมจมอน ห้ามใส่ time scaling ซ้ำใน incoming resolver เพราะ contact snapshot scale มาแล้ว Ignite/Chain/Ember secondary hits ยังใช้ multiplier/rounding และ recursion guard เดิม Armor สำหรับศัตรูไม่อยู่ใน scope; หากจำเป็นต้องเพิ่มภายหลังให้เป็น explicit defense profile แยก

## 8. Lifecycle checklist

| Lifecycle | Required reset |
|---|---|
| StartRun | queue/IDs/contact pool state, hitGrace=0, dashImmune=0, spawnImmune=now+2, Armor rank via existing perk reset |
| SpawnEnemy/recycle | generation increment/new ID, contact timers, profile; no old burn/flash/pending contact |
| SpawnBoss | clear queue, guardian contact off, spawnImmune=max(existing,now+2), existing arena/HP rules |
| TryDash | dashImmune=max(existing,now+.28); do not overwrite other timers |
| EnterCamp/FinishRun/ClearCombat | clear pending hits and transient combat protection; no saved Armor |
| Pause/Draft | no timer advancement; no direct API damage; do not reset protection deadlines |
| Destroy | existing Time.timeScale reset retained |

ระวัง ClearCombat ถูกเรียกภายใน SpawnBoss: ต้อง reset protection ก่อนตั้ง boss-entry protection ไม่ใช่ตั้งแล้วโดนล้าง

## 9. Validation / work discipline

ใช้ smoke runner เดิมและ test save suppression; แยก pure timing tests โดยส่ง `now` ไม่ sleep เพื่อทดสอบ cooldown Boundary ใช้ eps=0.0001f เพิ่ม coroutine test สำหรับ actual 1x/1.5x/2x progression แยกต่างหากและ restore speed/time/profile ใน finally

ตรวจไฟล์ที่จะอ่านด้วย targeted rg, รัน compile และ relevant tests เมื่อมีการแก้จริง ไม่พิมพ์ทั้ง source/log ไม่สร้าง agent/task เพิ่มเอง แผนนี้มีไว้ส่งต่อให้ผู้พัฒนาหนึ่งราย และยังไม่ใช่ authorization ให้ deploy หรือสร้างงานใหม่ใน sidebar

ก่อนส่งงาน ค้น `Health=`, `Health-=`, `HurtPlayer`, `invulnerableUntil`, `UpdateHazards` เพื่อยืนยัน incoming gateway เดียวและไม่มี dual invocation แยก legitimate healing/reset จาก damage mutation ในผลตรวจ อัปเดต docs กับ test results ตามจริง และระบุข้อจำกัดเรื่อง balance/performance ที่ยังไม่ได้ทดลอง
