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
            Label(87,91,"THE WAYFARER'S CAMP    /    REST. REBUILD. RETURN.",heading,gold,1000);
            Label(1190,57,"BASE LV. "+Game.BaseLevel+"   /   "+Game.Shards+" AMBER",heading,gold,340);
            Panel(new(80,150,345,590));
            Label(105,174,"01  /  YOUR WANDERER",heading,gold,300);
            GUI.DrawTexture(new(180,224,140,190),portrait,ScaleMode.ScaleToFit);
            Label(105,435,Game.Class.ToString().ToUpperInvariant(),title,cream,300);
            Label(105,483,"BASE LV. "+Game.BaseLevel+"   /   "+Game.StatPoints+" STAT POINTS",heading,mint,300);
            Bar(105,522,295,(float)Game.BaseExperience/CombatRules.ExperienceToLevel(Game.BaseLevel),gold);
            Label(105,540,Game.BaseExperience+" / "+CombatRules.ExperienceToLevel(Game.BaseLevel)+" permanent XP",small,muted,300);
            if(Button(new(105,584,295,46),"ALLOCATE STATS  /  FREE RESPEC"))Game.StatsPanel=true;
            string supply=Game.SupplyRank>=5?"CAMP SUPPLIES MAXED":ShardsEnough()?"UPGRADE SUPPLIES  /  "+Game.SupplyCost+" AMBER":"SUPPLIES  /  NEED "+Game.SupplyCost+" AMBER";
            if(Button(new(105,648,295,39),supply,ShardsEnough()))Game.BuySupplies();
            Label(105,699,"Rank "+Game.SupplyRank+" / 5  •  +5 permanent HP per rank",small,muted,300);

            Panel(new(447,150,510,590));
            Label(474,174,"02  /  CHOOSE YOUR PATH",heading,gold,450);
            Paragraph(new(474,211,452,45),Game.ClassesUnlocked?"Switch classes freely. Your stats and starting support follow you.":"Unlock all classes at Base Lv. 10 or by defeating Elder Bloom.",muted,15);
            for(int i=0;i<4;i++)
            {
                var kind=(HeroClass)i;float x=474+(i%2)*237,y=285+(i/2)*163;bool unlocked=i==0||Game.ClassesUnlocked;
                if(Button(new(x,y,215,43),kind.ToString().ToUpperInvariant()+(unlocked?"":"  /  LOCKED"),Game.Class==kind)&&unlocked)Game.SelectClass(kind);
                Paragraph(new(x+4,y+56,207,85),UmbraPrototype.ClassDescriptions[i],unlocked?cream:muted,14);
            }
            Label(474,625,"STARTING SUPPORT",small,gold,400);
            if(Button(new(474,653,452,46),CombatRules.RuneName(Game.Rune)+"  /  CHANGE"))Game.RunePanel=true;

            Panel(new(979,150,541,590));
            Label(1006,174,"03  /  CHOOSE AN EXPEDITION",heading,gold,490);
            for(int i=0;i<3;i++)
            {
                float y=226+i*159;bool unlocked=Game.MapUnlocked(i);
                if(Button(new(1006,y,487,43),UmbraPrototype.MapNames[i]+(unlocked?"":"  /  LOCKED"),Game.SelectedMap==i)&&unlocked)Game.SelectMap(i);
                Paragraph(new(1015,y+55,468,80),unlocked?UmbraPrototype.MapDescriptions[i]:"Clear "+UmbraPrototype.MapNames[i-1]+" to open this path.",unlocked?cream:muted,15);
            }
            if(Button(new(80,774,170,44),"HOW TO PLAY"))Game.HelpPanel=true;
            if(Button(new(268,774,150,44),Game.Muted?"SOUND: OFF":"SOUND: ON"))Game.ToggleSound();
            if(Button(new(435,774,180,44),Game.SoftFocus?"SOFT FOCUS: ON":"SOFT FOCUS: OFF"))Game.ToggleFocus();
            if(Button(new(630,774,210,44),Game.IsMobile?"INPUT: MOBILE TOUCH":"INPUT: DESKTOP"))Game.ToggleMobileInput();
            if(Button(new(979,772,541,58),"BEGIN EXPEDITION   /   UP TO 15 MINUTES",true))Game.StartRun();
            Label(84,850,"v"+UmbraPrototype.Version+"  /  SINGLE PLAYER  /  PROGRESS SAVED ON THIS DEVICE",small,muted,950);
            Label(982,850,"Keep earned XP and collected amber on defeat. Boons reset.",small,cream,560);
        }
        bool ShardsEnough()=>Game.SupplyRank<5&&Game.Shards>=Game.SupplyCost;
        void DrawResults()
        {
            Box(Backdrop,new(.01f,.02f,.015f,.78f));
            Panel(new(400,120,800,650));
            Label(455,162,Game.Won?"T H E   G R O V E   R E M E M B E R S":"E V E R Y   P A T H   T E A C H E S",heading,gold,700);
            Label(455,210,Game.Won?"EXPEDITION COMPLETE":"EXPEDITION ENDED",title,cream,700);
            Label(455,269,Game.ResultReason,text,muted,700);
            Box(new(455,318,690,1),gold*.5f);
            int mins=(int)Game.RunTimer/60,secs=(int)Game.RunTimer%60;
            Label(455,352,"TIME IN THE FIELD",small,muted);Label(890,343,$"{mins:00}:{secs:00}",number,cream,220);
            Label(455,404,"CREATURES DEFEATED",small,muted);Label(890,395,Game.Kills.ToString(),number,cream,220);
            Label(455,456,"AMBER BANKED",small,gold);Label(890,447,"+"+Game.RunShards,number,gold,220);
            Label(455,508,"PERMANENT EXPERIENCE",small,mint);Label(890,499,"+"+Game.EarnedExperience,number,mint,220);
            Paragraph(new(455,563,690,56),Game.Won?"Guardian bounty included. New paths open at camp. Rebuild your stats and try a different class.":"Your earned XP and collected amber are safe. Spend stat points or improve camp supplies before trying again.",muted,16);
            if(Button(new(455,656,690,56),"RETURN TO CAMP  /  BASE LV. "+Game.BaseLevel,true))Game.EnterCamp();
            Label(455,726,"Saved locally. Run boons reset on your next expedition.",small,muted,690);
        }
    }
}
