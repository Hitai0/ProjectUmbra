using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Random = UnityEngine.Random;

namespace Umbra
{
    public enum RunPhase { Camp, Running, Results }
    public enum HeroClass { Novice, Archer, Mage, Swordsman }

    public sealed partial class UmbraPrototype
    {
        public RunPhase Phase { get; private set; }
        public HeroClass Class { get; private set; }
        public int SelectedMap { get; private set; }
        public int ClearedMaps { get; private set; }
        public int RunLevel { get; private set; } = 1;
        public int RunExperience { get; private set; }
        public int RunShards { get; private set; }
        public int EarnedExperience { get; private set; }
        public int SupplyRank { get; private set; }
        public int SupplyCost => 60 + SupplyRank * 60;
        public int Rerolls { get; private set; }
        public bool Won { get; private set; }
        public string ResultReason { get; private set; }
        public bool ClassesUnlocked => BaseLevel >= 10 || (ClearedMaps & 1) != 0;
        public bool BossActive => boss != null && boss.hp > 0 && boss.root.gameObject.activeSelf;
        public float BossHealth => BossActive ? (float)boss.hp / boss.maxHp : 0;
        public string MapName => MapNames[SelectedMap];
        public string BossName => BossNames[SelectedMap];
        public string StageName => BossActive ? "FINAL GUARDIAN" : RunTimer >= 600 ? "THE CLIMAX" : RunTimer >= 300 ? "THE SWARM" : "THE AWAKENING";
        public bool IsModal => Drafting || Paused || RunePanel || HelpPanel || StatsPanel;
        bool CanAct => Phase == RunPhase.Running && !IsModal;
        public static readonly string[] MapNames = { "AMBERFALL GROVE", "TWILIGHT MIRE", "ASHFALL RIDGE" };
        public static readonly string[] BossNames = { "ELDER BLOOM", "VENOMOUS CHITIN", "INFERNAL WARDEN" };
        public static readonly string[] ClassDescriptions = {
            "Spirit arrows. Balanced and forgiving. Learn the trail.",
            "20% faster attacks. All projectile supports apply.",
            "25% stronger hits. INT adds spell damage. Shared supports apply.",
            "Wide spirit slash. 20% less damage taken. Supports reshape its reach and impact."
        };
        public static readonly string[] MapDescriptions = {
            "Lv. 1-10  /  Golden woodland\nA forgiving first expedition. Dodge the Elder Bloom's seed volleys.",
            "Lv. 10-20  /  Violet wetland\nFoes move 40% faster and leave toxic pools. Keep moving.",
            "Lv. 20-30  /  Ember wilderness\nFallen foes explode after 1.5 seconds. Watch the warning circles."
        };
        public int AttackDamage => Mathf.RoundToInt(CombatRules.Damage(Rune, BaseLevel, STR, DEX)
            * (1 + Rank(CombatRules.PerkKind.BattleFocus) * .08f)
            * (Class == HeroClass.Mage ? 1.25f + (INT - 1) * .01f : Class == HeroClass.Swordsman ? 1.4f : 1f));
        public bool Muted { get; private set; }
        public bool SoftFocus { get; private set; } = true;
        readonly Dictionary<CombatRules.PerkKind,int> perkRanks = new();
        readonly List<Hazard> hazards = new();
        readonly List<HostileShot> hostileShots = new();
        readonly Dictionary<Material,Color> originalColors = new();
        Enemy boss;
        RuneKind campRune;
        int pendingDrafts;
        float regenBank, nextBossAttack, bossReleaseAt;
        bool miniSpawned, bossSpawned, suppressSave;
        AudioSource audioSource;
        AudioClip[] cues;
        LineRenderer arenaBoundary;
        sealed class Hazard { public Vector3 point; public float radius, activates, expires, tick; public int damage; public LineRenderer ring; }
        sealed class HostileShot { public Transform root; public Vector3 velocity; public float expires; }
        bool HasSproutCard => Rank(CombatRules.PerkKind.SproutCard)>0;
        bool HasMushroomCard => Rank(CombatRules.PerkKind.MushroomCard)>0;
        public int Rank(CombatRules.PerkKind kind) => perkRanks.TryGetValue(kind,out int n)?n:0;

        void LoadCampaignProfile()
        {
            ClearedMaps=PlayerPrefs.GetInt("Umbra.ClearedMaps",0)&7;
            Class=(HeroClass)Mathf.Clamp(PlayerPrefs.GetInt("Umbra.Class",0),0,3);
            if(!ClassesUnlocked)Class=HeroClass.Novice;
            SupplyRank=Mathf.Clamp(PlayerPrefs.GetInt("Umbra.SupplyRank",0),0,5);
            Muted=PlayerPrefs.GetInt("Umbra.Muted",0)!=0;
            SoftFocus=PlayerPrefs.GetInt("Umbra.SoftFocus",1)!=0;
            campRune=Rune;
        }
        void SaveCampaignProfile()
        {
            PlayerPrefs.SetInt("Umbra.ClearedMaps",ClearedMaps);
            PlayerPrefs.SetInt("Umbra.Class",(int)Class);
            PlayerPrefs.SetInt("Umbra.SupplyRank",SupplyRank);
            PlayerPrefs.SetInt("Umbra.Muted",Muted?1:0);
            PlayerPrefs.SetInt("Umbra.SoftFocus",SoftFocus?1:0);
            PlayerPrefs.SetInt("Umbra.SaveVersion",2);
        }
        public bool MapUnlocked(int map) => map>=0&&map<3&&(map==0||(ClearedMaps&(1<<(map-1)))!=0);
        public void SelectMap(int map){if(Phase==RunPhase.Camp&&MapUnlocked(map)){SelectedMap=map;ApplyMapPalette();}}
        public void SelectClass(HeroClass kind)
        {
            if(Phase!=RunPhase.Camp||!Enum.IsDefined(typeof(HeroClass),kind)||(!ClassesUnlocked&&kind!=HeroClass.Novice))return;
            Class=kind;SaveProfile();
        }
        public void BuySupplies()
        {
            if(Phase!=RunPhase.Camp||SupplyRank>=5||Shards<SupplyCost)return;
            Shards-=SupplyCost;SupplyRank++;Health=MaxHealth;SaveProfile();PlayCue(2);
        }
        public void ToggleSound(){Muted=!Muted;SaveProfile();}
        public void ToggleFocus(){SoftFocus=!SoftFocus;if(dof!=null)dof.active=SoftFocus;SaveProfile();}
        public void TogglePause()
        {
            if(Phase!=RunPhase.Running)return;
            if(HelpPanel)HelpPanel=false;
            else if(StatsPanel)StatsPanel=false;
            else if(RunePanel)RunePanel=false;
            else if(!Drafting)Paused=!Paused;
            SyncPause();
        }
        void SyncPause(){Time.timeScale=Phase==RunPhase.Running&&!IsModal?1:0;}
        void OnApplicationFocus(bool focus){}

        public void EnterCamp()
        {
            Phase=RunPhase.Camp;ClearCombat();
            Drafting=Paused=RunePanel=StatsPanel=HelpPanel=false;
            MaxHealthBonus=0;Health=MaxHealth;
            Player.position=new(0,0,-2);walkingTo=false;
            if(dof!=null)dof.active=SoftFocus;
            SyncPause();
        }
        public void StartRun()
        {
            if(Phase!=RunPhase.Camp||!MapUnlocked(SelectedMap))return;
            ClearCombat();campRune=Rune;perkRanks.Clear();
            RunTimer=0;RunLevel=1;RunExperience=EarnedExperience=RunShards=Kills=Collected=pendingDrafts=0;
            MaxHealthBonus=BonusProjectiles=0;AttackSpeedMultiplier=MoveSpeedMultiplier=1;PickupRadius=5;HasNovaPulse=false;
            Health=MaxHealth;regenBank=0;miniSpawned=bossSpawned=false;boss=null;
            Rerolls=1;Won=false;ResultReason="";
            attackAt=volleyAt=dashAt=healAt=0;dashUntil=0;invulnerableUntil=Time.time+2;nextHordeSpawn=Time.time+2;
            Drafting=Paused=RunePanel=StatsPanel=HelpPanel=false;walkingTo=false;
            Player.position=new(0,0,-2);Phase=RunPhase.Running;ApplyMapPalette();
            TriggerLevelUp();Notify("Choose your first boon. Your 15-minute expedition begins.");
        }
        void ClearCombat()
        {
            if(arenaBoundary)Destroy(arenaBoundary.gameObject);
            if(activePillar){Destroy(activePillar);activePillar=null;}
            if(activePillarLight){Destroy(activePillarLight.gameObject);activePillarLight=null;}
            foreach(var e in enemies){e.hp=0;e.boss=false;e.windupUntil=e.burnUntil=0;e.root.gameObject.SetActive(false);e.visual.localScale=Vector3.one*(e.elite?1.5f:1);}
            foreach(var s in shots)if(s.root)Destroy(s.root.gameObject);shots.Clear();
            foreach(var s in hostileShots)if(s.root)Destroy(s.root.gameObject);hostileShots.Clear();
            foreach(var l in loot)if(l.root)Destroy(l.root.gameObject);loot.Clear();
            foreach(var r in rings)if(r.line)Destroy(r.line.gameObject);rings.Clear();
            foreach(var h in hazards)if(h.ring)Destroy(h.ring.gameObject);hazards.Clear();
            floats.Clear();boss=null;
        }
        public void FinishRun(bool victory,string reason)
        {
            if(Phase!=RunPhase.Running)return;
            Phase=RunPhase.Results;Won=victory;ResultReason=reason;
            if(victory){ClearedMaps|=1<<SelectedMap;RunShards+=75*(SelectedMap+1);EarnedExperience+=100*(SelectedMap+1);}
            Shards+=RunShards;BaseExperience+=EarnedExperience;
            while(BaseLevel<99&&BaseExperience>=CombatRules.ExperienceToLevel(BaseLevel))
            {BaseExperience-=CombatRules.ExperienceToLevel(BaseLevel);BaseLevel++;StatPoints+=CombatRules.StatPointsPerLevel;}
            if(BaseLevel==99)BaseExperience=Mathf.Min(BaseExperience,CombatRules.ExperienceToLevel(99)-1);
            Rune=campRune;Drafting=Paused=StatsPanel=RunePanel=HelpPanel=false;ActiveDraft.Clear();
            SaveProfile();SyncPause();PlayCue(victory?2:1);
        }
        void AwardRunExperience(int amount)
        {
            EarnedExperience+=Mathf.Max(1,amount/2);RunExperience+=amount;
            while(RunExperience>=CombatRules.RunExperienceToLevel(RunLevel))
            {RunExperience-=CombatRules.RunExperienceToLevel(RunLevel);RunLevel++;pendingDrafts++;}
            if(pendingDrafts>0&&!Drafting&&!BossActive){pendingDrafts--;TriggerLevelUp();}
        }
        int RankCap(CombatRules.PerkKind k) => k switch {
            CombatRules.PerkKind.RapidFire or CombatRules.PerkKind.Vitality=>4,
            CombatRules.PerkKind.SwiftBoots=>3,
            CombatRules.PerkKind.Multishot or CombatRules.PerkKind.Magnetism=>2,
            CombatRules.PerkKind.BattleFocus=>999,
            _=>1
        };
        List<CombatRules.Perk> RollDraft()
        {
            var pool=new List<CombatRules.Perk>();
            foreach(var p in CombatRules.AllPerks)
            {
                if(Rank(p.Kind)>=RankCap(p.Kind))continue;
                if((p.Kind==CombatRules.PerkKind.RunePierce&&Rune==RuneKind.Pierce)||(p.Kind==CombatRules.PerkKind.RuneScatter&&Rune==RuneKind.Scatter)||(p.Kind==CombatRules.PerkKind.RuneEmber&&Rune==RuneKind.Ember))continue;
                pool.Add(p);
            }
            var result=new List<CombatRules.Perk>();
            while(result.Count<3&&pool.Count>0)
            {
                float Weight(CombatRules.Perk p)=>p.Rarity=="COMMON"?1f:.40f+Mathf.Min(.6f,(LUK-1)*.01f);
                float total=0;foreach(var p in pool)total+=Weight(p);
                float roll=Random.value*total;int index=pool.Count-1;
                for(int i=0;i<pool.Count;i++){roll-=Weight(pool[i]);if(roll<=0){index=i;break;}}
                result.Add(pool[index]);pool.RemoveAt(index);
            }
            return result;
        }
        public void RerollDraft(){if(!Drafting||Rerolls<=0)return;Rerolls--;DraftOpenedAt=Time.unscaledTime;ActiveDraft=RollDraft();PlayCue(0);}
        void CastNova()
        {
            for(int i=0;i<12;i++)Fire(Quaternion.Euler(0,i*30,0)*Vector3.forward,Rune,Mathf.RoundToInt(AttackDamage*.8f));
            Pulse(Player.position,3.8f,mint,.5f);PlayCue(2);
        }
        void Slash(Vector3 direction,int damage,bool crit)
        {
            float reach=Rune==RuneKind.Pierce?3.3f:2.5f;
            reach+=HasSproutCard?.4f:0;
            float arc=Rune==RuneKind.Scatter?-.3f:.25f;int extra=BonusProjectiles;
            Pulse(Player.position+direction,reach,new(.5f,1f,.85f),.22f);
            foreach(var e in enemies)
            {
                Vector3 delta=e.root.position-Player.position;
                if(e.hp<=0||delta.magnitude>reach||Vector3.Dot(direction,delta.normalized)<arc)continue;
                int hit=Mathf.RoundToInt(damage*(1+extra*.2f));DamageEnemy(e,hit,crit);ApplyHitSupports(e,hit,true);
                if(Rune==RuneKind.Ember)foreach(var other in enemies)if(other!=e&&other.hp>0&&Vector3.Distance(other.root.position,e.root.position)<1.8f)DamageEnemy(other,hit/2);
            }
        }
        void ApplyHitSupports(Enemy enemy,int damage,bool chain)
        {
            if(Rank(CombatRules.PerkKind.Ignite)>0&&enemy.hp>0){enemy.burnUntil=Time.time+3.1f;enemy.burnDamage=Mathf.Max(1,damage/10);if(enemy.burnTick<Time.time)enemy.burnTick=Time.time+1;}
            if(chain&&Rank(CombatRules.PerkKind.Chain)>0)
            {
                Enemy other=null;float best=16;
                foreach(var e in enemies){float sq=(e.root.position-enemy.root.position).sqrMagnitude;if(e!=enemy&&e.hp>0&&sq<best){best=sq;other=e;}}
                if(other!=null){Pulse(other.root.position,.8f,mint,.25f);DamageEnemy(other,Mathf.RoundToInt(damage*.4f));}
            }
        }
        bool TrySpawnPosition(out Vector3 pos)
        {
            for(int i=0;i<32;i++)
            {
                float angle=Random.value*Mathf.PI*2;pos=Player.position+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*Random.Range(10f,15f);
                if(IsWalkable(pos)&&(pos-Player.position).sqrMagnitude>=64)return true;
            }
            pos=Vector3.zero;return false;
        }
        void SpawnEnemy(Enemy e,Vector3 pos,int hp)
        {
            e.root.position=pos;e.home=pos;e.maxHp=e.hp=hp;e.windupUntil=e.burnUntil=e.flashUntil=0;
            e.attackAt=Time.time+1;e.boss=false;e.root.gameObject.SetActive(true);
            e.visual.localScale=Vector3.one*(e.elite?1.5f:1);
        }
        void SlideEnemy(Enemy e,Vector3 motion)
        {
            Vector3 p=e.root.position;if(IsWalkable(p+motion)){e.root.position=p+motion;return;}
            Vector3 side=new(-motion.z,0,motion.x);
            if(IsWalkable(p+side))e.root.position=p+side;
            else if(IsWalkable(p-side))e.root.position=p-side;
        }
        void UpdateRunDirector(float dt)
        {
            if(RunTimer>=CombatRules.RunDuration){RunTimer=CombatRules.RunDuration;FinishRun(false,"The expedition ran out of time");return;}
            if(RunTimer>=CombatRules.BossTime&&!bossSpawned)SpawnBoss();
            else if(RunTimer>=600&&!miniSpawned)
            {
                miniSpawned=true;
                var e=enemies[7];if(TrySpawnPosition(out Vector3 p)){SpawnEnemy(e,p,600+SelectedMap*200);e.visual.localScale=Vector3.one*2;Notify("CHAMPION AWAKENED  /  Keep your distance");}
            }
            UpdateHazards(dt);
        }
        void SpawnBoss()
        {
            bossSpawned=true;
            // A clear central arena prevents scenery and river geometry from trapping the finale.
            ClearCombat();Player.position=new(0,0,-2);walkingTo=false;invulnerableUntil=Time.time+2;
            boss=enemies[7];SpawnEnemy(boss,new(0,0,5),1800+SelectedMap*800);boss.boss=true;
            boss.visual.localScale=Vector3.one*2.6f;nextBossAttack=Time.time+2;bossReleaseAt=0;
            var boundary=new GameObject("Guardian arena boundary",typeof(LineRenderer));boundary.transform.SetParent(transform);
            arenaBoundary=boundary.GetComponent<LineRenderer>();arenaBoundary.sharedMaterial=Mat("arena amber",new(1.8f,.6f,.15f),true);arenaBoundary.widthMultiplier=.13f;arenaBoundary.positionCount=5;
            arenaBoundary.SetPositions(new[]{new Vector3(-4.8f,.1f,-7),new Vector3(4.8f,.1f,-7),new Vector3(4.8f,.1f,7),new Vector3(-4.8f,.1f,7),new Vector3(-4.8f,.1f,-7)});
            Health=Mathf.Min(MaxHealth,Health+Mathf.RoundToInt(MaxHealth*.25f));
            Notify(BossName+"  /  60 seconds. Dodge the warning rings.");PlayCue(1);
        }
        void UpdateBoss(Enemy e,float dt)
        {
            e.sprite.color=Time.time<e.flashUntil?new(1,.6f,.3f):new(1,.86f,.65f);
            if(bossReleaseAt>0&&Time.time>=bossReleaseAt)
            {
                bossReleaseAt=0;int count=SelectedMap==0?8:12;
                for(int i=0;i<count;i++)
                {
                    var direction=Quaternion.Euler(0,i*360f/count+(RunTimer*7)%45,0)*Vector3.forward;
                    var orb=Shape("Guardian seed",PrimitiveType.Sphere,e.root.position+Vector3.up*.5f,Vector3.one*.28f,Mat("danger",new(2,.35f,.12f),true),transform);
                    hostileShots.Add(new HostileShot{root=orb.transform,velocity=direction*(SelectedMap==1?4.5f:3.5f),expires=Time.time+6});
                }
            }
            if(Time.time>=nextBossAttack)
            {
                nextBossAttack=Time.time+(e.hp<e.maxHp*.5f?2.2f:3.2f);bossReleaseAt=Time.time+.9f;
                Pulse(e.root.position,2,new(1,.3f,.12f),.9f);
                AddHazard(Player.position,SelectedMap==0?1.6f:2f,1.2f,SelectedMap==1?3f:.3f,22+SelectedMap*5);
            }
        }
        void AddHazard(Vector3 point,float radius,float delay,float duration,int damage)
        {
            if(hazards.Count>=40||Phase!=RunPhase.Running)return;
            var go=new GameObject("Danger telegraph",typeof(LineRenderer));go.transform.SetParent(transform);
            var line=go.GetComponent<LineRenderer>();line.sharedMaterial=RingMaterial();line.startColor=line.endColor=new Color(1,.75f,.2f);line.widthMultiplier=.10f;line.positionCount=49;
            for(int i=0;i<49;i++){float a=i*Mathf.PI*2/48;line.SetPosition(i,point+new Vector3(Mathf.Cos(a)*radius,.06f,Mathf.Sin(a)*radius));}
            hazards.Add(new Hazard{point=point,radius=radius,activates=Time.time+delay,expires=Time.time+delay+duration,damage=damage,ring=line});
        }
        void UpdateHazards(float dt)
        {
            for(int i=hazards.Count-1;i>=0;i--)
            {
                var h=hazards[i];if(Time.time>h.expires){Destroy(h.ring.gameObject);hazards.RemoveAt(i);continue;}
                bool active=Time.time>=h.activates;h.ring.startColor=h.ring.endColor=active?new Color(1,.2f,.12f):new Color(1,.75f,.2f);
                if(active&&Time.time>=h.tick){h.tick=Time.time+.7f;if(Vector3.Distance(Player.position,h.point)<h.radius)HurtPlayer(h.damage);}
            }
            for(int i=hostileShots.Count-1;i>=0;i--)
            {
                var s=hostileShots[i];s.root.position+=s.velocity*dt;Vector3 delta=s.root.position-Player.position;delta.y=0;
                bool hit=delta.sqrMagnitude<.45f;
                if(hit)HurtPlayer(18+SelectedMap*5);
                if(hit||Time.time>s.expires){Destroy(s.root.gameObject);hostileShots.RemoveAt(i);}
            }
        }
        void ApplyMapPalette()
        {
            Color tint=SelectedMap==1?new(.64f,.65f,1):SelectedMap==2?new(1,.55f,.4f):Color.white;
            foreach(var pair in materials)
            {
                if(pair.Key.StartsWith("canopy")||pair.Key=="moss"||pair.Key=="river jade")
                {if(!originalColors.ContainsKey(pair.Value))originalColors[pair.Value]=pair.Value.color;pair.Value.color=originalColors[pair.Value]*tint;}
            }
            var volume=FindFirstObjectByType<Volume>();
            if(volume&&volume.profile.TryGet<ColorAdjustments>(out var grade))grade.colorFilter.Override(SelectedMap==1?new(.80f,.82f,1):SelectedMap==2?new(1,.77f,.67f):new(1,.96f,.89f));
            playerSprite.color=Color.white;
        }
        Material RingMaterial()
        {
            if(materials.TryGetValue("vertex rings",out var mat))return mat;
            mat=new Material(Shader.Find("Umbra/VertexColor"));materials.Add("vertex rings",mat);owned.Add(mat);return mat;
        }
        void PlayCue(int cue)
        {
            if(Muted||suppressSave)return;
            if(!audioSource)
            {
                audioSource=gameObject.AddComponent<AudioSource>();audioSource.playOnAwake=false;audioSource.volume=.12f;
                audioSource.ignoreListenerPause=true;
                cues=new AudioClip[3];
                for(int c=0;c<3;c++)
                {
                    if(c==2)
                    {
                        int sr=22050;
                        int n=(int)(sr*0.48f);
                        float[] data=new float[n];
                        float[] freqs={523.25f,659.25f,783.99f,1046.50f};
                        int noteStep=(int)(sr*0.08f);
                        for(int i=0;i<n;i++)
                        {
                            float sample=0f;
                            for(int note=0;note<freqs.Length;note++)
                            {
                                int startSample=note*noteStep;
                                if(i>=startSample)
                                {
                                    int noteIdx=i-startSample;
                                    float t=(float)noteIdx/sr;
                                    float dur=note==freqs.Length-1?(n-startSample):(sr*0.22f);
                                    float env=Mathf.Clamp01(1f-(float)noteIdx/dur);
                                    float wave=Mathf.Sin(t*Mathf.PI*2*freqs[note])*0.75f+Mathf.Sin(t*Mathf.PI*4*freqs[note])*0.25f;
                                    sample+=wave*env*env;
                                }
                            }
                            data[i]=Mathf.Clamp(sample*0.65f,-1f,1f);
                        }
                        cues[c]=AudioClip.Create("Umbra cue "+c,n,1,sr,false);cues[c].SetData(data,0);owned.Add(cues[c]);
                    }
                    else
                    {
                        int n=c==0?1500:6000;float[] data=new float[n];
                        for(int i=0;i<n;i++){float t=(float)i/22050;float envelope=(1f-(float)i/n);data[i]=Mathf.Sin(t*Mathf.PI*2*(c==0?640:140))*envelope*envelope;}
                        cues[c]=AudioClip.Create("Umbra cue "+c,n,1,22050,false);cues[c].SetData(data,0);owned.Add(cues[c]);
                    }
                }
            }
            audioSource.PlayOneShot(cues[cue]);
        }
    }
}
