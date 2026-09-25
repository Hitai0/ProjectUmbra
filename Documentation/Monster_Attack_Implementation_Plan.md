# Monster Attack & Damage Calculation — Implementation Plan

สถานะ: **แผนส่งต่อ ยังไม่ได้ implement** | สำรวจโค้ดวันที่ 2026-09-25
ผู้รับงาน: agent ที่จะพัฒนาระบบต่อสู้ใน Unity project นี้

## 1. เป้าหมายและขอบเขต

ทำการต่อสู้แบบ contact-pressure survivor: มอนทั่วไปเดินเข้าประชิดและทำดาเมจเมื่อสัมผัส ไม่หยุดง้างทุกครั้ง ผู้เล่นเอาตัวรอดด้วยตำแหน่ง การกำจัดฝูง เกราะ และช่วงอมตะสั้น ๆ บอสยังใช้ท่าเตือนล่วงหน้า ห้ามเพิ่มความยากด้วยการปล่อยให้หลายสิบตัวหัก HP ในเฟรมเดียว

อ่านคู่กับ [Code and Structure Guide](Monster_Attack_Code_Guide.md), [Spawn Design](Spawn_Design.md) และส่วนที่เกี่ยวข้องใน [Code Index](Code_Index.md) ไม่ต้องอ่านทั้ง repository

### Reference ที่ยืนยันได้

[หน้าเกมของ poncle](https://poncle.itch.io/vampire-survivors) ระบุ Armor เป็นหนึ่งใน power-up และ changelog ของ demo ระบุช่วง invulnerability หลังรับ treasure รวมถึง feedback เมื่อโดนศัตรู หน้าเดียวกันบอกชัดว่า demo ต่างจากเวอร์ชัน Steam ที่อัปเดตต่อเนื่อง

แหล่งดังกล่าว **ไม่ได้เปิดสูตร Armor, contact tick หรือระยะ invulnerability หลังโดนโจมตีของเวอร์ชันปัจจุบัน** ดังนั้น contact model, สูตร และค่าด้านล่างคือข้อกำหนดของ Umbra ที่ตั้งใจให้ได้รูปแบบการเล่นใกล้เคียง ไม่ใช่การอ้างว่าเป็นสูตร Vampire Survivors แบบ 1:1 ห้ามเขียนรายงานว่าคัดลอกระบบต้นฉบับได้ตรงทั้งหมด

### สิ่งที่ต้องส่งมอบ

1. Contact attack สำหรับ common/elite/champion พร้อมขนาดตัวและคูลดาวน์รายตัว
2. Damage pipeline กลางสำหรับดาเมจที่ผู้เล่นได้รับจาก contact, projectile และ hazard
3. Flat Armor ที่ใช้งานได้จริงผ่าน run boon พร้อม UI แสดงค่า
4. แยก hit grace ออกจาก dash/spawn protection
5. ทดสอบหลายแหล่งโจมตี, pooling, pause และ Game Speed
6. อัปเดต GDD/Code Index/Validation ตามผลจริง

ไม่รวม: เปลี่ยน spawn wave, เพิ่มแผนที่/asset, ทำ multiplayer, เปลี่ยน save schema เดิม, ยกเครื่องอาวุธผู้เล่น, เพิ่ม random damage/monster crit/evasion/resistance/retaliation, rebuild/deploy WebGL เว้นแต่จำเป็นหรือผู้ใช้สั่ง

## 2. จุดเริ่มต้นในโค้ดปัจจุบัน

| ตำแหน่ง | ปัจจุบัน | งานที่ต้องทำ |
|---|---|---|
| `UmbraPrototype.cs / Enemy` | `contactDamage`, `moveSpeed` snapshot ที่ spawn; `windupUntil`, `attackAt` | เพิ่ม contact radius, next contact time และ spawn identity |
| `UpdateEnemies` | ง้าง .5s, ระยะเริ่ม 1.05m/โดน 1.35m, recovery 1s | เปลี่ยน common/elite เป็น overlap contact ต่อเนื่อง |
| `HurtPlayer(int)` | Swordsman ลด 20%, grace .55s, ใช้ `invulnerableUntil` ร่วมกับ Dash | adapter ไป pipeline ใหม่; ห้ามหัก HP ซ้ำ |
| `TryDash` | immunity .28s; assignment ทับเวลาเดิมได้ | แยก dash protection และต่ออายุด้วย Max |
| `UmbraCampaign.cs / SpawnEnemy` | snapshot HP, damage, speed | snapshot contact profile และ reset cooldown/identity |
| `UpdateBoss` | pursuit, radial shots, body/aimed telegraphs | คงท่าเดิม; ไม่เพิ่ม contact damage ซ้อน body hazard |
| `UpdateHazards` | tick .7s; projectile ตรวจตำแหน่งปลายเฟรม | เข้าคิว damage; projectile ใช้ swept segment |
| `CombatRules.cs` | สูตร monster damage 16/30 × map/time | แยก tuning contact ใหม่จาก boss/hazard |
| `DamageEnemy`, `AttackDamage`, `ApplyHitSupports` | ดาเมจผู้เล่นและรางวัลเดิม | คงพฤติกรรม; ห้ามใช้ incoming formula กับ outgoing |
| `SyncPause` | 0 เมื่อ modal; 1/1.5/2 เมื่อเล่น | ใช้ simulation time เดียวกัน ไม่คูณ GameSpeed ซ้ำ |

ชื่อไฟล์จริงทั้งหมดอยู่ใต้ `Assets/Umbra/Runtime/` ใช้ชื่อ method ในการค้นแทนเลขบรรทัดที่อาจเปลี่ยน

## 3. ข้อกำหนดการโจมตี

### Contact และการเคลื่อนที่

- คำนวณบนระนาบ XZ จากตำแหน่ง root; ไม่นับความสูง sprite/bobbing
- Player radius .45m; common .55m; elite .75m; champion .9m
- overlap เมื่อ squared distance <= squared sum of radii
- มอนยังเดินไล่/แยกตัวกัน หยุดเข้าใกล้ที่ประมาณ .8 × sum of radii เพื่อให้เกิด contact ได้จริง ไม่คงเงื่อนไขเดิมที่หยุดก่อนแตะ hitbox
- Common/elite/champion ไม่มี windup หรือวงเตือนโจมตีธรรมดาอีกต่อไป
- Contact สำเร็จแล้วตั้ง `nextContactAt = now + .60s`; หากถูก immunity ปฏิเสธ ไม่ reset contact cooldown
- ออกจากระยะแล้วกลับเข้าใหม่ไม่ล้าง cooldown; pool reuse ต้องล้าง cooldown และเปลี่ยน spawn identity
- Guardian ไม่มี contact ธรรมดาใน pass นี้ เพราะมี body hazard อยู่แล้ว
- การรีไซเคิลมอนไกลยังไม่ให้ kill/XP/loot

### ตัวเลขเริ่มทดสอบ (Umbra tuning)

| ค่า | เริ่มต้น |
|---|---:|
| Common base contact | 10 |
| Elite base contact | 20 |
| Champion base contact | 24 |
| Scaling contact | RoundToInt(base × (1 + mapIndex × .3 + spawnRunSeconds / 1200)) |
| Per-enemy contact interval | .60 simulation seconds |
| Global hit grace | .35 simulation seconds |
| Dash protection | .28 simulation seconds |
| Spawn/boss-entry protection | คง 2 simulation seconds |
| Hazard tick | คง .70 simulation seconds |
| Armor | 0 เริ่มรัน; +1 ต่อ Iron Bark boon; cap 5 |
| Swordsman reduction | คง 20% |

Contact ถูกลงจากเดิมเพราะไม่ต้องรอ windup อีกต่อไป ตัวเลขทั้งหมดต้องวัดผ่าน playtest ก่อนกล่าวว่าบาลานซ์แล้ว ไม่ลด hit grace โดยยังคงดาเมจเดิมอย่างเงียบ ๆ

### Incoming damage formula (ทุก source ใช้สูตรเดียวกัน)

```text
if raw <= 0: reject (ไม่ heal, ไม่สร้าง grace)
afterArmor = max(1, raw - max(0, armor))
finalDamage = max(1, ceil(afterArmor × (1 - clamp(reduction, 0, .8))))
actualLost = min(currentHealth, finalDamage)
```

Armor ลดก่อน percentage reduction, ไม่มี random variance หรือ crit ของมอน; minimum 1 ใช้เฉพาะ positive raw damage

| Raw | Armor | Reduction | Final |
|---:|---:|---:|---:|
| 10 | 0 | 0 | 10 |
| 10 | 3 | 0 | 7 |
| 10 | 3 | .20 | 6 |
| 3 | 5 | .20 | 1 |
| 0 / negative | any | any | rejected |

ใช้ `actualLost` ใน popup/telemetry และผลลัพธ์การโจมตี ไม่แสดง overkill เป็น HP ที่เสียจริง

### หลาย hit ในเฟรมเดียว

- เก็บ candidate ที่ผ่าน geometry/source cooldown; ยังไม่หัก HP ระหว่างตรวจแต่ละ source
- กรอง source ที่ไม่ active หรือ identity ไม่ตรง และ immunity ก่อนเลือกผู้ชนะ
- เลือก **finalDamage สูงสุดเพียงหนึ่ง hit ต่อ player ต่อ simulation frame**; เสมอกันเลือก SourceId ต่ำสุดเพื่อผล deterministic
- เลือกจากดาเมจหลัง mitigation ไม่ใช่ลำดับ list; common จึงไม่แย่ง grace จาก boss projectile ที่แรงกว่าเพียงเพราะถูก update ก่อน
- เมื่อรับ hit: หัก HP ครั้งเดียว, set hit grace, commit contact cooldown ของผู้ชนะ, feedback ครั้งเดียว; ตายแล้ว settle ครั้งเดียวและหยุด update ต่อ
- วิธีเลือกสูงสุดนี้เป็นนโยบายของ Umbra ไม่ใช่ข้ออ้างว่า VS ทำเหมือนกัน
- Projectile ถูกใช้หมดเมื่อชน แม้โดน immunity ปฏิเสธ; hazard ขยับ tick เมื่อ due แม้ไม่มี hit; contact cooldown ขยับเฉพาะ hit สำเร็จ

### Protection และเวลา

แยก `hitGraceUntil`, `dashImmuneUntil`, `spawnImmuneUntil`; ปฏิเสธ damage เมื่อ `now < Max(...)` เปิดรับได้ที่เวลาเท่ากัน ห้าม Dash ตัดช่วง spawn protection ให้สั้นลง

ใช้ `Time.time`/`Time.deltaTime` ใน runtime และส่ง `now` เข้า helper ให้ทดสอบได้ ทุก cooldown อยู่ใน simulation seconds; 2x ใช้ wall time ประมาณครึ่งหนึ่ง Pause/boon/modal ไม่รับ damage และไม่เดิน cooldown ใช้ `Time.unscaledTime` เฉพาะ UI ตามเดิม

## 4. Implementation phases

### Phase 1 — Pure rules และ data contract

เพิ่ม `DamageRules.cs`, `DamageTypes.cs` ตาม Code Guide พร้อม numeric tests ตรวจ rounding/minimum/negative ก่อนแตะ Update loop ไม่เพิ่ม dependencies หรือ framework ใหม่

### Phase 2 — Player damage gateway

เพิ่ม `UmbraDamage.cs` partial สำหรับ queue, mitigation, immunity, feedback และ death gateway เปลี่ยน `HurtPlayer` เป็น compatibility adapter ตรวจ direct call sites ทั้งหมด แยก immunity fields และแก้ทุก assignment ใน StartRun, SpawnBoss, TryDash, EnterCamp, ClearCombat, player tint และ tests

### Phase 3 — Contact และ frame ordering

เพิ่ม `UmbraMonsterCombat.cs` partial; ย้ายเฉพาะ contact helper ไม่ย้าย world generation หรือ entire enemy loop เปลี่ยน movement stop distance ให้สอดคล้อง hit radius นำ normal windup ออก; champion ต้องมี profile แยกจาก `elite` boolean ที่ใช้ร่วมกัน

แยก `UpdateHazards` ออกจาก `UpdateRunDirector` ให้เรียกครั้งเดียวหลัง movement และ `UpdateEnemies` จากนั้น resolve queued incoming hit ก่อน `UpdateShots` ของผู้เล่น รายละเอียด ordering ใน Code Guide; เมื่อ director จบรันให้ทิ้ง queue ทันที

### Phase 4 — Boss/projectile/hazard integration

ใช้สูตรและ queue เดียวกัน คง damage/telegraph ของบอสเดิมไว้ Snapshot source damage เมื่อสร้าง projectile/hazard; ใช้ source identity ของตัว projectile/hazard เอง จึงไม่สูญ hit หลัง projectile visual ถูกทำลาย ตรวจ segment swept collision เพื่อไม่ทะลุผู้เล่นเมื่อ 2x/เฟรมตก

### Phase 5 — Armor boon และ UX

ต่อ enum `IronBark` **ท้าย** `PerkKind` ห้ามเปลี่ยนหมายเลขเก่า เพิ่ม AllPerks, RankCap=5, ApplyPerk, draft cap eligibility, คำอธิบาย และแสดง Armor ใน Stats/Runes จากสูตรเดียวกัน ใช้ `Rank(IronBark)` เป็นแหล่งค่าเดียว ไม่สร้าง Armor field อีกอัน Run-only ไม่เขียน PlayerPrefs; VIT ยังคง HP/regen ไม่เพิ่ม Armor แฝง

### Phase 6 — Validation และ handoff

รัน smoke suite เดิมและเพิ่ม meaningful cases ด้านล่าง ตรวจภาพ telegraph/tint และลองเล่น common swarm/champion/guardian ที่ 1x และ 2x บันทึกผลจริงกับสิ่งที่ยังไม่ได้ตรวจ อัปเดต docs โดยไม่อ้างว่าทดสอบแล้วหากรันไม่ได้

## 5. Acceptance tests

- สูตรตามตัวอย่างครบ; negative/zero ไม่ heal ไม่เปิด grace ไม่เกิด popup
- contact นอก/บน/ในขอบ hitbox; sprite ลอยไม่เปลี่ยนระยะ; cooldown ไม่ reset เมื่อออกเข้าใหม่
- 20 common + boss hit ในเฟรมเดียวลด HP ครั้งเดียวด้วย strongest mitigated hit; shuffle candidate order ให้ผลเท่ากัน
- ที่เวลา `until - epsilon` ปฏิเสธ, ที่ `until` รับได้; source interval กับ global grace ทดสอบแยก
- Dash ระหว่าง spawn protection ไม่ย่น protection; หมด Dash ยังเหลือ spawn protection
- ปฏิเสธระหว่าง Camp/Results/ทุก modal แม้มีคนเรียก API โดยตรง
- pause/resume ที่ 1x/1.5x/2x; เทียบผลที่ simulation timestamps เดียวกัน และตรวจ wall-time pacing ผ่าน coroutine แยกจาก pure tests
- contact cooldown commit เฉพาะ accepted hit; projectile consumed on immune collision; hazard tick ไม่มี catch-up burst
- pool recycle/reuse ไม่รับ stale contact จาก spawn เก่า; clear/new run ไม่เหลือ queue หรือ immunity ที่ไม่ตั้งใจ
- swept projectile ข้ามผู้เล่นทั้งเส้นในเฟรมเดียวต้องโดน; ไม่โดนเมื่อ segment อยู่นอกรัศมี
- Armor boon rank/cap/reset; save เดิมโหลดได้; Swordsman ลดซ้ำไม่ได้
- outgoing crit/chain/ignite/Ember/nova และ death settlement/amber-only XP ผ่าน regression
- Guardian body damage ไม่ถูกคูณกับ contact ใหม่; queued hit ไม่ทำให้ settlement สองครั้ง

## 6. Definition of done

Agent ต้องส่งโค้ดที่คอมไพล์, tests ที่ผ่านจริง, รายการไฟล์, สูตรที่ใช้, ผล playtest/ข้อจำกัด และ docs ที่ตรงกับ implementation ห้ามอ้าง exact VS parity หรือ final balance จาก smoke tests อย่างเดียว ไม่แก้ระบบ map/spawn/UI ordering/Game Speed ที่เสร็จแล้วนอกเหนือจุดเชื่อมที่จำเป็น

## Prompt สำหรับส่งให้ agent ผู้พัฒนา

> Implement ระบบตาม Documentation/Monster_Attack_Implementation_Plan.md และ Documentation/Monster_Attack_Code_Guide.md เริ่มจากตรวจสถานะ Git และอ่านเฉพาะ sections/methods ที่เกี่ยวข้อง รักษางานเดิมและ partial-class identity ทำ contact combat, incoming damage pipeline, run-only Armor boon, immunity แยก source, Game Speed integration และ tests ตาม acceptance criteria คง outgoing combat/XP/save/spawn/boss schedule เดิม ไม่ rebuild หรือ deploy WebGL โดยอัตโนมัติ รายงานสิ่งที่ implement และตรวจสอบได้จริง แยกจากงานที่ยังไม่ได้ validate
