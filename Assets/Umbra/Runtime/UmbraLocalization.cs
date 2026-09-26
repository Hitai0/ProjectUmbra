using UnityEngine;

namespace Umbra
{
    public enum GameLanguage
    {
        English = 0,
        Thai = 1
    }

    public static class Loc
    {
        public static GameLanguage Current = GameLanguage.English;

        public static string T(string en, string th) => Current == GameLanguage.Thai ? th : en;

        public static string ClassName(HeroClass heroClass) => heroClass switch
        {
            HeroClass.Novice => T("NOVICE", "โนวิซ"),
            HeroClass.Archer => T("ARCHER", "นักธนู"),
            HeroClass.Mage => T("MAGE", "นักเวท"),
            HeroClass.Swordsman => T("SWORDSMAN", "นักดาบ"),
            _ => heroClass.ToString().ToUpperInvariant()
        };

        public static string ClassDescription(int index) => index switch
        {
            0 => T("Spirit arrows. Balanced and forgiving. Learn the trail.",
                   "ศรวิญญาณ สมดุลและควบคุมง่าย เหมาะสำหรับเริ่มเรียนรู้เส้นทาง"),
            1 => T("20% faster attacks. All projectile supports apply.",
                   "โจมตีเร็วขึ้น 20% รองรับบัฟและกระสุนเสริมทุกชนิด"),
            2 => T("Targeted lightning from above. Extra shots strike distinct foes. INT boosts damage.",
                   "เรียกสายฟ้าผ่ารายตัว ไม่มีระเบิดวงกว้าง เพิ่มกระสุนเพื่อผ่าหลายเป้าหมาย INT เพิ่มดาเมจ"),
            3 => T("Wide spirit slash. 20% less damage taken. Supports reshape its reach and impact.",
                   "ฟันดาบวิญญาณวงกว้าง ลดดาเมจที่ได้รับ 20% บัฟช่วยเพิ่มระยะและพลังฟัน"),
            _ => ""
        };

        public static string MapName(int index) => index switch
        {
            0 => T("AMBERFALL GROVE", "ป่าแอมเบอร์ฟอล"),
            1 => T("TWILIGHT MIRE", "บึงรัตติกาล"),
            2 => T("ASHFALL RIDGE", "สันเขาเถ้าธุลี"),
            _ => "MAP " + (index + 1)
        };

        public static string MapDescription(int index) => index switch
        {
            0 => T("Lv. 1-10  /  Golden woodland\nA forgiving first expedition. Dodge the Elder Bloom's seed volleys.",
                   "เลเวล 1-10  /  ป่าไม้สีทอง\nเส้นทางสำรวจแรกที่ท้าทาย หลบห่ากระสุนเมล็ดพันธุ์ของ Elder Bloom"),
            1 => T("Lv. 10-20  /  Violet wetland\nFoes move 40% faster and leave toxic pools. Keep moving.",
                   "เลเวล 10-20  /  หนองน้ำสีม่วง\nศัตรูเคลื่อนที่เร็วขึ้น 40% และทิ้งบ่อพิษไว้ จงเคลื่อนที่ตลอดเวลา"),
            2 => T("Lv. 20-30  /  Ember wilderness\nFallen foes explode after 1.5 seconds. Watch the warning circles.",
                   "เลเวล 20-30  /  ทุ่งเปลวเพลิง\nศัตรูที่ล้มลงจะระเบิดใน 1.5 วินาที ระวังวงกลมเตือนภัย"),
            _ => ""
        };

        public static string BossName(int index) => index switch
        {
            0 => T("ELDER BLOOM", "เอลเดอร์ บลูม"),
            1 => T("VENOMOUS CHITIN", "ไคตินพิษ"),
            2 => T("INFERNAL WARDEN", "ผู้คุมอัคคี"),
            _ => "BOSS"
        };

        public static string StageName(bool bossActive, float runTimer)
        {
            if (bossActive) return T("FINAL GUARDIAN", "ผู้พิทักษ์คนสุดท้าย");
            if (runTimer >= 600f) return T("THE CLIMAX", "ช่วงไคลแมกซ์");
            if (runTimer >= 300f) return T("THE SWARM", "ฝูงคลั่งถาโถม");
            return T("THE AWAKENING", "การตื่นรู้");
        }

        public static string RuneName(RuneKind rune) => rune switch
        {
            RuneKind.Pierce => T("PIERCING WIND", "วายุทะลวง"),
            RuneKind.Scatter => T("SPLIT SHOT", "ศรกระจาย"),
            _ => T("EMBER SEED", "เมล็ดเพลิง")
        };

        public static string RuneDescription(RuneKind rune) => rune switch
        {
            RuneKind.Pierce => T("One arrow. Passes through up to three enemies.",
                                 "ศร 1 ดอก พุ่งทะลวงศัตรูได้สูงสุด 3 ตัว"),
            RuneKind.Scatter => T("Three arrows in a fan. Lower damage per arrow.",
                                  "ศร 3 ดอกแผ่เป็นรูปพัด ดาเมจต่อดอกเบาลง"),
            _ => T("Arrows burst on impact, damaging nearby enemies.",
                   "ศรจะระเบิดเมื่อปะทะ สร้างความเสียหายแก่ศัตรูรอบข้าง")
        };

        public static string PerkTitle(CombatRules.PerkKind kind) => kind switch
        {
            CombatRules.PerkKind.RunePierce => T("Piercing Wind", "วายุทะลวง"),
            CombatRules.PerkKind.RuneScatter => T("Split Shot", "ศรกระจาย"),
            CombatRules.PerkKind.RuneEmber => T("Ember Seed", "เมล็ดเพลิง"),
            CombatRules.PerkKind.RapidFire => T("Quickdraw", "ศรฉับไว"),
            CombatRules.PerkKind.SwiftBoots => T("Forest Stride", "ก้าวย่างพงไพร"),
            CombatRules.PerkKind.Multishot => T("Twin Flight", "ปีกคู่ทะยาน"),
            CombatRules.PerkKind.Vitality => T("Roots of Life", "รากแห่งชีวิต"),
            CombatRules.PerkKind.Magnetism => T("Amber Attraction", "แรงดึงดูดอำพัน"),
            CombatRules.PerkKind.WindNovaPulse => T("Gale Ward", "ม่านพายุ"),
            CombatRules.PerkKind.Chain => T("Branching Spirit", "วิญญาณแตกแขนง"),
            CombatRules.PerkKind.Ignite => T("Kindling", "จุดประกายเพลิง"),
            CombatRules.PerkKind.SproutCard => T("Sprout Card", "การ์ดต้นกล้า"),
            CombatRules.PerkKind.MushroomCard => T("Mushroom Card", "การ์ดเห็ดป่า"),
            CombatRules.PerkKind.BattleFocus => T("Battle Focus", "สมาธิต่อสู้"),
            CombatRules.PerkKind.IronBark => T("Iron Bark", "เปลือกไม้เหล็ก"),
            _ => kind.ToString()
        };

        public static string PerkDescription(CombatRules.PerkKind kind) => kind switch
        {
            CombatRules.PerkKind.RunePierce => T("Arrows pierce up to 3 enemies in a straight line.",
                                                 "ศรทะลวงศัตรูได้สูงสุด 3 ตัวเป็นเส้นตรง"),
            CombatRules.PerkKind.RuneScatter => T("Fires 3 arrows in a wide spread fan.",
                                                  "ยิงศร 3 ดอกแผ่ออกไปเป็นรูปพัดกว้าง"),
            CombatRules.PerkKind.RuneEmber => T("Arrows explode on impact, damaging nearby foes.",
                                                "ศรระเบิดเมื่อปะทะ สร้างดาเมจแก่ศัตรูใกล้เคียง"),
            CombatRules.PerkKind.RapidFire => T("+15% attack rate. Up to 4 ranks.",
                                                "+15% ความเร็วในการโจมตี (สูงสุด 4 ขั้น)"),
            CombatRules.PerkKind.SwiftBoots => T("+8% movement speed. Up to 3 ranks.",
                                                 "+8% ความเร็วในการเคลื่อนที่ (สูงสุด 3 ขั้น)"),
            CombatRules.PerkKind.Multishot => T("+1 projectile. Swordsman gains +20% slash damage instead. Up to 2 ranks.",
                                                "+1 กระสุน (นักดาบจะได้ +20% ดาเมจฟันแทน, สูงสุด 2 ขั้น)"),
            CombatRules.PerkKind.Vitality => T("+20 Max Health and restore 30 HP. Up to 4 ranks.",
                                               "+20 พลังชีวิตสูงสุด และฟื้นฟู 30 HP (สูงสุด 4 ขั้น)"),
            CombatRules.PerkKind.Magnetism => T("+2m shard pickup radius. Up to 2 ranks.",
                                                "+2 เมตร รัศมีเก็บผลึกอำพัน (สูงสุด 2 ขั้น)"),
            CombatRules.PerkKind.WindNovaPulse => T("Unleash a radial nova every 10 seconds, independently of Q.",
                                                    "ปล่อยคลื่นพายุ Wind Nova อัตโนมัติทุก 10 วินาที แยกจากสกิล Q"),
            CombatRules.PerkKind.Chain => T("Each projectile's first hit chains to one nearby foe for 40% damage.",
                                            "การโจมตีแรกของกระสุนจะชิ่งไปโดนศัตรูใกล้เคียง สร้างดาเมจ 40%"),
            CombatRules.PerkKind.Ignite => T("Hits burn for 30% damage over 3 seconds. Refreshes; does not stack.",
                                             "การโจมตีจะเผาศัตรูสร้างดาเมจ 30% ใน 3 วินาที (รีเฟรชระยะเวลา ไม่ทับซ้อน)"),
            CombatRules.PerkKind.SproutCard => T("+1 projectile pierce. Swordsman gains +0.4m slash reach instead.",
                                                 "+1 การทะลวงของกระสุน (นักดาบจะได้ระยะฟันเพิ่ม +0.4 เมตร)"),
            CombatRules.PerkKind.MushroomCard => T("Defeating an enemy restores 1 HP.",
                                                   "ฟื้นฟู 1 HP ทุกครั้งที่กำจัดศัตรู"),
            CombatRules.PerkKind.BattleFocus => T("+8% damage this run. Always available.",
                                                  "+8% ดาเมจในการสำรวจรอบนี้ เลือกได้ไม่จำกัด"),
            CombatRules.PerkKind.IronBark => T("+1 Armor (reduces incoming damage). Up to 5 ranks.",
                                               "+1 เกราะ (ลดความเสียหายที่ได้รับลงโดยตรง, สูงสุด 5 ขั้น)"),
            _ => ""
        };

        public static string PerkCategory(string category) => category switch
        {
            "SUPPORT RUNE" => T("SUPPORT RUNE", "รูนสนับสนุน"),
            "STAT BOON" => T("STAT BOON", "บูนสเตตัส"),
            "COMBAT BOON" => T("COMBAT BOON", "บูนการต่อสู้"),
            "SURVIVAL BOON" => T("SURVIVAL BOON", "บูนเอาชีวิตรอด"),
            "UTILITY BOON" => T("UTILITY BOON", "บูนเสริมพลัง"),
            "PASSIVE BOON" => T("PASSIVE BOON", "บูนติดตัว"),
            "SHARED SUPPORT" => T("SHARED SUPPORT", "สนับสนุนร่วม"),
            "MONSTER CARD" => T("MONSTER CARD", "การ์ดมอนสเตอร์"),
            "RUNE" => T("RUNE", "รูน"),
            "OFFENSE" => T("OFFENSE", "โจมตี"),
            "DEFENSE" => T("DEFENSE", "ป้องกัน"),
            "UTILITY" => T("UTILITY", "เสริมพลัง"),
            "CARD" => T("CARD", "การ์ด"),
            _ => category
        };

        public static string PerkRarity(string rarity) => rarity switch
        {
            "COMMON" => T("COMMON", "ทั่วไป"),
            "RARE" => T("RARE", "หายาก"),
            "EPIC" => T("EPIC", "มหากาพย์"),
            _ => rarity
        };

        public static string StatName(CombatRules.StatKind s) => s switch
        {
            CombatRules.StatKind.STR => T("STR", "STR"),
            CombatRules.StatKind.AGI => T("AGI", "AGI"),
            CombatRules.StatKind.VIT => T("VIT", "VIT"),
            CombatRules.StatKind.INT => T("INT", "INT"),
            CombatRules.StatKind.DEX => T("DEX", "DEX"),
            _ => T("LUK", "LUK")
        };

        public static string StatDescription(CombatRules.StatKind s, int str, int agi, int vit, int intel, int dex, int luk) => s switch
        {
            CombatRules.StatKind.STR => T("+" + ((str - 1) * 2) + "% Base Damage",
                                          "+" + ((str - 1) * 2) + "% พลังโจมตีพื้นฐาน"),
            CombatRules.StatKind.AGI => T("+" + ((agi - 1) * 1.5f).ToString("0.0") + "% Atk Speed  •  +" + ((agi - 1) * 0.4f).ToString("0.0") + "% Move Speed",
                                          "+" + ((agi - 1) * 1.5f).ToString("0.0") + "% ความเร็วโจมตี  •  +" + ((agi - 1) * 0.4f).ToString("0.0") + "% ความเร็วเดิน"),
            CombatRules.StatKind.VIT => T("+" + ((vit - 1) * 8) + " Max HP  •  +" + CombatRules.HealthRegenPerSecond(vit).ToString("0.00") + " HP/s Regen",
                                          "+" + ((vit - 1) * 8) + " Max HP  •  +" + CombatRules.HealthRegenPerSecond(vit).ToString("0.00") + " ฟื้นฟู HP/วินาที"),
            CombatRules.StatKind.INT => T("-" + (CombatRules.CooldownReduction(intel) * 100).ToString("0.0") + "% Cooldowns  /  Mage spell damage",
                                          "-" + (CombatRules.CooldownReduction(intel) * 100).ToString("0.0") + "% คูลดาวน์  /  พลังเวทนักเวท"),
            CombatRules.StatKind.DEX => T("+" + ((dex - 1) * 2) + "% Arrow Speed  •  +" + ((dex - 1) * 1.5f).ToString("0.0") + "% Arrow Dmg",
                                          "+" + ((dex - 1) * 2) + "% ความเร็วศร  •  +" + ((dex - 1) * 1.5f).ToString("0.0") + "% ดาเมจศร"),
            _ => T((CombatRules.CritChance(luk) * 100).ToString("0.0") + "% Crit Chance (1.75x)  •  Rare Boon Luck",
                   (CombatRules.CritChance(luk) * 100).ToString("0.0") + "% โอกาสคริติคอล (1.75x)  •  โอกาสพบบูนหายาก")
        };

        public static string ResultReason(string reason) => reason switch
        {
            "Returned safely to camp" => T("Returned safely to camp", "เดินทางกลับสู่แคมป์อย่างปลอดภัย"),
            "Defeated in the field" => T("Defeated in the field", "พ่ายแพ้ในสนามรบ"),
            "Time expired" => T("Time expired", "หมดเวลาสำรวจ 15 นาที"),
            _ => reason
        };
    }
}
