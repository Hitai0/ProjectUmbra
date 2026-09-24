using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Random = UnityEngine.Random;

namespace Umbra
{
    public sealed class UmbraPrototype : MonoBehaviour
    {
        public const string Version = "0.1.0";
        public Camera WorldCamera { get; private set; }
        public Transform Player { get; private set; }
        public int Health { get; private set; } = CombatRules.MaxHealth;
        public int Level { get; private set; } = 1;
        public int Experience { get; private set; }
        public int Kills { get; private set; }
        public int Shards { get; private set; }
        public int Collected { get; private set; }
        public RuneKind Rune { get; private set; }
        public bool RunePanel;
        public bool HelpPanel;
        public bool Paused;
        public bool Ready { get; private set; }
        public string Notice { get; private set; } = "Follow the old road. The grove remembers.";
        public float NoticeUntil { get; private set; } = 8f;
        public float AttackRemaining => Mathf.Max(0, attackAt - Time.time);
        public float VolleyRemaining => Mathf.Max(0, volleyAt - Time.time);
        public float DashRemaining => Mathf.Max(0, dashAt - Time.time);
        public float HealRemaining => Mathf.Max(0, healAt - Time.time);
        public IReadOnlyList<Enemy> Enemies => enemies;

        public sealed class Enemy
        {
            public Transform root, visual;
            public SpriteRenderer sprite;
            public Vector3 home;
            public int hp, maxHp;
            public float attackAt, windupUntil, respawnAt, flashUntil, phase;
            public bool elite;
        }
        sealed class Shot
        {
            public Transform root;
            public Vector3 velocity;
            public float expiry;
            public int damage, remaining;
            public RuneKind rune;
            public HashSet<Enemy> hit = new();
        }
        sealed class Loot { public Transform root; public float phase; }
        sealed class FloatText { public Vector3 point; public string text; public Color color; public float until; }
        sealed class Ring { public LineRenderer line; public Vector3 center; public float born, lifetime, radius; public Color color; }

        readonly List<Enemy> enemies = new();
        readonly List<Shot> shots = new();
        readonly List<Loot> loot = new();
        readonly List<FloatText> floats = new();
        readonly List<Ring> rings = new();
        readonly List<Vector3> obstacles = new();
        readonly Dictionary<string, Material> materials = new();
        readonly List<UnityEngine.Object> owned = new();
        Transform world, actorVisual;
        Mesh crownMesh;
        SpriteRenderer playerSprite;
        Sprite[] rangerFrames;
        Sprite sproutSprite, mushroomSprite;
        Vector3 moveTarget, facing = Vector3.forward, dashDirection;
        bool walkingTo, questComplete;
        float attackAt, volleyAt, dashAt, healAt, invulnerableUntil, dashUntil, lastHitAt, elapsed;
        readonly Vector3 cameraOffset = new(0, 16f, -13.5f);
        readonly Color gold = new(1f, .72f, .27f);
        readonly Color mint = new(.46f, .91f, .72f);

        void Start()
        {
            Application.targetFrameRate = 60;
            Shards = PlayerPrefs.GetInt("Umbra.Shards", 0);
            Rune = (RuneKind)Mathf.Clamp(PlayerPrefs.GetInt("Umbra.Rune", 0), 0, 2);
            BuildWorld();
            gameObject.AddComponent<UmbraHud>().Game = this;
            Ready = true;
        }

        Material Mat(string key, Color color, bool unlit = false)
        {
            if (materials.TryGetValue(key, out Material existing)) return existing;
            var template=Resources.Load<Material>("Umbra/Materials/"+(unlit?"Unlit":"Lit"));
            var material = template ? new Material(template) : new Material(Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit"));
            material.name=key;material.color=color;
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .06f);
            material.enableInstancing = true;
            materials.Add(key, material);
            owned.Add(material);
            return material;
        }

        GameObject Shape(string label, PrimitiveType type, Vector3 position, Vector3 scale, Material material, Transform parent = null)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = label;
            go.transform.SetParent(parent == null ? world : parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            var collider = go.GetComponent<Collider>();
            if (collider) Destroy(collider);
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        void BuildWorld()
        {
            Random.InitState(24816);
            world = new GameObject("Amberfall • environment").transform;
            world.SetParent(transform);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.58f, .63f, .60f);
            RenderSettings.ambientEquatorColor = new Color(.42f, .45f, .31f);
            RenderSettings.ambientGroundColor = new Color(.22f, .26f, .20f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(.35f, .40f, .35f);
            RenderSettings.fogStartDistance = 34;
            RenderSettings.fogEndDistance = 72;
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(UniversalAdditionalCameraData));
            cameraObject.tag = "MainCamera";
            WorldCamera = cameraObject.GetComponent<Camera>();
            WorldCamera.orthographic = true;
            WorldCamera.orthographicSize = 9.3f;
            WorldCamera.nearClipPlane = .1f;
            WorldCamera.farClipPlane = 95;
            WorldCamera.backgroundColor = RenderSettings.fogColor;
            WorldCamera.clearFlags = CameraClearFlags.SolidColor;
            cameraObject.transform.position = cameraOffset;
            cameraObject.transform.rotation = Quaternion.Euler(49.8f, 0, 0);
            var cameraData = cameraObject.GetComponent<UniversalAdditionalCameraData>();
            cameraData.renderPostProcessing = true;
            cameraData.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
            var sunlight = new GameObject("Late afternoon sun", typeof(Light)).GetComponent<Light>();
            sunlight.type = LightType.Directional;
            sunlight.color = new Color(1, .81f, .53f);
            sunlight.intensity = 1.65f;
            sunlight.shadows = LightShadows.Soft;
            sunlight.shadowStrength = .65f;
            sunlight.transform.rotation = Quaternion.Euler(42, -42, 0);
            var volume = new GameObject("Amber atmosphere", typeof(Volume)).GetComponent<Volume>();
            volume.isGlobal = true;
            volume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
            owned.Add(volume.profile);
            var grading = volume.profile.Add<ColorAdjustments>();
            grading.postExposure.Override(.10f);
            grading.contrast.Override(12);
            grading.saturation.Override(5);
            var vignette = volume.profile.Add<Vignette>();
            vignette.intensity.Override(.23f);
            vignette.smoothness.Override(.65f);
            var bloom = volume.profile.Add<Bloom>();
            bloom.intensity.Override(.24f);
            bloom.threshold.Override(1.2f);
            crownMesh=MakeCrownMesh();
            var ground=Mat("moss",new(.8f,.8f,.8f));
            ground.mainTexture=Resources.Load<Texture2D>("Umbra/ground");ground.mainTextureScale=new Vector2(12,12);
            Shape("Forest floor", PrimitiveType.Cube, new(0, -.27f, 0), new(64, .5f, 64), ground);
            BuildGround();
            var bark = Mat("bark", new(.23f, .15f, .095f));
            Color[] leaves = { new(.52f, .30f, .10f), new(.73f, .40f, .10f), new(.83f, .52f, .16f), new(.38f, .40f, .16f), new(.27f, .35f, .19f) };
            for (int i = 0; i < 108; i++)
            {
                float x = Random.Range(-24f, 24f), z = Random.Range(-21f, 25f);
                if (Mathf.Abs(x - Mathf.Sin(z * .15f) * 2) < 5.7f && z < 14) continue;
                if (x > 9.4f && x < 14.6f) continue;
                if (Vector2.Distance(new(x, z), new(0, 9)) < 5) continue;
                Tree(new(x, 0, z), Random.Range(2.8f, 5.7f), bark, Mat("canopy" + i % 5, leaves[i % 5]));
            }
            for (int i = 0; i < 90; i++)
            {
                Vector3 p = new(Random.Range(-22f, 22f), .12f, Random.Range(-20f, 24f));
                if (Mathf.Abs(p.x) < 2.7f || (p.x > 10 && p.x < 14)) continue;
                float s = Random.Range(.25f, .9f);
                var rock = Shape("Moss stone", PrimitiveType.Sphere, p, new(s * 1.4f, s * .8f, s), Mat("stone" + i % 3, Color.Lerp(new(.28f,.32f,.28f), new(.46f,.47f,.34f), (i % 3) * .3f)));
                rock.transform.rotation = Random.rotation;
            }
            BuildRuins();
            BuildRiver();
            StaticBatchingUtility.Combine(world.gameObject);
            BuildMotes();
            BuildActors();
        }

        void BuildGround()
        {
            // Thousands of colored triangles in a few meshes rather than thousands of renderers.
            List<Vector3> verts = new(); List<int> tris = new(); List<Color> colors = new();
            Color[] palette = { new(.34f,.40f,.18f), new(.41f,.43f,.21f), new(.27f,.34f,.17f), new(.52f,.43f,.21f), new(.64f,.50f,.22f) };
            for (int i = 0; i < 7600; i++)
            {
                float x = Random.Range(-29f,29f), z = Random.Range(-28f,29f);
                float path = Mathf.Sin(z * .15f) * 2;
                bool onRoad = Mathf.Abs(x - path) < 1.5f;
                Color c = onRoad ? new Color(.42f,.35f,.23f) : palette[Random.Range(0,palette.Length)];
                float s = onRoad ? Random.Range(.035f,.09f) : Random.Range(.055f,.16f);
                int n = verts.Count;
                verts.Add(new(x-s,.008f,z-s)); verts.Add(new(x,.008f,z+s)); verts.Add(new(x+s,.008f,z-s));
                tris.Add(n); tris.Add(n+1); tris.Add(n+2);
                colors.Add(c); colors.Add(c); colors.Add(c);
                if (!onRoad && i % 2 == 0)
                {
                    n = verts.Count;
                    float h = Random.Range(.08f,.22f);
                    verts.Add(new(x-s*.2f,.012f,z)); verts.Add(new(x,h,z)); verts.Add(new(x+s*.2f,.012f,z));
                    tris.Add(n); tris.Add(n+1); tris.Add(n+2);
                    colors.Add(c*.82f); colors.Add(c*1.15f); colors.Add(c*.82f);
                }
            }
            var mesh = new Mesh { name = "Forest ground details", indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(verts); mesh.SetTriangles(tris,0); mesh.SetColors(colors); mesh.RecalculateNormals();
            owned.Add(mesh);
            var go = new GameObject("Grass and fallen leaves", typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(world);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var mat = new Material(Shader.Find("Umbra/VertexColor")); owned.Add(mat);
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            var dirt = Mat("old road", new(.40f,.34f,.22f));
            for (int z = -27; z < 26; z++)
                Shape("Old pilgrim road", PrimitiveType.Cube, new(Mathf.Sin(z*.15f)*2,-.014f,z),new(2.8f,.026f,1.2f),dirt);
        }

        void Tree(Vector3 p, float height, Material bark, Material foliage)
        {
            var root = new GameObject("Amber oak").transform; root.SetParent(world); root.position = p;
            Shape("Trunk",PrimitiveType.Cylinder,new(0,height*.35f,0),new(.36f,height*.35f,.36f),bark,root);
            for (int b = 0; b < 3; b++)
            {
                var branch = Shape("Branch",PrimitiveType.Cylinder,new((b-1)*.32f,height*.53f,0),new(.12f,height*.18f,.12f),bark,root);
                branch.transform.localRotation = Quaternion.Euler(0,0,(b-1)*-32);
            }
            for (int j = 0; j < 16; j++)
            {
                Vector3 offset = new(Random.Range(-1.05f,1.05f),height*.70f + Random.Range(-.5f,.8f),Random.Range(-.9f,.9f));
                float s = height * Random.Range(.20f,.31f);
                var crown = new GameObject("Foliage",typeof(MeshFilter),typeof(MeshRenderer));
                crown.transform.SetParent(root,false);crown.transform.localPosition=offset;crown.transform.localScale=new(s,s*.85f,s);
                crown.GetComponent<MeshFilter>().sharedMesh=crownMesh;crown.GetComponent<MeshRenderer>().sharedMaterial=foliage;
                crown.transform.localRotation = Random.rotation;
            }
            obstacles.Add(new(p.x,.48f,p.z));
        }

        Mesh MakeCrownMesh()
        {
            var vertices=new List<Vector3>();var triangles=new List<int>();
            Vector3 P(int lat,int lon){float a=Mathf.PI*lat/5;float b=Mathf.PI*2*lon/8;return new Vector3(Mathf.Sin(a)*Mathf.Cos(b),Mathf.Cos(a),Mathf.Sin(a)*Mathf.Sin(b))*.6f;}
            void Tri(Vector3 a,Vector3 b,Vector3 c){int n=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);triangles.Add(n);triangles.Add(n+1);triangles.Add(n+2);}
            for(int lat=0;lat<5;lat++)for(int lon=0;lon<8;lon++){Tri(P(lat,lon),P(lat,lon+1),P(lat+1,lon));Tri(P(lat,lon+1),P(lat+1,lon+1),P(lat+1,lon));}
            var mesh=new Mesh{name="Faceted leaf cluster"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();owned.Add(mesh);return mesh;
        }

        void BuildRuins()
        {
            var stone = Mat("weathered shrine",new(.48f,.48f,.37f));
            var dark = Mat("shrine recess",new(.19f,.25f,.23f));
            Shape("Shrine platform",PrimitiveType.Cylinder,new(0,.06f,10),new(5.6f,.1f,5.6f),stone);
            for (int side = -1; side <= 1; side += 2)
            {
                Shape("Gate foot",PrimitiveType.Cube,new(side*2.2f,.3f,10),new(1.3f,.6f,1.2f),stone);
                Shape("Gate pillar",PrimitiveType.Cube,new(side*2.2f,1.8f,10),new(.8f,3.1f,.8f),stone);
                Shape("Gate cap",PrimitiveType.Cube,new(side*2.2f,3.4f,10),new(1.2f,.35f,1.1f),stone);
                Shape("Inset",PrimitiveType.Cube,new(side*2.2f,1.9f,9.57f),new(.30f,1.4f,.04f),dark);
            }
            Shape("Ancient lintel",PrimitiveType.Cube,new(0,3.65f,10),new(5.4f,.6f,1),stone);
            Shape("Shrine altar",PrimitiveType.Cube,new(0,.5f,11),new(1.5f,1,1.2f),dark);
            var crystal = Shape("Heart of the grove",PrimitiveType.Cube,new(0,1.6f,11),new(.52f,.8f,.52f),Mat("shrine glow",new(.36f,1.5f,1.1f),true));
            crystal.transform.rotation = Quaternion.Euler(25,45,30);
            var point = new GameObject("Shrine light",typeof(Light)).GetComponent<Light>();
            point.transform.position = new(0,2,10); point.type = LightType.Point; point.range = 5; point.color = mint; point.intensity = 2;
            for (int i = 0; i < 14; i++)
            {
                float a = i * Mathf.PI * 2 / 14;
                Shape("Circle stones",PrimitiveType.Cube,new(Mathf.Sin(a)*3.2f,.14f,10+Mathf.Cos(a)*3.2f),new(.44f,.28f,.6f),stone).transform.rotation=Quaternion.Euler(0,a*Mathf.Rad2Deg,0);
            }
        }

        void BuildRiver()
        {
            var river = Mat("river jade",new(.18f,.42f,.40f));
            Shape("Quiet stream",PrimitiveType.Cube,new(12,-.015f,0),new(3.1f,.03f,60),river);
            var shine = Mat("water glints",new(.62f,.77f,.64f),true);
            for (int i=0;i<80;i++) Shape("Ripple",PrimitiveType.Cube,new(Random.Range(10.6f,13.4f),.016f,Random.Range(-28f,28f)),new(Random.Range(.1f,.55f),.01f,.026f),shine);
            var wood = Mat("bridge planks",new(.37f,.25f,.15f));
            for (int i=0;i<13;i++) Shape("Footbridge plank",PrimitiveType.Cube,new(9.7f+i*.37f,.12f,3),new(.34f,.2f,2.2f),wood);
        }

        void BuildMotes()
        {
            var go = new GameObject("Windblown amber motes",typeof(ParticleSystem));
            go.transform.position = new(0,4,0);
            var ps = go.GetComponent<ParticleSystem>(); ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=ps.main; main.startLifetime=12; main.startSpeed=.1f; main.startSize=.045f; main.startColor=new Color(1,.76f,.32f,.7f); main.maxParticles=180; main.simulationSpace=ParticleSystemSimulationSpace.World;
            var emission=ps.emission; emission.rateOverTime=12;
            var shape=ps.shape; shape.shapeType=ParticleSystemShapeType.Box; shape.scale=new(28,6,24);
            var velocity=ps.velocityOverLifetime; velocity.enabled=true; velocity.x=.15f; velocity.y=-.10f; velocity.z=.06f;
            var renderer=go.GetComponent<ParticleSystemRenderer>(); renderer.sharedMaterial=Mat("mote light",new Color(1,.78f,.35f),true);
            ps.Play();
        }

        Sprite LoadSprite(string name)
        {
            var tex=Resources.Load<Texture2D>("Umbra/"+name);
            if (!tex) throw new InvalidOperationException("Missing generated art: "+name);
            var sprite=Sprite.Create(tex,new Rect(0,0,tex.width,tex.height),new Vector2(.5f,.05f),24);
            owned.Add(sprite); return sprite;
        }
        SpriteRenderer SpriteActor(Transform root, Sprite sprite, float scale=1)
        {
            var visual=new GameObject("Pixel sprite",typeof(SpriteRenderer)); visual.transform.SetParent(root,false);
            visual.transform.localScale=Vector3.one*scale;
            visual.transform.rotation=WorldCamera.transform.rotation;
            var sr=visual.GetComponent<SpriteRenderer>(); sr.sprite=sprite;
            sr.sharedMaterial=Mat("sprite unlit",Color.white,true);
            // Sprite shader preserves alpha and vertex tint on the URP 3D renderer.
            if (sr.sharedMaterial.shader.name != "Universal Render Pipeline/2D/Sprite-Unlit-Default")
            {
                var shader=Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                if(shader) sr.sharedMaterial.shader=shader;
            }
            return sr;
        }
        void Shadow(Transform root,float size)
        {
            Shape("Ground contact shadow",PrimitiveType.Cylinder,new(0,.018f,0),new(size,.009f,size*.65f),Mat("shadow",new(.13f,.18f,.12f)),root);
        }
        void BuildActors()
        {
            rangerFrames=new[]{LoadSprite("ranger_idle"),LoadSprite("ranger_step1"),LoadSprite("ranger_step2")};
            sproutSprite=LoadSprite("sprout"); mushroomSprite=LoadSprite("mushroom");
            Player=new GameObject("Wanderer • player").transform; Player.SetParent(transform); Player.position=new(0,0,-2);
            Shadow(Player,.75f); playerSprite=SpriteActor(Player,rangerFrames[0]); actorVisual=playerSprite.transform;
            Vector3[] homes={new(3,0,2),new(-3,0,3),new(4,0,6),new(-3,0,8),new(1,0,13),new(6,0,-3),new(-5,0,-5),new(5,0,12)};
            for(int i=0;i<homes.Length;i++)
            {
                var enemy=new Enemy{home=homes[i],maxHp=i==7?180:65,elite=i==7,phase=Random.value*6.28f}; enemy.hp=enemy.maxHp;
                enemy.root=new GameObject(enemy.elite?"Elder bloom":"Grove sprout").transform; enemy.root.SetParent(transform); enemy.root.position=enemy.home;
                Shadow(enemy.root,enemy.elite?1.1f:.6f);
                enemy.sprite=SpriteActor(enemy.root,i%3==0?mushroomSprite:sproutSprite,enemy.elite?1.5f:1);
                enemy.visual=enemy.sprite.transform; enemies.Add(enemy);
            }
        }

        void Update()
        {
            if(!Ready) return;
            var keyboard=Keyboard.current;
            if(keyboard!=null)
            {
                if(keyboard.escapeKey.wasPressedThisFrame){Paused=!Paused; Time.timeScale=Paused?0:1;}
                if(keyboard.tabKey.wasPressedThisFrame) RunePanel=!RunePanel;
                if(keyboard.hKey.wasPressedThisFrame) HelpPanel=!HelpPanel;
            }
            if(Paused) return;
            float dt=Time.deltaTime; elapsed+=dt;
            Vector2 input=Vector2.zero;
            if(keyboard!=null)
            {
                input.x=(keyboard.dKey.isPressed||keyboard.rightArrowKey.isPressed?1:0)-(keyboard.aKey.isPressed||keyboard.leftArrowKey.isPressed?1:0);
                input.y=(keyboard.wKey.isPressed||keyboard.upArrowKey.isPressed?1:0)-(keyboard.sKey.isPressed||keyboard.downArrowKey.isPressed?1:0);
                if(keyboard.rKey.wasPressedThisFrame) SetRune((RuneKind)(((int)Rune+1)%3));
                if(keyboard.digit1Key.wasPressedThisFrame) SetRune(RuneKind.Pierce);
                if(keyboard.digit2Key.wasPressedThisFrame) SetRune(RuneKind.Scatter);
                if(keyboard.digit3Key.wasPressedThisFrame) SetRune(RuneKind.Ember);
                if(keyboard.spaceKey.wasPressedThisFrame) TryDash();
                if(keyboard.qKey.wasPressedThisFrame) TryVolley();
                if(keyboard.eKey.wasPressedThisFrame) TryHeal();
            }
            var mouse=Mouse.current;
            if(mouse!=null&&!PointerOverHud())
            {
                if(mouse.rightButton.wasPressedThisFrame){moveTarget=MousePoint();walkingTo=true;}
                if(mouse.leftButton.isPressed) TryAttack(MousePoint());
            }
            Vector3 direction=new(input.x,0,input.y);
            if(input.sqrMagnitude>0) walkingTo=false;
            else if(walkingTo)
            {
                direction=moveTarget-Player.position; direction.y=0;
                if(direction.sqrMagnitude<.08f){walkingTo=false;direction=Vector3.zero;}
            }
            if(direction.sqrMagnitude>0){direction.Normalize();facing=direction;}
            bool moving=direction.sqrMagnitude>.01f;
            if(Time.time<dashUntil){direction=dashDirection;moving=true;}
            MovePlayer(direction*(Time.time<dashUntil?15:4.2f)*dt);
            if(moving) playerSprite.flipX=facing.x<-.05f;
            playerSprite.sprite=moving?rangerFrames[1+(int)(elapsed*9)%2]:rangerFrames[0];
            actorVisual.localPosition=new(0,moving?Mathf.Abs(Mathf.Sin(elapsed*12))*.05f:0,0);
            playerSprite.color=Time.time<invulnerableUntil?new Color(.62f,1,1):Color.white;
            UpdateEnemies(dt); UpdateShots(dt); UpdateLoot(dt); UpdateRings();
            if(Time.time-lastHitAt>6&&Health<CombatRules.MaxHealth) Health=Mathf.Min(CombatRules.MaxHealth,Health+(Mathf.FloorToInt(elapsed*2)!=Mathf.FloorToInt((elapsed-dt)*2)?1:0));
            if(!questComplete&&Kills>=6){questComplete=true;Shards+=25;SaveCollection();Notify("GROVE RESTORED  /  +25 amber shards");Pulse(Player.position,4,mint,.8f);}
        }

        void LateUpdate()
        {
            if(!Ready)return;
            WorldCamera.transform.position=Vector3.Lerp(WorldCamera.transform.position,Player.position+cameraOffset,1-Mathf.Exp(-6*Time.deltaTime));
            if(Mouse.current!=null&&!PointerOverHud())WorldCamera.orthographicSize=Mathf.Clamp(WorldCamera.orthographicSize-Mouse.current.scroll.ReadValue().y*.003f,7.5f,12f);
        }
        bool PointerOverHud()
        {
            if(RunePanel||HelpPanel||Paused)return true;
            if(Mouse.current==null)return false;
            Vector2 p=Mouse.current.position.ReadValue();
            return p.y<Screen.height*.16f || p.y>Screen.height*.85f || (p.x>Screen.width*.80f&&p.y>Screen.height*.49f);
        }
        Vector3 MousePoint()
        {
            Ray ray=WorldCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            return new Plane(Vector3.up,Vector3.zero).Raycast(ray,out float t)?ray.GetPoint(t):Player.position+facing*6;
        }
        public bool IsWalkable(Vector3 p)
        {
            if(Mathf.Abs(p.x)>22||p.z< -20||p.z>22)return false;
            if(p.x>10.3f&&p.x<13.7f&&Mathf.Abs(p.z-3)>1)return false;
            foreach(var o in obstacles) if(new Vector2(p.x-o.x,p.z-o.z).sqrMagnitude<.64f)return false;
            if(Mathf.Abs(p.z-10)<.65f&&Mathf.Abs(Mathf.Abs(p.x)-2.2f)<.8f)return false;
            return true;
        }
        public void MovePlayer(Vector3 delta)
        {
            Vector3 p=Player.position;
            Vector3 x=p+new Vector3(delta.x,0,0); if(IsWalkable(x))p=x;
            Vector3 z=p+new Vector3(0,0,delta.z); if(IsWalkable(z))p=z;
            Player.position=p;
        }
        public void SetRune(RuneKind rune){Rune=rune;SaveCollection();Notify(CombatRules.RuneName(rune)+" equipped");}
        public bool TryAttack(Vector3 point)
        {
            if(Paused||Time.time<attackAt)return false;
            attackAt=Time.time+CombatRules.AttackCooldown;
            Vector3 dir=point-Player.position;dir.y=0;if(dir.sqrMagnitude<.01f)dir=facing;dir.Normalize();
            facing=dir;playerSprite.flipX=dir.x<0;
            int count=CombatRules.ProjectileCount(Rune);
            for(int i=0;i<count;i++) Fire(Quaternion.Euler(0,(i-(count-1)*.5f)*13,0)*dir,Rune,CombatRules.Damage(Rune,Level));
            return true;
        }
        void Fire(Vector3 direction,RuneKind rune,int damage)
        {
            var shot=new Shot{velocity=direction*16,damage=damage,rune=rune,remaining=rune==RuneKind.Pierce?3:1,expiry=Time.time+1.1f};
            Color c=rune==RuneKind.Ember?new(1.8f,.55f,.12f):new(.6f,1.6f,1.2f);
            var go=Shape("Spirit arrow",PrimitiveType.Cube,Player.position+Vector3.up*.6f,new(.055f,.055f,.8f),Mat("arrow"+rune,c,true),transform);
            go.transform.rotation=Quaternion.LookRotation(direction);
            shot.root=go.transform; shots.Add(shot);
        }
        public bool TryVolley()
        {
            if(Paused||Time.time<volleyAt)return false;volleyAt=Time.time+CombatRules.VolleyCooldown;
            for(int i=0;i<12;i++)Fire(Quaternion.Euler(0,i*30,0)*Vector3.forward,Rune,CombatRules.Damage(Rune,Level));
            Pulse(Player.position,3.8f,mint,.5f);Notify("WIND NOVA");return true;
        }
        public bool TryDash()
        {
            if(Paused||Time.time<dashAt)return false;dashAt=Time.time+CombatRules.DashCooldown;
            dashUntil=Time.time+.18f;invulnerableUntil=Time.time+.28f;dashDirection=facing;
            Pulse(Player.position,1,mint,.28f);return true;
        }
        public bool TryHeal()
        {
            if(Paused||Time.time<healAt||Health>=CombatRules.MaxHealth)return false;
            healAt=Time.time+CombatRules.HealCooldown;Health=Mathf.Min(CombatRules.MaxHealth,Health+55);
            Popup(Player.position+Vector3.up,"+55",mint);Pulse(Player.position,2,mint,.6f);return true;
        }
        void UpdateEnemies(float dt)
        {
            foreach(var e in enemies)
            {
                if(e.hp<=0){if(Time.time>e.respawnAt){e.hp=e.maxHp;e.root.position=e.home;e.root.gameObject.SetActive(true);}else continue;}
                Vector3 delta=Player.position-e.root.position;float distance=delta.magnitude;
                Vector3 goal=distance<6.5f?Player.position:e.home+new Vector3(Mathf.Sin(elapsed*.35f+e.phase),0,Mathf.Cos(elapsed*.35f+e.phase));
                if(e.windupUntil>0)
                {
                    if(Time.time>=e.windupUntil){if(distance<1.35f)HurtPlayer(e.elite?24:12);e.windupUntil=0;e.attackAt=Time.time+1.5f;}
                }
                else if(distance<1.05f&&Time.time>=e.attackAt){e.windupUntil=Time.time+.7f;Pulse(e.root.position,1.3f,new(1,.32f,.2f),.7f);}
                else if(distance>1.0f)
                {
                    Vector3 next=Vector3.MoveTowards(e.root.position,goal,dt*(distance<6.5f?1.25f:.4f));
                    if(IsWalkable(next))e.root.position=next;
                }
                e.visual.localPosition=new(0,Mathf.Abs(Mathf.Sin(elapsed*4+e.phase))*.10f,0);
                e.sprite.flipX=delta.x<0;
                e.sprite.color=Time.time<e.flashUntil?new(1,.52f,.36f):(e.windupUntil>0?new(1,.68f,.5f):Color.white);
            }
        }
        void UpdateShots(float dt)
        {
            for(int i=shots.Count-1;i>=0;i--)
            {
                Shot s=shots[i];Vector3 before=s.root.position;Vector3 after=before+s.velocity*dt;s.root.position=after;
                foreach(var e in enemies)
                {
                    if(e.hp<=0||s.hit.Contains(e))continue;
                    Vector3 p=e.root.position+Vector3.up*.6f;
                    Vector3 segment=after-before;
                    float t=Mathf.Clamp01(Vector3.Dot(p-before,segment)/Mathf.Max(.0001f,segment.sqrMagnitude));
                    if((before+segment*t-p).sqrMagnitude>(e.elite?.75f:.4f))continue;
                    s.hit.Add(e);DamageEnemy(e,s.damage);s.remaining--;
                    if(s.rune==RuneKind.Ember){Pulse(e.root.position,1.8f,new(1,.5f,.15f),.45f);foreach(var other in enemies)if(other!=e&&other.hp>0&&Vector3.Distance(other.root.position,e.root.position)<1.8f)DamageEnemy(other,s.damage/2);}
                    if(s.remaining<=0)break;
                }
                if(s.remaining<=0||Time.time>s.expiry){Destroy(s.root.gameObject);shots.RemoveAt(i);}
            }
        }
        public void DamageEnemy(Enemy e,int damage)
        {
            if(e.hp<=0)return;e.hp-=damage;e.flashUntil=Time.time+.12f;
            Popup(e.root.position+Vector3.up*1.2f,damage.ToString(),gold);
            if(e.hp>0)return;
            e.root.gameObject.SetActive(false);e.respawnAt=Time.time+18;e.windupUntil=0;Kills++;
            Experience+=e.elite?50:20;
            if(Experience>=CombatRules.ExperienceToLevel(Level)){Experience-=CombatRules.ExperienceToLevel(Level);Level++;Health=CombatRules.MaxHealth;Notify("LEVEL "+Level+"  /  Health restored");Pulse(Player.position,4,gold,.8f);}
            var item=Shape("Amber shard",PrimitiveType.Cube,e.root.position+Vector3.up*.35f,Vector3.one*.22f,Mat("loot amber",new(1.6f,.8f,.19f),true),transform);
            loot.Add(new Loot{root=item.transform,phase=Random.value*5});
        }
        public void HurtPlayer(int damage)
        {
            if(Time.time<invulnerableUntil)return;Health=Mathf.Max(0,Health-damage);lastHitAt=Time.time;
            Popup(Player.position+Vector3.up*1.6f,"-"+damage,new(1,.4f,.3f));
            if(Health==0){Player.position=new(0,0,-2);Health=CombatRules.MaxHealth;invulnerableUntil=Time.time+3;walkingTo=false;Notify("The grove returns you to the trail. No shards lost.");foreach(var e in enemies){e.root.position=e.home;e.windupUntil=0;}}
        }
        void UpdateLoot(float dt)
        {
            for(int i=loot.Count-1;i>=0;i--)
            {
                var l=loot[i];l.root.Rotate(0,80*dt,0);var p=l.root.position;p.y=.4f+Mathf.Sin(elapsed*3+l.phase)*.10f;l.root.position=p;
                if(Vector2.Distance(new(p.x,p.z),new(Player.position.x,Player.position.z))<1.3f){Shards+=3;Collected++;SaveCollection();Popup(Player.position+Vector3.up,"+3 amber",gold);Destroy(l.root.gameObject);loot.RemoveAt(i);}
            }
        }
        public void Notify(string message){Notice=message;NoticeUntil=Time.time+4;}
        void Popup(Vector3 p,string message,Color color){floats.Add(new FloatText{point=p,text=message,color=color,until=Time.time+1.1f});}
        public void DrawWorldLabels(GUIStyle style)
        {
            foreach(var e in enemies)
            {
                if(e.hp<=0||e.hp==e.maxHp)continue;
                Vector3 p=WorldCamera.WorldToScreenPoint(e.root.position+Vector3.up*(e.elite?2:1.45f));if(p.z<0)continue;
                GUI.color=new(.07f,.12f,.1f,.9f);GUI.DrawTexture(new Rect(p.x-27,Screen.height-p.y,54,5),Texture2D.whiteTexture);
                GUI.color=e.elite?gold:mint;GUI.DrawTexture(new Rect(p.x-26,Screen.height-p.y+1,52*Mathf.Clamp01((float)e.hp/e.maxHp),3),Texture2D.whiteTexture);
            }
            for(int i=floats.Count-1;i>=0;i--)
            {
                var f=floats[i];if(Time.time>f.until){floats.RemoveAt(i);continue;}
                Vector3 p=WorldCamera.WorldToScreenPoint(f.point+Vector3.up*(1.1f-(f.until-Time.time))*.7f);
                GUI.color=f.color;GUI.Label(new Rect(p.x-65,Screen.height-p.y,130,28),f.text,style);
            }
            GUI.color=Color.white;
        }
        void Pulse(Vector3 center,float radius,Color color,float lifetime)
        {
            var go=new GameObject("Skill ring",typeof(LineRenderer));go.transform.SetParent(transform);
            var line=go.GetComponent<LineRenderer>();line.sharedMaterial=Mat("ring",Color.white,true);line.positionCount=49;line.widthMultiplier=.035f;line.loop=false;
            rings.Add(new Ring{line=line,center=center,born=Time.time,lifetime=lifetime,radius=radius,color=color});
        }
        void UpdateRings()
        {
            for(int i=rings.Count-1;i>=0;i--)
            {
                var r=rings[i];float t=(Time.time-r.born)/r.lifetime;
                if(t>=1){Destroy(r.line.gameObject);rings.RemoveAt(i);continue;}
                float radius=r.radius*Mathf.Lerp(.45f,1,t);
                r.line.startColor=r.line.endColor=r.color*(1-t*.7f);
                for(int j=0;j<49;j++){float a=j*Mathf.PI*2/48;r.line.SetPosition(j,r.center+new Vector3(Mathf.Sin(a)*radius,.045f,Mathf.Cos(a)*radius));}
            }
        }
        void SaveCollection(){PlayerPrefs.SetInt("Umbra.Shards",Shards);PlayerPrefs.SetInt("Umbra.Rune",(int)Rune);PlayerPrefs.Save();}
        void OnDestroy(){Time.timeScale=1;foreach(var o in owned)if(o)Destroy(o);}

        public IEnumerator SmokeTest(Action<string> complete)
        {
            yield return null;
            Health=CombatRules.MaxHealth;healAt=volleyAt=0;invulnerableUntil=0;
            int originalShards=Shards;
            List<string> checks=new();
            Vector3 start=Player.position;MovePlayer(Vector3.right*.5f);
            if(Vector3.Distance(start,Player.position)<.4f)throw new Exception("Movement failed");checks.Add("movement");
            if(IsWalkable(new Vector3(12,0,0)))throw new Exception("River collision failed");checks.Add("river collision");
            foreach(RuneKind kind in Enum.GetValues(typeof(RuneKind)))if(CombatRules.ProjectileCount(kind)<1)throw new Exception("Rune invalid");checks.Add("three rune definitions");
            var e=enemies[0];int before=e.hp;DamageEnemy(e,10);if(e.hp!=before-10)throw new Exception("Damage failed");checks.Add("enemy damage");
            int kills=Kills;DamageEnemy(e,999);if(Kills!=kills+1||e.root.gameObject.activeSelf)throw new Exception("Death failed");checks.Add("death, XP and drop");
            Player.position=e.root.position;int shards=Shards;UpdateLoot(0);if(Shards!=shards+3)throw new Exception("Pickup failed");checks.Add("pickup and persistence");
            Health=CombatRules.MaxHealth;invulnerableUntil=0;HurtPlayer(30);if(!TryHeal()||Health!=CombatRules.MaxHealth)throw new Exception("Heal failed");checks.Add("healing");
            if(!TryVolley()||TryVolley())throw new Exception("Cooldown failed");checks.Add("cooldown enforcement");
            Health=1;invulnerableUntil=0;HurtPlayer(2);if(Health!=CombatRules.MaxHealth)throw new Exception("Respawn failed");checks.Add("player respawn");
            Player.position=start;Shards=originalShards;SaveCollection();complete("PASS: "+string.Join(", ",checks));
        }
    }
}
