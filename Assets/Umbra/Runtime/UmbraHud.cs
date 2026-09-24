using UnityEngine;

namespace Umbra
{
    // Immediate-mode HUD keeps this playable slice self-contained, with no external UI packages.
    public sealed class UmbraHud : MonoBehaviour
    {
        public UmbraPrototype Game;
        GUIStyle text, small, title, number, button, centered, damage, heading;
        Texture2D portrait;
        readonly Color cream=new(.93f,.88f,.73f), gold=new(.77f,.60f,.31f), muted=new(.60f,.65f,.57f), mint=new(.42f,.79f,.63f);
        float scale;
        void Styles()
        {
            if(text!=null)return;
            Font font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text=new GUIStyle{font=font,fontSize=17,normal={textColor=cream}};
            small=new GUIStyle(text){fontSize=12};
            title=new GUIStyle(text){fontSize=32,fontStyle=FontStyle.Bold};
            heading=new GUIStyle(text){fontSize=13,fontStyle=FontStyle.Bold};
            number=new GUIStyle(text){fontSize=21,fontStyle=FontStyle.Bold};
            centered=new GUIStyle(text){alignment=TextAnchor.MiddleCenter};
            button=new GUIStyle(centered){fontSize=14,normal={textColor=cream},hover={textColor=Color.white}};
            damage=new GUIStyle(text){fontSize=19,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter};
            portrait=Resources.Load<Texture2D>("Umbra/ranger_idle");
        }
        void Box(Rect r,Color c){GUI.color=c;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=Color.white;}
        void Panel(Rect r)
        {
            Box(new Rect(r.x-1,r.y-1,r.width+2,r.height+2),new Color(gold.r,gold.g,gold.b,.7f));
            Box(r,new Color(.045f,.075f,.062f,.94f));
            Box(new Rect(r.x+5,r.y+5,r.width-10,1),new Color(gold.r,gold.g,gold.b,.22f));
        }
        void Label(float x,float y,string value,GUIStyle style=null,Color? color=null,float width=500)
        {
            GUI.color=color??Color.white;GUI.Label(new Rect(x,y,width,40),value,style??text);GUI.color=Color.white;
        }
        bool Button(Rect r,string value,bool active=false)
        {
            Box(r,active?new Color(.23f,.30f,.20f,.96f):new Color(.085f,.13f,.11f,.95f));
            Box(new Rect(r.x,r.y+r.height-2,r.width,2),active?gold:new Color(.22f,.27f,.20f));
            return GUI.Button(r,value,button);
        }
        void Bar(float x,float y,float width,float fraction,Color color)
        {
            Box(new(x,y,width,7),new(.02f,.04f,.03f));
            Box(new(x,y,width*Mathf.Clamp01(fraction),7),color);
            Box(new(x,y,width*Mathf.Clamp01(fraction),1),new(color.r*1.3f,color.g*1.3f,color.b*1.3f));
        }
        void OnGUI()
        {
            if(Game==null||!Game.Ready)return;Styles();
            Game.DrawWorldLabels(damage);
            scale=Mathf.Min(Screen.width/1600f,Screen.height/900f);
            float offsetX=(Screen.width-1600*scale)/2,offsetY=(Screen.height-900*scale)/2;
            GUI.matrix=Matrix4x4.TRS(new Vector3(offsetX,offsetY,0),Quaternion.identity,new Vector3(scale,scale,1));
            Panel(new(26,26,322,113));
            Box(new(39,39,64,82),new(.15f,.20f,.14f));
            GUI.DrawTexture(new Rect(43,38,56,84),portrait,ScaleMode.ScaleToFit);
            Label(119,39,"WANDERER",heading);
            Label(278,39,"Lv. "+Game.Level,heading,gold);
            Label(119,59,"Wayfarer  /  Ranger",small,muted);
            Bar(119,83,210,(float)Game.Health/CombatRules.MaxHealth,new(.70f,.29f,.22f));
            Label(119,96,Game.Health+" / "+CombatRules.MaxHealth+" HP",small);
            Label(243,96,Game.Experience+" XP",small,gold);
            Bar(39,126,293,(float)Game.Experience/CombatRules.ExperienceToLevel(Game.Level),gold);

            Label(640,30,"P R O J E C T   U M B R A",heading,cream,450);
            Label(686,53,"A M B E R F A L L   G R O V E",small,muted,400);
            Panel(new(1265,26,308,52));
            if(Button(new(1275,36,90,31),"RUNES  [TAB]",Game.RunePanel))Game.RunePanel=!Game.RunePanel;
            if(Button(new(1373,36,88,31),"GUIDE  [H]",Game.HelpPanel))Game.HelpPanel=!Game.HelpPanel;
            if(Button(new(1469,36,94,31),"PAUSE  [ESC]",Game.Paused)){Game.Paused=!Game.Paused;Time.timeScale=Game.Paused?0:1;}

            Panel(new(1334,97,239,214));
            Label(1350,110,"THE OLD PILGRIM ROAD",heading,gold);
            Box(new(1351,143,204,135),new(.12f,.18f,.13f));
            Box(new(1436,149,9,122),new(.43f,.37f,.23f));
            Box(new(1504,143,12,135),new(.19f,.35f,.32f));
            foreach(var e in Game.Enemies)
            {
                if(e.hp<=0)continue;
                Vector3 p=e.root.position;Box(new(1449+p.x*4,213-p.z*2.8f,4,4),new(.80f,.42f,.28f));
            }
            Vector3 player=Game.Player.position;
            Box(new(1447+player.x*4,211-player.z*2.8f,7,7),cream);
            Label(1350,287,"N  /  FIELD 01",small,muted);
            Label(1472,287,"LOCAL",small,mint);
            Panel(new(1334,329,239,156));
            Label(1350,344,"A WHISPER IN THE LEAVES",heading,gold);
            Label(1350,374,"Restore the restless grove.",small,cream);
            Label(1350,400,"Defeat grove creatures",small,muted);
            Label(1512,399,Mathf.Min(Game.Kills,6)+" / 6",small,cream);
            Bar(1350,425,205,Game.Kills/6f,mint);
            Label(1350,446,Game.Kills>=6?"COMPLETE  /  +25 amber":"REWARD  /  25 amber shards",small,Game.Kills>=6?mint:gold);

            if(Time.time<Game.NoticeUntil)
            {
                var style=new GUIStyle(centered){fontSize=16};
                Box(new(425,112,750,38),new(.055f,.10f,.075f,.80f));
                GUI.Label(new Rect(425,112,750,38),Game.Notice,style);
            }
            Panel(new(26,792,304,79));
            Label(43,803,"AMBER COLLECTION",small,muted);
            Label(43,824,Game.Shards.ToString("N0")+"  shards",number,gold);
            Label(200,835,"Saved locally",small,muted);

            Panel(new(447,789,704,83));
            Skill(461,"LMB","SPIRIT ARROW",Game.AttackRemaining,CombatRules.AttackCooldown,()=>Game.Notify("Hold left mouse to aim and fire."));
            Skill(598,"Q","WIND NOVA",Game.VolleyRemaining,CombatRules.VolleyCooldown,()=>Game.TryVolley());
            Skill(735,"SPACE","QUICKSTEP",Game.DashRemaining,CombatRules.DashCooldown,()=>Game.TryDash());
            Skill(872,"E","MEND",Game.HealRemaining,CombatRules.HealCooldown,()=>Game.TryHeal());
            if(Button(new(1009,802,126,55),"R\nCHANGE RUNE"))Game.SetRune((RuneKind)(((int)Game.Rune+1)%3));
            Label(535,759,"EQUIPPED  /  "+CombatRules.RuneName(Game.Rune),small,gold);
            Label(1254,826,"WASD move   /   RMB travel",small,cream);
            Label(1254,847,"Scroll zoom   /   H controls",small,muted);
            Label(28,880,"PRE-ALPHA  "+UmbraPrototype.Version+"   /   SINGLE-PLAYER PROTOTYPE",small,muted,700);

            if(Game.RunePanel)DrawRunes();
            if(Game.HelpPanel)DrawGuide();
            if(Game.Paused)
            {
                Box(new(0,0,1600,900),new(0,0,0,.5f));Panel(new(560,345,480,210));
                Label(610,379,"A MOMENT OF STILLNESS",number);
                Label(610,424,"The grove can wait.",text,muted);
                if(Button(new(610,479,380,45),"RETURN TO THE GROVE")){Game.Paused=false;Time.timeScale=1;}
            }
            GUI.matrix=Matrix4x4.identity;
        }
        void Skill(float x,string key,string name,float remaining,float cooldown,System.Action action)
        {
            Rect rect=new(x,802,126,55);
            if(Button(rect,key+"\n"+(remaining>0?remaining.ToString("0.0")+"s":name)))action();
            if(remaining>0)Bar(x,856,126,1-remaining/cooldown,mint);
        }
        void DrawRunes()
        {
            Box(new(0,0,1600,900),new(0,0,0,.45f));Panel(new(376,237,848,409));
            Label(408,264,"THE ART OF THE ARROW",title);
            Label(410,312,"One skill. Three ways to shape it. Switch freely while exploring.",text,muted);
            for(int i=0;i<3;i++)
            {
                RuneKind rune=(RuneKind)i;float x=407+i*269;
                Panel(new(x,367,248,189));
                Label(x+17,385,"0"+(i+1)+"  /  SUPPORT RUNE",small,gold);
                Label(x+17,414,CombatRules.RuneName(rune),heading);
                var wrap=new GUIStyle(small){wordWrap=true};
                GUI.Label(new Rect(x+17,446,215,53),CombatRules.RuneDescription(rune),wrap);
                if(Button(new(x+17,508,214,33),Game.Rune==rune?"EQUIPPED":"EQUIP  ["+(i+1)+"]",Game.Rune==rune))Game.SetRune(rune);
            }
            if(Button(new(945,589,246,34),"BACK TO THE GROVE  [TAB]"))Game.RunePanel=false;
            Label(409,596,"Rune choice is saved on this device.",small,muted);
        }
        void DrawGuide()
        {
            Box(new(0,0,1600,900),new(0,0,0,.45f));Panel(new(480,228,640,455));
            Label(516,254,"WELCOME, WANDERER",title);
            string[] lines={"WASD / Arrow keys     Move through the grove","Right mouse                 Travel to a point (no pathfinding)","Hold left mouse            Aim and fire spirit arrows","Q                                   Wind Nova: a ring of arrows","Space                            Quickstep with brief invulnerability","E                                    Mend: restore health","R / 1 / 2 / 3                   Change your support rune","Tab                                Open the rune collection","Walk over amber shards to collect them.","Shards and rune choice persist. Combat resets each session."};
            for(int i=0;i<lines.Length;i++)Label(516,316+i*27,lines[i],i>7?small:text,i>7?muted:cream);
            if(Button(new(516,618,568,38),"BEGIN EXPLORING  [H]"))Game.HelpPanel=false;
        }
    }
}
