using UnityEngine;

namespace Umbra
{
    // Immediate-mode HUD keeps this playable slice self-contained, with no external UI packages.
    public sealed partial class UmbraHud : MonoBehaviour
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
            if(Game.Phase!=RunPhase.Running)
            {
                if(Game.Phase==RunPhase.Camp)DrawCamp();else DrawResults();
                if(Game.StatsPanel)DrawStats();
                if(Game.RunePanel)DrawRunes();
                if(Game.HelpPanel)DrawGuide();
                GUI.matrix=Matrix4x4.identity;return;
            }
            Panel(new(26,26,322,113));
            if(GUI.Button(new Rect(26,26,322,113),GUIContent.none,GUIStyle.none)) Game.StatsPanel=!Game.StatsPanel;
            Box(new(39,39,64,82),new(.15f,.20f,.14f));
            GUI.DrawTexture(new Rect(43,38,56,84),portrait,ScaleMode.ScaleToFit);
            Label(119,39,"WANDERER",heading);
            Label(278,39,"Lv. "+Game.Level,heading,gold);
            if(Game.StatPoints>0) Label(260,59,"+"+Game.StatPoints+" PTS",small,mint);
            else Label(119,59,Game.Class+"  /  Run Lv. "+Game.RunLevel,small,muted);
            Bar(119,83,210,(float)Game.Health/Game.MaxHealth,new(.70f,.29f,.22f));
            Label(119,96,Game.Health+" / "+Game.MaxHealth+" HP",small);
            Label(243,96,Game.RunExperience+" XP",small,gold);
            Bar(39,126,293,(float)Game.RunExperience/CombatRules.RunExperienceToLevel(Game.RunLevel),gold);

            Panel(new(600,18,365,88));
            Label(640,25,"P R O J E C T   U M B R A",heading,cream,450);
            Label(686,46,Game.MapName,small,muted,400);
            int mins=(int)(Game.RunTimer/60f), secs=(int)(Game.RunTimer%60f);
            Label(720,67,string.Format("{0:00}:{1:00} / 15:00", mins, secs),number,mint,200);

            Panel(new(1015,26,115,52));
            string statsLabel = Game.StatPoints>0 ? "STATS ["+Game.StatPoints+"]" : "STATS  [C]";
            if(Button(new(1023,36,99,31), statsLabel, Game.StatsPanel || Game.StatPoints>0)) Game.StatsPanel=!Game.StatsPanel;

            Panel(new(1140,26,115,52));
            if(Button(new(1148,36,99,31),Game.AutoAim?"AIM: AUTO":"AIM: MANUAL",Game.AutoAim))Game.AutoAim=!Game.AutoAim;

            Panel(new(1265,26,308,52));
            if(Button(new(1275,36,90,31),"RUNES  [TAB]",Game.RunePanel))Game.RunePanel=!Game.RunePanel;
            if(Button(new(1373,36,88,31),"GUIDE  [H]",Game.HelpPanel))Game.HelpPanel=!Game.HelpPanel;
            if(Button(new(1469,36,94,31),"PAUSE  [ESC]",Game.Paused))Game.TogglePause();

            Panel(new(1334,97,239,214));
            Label(1350,110,"THE OLD PILGRIM ROAD",heading,gold);
            Box(new(1351,143,204,135),new(.12f,.18f,.13f));
            Box(new(1436,149,9,122),new(.43f,.37f,.23f));
            Box(new(1504,143,12,135),new(.19f,.35f,.32f));
            foreach(var e in Game.Enemies)
            {
                if(e.hp<=0||!e.root.gameObject.activeSelf)continue;
                Vector3 p=e.root.position;Box(new(1449+p.x*4,213-p.z*2.8f,4,4),new(.80f,.42f,.28f));
            }
            Vector3 player=Game.Player.position;
            Box(new(1447+player.x*4,211-player.z*2.8f,7,7),cream);
            Label(1350,287,"N  /  FIELD 01",small,muted);
            Label(1472,287,"LOCAL",small,mint);
            Panel(new(1334,329,239,156));
            Label(1350,344,Game.StageName,heading,gold);
            Label(1350,374,Game.BossActive?Game.BossName:"Guardian arrives at 14:00",small,cream);
            Label(1350,400,"Defeated: "+Game.Kills,small,muted);
            Bar(1350,425,205,Game.BossActive?Game.BossHealth:Game.RunTimer/840f,Game.BossActive?new Color(.85f,.3f,.2f):mint);
            Label(1350,446,Game.BossActive?"Defeat it before 15:00":"Dodge amber warning circles",small,gold);

            if(Time.unscaledTime<Game.NoticeUntil)
            {
                var style=new GUIStyle(centered){fontSize=16};
                Box(new(425,112,750,38),new(.055f,.10f,.075f,.80f));
                GUI.Label(new Rect(425,112,750,38),Game.Notice,style);
            }
            Panel(new(26,792,304,79));
            Label(43,803,"THIS EXPEDITION",small,muted);
            Label(43,824,Game.RunShards.ToString("N0")+"  amber",number,gold);
            Label(200,835,"Banked at end",small,muted);

            Panel(new(447,789,704,83));
            Skill(461,"LMB","SPIRIT ARROW",Game.AttackRemaining,CombatRules.AttackCooldown,()=>Game.Notify(Game.AutoAim?"Auto-firing at nearest foe.":"Hold LMB to aim and fire."));
            Skill(598,"Q","WIND NOVA",Game.VolleyRemaining,CombatRules.VolleyCooldown,()=>Game.TryVolley());
            Skill(735,"SPACE","QUICKSTEP",Game.DashRemaining,CombatRules.DashCooldown,()=>Game.TryDash());
            Skill(872,"E","MEND",Game.HealRemaining,CombatRules.HealCooldown,()=>Game.TryHeal());
            if(Button(new(1009,802,126,55),"TAB\nBUILD DETAILS"))Game.RunePanel=!Game.RunePanel;
            Label(535,759,"EQUIPPED  /  "+CombatRules.RuneName(Game.Rune)+"  •  "+(Game.AutoAim?"AUTO-AIM ON":"MANUAL AIM"),small,gold);
            Label(1254,826,"WASD move   /   RMB travel",small,cream);
            Label(1254,847,"Scroll zoom   /   H controls",small,muted);
            Label(28,880,"PRE-ALPHA  "+UmbraPrototype.Version+"   /   ROGUELITE HORDE SURVIVAL",small,muted,700);

            if(Game.RunePanel)DrawRunes();
            if(Game.HelpPanel)DrawGuide();
            if(Game.StatsPanel)DrawStats();
            if(Game.Drafting)DrawDraft();
            if(Game.Paused)
            {
                Box(new(0,0,1600,900),new(0,0,0,.5f));Panel(new(560,280,480,355));
                Label(610,379,"A MOMENT OF STILLNESS",number);
                Label(610,424,"The grove can wait.",text,muted);
                if(Button(new(610,465,380,40),"RETURN TO THE GROVE"))Game.TogglePause();
                if(Button(new(610,515,185,36),Game.Muted?"SOUND: OFF":"SOUND: ON"))Game.ToggleSound();
                if(Button(new(805,515,185,36),Game.SoftFocus?"SOFT FOCUS: ON":"SOFT FOCUS: OFF"))Game.ToggleFocus();
                if(Button(new(610,568,380,40),"END RUN & BANK COLLECTED REWARDS"))Game.FinishRun(false,"Returned safely to camp");
            }
            GUI.matrix=Matrix4x4.identity;
        }
        void Skill(float x,string key,string name,float remaining,float cooldown,System.Action action)
        {
            Rect rect=new(x,802,126,55);
            if(Button(rect,key+"\n"+(remaining>0?remaining.ToString("0.0")+"s":name)))action();
            if(remaining>0)Bar(x,856,126,1-remaining/cooldown,mint);
        }
        float EaseOutBack(float x)
        {
            if (x >= 1f) return 1f;
            float c1 = 1.70158f;
            float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3) + c1 * Mathf.Pow(x - 1f, 2);
        }
        void DrawDraft()
        {
            float animTime = Time.unscaledTime - Game.DraftOpenedAt;
            if (animTime < 0f) animTime = 0f;

            // Smooth backdrop fade (0.15s)
            float backdropAlpha = Mathf.Lerp(0f, 0.78f, Mathf.Clamp01(animTime / 0.15f));
            Box(new Rect(0, 0, 1600, 900), new Color(0, 0, 0, backdropAlpha));

            Panel(new(250, 150, 1100, 580));
            Label(570, 180, "C H O O S E   A   B O O N", title, gold);
            Label(480, 230, "Choose a boon to empower your spirit in the grove:", text, cream);

            if (Game.Rerolls > 0 && Button(new(1035, 192, 250, 36), "REROLL [R]  /  " + Game.Rerolls + " LEFT"))
            {
                Game.RerollDraft();
                return;
            }

            var perks = Game.ActiveDraft;
            if (perks == null || perks.Count == 0) return;

            // Quick-select hotkeys: keyboard 1, 2, 3 or numpad 1, 2, 3 and R
            int hotkeyChoice = -1;
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null)
            {
                if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) hotkeyChoice = 0;
                else if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) hotkeyChoice = 1;
                else if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) hotkeyChoice = 2;
                else if (kb.rKey.wasPressedThisFrame && Game.Rerolls > 0)
                {
                    Game.RerollDraft();
                    return;
                }
            }
            if (Event.current.type == EventType.KeyDown)
            {
                if (Event.current.keyCode == KeyCode.Alpha1 || Event.current.keyCode == KeyCode.Keypad1) hotkeyChoice = 0;
                else if (Event.current.keyCode == KeyCode.Alpha2 || Event.current.keyCode == KeyCode.Keypad2) hotkeyChoice = 1;
                else if (Event.current.keyCode == KeyCode.Alpha3 || Event.current.keyCode == KeyCode.Keypad3) hotkeyChoice = 2;
                else if (Event.current.keyCode == KeyCode.R && Game.Rerolls > 0)
                {
                    Game.RerollDraft();
                    Event.current.Use();
                    return;
                }
            }

            if (hotkeyChoice >= 0 && hotkeyChoice < perks.Count)
            {
                Game.ApplyPerk(perks[hotkeyChoice]);
                if (Event.current.type == EventType.KeyDown) Event.current.Use();
                return;
            }

            float cardWidth = 310;
            float cardHeight = 420;
            float spacing = 35;
            float startX = 250 + (1100 - (perks.Count * cardWidth + (perks.Count - 1) * spacing)) / 2f;
            Vector2 mouseGui = (Vector2)GUI.matrix.inverse.MultiplyPoint3x4(Event.current.mousePosition);

            for (int i = 0; i < perks.Count; i++)
            {
                var perk = perks[i];
                float x = startX + i * (cardWidth + spacing);
                float baseY = 275;

                // Staggered entrance: card 0 at 0.00s, card 1 at 0.05s, card 2 at 0.10s
                float cardStart = i * 0.05f;
                float cardProgress = Mathf.Clamp01((animTime - cardStart) / 0.22f);
                float easeY = EaseOutBack(cardProgress);
                float animY = Mathf.LerpUnclamped(baseY + 70f, baseY, easeY);

                // Hover lift (8px upward) once card has mostly entered
                Rect hoverRect = new Rect(x, baseY, cardWidth, cardHeight);
                bool isHovered = hoverRect.Contains(mouseGui);
                float y = animY - (isHovered && cardProgress >= 0.95f ? 8f : 0f);

                Rect cardRect = new(x, y, cardWidth, cardHeight);

                Color rarityColor = perk.Rarity switch
                {
                    "RARE" => gold,
                    "EPIC" => new Color(.76f, .58f, 1f),
                    "UNCOMMON" => mint,
                    _ => cream
                };

                if (isHovered) rarityColor = Color.Lerp(rarityColor, Color.white, 0.28f);

                Box(new Rect(x - 2, y - 2, cardWidth + 4, cardHeight + 4), rarityColor * (isHovered ? 0.95f : 0.70f));
                Box(cardRect, new Color(.055f, .085f, .07f, .97f));
                Box(new Rect(x + 5, y + 5, cardWidth - 10, 1), rarityColor * 0.4f);

                Label(x + 20, y + 20, perk.Category + "  •  " + perk.Rarity, small, rarityColor, cardWidth - 40);
                var nameStyle = new GUIStyle(heading) { fontSize = 18, fontStyle = FontStyle.Bold };
                Label(x + 20, y + 48, perk.Title, nameStyle, cream, cardWidth - 40);
                Box(new Rect(x + 20, y + 82, cardWidth - 40, 2), new Color(rarityColor.r, rarityColor.g, rarityColor.b, .35f));

                var wrapStyle = new GUIStyle(text) { wordWrap = true, fontSize = 15, normal = { textColor = cream } };
                GUI.Label(new Rect(x + 20, y + 100, cardWidth - 40, 220), perk.Description, wrapStyle);

                string btnLabel = "[" + (i + 1) + "] ACCEPT BOON";
                if (Button(new Rect(x + 25, y + cardHeight - 65, cardWidth - 50, 44), btnLabel, true))
                {
                    Game.ApplyPerk(perk);
                }
            }
        }
        void DrawRunes()
        {
            Box(new(0,0,1600,900),new(0,0,0,.45f));Panel(new(376,237,848,409));
            Label(408,264,"THE ART OF THE ARROW",title);
            Label(410,312,Game.Phase==RunPhase.Camp?"Choose a starting rune. Shared by every class.":"Your run build. Boons reset when you return to camp.",text,muted);
            for(int i=0;i<3;i++)
            {
                RuneKind rune=(RuneKind)i;float x=407+i*269;
                Panel(new(x,367,248,189));
                Label(x+17,385,"0"+(i+1)+"  /  SUPPORT RUNE",small,gold);
                Label(x+17,414,CombatRules.RuneName(rune),heading);
                var wrap=new GUIStyle(small){wordWrap=true};
                GUI.Label(new Rect(x+17,446,215,53),CombatRules.RuneDescription(rune),wrap);
                if(Game.Phase==RunPhase.Camp){if(Button(new(x+17,508,214,33),Game.Rune==rune?"EQUIPPED":"EQUIP",Game.Rune==rune))Game.SetRune(rune);}
                else Label(x+17,515,Game.Rune==rune?"EQUIPPED":"Available through boon draft",small,Game.Rune==rune?gold:muted,220);
            }
            if(Button(new(945,589,246,34),"BACK TO THE GROVE  [TAB]"))Game.RunePanel=false;
            Label(409,584,"ATK "+Game.AttackDamage+"  /  SPEED "+Game.AttackSpeedMultiplier.ToString("0.00")+"x  /  EXTRA SHOTS "+Game.BonusProjectiles,small,muted);
            Label(409,609,"Chain "+Game.Rank(CombatRules.PerkKind.Chain)+"  /  Ignite "+Game.Rank(CombatRules.PerkKind.Ignite)+"  /  Sprout "+Game.Rank(CombatRules.PerkKind.SproutCard)+"  /  Mushroom "+Game.Rank(CombatRules.PerkKind.MushroomCard),small,muted);
        }
        void DrawStats()
        {
            Box(new Rect(0,0,1600,900),new Color(0,0,0,.72f));
            Panel(new(460,130,680,640));
            Label(570,155,"C H A R A C T E R   S T A T U S",title,gold);
            Label(525,205,Game.Class+"  •  BASE LV. " + Game.Level + "  (" + Game.Experience + " / " + CombatRules.ExperienceToLevel(Game.Level) + " EXP)",text,cream);

            // Points Banner
            Panel(new(490,240,620,48));
            bool hasPts = Game.StatPoints > 0 && Game.Phase==RunPhase.Camp;
            Label(510,252,"AVAILABLE STAT POINTS:  " + Game.StatPoints,heading,hasPts?mint:muted);
            if(Game.Phase==RunPhase.Camp){if(Button(new(930,248,160,32),"RESET STATS (FREE)")) Game.ResetStats();}
            else Label(925,256,"Allocate at camp",small,muted,170);

            // 6 Stats rows
            CombatRules.StatKind[] stats = (CombatRules.StatKind[])System.Enum.GetValues(typeof(CombatRules.StatKind));
            float startY = 302;
            for(int i = 0; i < stats.Length; i++)
            {
                var s = stats[i];
                float y = startY + i * 58;
                Box(new(490, y, 620, 50), new Color(.07f,.11f,.09f,.92f));
                Box(new(490, y+48, 620, 2), new Color(gold.r,gold.g,gold.b,.25f));

                Label(510, y+13, s.ToString(), heading, gold);
                Label(570, y+13, Game.GetStat(s).ToString("00"), number, cream);

                string desc = s switch
                {
                    CombatRules.StatKind.STR => "+" + ((Game.STR-1)*2) + "% Base Damage",
                    CombatRules.StatKind.AGI => "+" + ((Game.AGI-1)*1.5f).ToString("0.0") + "% Atk Speed  •  +" + ((Game.AGI-1)*0.4f).ToString("0.0") + "% Move Speed",
                    CombatRules.StatKind.VIT => "+" + ((Game.VIT-1)*8) + " Max HP  •  +" + CombatRules.HealthRegenPerSecond(Game.VIT).ToString("0.00") + " HP/s Regen",
                    CombatRules.StatKind.INT => "-" + (CombatRules.CooldownReduction(Game.INT)*100).ToString("0.0") + "% Cooldowns  /  Mage spell damage",
                    CombatRules.StatKind.DEX => "+" + ((Game.DEX-1)*2) + "% Arrow Speed  •  +" + ((Game.DEX-1)*1.5f).ToString("0.0") + "% Arrow Dmg",
                    _ => (CombatRules.CritChance(Game.LUK)*100).ToString("0.0") + "% Crit Chance (1.75x)  •  Rare Boon Luck"
                };
                Label(625, y+16, desc, small, muted, 360);

                if(hasPts)
                {
                    if(Button(new(1030, y+9, 65, 32), "+1", true))
                    {
                        Game.TryAddStat(s);
                    }
                }
            }

            if(Button(new(700, 672, 200, 42), "CLOSE  [C]")) Game.StatsPanel = false;
        }
        void DrawGuide()
        {
            Box(new(0,0,1600,900),new(0,0,0,.45f));Panel(new(480,228,640,460));
            Label(516,254,"WELCOME, WANDERER",title);
            string[] lines={"WASD / Arrow keys     Move through the grove",
                            "Right mouse                 Travel to a point (no pathfinding)",
                            "Left mouse / Auto-aim  Fire spirit arrows at nearby enemies",
                            "C                                    Character Stats: distribute STR, AGI, VIT, INT, DEX, LUK",
                            "Q                                   Wind Nova: a ring of arrows",
                            "Space                            Quickstep with brief invulnerability",
                            "E                                    Mend: restore health",
                            "Tab                                Inspect your run build (pauses)",
                            "Camp                             Change class, rune and stats for free",
                            "Defeat the guardian at 14:00 before time expires at 15:00."};
            for(int i=0;i<lines.Length;i++)Label(516,314+i*27,lines[i],i>7?small:text,i>7?muted:cream);
            if(Button(new(516,624,568,38),"BEGIN EXPLORING  [H]")) Game.HelpPanel=false;
        }
    }
}
