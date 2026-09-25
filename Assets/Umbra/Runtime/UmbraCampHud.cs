using UnityEngine;

namespace Umbra
{
    public sealed partial class UmbraHud
    {
        void Paragraph(Rect rect,string value,Color? color=null,int size=16)
        {
            var style=new GUIStyle(text){wordWrap=true,fontSize=size,normal={textColor=color??cream}};
            GUI.Label(rect,value,style);
        }
        void DrawCamp()
        {
            Box(Backdrop,new(.015f,.035f,.025f,.58f));
            Label(85,44,"P R O J E C T   U M B R A",title,cream,1000);
            Label(87,91,Loc.T("THE WAYFARER'S CAMP    /    REST. REBUILD. RETURN.","แคมป์ผู้พเนจร    /    พักผ่อน. ฝึกฝน. หวนคืนสู่ป่า."),heading,gold,1000);
            if(Button(new(1040,50,135,38),Game.LanguageLabel))Game.ToggleLanguage();
            Label(1190,57,Loc.T("BASE LV. ","เลเวลหลัก ")+Game.BaseLevel+"   /   "+Game.Shards+Loc.T(" AMBER"," อำพัน"),heading,gold,340);
            Panel(new(80,150,345,590));
            Label(105,174,Loc.T("01  /  YOUR WANDERER","01  /  ผู้พเนจรของคุณ"),heading,gold,300);
            GUI.DrawTexture(new(180,224,140,190),portrait,ScaleMode.ScaleToFit);
            Label(105,435,Loc.ClassName(Game.Class),title,cream,300);
            Label(105,483,Loc.T("BASE LV. ","เลเวลหลัก ")+Game.BaseLevel+"   /   "+Game.StatPoints+Loc.T(" STAT POINTS"," แต้มสเตตัส"),heading,mint,300);
            Bar(105,522,295,(float)Game.BaseExperience/CombatRules.ExperienceToLevel(Game.BaseLevel),gold);
            Label(105,540,Game.BaseExperience+" / "+CombatRules.ExperienceToLevel(Game.BaseLevel)+Loc.T(" permanent XP"," EXP ถาวร"),small,muted,300);
            if(Button(new(105,584,295,46),Loc.T("ALLOCATE STATS  /  FREE RESPEC","จัดสรรสเตตัส  /  รีเซ็ตฟรี")))Game.StatsPanel=true;
            string supply=Game.SupplyRank>=5?Loc.T("CAMP SUPPLIES MAXED","เสบียงแคมป์เต็มแล้ว"):ShardsEnough()?Loc.T("UPGRADE SUPPLIES  /  ","อัปเกรดเสบียง  /  ")+Game.SupplyCost+Loc.T(" AMBER"," อำพัน"):Loc.T("SUPPLIES  /  NEED ","เสบียง  /  ขาดอีก ")+Game.SupplyCost+Loc.T(" AMBER"," อำพัน");
            if(Button(new(105,648,295,39),supply,ShardsEnough()))Game.BuySupplies();
            Label(105,699,Loc.T("Rank ","ขั้น ")+Game.SupplyRank+Loc.T(" / 5  •  +5 permanent HP per rank"," / 5  •  +5 HP ถาวรต่อขั้น"),small,muted,300);

            Panel(new(447,150,510,590));
            Label(474,174,Loc.T("02  /  CHOOSE YOUR PATH","02  /  เลือกสายอาชีพ"),heading,gold,450);
            Paragraph(new(474,211,452,45),Game.ClassesUnlocked?Loc.T("Switch classes freely. Your stats and starting support follow you.","เปลี่ยนอาชีพได้อย่างอิสระ สเตตัสและพลังตั้งต้นจะคงอยู่กับคุณ"):Loc.T("Unlock all classes at Base Lv. 10 or by defeating Elder Bloom.","ปลดล็อกทุกอาชีพเมื่อเลเวล 10 หรือกำจัด Elder Bloom"),muted,15);
            for(int i=0;i<4;i++)
            {
                var kind=(HeroClass)i;float x=474+(i%2)*237,y=285+(i/2)*163;bool unlocked=i==0||Game.ClassesUnlocked;
                if(Button(new(x,y,215,43),Loc.ClassName(kind)+(unlocked?"":Loc.T("  /  LOCKED","  /  ล็อก")),Game.Class==kind)&&unlocked)Game.SelectClass(kind);
                Paragraph(new(x+4,y+56,207,85),Loc.ClassDescription(i),unlocked?cream:muted,14);
            }
            Label(474,625,Loc.T("STARTING SUPPORT","พลังตั้งต้น"),small,gold,400);
            if(Button(new(474,653,452,46),Loc.RuneName(Game.Rune)+Loc.T("  /  CHANGE","  /  เปลี่ยน")))Game.RunePanel=true;

            Panel(new(979,150,541,590));
            Label(1006,174,Loc.T("03  /  CHOOSE AN EXPEDITION","03  /  เลือกพื้นที่สำรวจ"),heading,gold,490);
            for(int i=0;i<3;i++)
            {
                float y=226+i*159;bool unlocked=Game.MapUnlocked(i);
                if(Button(new(1006,y,487,43),Loc.MapName(i)+(unlocked?"":Loc.T("  /  LOCKED","  /  ล็อก")),Game.SelectedMap==i)&&unlocked)Game.SelectMap(i);
                Paragraph(new(1015,y+55,468,80),unlocked?Loc.MapDescription(i):Loc.T("Clear "+Loc.MapName(i-1)+" to open this path.","พิชิต "+Loc.MapName(i-1)+" เพื่อปลดล็อกเส้นทางนี้"),unlocked?cream:muted,15);
            }
            if(Button(new(80,774,170,44),Loc.T("HOW TO PLAY","วิธีเล่น")))Game.HelpPanel=true;
            if(Button(new(268,774,150,44),Game.Muted?Loc.T("SOUND: OFF","เสียง: ปิด"):Loc.T("SOUND: ON","เสียง: เปิด")))Game.ToggleSound();
            if(Button(new(435,774,180,44),Game.SoftFocus?Loc.T("SOFT FOCUS: ON","เบลอฉาก: เปิด"):Loc.T("SOFT FOCUS: OFF","เบลอฉาก: ปิด")))Game.ToggleFocus();
            if(Button(new(630,774,210,44),Game.IsMobile?Loc.T("INPUT: MOBILE TOUCH","ควบคุม: สัมผัส"):Loc.T("INPUT: DESKTOP","ควบคุม: คีย์บอร์ด")))Game.ToggleMobileInput();
            if(Button(new(851,774,110,44),Loc.T("SPEED: ","ความเร็ว: ")+Game.GameSpeedLabel))Game.CycleGameSpeed();
            if(Button(new(979,772,541,58),Loc.T("BEGIN EXPEDITION   /   UP TO 15 MINUTES","เริ่มการสำรวจ   /   สูงสุด 15 นาที"),true))Game.StartRun();
            Label(84,850,Loc.T("v"+UmbraPrototype.Version+"  /  SINGLE PLAYER  /  PROGRESS SAVED ON THIS DEVICE","v"+UmbraPrototype.Version+"  /  เล่นคนเดียว  /  บันทึกข้อมูลในเครื่องนี้"),small,muted,950);
            Label(982,850,Loc.T("Keep earned XP and collected amber on defeat. Boons reset.","แม้พ่ายแพ้ก็ยังคงรักษา XP และอำพันที่เก็บได้ บูนจะถูกรีเซ็ตใหม่"),small,cream,560);
        }
        bool ShardsEnough()=>Game.SupplyRank<5&&Game.Shards>=Game.SupplyCost;
        void DrawResults()
        {
            Box(Backdrop,new(.01f,.02f,.015f,.78f));
            Panel(new(400,120,800,650));
            Label(455,162,Game.Won?Loc.T("T H E   G R O V E   R E M E M B E R S","ป่ า ไ ม้ ยั ง ค ง จด จำ"):Loc.T("E V E R Y   P A T H   T E A C H E S","ทุ ก เ ส้ น ท า ง ม อ บ บ ท เ รี ย น"),heading,gold,700);
            Label(455,210,Game.Won?Loc.T("EXPEDITION COMPLETE","การสำรวจสำเร็จ"):Loc.T("EXPEDITION ENDED","การสำรวจสิ้นสุด"),title,cream,700);
            Label(455,269,Loc.ResultReason(Game.ResultReason),text,muted,700);
            Box(new(455,318,690,1),gold*.5f);
            int mins=(int)Game.RunTimer/60,secs=(int)Game.RunTimer%60;
            Label(455,352,Loc.T("TIME IN THE FIELD","เวลาในสนามรบ"),small,muted);Label(890,343,$"{mins:00}:{secs:00}",number,cream,220);
            Label(455,404,Loc.T("CREATURES DEFEATED","ศัตรูที่กำจัดได้"),small,muted);Label(890,395,Game.Kills.ToString(),number,cream,220);
            Label(455,456,Loc.T("AMBER BANKED","อำพันที่ได้รับ"),small,gold);Label(890,447,"+"+Game.RunShards,number,gold,220);
            Label(455,508,Loc.T("PERMANENT EXPERIENCE","ค่าประสบการณ์ถาวร"),small,mint);Label(890,499,"+"+Game.EarnedExperience,number,mint,220);
            Paragraph(new(455,563,690,56),Game.Won?Loc.T("Guardian bounty included. New paths open at camp. Rebuild your stats and try a different class.","รวมรางวัลปราบผู้พิทักษ์แล้ว เส้นทางใหม่เปิดขึ้นที่แคมป์ รีเซ็ตสเตตัสและลองเล่นอาชีพอื่นได้"):Loc.T("Your earned XP and collected amber are safe. Spend stat points or improve camp supplies before trying again.","XP และอำพันที่คุณสะสมไว้ปลอดภัย อัปเกรดสเตตัสหรือเสบียงแคมป์ก่อนออกสำรวจอีกครั้ง"),muted,16);
            if(Button(new(455,656,690,56),Loc.T("RETURN TO CAMP  /  BASE LV. ","กลับสู่แคมป์  /  เลเวลหลัก ")+Game.BaseLevel,true))Game.EnterCamp();
            Label(455,726,Loc.T("Saved locally. Run boons reset on your next expedition.","บันทึกข้อมูลในเครื่อง บูนการสำรวจจะรีเซ็ตในการเดินทางครั้งถัดไป"),small,muted,690);
        }
    }
}
