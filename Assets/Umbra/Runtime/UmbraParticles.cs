using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Umbra
{
    public sealed partial class UmbraPrototype
    {
        const int CombatParticleCap=640, CelebrationParticleCap=96, AmbientParticleCap=80;
        const int ParticleFrameBudget=96;
        ParticleSystem combatParticles, celebrationParticles, ambientParticles;
        readonly System.Random particleRandom=new(7183); // Cosmetic randomness must not change loot, crits or drafts.
        int particleFrame=-1, particlesThisFrame;
        float ambientParticleBank, nextDashParticleAt;
        static readonly Color SpiritGold=new(1.5f,1.02f,.40f,.85f);
        static readonly Color SpiritSage=new(.60f,1.15f,.83f,.75f);
        static readonly Color SpiritIce=new(.65f,1.05f,1.55f,.85f);

        float ParticleRange(float min,float max)=>Mathf.Lerp(min,max,(float)particleRandom.NextDouble());
        Color RuneParticleColor(RuneKind rune)=>rune==RuneKind.Ember?new Color(1.65f,.62f,.20f,.8f):rune==RuneKind.Scatter?SpiritGold:SpiritSage;

        void BuildParticleEffects()
        {
            var shader=Resources.Load<Shader>("Umbra/SpiritParticles");
            if(!shader)throw new InvalidOperationException("Missing SpiritParticles shader");
            var material=new Material(shader){name="Shared spirit particles"};owned.Add(material);
            combatParticles=CreateParticlePool("Combat sparks",CombatParticleCap,material,false);
            celebrationParticles=CreateParticlePool("Level-up stardust",CelebrationParticleCap,material,true);
            ambientParticles=CreateParticlePool("Woodland fireflies",AmbientParticleCap,material,false);
        }

        ParticleSystem CreateParticlePool(string label,int cap,Material material,bool unscaled)
        {
            var go=new GameObject(label,typeof(ParticleSystem));go.transform.SetParent(transform,false);
            var ps=go.GetComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.useAutoRandomSeed=false;ps.randomSeed=(uint)cap;
            var main=ps.main;main.loop=true;main.playOnAwake=false;main.maxParticles=cap;
            main.simulationSpace=ParticleSystemSimulationSpace.World;main.useUnscaledTime=unscaled;
            main.startSpeed=0;main.startLifetime=1;main.startSize=.1f;
            main.cullingMode=ParticleSystemCullingMode.AlwaysSimulate;
            var emission=ps.emission;emission.enabled=false;
            var shape=ps.shape;shape.enabled=false;
            var colors=ps.colorOverLifetime;colors.enabled=true;
            var gradient=new Gradient();gradient.SetKeys(
                new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},
                new[]{new GradientAlphaKey(.3f,0),new GradientAlphaKey(1,.08f),new GradientAlphaKey(.6f,.45f),new GradientAlphaKey(0,1)});
            colors.color=gradient;
            var size=ps.sizeOverLifetime;size.enabled=true;
            size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.EaseInOut(0,1,1,.08f));
            var renderer=go.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=material;
            renderer.renderMode=ParticleSystemRenderMode.Billboard;renderer.maxParticleSize=.035f;
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            renderer.lightProbeUsage=LightProbeUsage.Off;renderer.reflectionProbeUsage=ReflectionProbeUsage.Off;
            ps.Play();return ps;
        }

        void EmitSpirit(Vector3 position,Color color,int count,float speed=1.8f,float lifetime=.5f,float size=.15f,
            float spread=.12f,bool rising=false,bool celebration=false)
        {
            var pool=celebration?celebrationParticles:combatParticles;if(!pool)return;
            if(particleFrame!=Time.frameCount){particleFrame=Time.frameCount;particlesThisFrame=0;}
            count=Mathf.Min(count,celebration?CelebrationParticleCap:ParticleFrameBudget-particlesThisFrame);
            if(!celebration)particlesThisFrame+=count;
            for(int i=0;i<count;i++)
            {
                float angle=ParticleRange(0,Mathf.PI*2),radius=ParticleRange(0,spread);
                var radial=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                var p=new ParticleSystem.EmitParams {
                    position=position+radial*radius,
                    velocity=radial*ParticleRange(speed*.25f,speed)+(Vector3.up*ParticleRange(rising?speed*.8f:.15f,rising?speed*1.6f:speed*.8f)),
                    startColor=color,startSize=ParticleRange(size*.6f,size*1.2f),
                    startLifetime=ParticleRange(lifetime*.7f,lifetime),rotation=ParticleRange(0,360)
                };
                if(rising)p.velocity=new Vector3(p.velocity.x*.25f,p.velocity.y,p.velocity.z*.25f);
                pool.Emit(p,1);
            }
        }

        void UpdateParticleEffects(float dt)
        {
            if(!Player||!ambientParticles||!CanAct)return;
            ambientParticleBank=Mathf.Min(ambientParticleBank+dt*7,4);
            while(ambientParticleBank>=1)
            {
                ambientParticleBank--;
                ambientParticles.Emit(new ParticleSystem.EmitParams {
                    position=Player.position+new Vector3(ParticleRange(-12,12),ParticleRange(.2f,3),ParticleRange(-8,12)),
                    velocity=new Vector3(.13f,.05f,.08f),startSize=ParticleRange(.035f,.075f),
                    startLifetime=ParticleRange(3,6),startColor=new Color(1,.83f,.47f,.38f)
                },1);
            }
            if(Time.time<dashUntil&&Time.time>=nextDashParticleAt)
            {
                nextDashParticleAt=Time.time+.025f;
                EmitSpirit(Player.position+Vector3.up*.2f,SpiritSage,5,.5f,.35f,.18f,.25f);
            }
        }

        void ClearParticleEffects()
        {
            if(combatParticles)combatParticles.Clear();
            if(celebrationParticles)celebrationParticles.Clear();
            if(ambientParticles)ambientParticles.Clear();
            ambientParticleBank=0;nextDashParticleAt=0;particlesThisFrame=0;particleFrame=-1;
        }

#if UNITY_EDITOR
        public System.Collections.IEnumerator PreviewParticles()
        {
            PreviewArtDirection(false);
            Health=MaxHealth-40;healAt=volleyAt=0;TryHeal();TryVolley();
            yield return new WaitForSeconds(.16f);
            ScreenCapture.CaptureScreenshot("Recordings/Particles-Skills.png");
            yield return new WaitForSeconds(1.2f);
            TriggerLevelUp();
            yield return new WaitForSecondsRealtime(.18f);
            ScreenCapture.CaptureScreenshot("Recordings/Particles-LevelUp.png");
        }
#endif

        void RunParticleTests(Action<bool,string> check)
        {
            ClearParticleEffects();
            check(combatParticles&&celebrationParticles&&ambientParticles,"particle pools initialized");
            check(combatParticles.main.maxParticles==640&&celebrationParticles.main.maxParticles==96&&ambientParticles.main.maxParticles==80,"particle capacity bounded at 816");
            check(!combatParticles.main.useUnscaledTime&&!ambientParticles.main.useUnscaledTime&&celebrationParticles.main.useUnscaledTime,"combat pauses while level-up celebration uses real time");
            check(combatParticles.GetComponent<ParticleSystemRenderer>().sharedMaterial.shader.isSupported,"spirit particle shader supported");
            var state=UnityEngine.Random.state;float expected=UnityEngine.Random.value;UnityEngine.Random.state=state;
            EmitSpirit(Player.position,SpiritGold,1000);
            check(UnityEngine.Random.value==expected,"cosmetic particles preserve gameplay randomness");UnityEngine.Random.state=state;
            check(combatParticles.particleCount==ParticleFrameBudget,"particle storm limited per frame");
            EmitSpirit(Player.position,SpiritGold,20);
            check(combatParticles.particleCount==ParticleFrameBudget,"repeated hits share particle budget");
            EmitSpirit(Player.position,SpiritGold,32,celebration:true);
            check(celebrationParticles.particleCount==32,"celebration remains visible when combat budget is exhausted");
            ClearCombat();
            check(combatParticles.particleCount==0&&celebrationParticles.particleCount==0&&ambientParticles.particleCount==0,"combat cleanup clears all particle pools");
        }
    }
}
