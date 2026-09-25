using System;
using System.Collections.Generic;
using UnityEngine;

namespace Umbra
{
    public sealed partial class UmbraPrototype
    {
#if UNITY_EDITOR
        public void PreviewGuardian()
        {
            suppressSave=true;EnterCamp();SelectedMap=0;StartRun();ApplyPerk(ActiveDraft[0]);
            RunTimer=CombatRules.BossTime;UpdateRunDirector(0);SyncPause();
        }
#endif
        // Invoked by the editor smoke-test menu. Save writes are disabled for the entire suite.
        void RunCampaignTests(Action<string> complete)
        {
            var checks=new List<string>();var randomState=UnityEngine.Random.state;
            void Check(bool ok,string name){if(!ok)throw new Exception(name);checks.Add(name);}
            suppressSave=true;
            try
            {
                foreach (var size in new[] { new Vector2(1600,900), new Vector2(2560,1080), new Vector2(1024,768), new Vector2(390,844), new Vector2(844,390) })
                {
                    var hud = new HudLayout(size.x, size.y);
                    Vector2 topLeft = hud.Matrix(0,0).MultiplyPoint3x4(new Vector2(26,26));
                    Vector2 bottomRight = hud.Matrix(1,1).MultiplyPoint3x4(new Vector2(1573,872));
                    Check(Vector2.Distance(topLeft, Vector2.one * (26 * hud.Scale)) < .01f &&
                        Mathf.Abs(size.x - bottomRight.x - 27 * hud.Scale) < .01f &&
                        Mathf.Abs(size.y - bottomRight.y - 28 * hud.Scale) < .01f, "HUD edge anchors " + size);
                    Vector2 center = hud.Matrix(.5f,.5f).MultiplyPoint3x4(new Vector2(800,450));
                    Check(Vector2.Distance(center, size / 2) < .01f, "HUD modal center " + size);
                    Vector2 skill = hud.Matrix(.5f,1).MultiplyPoint3x4(new Vector2(800,825));
                    Check(hud.OverHud(hud.FromScreen(new Vector2(skill.x, size.y - skill.y)), false) &&
                        !hud.OverHud(hud.FromScreen(size / 2), false), "HUD pointer alignment " + size);
                    Vector2 dodge = hud.Matrix(1,1).MultiplyPoint3x4(new Vector2(1430,730));
                    Check(Vector2.Distance(hud.FromScreen(new Vector2(dodge.x,size.y-dodge.y)),
                        hud.Point(new Vector2(1430,730),1,1)) < .01f, "HUD touch alignment " + size);
                }
                EnterCamp();BaseLevel=1;STR=AGI=VIT=INT=DEX=LUK=1;StatPoints=0;ClearedMaps=0;Shards=0;SupplyRank=0;Class=HeroClass.Novice;SelectedMap=0;Rune=RuneKind.Pierce;
                Check(!MapUnlocked(1)&&!ClassesUnlocked,"progression gates");
                Check(!TryAddStat(CombatRules.StatKind.STR),"no unearned stat points");
                BaseLevel=3;StatPoints=6;Check(TryAddStat(CombatRules.StatKind.VIT)&&VIT==2&&StatPoints==5,"camp allocation");
                ResetStats();Check(VIT==1&&StatPoints==6,"free respec refund");
                bool initMobile=IsMobile;ToggleMobileInput();Check(IsMobile!=initMobile,"mobile toggle switches state");ToggleMobileInput();Check(IsMobile==initMobile,"mobile toggle restores state");
                StartRun();Check(Drafting&&Time.timeScale==0&&RunTimer==0,"opening draft freezes timer");
                var offered=ActiveDraft[0];ApplyPerk(offered);int rank=Rank(offered.Kind);ApplyPerk(offered);
                Check(Rank(offered.Kind)==rank,"draft cannot be claimed twice");
                Check(!TryAddStat(CombatRules.StatKind.STR),"stats locked during expedition");
                StatsPanel=true;SyncPause();Check(Time.timeScale==0&&!TryAttack(Vector3.forward),"modal pauses combat");StatsPanel=false;SyncPause();
                Vector3 start=Player.position;MovePlayer(Vector3.right*.5f);Check(Vector3.Distance(start,Player.position)>.4f,"movement");
                Check(!IsWalkable(new(12,0,0)),"river collision");
                for(int i=0;i<100;i++)if(TrySpawnPosition(out var pos))Check(IsWalkable(pos)&&(pos-Player.position).sqrMagnitude>=64,"safe spawn "+i);
                ClearCombat();RunTimer=0;hordeCursor=0;nextHordeSpawn=0;UpdateHorde(0);
                Check(enemies.FindAll(enemy=>enemy.hp>0).Count==4,"opening horde batch");
                for(int wave=0;wave<20;wave++){nextHordeSpawn=0;UpdateHorde(0);}
                Check(enemies.FindAll(enemy=>enemy.hp>0).Count==18&&!enemies.Exists(enemy=>enemy.hp>0&&enemy.elite),"opening cap and elite gate");
                RunTimer=45;
                for(int wave=0;wave<20;wave++){nextHordeSpawn=0;UpdateHorde(0);}
                Check(enemies.Exists(enemy=>enemy.hp>0&&enemy.elite),"early elites enter rotating pool");
                RunTimer=120;UpdateRunDirector(0);
                Check(miniSpawned&&enemies[enemies.Count-1].hp==900,"two minute champion");
                ClearCombat();RunTimer=0;
                int beforeRunLevel=RunLevel;AwardRunExperience(130);
                Check(RunLevel==beforeRunLevel&&!Drafting,"thirteen common pickups do not trigger draft");
                AwardRunExperience(10);Check(RunLevel==beforeRunLevel+1&&Drafting,"fourteenth common pickup triggers draft");
                while(Drafting)ApplyPerk(ActiveDraft[0]);
                Health=MaxHealth;invulnerableUntil=0;HurtPlayer(CombatRules.EnemyDamage(false,0,0));
                Check(Health==MaxHealth-16,"opening common contact damage");
                Check(CombatRules.EnemyDamage(true,0,600)>30&&CombatRules.EnemyHealth(true,0,45)>240,"enemy pressure grows with time");
                var e=enemies[0];SpawnEnemy(e,Player.position+Vector3.forward*2,48);int xp=EarnedExperience;DamageEnemy(e,999);
                Check(Kills==1&&!e.root.gameObject.activeSelf&&EarnedExperience==xp,"no XP on monster death");
                Player.position=e.root.position;UpdateLoot(0);Check(RunShards==3&&Shards==0&&EarnedExperience>xp,"amber pickup grants XP and shards");
                int permanentLevel=BaseLevel;AwardRunExperience(200);Check(RunLevel>1&&BaseLevel==permanentLevel&&Drafting,"separate run progression");
                while(Drafting)ApplyPerk(ActiveDraft[0]);
                var e2=enemies[1];SpawnEnemy(e2,Player.position+Vector3.forward*1.5f,48);TriggerLevelUp();
                Check((e2.root.position-Player.position).magnitude>=3.49f,"level up safety knockback");
                while(Drafting)ApplyPerk(ActiveDraft[0]);
                foreach(var p in CombatRules.AllPerks)if(p.Kind!=CombatRules.PerkKind.BattleFocus)perkRanks[p.Kind]=RankCap(p.Kind);
                var capped=RollDraft();Check(capped.Count==1&&capped[0].Kind==CombatRules.PerkKind.BattleFocus,"exhausted draft fallback");
                Health=MaxHealth-40;healAt=0;Check(TryHeal()&&!TryHeal(),"mend cooldown");
                volleyAt=0;Check(TryVolley()&&!TryVolley(),"nova cooldown");
                Health=MaxHealth;invulnerableUntil=0;HurtPlayer(10);int hp=Health;HurtPlayer(10);Check(Health==hp,"hit grace prevents damage pileup");
                invulnerableUntil=0;Health=1;int bank=Shards;HurtPlayer(2);Check(Phase==RunPhase.Results&&!Won&&Health==0&&Shards==bank+RunShards,"real defeat banks collected rewards");
                bank=Shards;FinishRun(false,"duplicate");Check(Shards==bank,"reward settlement is idempotent");
                EnterCamp();ClearedMaps=7;
                for(int map=0;map<3;map++)
                {
                    SelectMap(map);StartRun();ApplyPerk(ActiveDraft[0]);RunTimer=839.9f;miniSpawned=true;UpdateRunDirector(0);Check(!BossActive,"boss not early "+map);
                    RunTimer=840;UpdateRunDirector(0);Check(BossActive&&FindNearestEnemy(Player.position,12)==boss,"guardian spawns "+map);
                    Check(!IsWalkable(new(8,0,0)),"boss arena boundary "+map);
                    Check(boss.hp==3200+map*1400,"guardian health budget "+map);
                    nextBossAttack=0;UpdateBoss(boss,0);
                    Check(hazards.Count==2&&hazards[0].activates>Time.time&&hazards[1].activates>Time.time,"guardian body and aimed telegraphs "+map);
                    Check(hostileShots.Count==0&&bossReleaseAt>Time.time,"guardian volley waits for warning "+map);
                    boss.hp=boss.maxHp/2-1;nextBossAttack=0;UpdateBoss(boss,0);
                    Check(nextBossAttack-Time.time<1.61f,"guardian enrage cadence "+map);
                    DamageEnemy(boss,100000);Check(Won&&Phase==RunPhase.Results&&RunShards==75*(map+1),"guardian victory and bounty "+map);EnterCamp();
                }
                SelectMap(0);StartRun();ApplyPerk(ActiveDraft[0]);RunTimer=900;UpdateRunDirector(0);Check(Phase==RunPhase.Results&&!Won,"15 minute hard limit");EnterCamp();
                Shards=60;SupplyRank=0;BuySupplies();Check(SupplyRank==1&&Shards==0,"camp shard sink");
                BaseLevel=10;
                foreach(HeroClass kind in Enum.GetValues(typeof(HeroClass)))
                {
                    SelectClass(kind);StartRun();ApplyPerk(ActiveDraft[0]);attackAt=0;Check(TryAttack(Player.position+Vector3.forward*3),"class attack "+kind);FinishRun(false,"test");EnterCamp();
                }
                Check(CombatRules.CritChance(999)<=.65f&&CombatRules.CooldownReduction(999)<=.5f,"stat caps");
                complete("PASS: "+checks.Count+" checks; "+string.Join(", ",checks.FindAll(x=>!x.StartsWith("safe spawn "))));
            }
            catch(Exception ex){complete("FAIL: "+ex);}
            finally
            {
                ClearCombat();perkRanks.Clear();MaxHealthBonus=0;LoadProfile();SelectedMap=0;EnterCamp();ApplyMapPalette();suppressSave=false;UnityEngine.Random.state=randomState;
            }
        }
    }
}
