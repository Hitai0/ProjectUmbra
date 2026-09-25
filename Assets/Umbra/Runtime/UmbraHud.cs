using UnityEngine;

namespace Umbra
{
    internal readonly struct HudLayout
    {
        public readonly float Scale, Width, Height;
        public Vector2 Extra => new Vector2(Width - 1600f, Height - 900f);
        public HudLayout(float width, float height)
        {
            Scale = Mathf.Max(.0001f, Mathf.Min(width / 1600f, height / 900f));
            Width = width / Scale;
            Height = height / Scale;
        }
        public Vector2 Point(Vector2 point, float x, float y) => point + Vector2.Scale(Extra, new Vector2(x, y));
        public Matrix4x4 Matrix(float x, float y) => Matrix4x4.TRS(
            Point(Vector2.zero, x, y) * Scale, Quaternion.identity, new Vector3(Scale, Scale, 1));
        public Vector2 FromScreen(Vector2 point) => new Vector2(point.x / Scale, Height - point.y / Scale);
        public bool Contains(Vector2 point, Rect rect, float x, float y)
        {
            rect.position = Point(rect.position, x, y);
            return rect.Contains(point);
        }
        public bool OverHud(Vector2 p, bool mobile)
        {
            if (Contains(p, new Rect(26, 26, 322, mobile ? 157 : 113), 0, 0) ||
                Contains(p, new Rect(600, 18, 365, 88), .5f, 0) ||
                Contains(p, new Rect(1015, 26, 558, 52), 1, 0) ||
                Contains(p, new Rect(1334, 97, 239, 388), 1, 0)) return true;
            if (mobile) return Contains(p, new Rect(1240, 480, 300, 335), 1, 1);
            return Contains(p, new Rect(26, 792, 304, 79), 0, 1) ||
                Contains(p, new Rect(447, 759, 704, 113), .5f, 1);
        }
    }

    // Immediate-mode HUD keeps this playable slice self-contained, with no external UI packages.
    public sealed partial class UmbraHud : MonoBehaviour
    {
        public UmbraPrototype Game;
        GUIStyle text, small, title, number, button, centered, damage, heading;
        Texture2D portrait, circleTex;
        readonly Color cream=new(.93f,.88f,.73f), gold=new(.77f,.60f,.31f), muted=new(.60f,.65f,.57f), mint=new(.42f,.79f,.63f);
        HudLayout layout;
        void Anchor(float x, float y) => GUI.matrix = layout.Matrix(x, y);
        Rect Backdrop => new Rect(-layout.Extra.x / 2, -layout.Extra.y / 2, layout.Width, layout.Height);
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
        Texture2D MakeCircleTexture(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = size / 2f;
            float radius = center - 1.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(center, center));
                    float a = Mathf.Clamp01(radius - d + 0.5f);
                    tex.SetPixel(x, y, new Color(1, 1, 1, a));
                }
            }
            tex.Apply();
            return tex;
        }
        void Circle(Vector2 center, float radius, Color color)
        {
            if (circleTex == null) circleTex = MakeCircleTexture(64);
            GUI.color = color;
            GUI.DrawTexture(new Rect(center.x - radius, center.y - radius, radius * 2, radius * 2), circleTex);
            GUI.color = Color.white;
        }
        void CircleRing(Vector2 center, float radius, float stroke, Color ringColor, Color fillColor)
        {
            Circle(center, radius + stroke, ringColor);
            Circle(center, radius, fillColor);
        }
        void OnGUI()
        {
            if(Game==null||!Game.Ready)return;Styles();
            var overlay=Game.TopOverlay;
            bool previousEnabled=GUI.enabled;
            int previousDepth=GUI.depth;
            GUI.depth=-100; // IMGUI HUD is above world labels/other default-depth GUI.
            Game.DrawWorldLabels(damage);
            GUI.enabled=previousEnabled&&overlay==UmbraPrototype.HudOverlay.None;
            layout = new HudLayout(Screen.width, Screen.height);
            Matrix4x4 previousMatrix = GUI.matrix;
            Anchor(.5f, .5f);
            if(Game.Phase!=RunPhase.Running)
            {
                if(Game.Phase==RunPhase.Camp)DrawCamp();else DrawResults();
                GUI.enabled=previousEnabled;DrawOverlay(overlay);
                GUI.matrix=previousMatrix;GUI.depth=previousDepth;GUI.enabled=previousEnabled;return;
            }
            Anchor(0, 0);
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

            Anchor(.5f, 0);
            Panel(new(600,18,365,88));
            Label(640,25,"P R O J E C T   U M B R A",heading,cream,450);
            Label(686,46,Game.MapName,small,muted,400);
            int mins=(int)(Game.RunTimer/60f), secs=(int)(Game.RunTimer%60f);
            Label(720,67,string.Format("{0:00}:{1:00} / 15:00  {2}", mins, secs, Game.GameSpeedLabel),number,mint,240);

            Anchor(1, 0);
            Panel(new(1015,26,115,52));
            string statsLabel = Game.StatPoints>0 ? "STATS ["+Game.StatPoints+"]" : "STATS  [C]";
            if(Button(new(1023,36,99,31), statsLabel, Game.StatsPanel || Game.StatPoints>0)) Game.StatsPanel=!Game.StatsPanel;

            Panel(new(1140,26,115,52));
            if(Button(new(1148,36,99,31),Game.AutoAim?"AIM: AUTO":"AIM: MANUAL",Game.AutoAim))Game.AutoAim=!Game.AutoAim;

            Panel(new(1265,26,308,52));
            if(Button(new(1275,36,90,31),"RUNES  [TAB]",Game.RunePanel))Game.RunePanel=!Game.RunePanel;
            if(Button(new(1373,36,88,31),Game.IsMobile?"TOUCH":"DESKTOP",Game.IsMobile))Game.ToggleMobileInput();
            if(Button(new(1469,36,94,31),"PAUSE  [ESC]",Game.Paused))Game.TogglePause();

            Panel(new(1334,97,239,214));
            Label(1350,110,"THE OPEN MEADOW",heading,gold);
            Box(new(1351,143,204,135),new(.12f,.18f,.13f));
            Vector2 MapPoint(Vector3 point) => new Vector2(
                1355+Mathf.InverseLerp(-CombatRules.FieldHalfSize,CombatRules.FieldHalfSize,point.x)*196,
                274-Mathf.InverseLerp(-CombatRules.FieldHalfSize,CombatRules.FieldHalfSize,point.z)*127);
            foreach(var e in Game.Enemies)
            {
                if(e.hp<=0||!e.root.gameObject.activeSelf)continue;
                Vector2 p=MapPoint(e.root.position);Box(new(p.x-2,p.y-2,4,4),new(.80f,.42f,.28f));
            }
            Vector2 player=MapPoint(Game.Player.position);
            Box(new(player.x-3.5f,player.y-3.5f,7,7),cream);
            Label(1350,287,"N  /  FIELD 01",small,muted);
            Label(1472,287,"LOCAL",small,mint);
            Panel(new(1334,329,239,156));
            Label(1350,344,Game.StageName,heading,gold);
            Label(1350,374,Game.BossActive?Game.BossName:"Guardian arrives at 14:00",small,cream);
            Label(1350,400,"Defeated: "+Game.Kills,small,muted);
            Bar(1350,425,205,Game.BossActive?Game.BossHealth:Game.RunTimer/840f,Game.BossActive?new Color(.85f,.3f,.2f):mint);
            Label(1350,446,Game.BossActive?"Defeat it before 15:00":"Dodge amber warning circles",small,gold);

            Anchor(.5f, 0);
            if(Time.unscaledTime<Game.NoticeUntil)
            {
                var style=new GUIStyle(centered){fontSize=16};
                Box(new(425,112,750,38),new(.055f,.10f,.075f,.80f));
                GUI.Label(new Rect(425,112,750,38),Game.Notice,style);
            }
            if(!Game.IsMobile)
            {
                Anchor(0, 1);
                Panel(new(26,792,304,79));
                Label(43,803,"THIS EXPEDITION",small,muted);
                Label(43,824,Game.RunShards.ToString("N0")+"  amber",number,gold);
                Label(200,835,"Banked at end",small,muted);

                Anchor(.5f, 1);
                Panel(new(447,789,704,83));
                Skill(461,"LMB","SPIRIT ARROW",Game.AttackRemaining,CombatRules.AttackCooldown,()=>Game.Notify(Game.AutoAim?"Auto-firing at nearest foe.":"Hold LMB to aim and fire."));
                Skill(598,"Q","WIND NOVA",Game.VolleyRemaining,CombatRules.VolleyCooldown,()=>Game.TryVolley());
                Skill(735,"SPACE","QUICKSTEP",Game.DashRemaining,CombatRules.DashCooldown,()=>Game.TryDash());
                Skill(872,"E","MEND",Game.HealRemaining,CombatRules.HealCooldown,()=>Game.TryHeal());
                if(Button(new(1009,802,126,55),"TAB\nBUILD DETAILS"))Game.RunePanel=!Game.RunePanel;
                Label(535,759,"EQUIPPED  /  "+CombatRules.RuneName(Game.Rune)+"  •  "+(Game.AutoAim?"AUTO-AIM ON":"MANUAL AIM"),small,gold);
                Anchor(1, 1);
                Label(1254,826,"WASD move   /   RMB travel",small,cream);
                Label(1254,847,"Scroll zoom   /   H controls",small,muted);
            }
            else
            {
                DrawMobileCombatHud();
            }
            Anchor(0, 1);
            Label(28,880,"PRE-ALPHA  "+UmbraPrototype.Version+"   /   ROGUELITE HORDE SURVIVAL",small,muted,700);

            Anchor(.5f, .5f);
            GUI.enabled=previousEnabled;DrawOverlay(overlay);
            GUI.matrix=previousMatrix;GUI.depth=previousDepth;GUI.enabled=previousEnabled;
        }
        void DrawOverlay(UmbraPrototype.HudOverlay overlay)
        {
            switch(overlay)
            {
                case UmbraPrototype.HudOverlay.Pause: DrawPause();break;
                case UmbraPrototype.HudOverlay.Draft: DrawDraft();break;
                case UmbraPrototype.HudOverlay.Stats: DrawStats();break;
                case UmbraPrototype.HudOverlay.Help: DrawGuide();break;
                case UmbraPrototype.HudOverlay.Runes: DrawRunes();break;
            }
            // The entire backdrop owns pointer events, including clicks outside the panel.
            if(overlay!=UmbraPrototype.HudOverlay.None && (Event.current.type==EventType.MouseDown||
                Event.current.type==EventType.MouseUp||Event.current.type==EventType.ScrollWheel))Event.current.Use();
        }
        void DrawPause()
        {
                Box(Backdrop,new(0,0,0,.5f));Panel(new(560,260,480,410));
                Label(610,310,"A MOMENT OF STILLNESS",number);
                Label(610,355,"The grove can wait.",text,muted);
                if(Button(new(610,395,380,40),"RETURN TO THE GROVE"))Game.TogglePause();
                if(Button(new(610,445,185,36),Game.Muted?"SOUND: OFF":"SOUND: ON"))Game.ToggleSound();
                if(Button(new(805,445,185,36),Game.SoftFocus?"SOFT FOCUS: ON":"SOFT FOCUS: OFF"))Game.ToggleFocus();
                if(Button(new(610,490,240,36),Game.IsMobile?"CONTROLS: MOBILE TOUCH":"CONTROLS: DESKTOP"))Game.ToggleMobileInput();
                if(Button(new(860,490,130,36),"SPEED: "+Game.GameSpeedLabel))Game.CycleGameSpeed();
                if(Button(new(610,535,380,40),"END RUN & BANK COLLECTED REWARDS"))Game.FinishRun(false,"Returned safely to camp");
        }
        void Skill(float x,string key,string name,float remaining,float cooldown,System.Action action)
        {
            Rect rect=new(x,802,126,55);
            if(Button(rect,key+"\n"+(remaining>0?remaining.ToString("0.0")+"s":name)))action();
            if(remaining>0)Bar(x,856,126,1-remaining/cooldown,mint);
        }
        void DrawMobileCombatHud()
        {
            Anchor(0, 0);
            Panel(new(26, 145, 322, 38));
            Label(38, 153, "EXPEDITION AMBER: " + Game.RunShards.ToString("N0"), heading, gold, 300);

            // 1. Virtual Joystick (Bottom-Left)
            Vector2 stickCenter = Game.IsJoystickActive ? Game.JoystickOrigin : layout.Point(new Vector2(175, 735), 0, 1);
            Vector2 knobPos = Game.IsJoystickActive ? Game.JoystickCurrent : stickCenter;

            CircleRing(stickCenter, 75f, 3f, new Color(gold.r, gold.g, gold.b, Game.IsJoystickActive ? 0.65f : 0.30f), new Color(0.04f, 0.07f, 0.05f, 0.65f));

            float tickDist = 45f;
            Box(new Rect(stickCenter.x - 1, stickCenter.y - tickDist - 8, 2, 8), new Color(gold.r, gold.g, gold.b, 0.35f));
            Box(new Rect(stickCenter.x - 1, stickCenter.y + tickDist, 2, 8), new Color(gold.r, gold.g, gold.b, 0.35f));
            Box(new Rect(stickCenter.x - tickDist - 8, stickCenter.y - 1, 8, 2), new Color(gold.r, gold.g, gold.b, 0.35f));
            Box(new Rect(stickCenter.x + tickDist, stickCenter.y - 1, 8, 2), new Color(gold.r, gold.g, gold.b, 0.35f));

            if (!Game.IsJoystickActive)
            {
                var moveStyle = new GUIStyle(centered) { fontSize = 12, normal = { textColor = new Color(cream.r, cream.g, cream.b, 0.55f) } };
                GUI.Label(new Rect(stickCenter.x - 60, stickCenter.y - 10, 120, 20), "DRAG TO MOVE", moveStyle);
            }

            CircleRing(knobPos, 32f, 2.5f, gold, new Color(0.12f, 0.20f, 0.16f, 0.95f));
            Circle(knobPos, 12f, Game.IsJoystickActive ? mint : gold);

            // 2. Action Buttons (Bottom-Right Arc)
            Anchor(1, 1);
            Vector2 dodgeCenter = new(1430, 730);
            Vector2 novaCenter = new(1295, 755);
            Vector2 healCenter = new(1430, 585);
            Vector2 aimCenter = new(1295, 635);

            var btnStyle = new GUIStyle(centered) { fontSize = 14, fontStyle = FontStyle.Bold };

            // A. DODGE Button (Radius 58)
            bool dodgeReady = Game.DashRemaining <= 0;
            Color dodgeRingColor = dodgeReady ? mint : new Color(0.4f, 0.45f, 0.4f, 0.5f);
            CircleRing(dodgeCenter, 58f, 3.5f, dodgeRingColor, new Color(0.06f, 0.10f, 0.08f, 0.92f));
            if (!dodgeReady)
            {
                float frac = Game.DashRemaining / CombatRules.DashCooldown;
                Circle(dodgeCenter, 58f * Mathf.Clamp01(frac), new Color(0.25f, 0.08f, 0.08f, 0.65f));
                Label(dodgeCenter.x - 50, dodgeCenter.y - 18, "DODGE\n" + Game.DashRemaining.ToString("0.0") + "s", btnStyle, cream, 100);
            }
            else
            {
                Label(dodgeCenter.x - 50, dodgeCenter.y - 18, "DODGE\nREADY", btnStyle, mint, 100);
            }

            // B. NOVA Button (Radius 48)
            bool novaReady = Game.VolleyRemaining <= 0;
            Color novaRingColor = novaReady ? gold : new Color(0.4f, 0.45f, 0.4f, 0.5f);
            CircleRing(novaCenter, 48f, 3f, novaRingColor, new Color(0.06f, 0.10f, 0.08f, 0.92f));
            if (!novaReady)
            {
                float frac = Game.VolleyRemaining / CombatRules.VolleyCooldown;
                Circle(novaCenter, 48f * Mathf.Clamp01(frac), new Color(0.25f, 0.08f, 0.08f, 0.65f));
                Label(novaCenter.x - 45, novaCenter.y - 18, "NOVA\n" + Game.VolleyRemaining.ToString("0.0") + "s", btnStyle, cream, 90);
            }
            else
            {
                Label(novaCenter.x - 45, novaCenter.y - 18, "NOVA\nREADY", btnStyle, gold, 90);
            }

            // C. MEND Button (Radius 48)
            bool healReady = Game.HealRemaining <= 0;
            Color healRingColor = healReady ? new Color(.35f, .85f, .45f) : new Color(0.4f, 0.45f, 0.4f, 0.5f);
            CircleRing(healCenter, 48f, 3f, healRingColor, new Color(0.06f, 0.10f, 0.08f, 0.92f));
            if (!healReady)
            {
                float frac = Game.HealRemaining / CombatRules.HealCooldown;
                Circle(healCenter, 48f * Mathf.Clamp01(frac), new Color(0.25f, 0.08f, 0.08f, 0.65f));
                Label(healCenter.x - 45, healCenter.y - 18, "MEND\n" + Game.HealRemaining.ToString("0.0") + "s", btnStyle, cream, 90);
            }
            else
            {
                Label(healCenter.x - 45, healCenter.y - 18, "MEND\nREADY", btnStyle, new Color(.35f, .85f, .45f), 90);
            }

            // D. AIM TOGGLE Button (Radius 40)
            Color aimRingColor = Game.AutoAim ? gold : muted;
            CircleRing(aimCenter, 40f, 2.5f, aimRingColor, new Color(0.06f, 0.10f, 0.08f, 0.88f));
            var aimStyle = new GUIStyle(centered) { fontSize = 12, fontStyle = FontStyle.Bold };
            Label(aimCenter.x - 40, aimCenter.y - 16, Game.AutoAim ? "AIM\nAUTO" : "AIM\nMANUAL", aimStyle, Game.AutoAim ? gold : cream, 80);

            // Quick mobile Tab / Runes button
            if (Button(new Rect(1430, 480, 110, 42), "BUILD [TAB]")) Game.RunePanel = !Game.RunePanel;
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
            Box(Backdrop, new Color(0, 0, 0, backdropAlpha));

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
            Vector2 mouseGui = Event.current.mousePosition;

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
                if (GUI.Button(cardRect, GUIContent.none, GUIStyle.none)) {Game.ApplyPerk(perk);return;}

                Label(x + 20, y + 20, perk.Category + "  •  " + perk.Rarity, small, rarityColor, cardWidth - 40);
                var nameStyle = new GUIStyle(heading) { fontSize = 18, fontStyle = FontStyle.Bold };
                Label(x + 20, y + 48, perk.Title, nameStyle, cream, cardWidth - 40);
                Box(new Rect(x + 20, y + 82, cardWidth - 40, 2), new Color(rarityColor.r, rarityColor.g, rarityColor.b, .35f));

                var wrapStyle = new GUIStyle(text) { wordWrap = true, fontSize = 15, normal = { textColor = cream } };
                GUI.Label(new Rect(x + 20, y + 100, cardWidth - 40, 220), perk.Description, wrapStyle);

                string btnLabel = Game.IsMobile ? "ACCEPT BOON" : "[" + (i + 1) + "] ACCEPT BOON";
                if (Button(new Rect(x + 25, y + cardHeight - 65, cardWidth - 50, 44), btnLabel, true))
                {
                    Game.ApplyPerk(perk);return;
                }
            }
        }
        void DrawRunes()
        {
            Box(Backdrop,new(0,0,0,.45f));Panel(new(376,237,848,409));
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
            Box(Backdrop,new Color(0,0,0,.72f));
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
            Box(Backdrop,new(0,0,0,.45f));Panel(new(480,228,640,460));
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
