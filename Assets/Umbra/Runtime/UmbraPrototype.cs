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
    public sealed partial class UmbraPrototype : MonoBehaviour
    {
        public const string Version = "0.2.4";
        public Camera WorldCamera { get; private set; }
        public Transform Player { get; private set; }
        public int MaxHealthBonus { get; private set; }
        public int MaxHealth => CombatRules.MaxHealth + CombatRules.BonusHealthFromVit(VIT) + MaxHealthBonus + SupplyRank*5;
        public int Health { get; private set; } = CombatRules.MaxHealth;
        public int BaseLevel { get; private set; } = 1;
        public int BaseExperience { get; private set; }
        public int Level => BaseLevel;
        public int Experience => BaseExperience;
        public int StatPoints { get; private set; }
        public int STR { get; private set; } = 1;
        public int AGI { get; private set; } = 1;
        public int VIT { get; private set; } = 1;
        public int INT { get; private set; } = 1;
        public int DEX { get; private set; } = 1;
        public int LUK { get; private set; } = 1;
        public int Kills { get; private set; }
        public int Shards { get; private set; }
        public int Collected { get; private set; }
        public RuneKind Rune { get; private set; }
        public float AttackSpeedMultiplier { get; private set; } = 1.0f;
        public float MoveSpeedMultiplier { get; private set; } = 1.0f;
        public int BonusProjectiles { get; private set; }
        public float PickupRadius { get; private set; } = 5.0f;
        public bool HasNovaPulse { get; private set; }
        public bool AutoAim { get; set; } = true;
        public bool IsMobile { get; set; }
        public Vector2 MobileMoveInput { get; private set; }
        public bool IsJoystickActive { get; private set; }
        public Vector2 JoystickOrigin { get; private set; }
        public Vector2 JoystickCurrent { get; private set; }
        int joystickFingerId = -1;
        public bool Drafting { get; private set; }
        public float DraftOpenedAt { get; private set; }
        public List<CombatRules.Perk> ActiveDraft { get; private set; } = new();
        public float RunTimer { get; private set; }
        public bool RunePanel;
        public bool HelpPanel;
        public bool StatsPanel;
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
            public int contactDamage;
            public float moveSpeed;
            public float attackAt, windupUntil, respawnAt, flashUntil, phase;
            public bool elite, boss;
            public float burnUntil, burnTick; public int burnDamage;
            public float nextContactAt;
            public float contactRadius;
            public ulong damageSourceId;
            public uint spawnGeneration;
            public bool isChampion;
        }
        sealed class Shot
        {
            public Transform root;
            public Vector3 velocity;
            public float expiry;
            public int damage, remaining;
            public RuneKind rune;
            public bool isCrit;
            public HashSet<Enemy> hit = new();
        }
        sealed class Loot { public Transform root; public float phase; public int shards, exp; }
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
        GameObject activePillar;
        Light activePillarLight;
        DepthOfField dof;
        Mesh crownMesh;
        SpriteRenderer playerSprite;
        Sprite[] rangerFrames;
        Sprite sproutSprite, mushroomSprite;
        Vector3 moveTarget, facing = Vector3.forward, dashDirection;
        bool walkingTo;
        float attackAt, volleyAt, dashAt, healAt, dashUntil, lastHitAt, elapsed;
        internal const int GroundPointerLayer = 8, BlockerPointerLayer = 9;
        // UI is handled before physics; triggers and Ignore Raycast are not world click blockers.
        [SerializeField] LayerMask pointerMask = Physics.DefaultRaycastLayers & ~(1 << 5);
        bool pointerCapturedByUi;
        int hordeCursor;
        float nextHordeSpawn = 2.5f, nextNovaPulse;
        readonly Vector3 cameraOffset = new(0, 18.5f, -15.5f);
        public const float MinZoomFov = 24f;
        public const float MaxZoomFov = 46f;
        public const float DefaultZoomFov = 34f;
        public const float ZoomStep = 2.2f;
        public float TargetFov { get; private set; } = DefaultZoomFov;

        public static float NormalizeScrollDelta(float scroll)
        {
            if (Mathf.Abs(scroll) < 0.001f) return 0f;
            if (Mathf.Abs(scroll) >= 30f) return scroll / 120f;
            if (Mathf.Abs(scroll) >= 2.5f && Mathf.Abs(scroll) <= 3.5f) return scroll / 3f;
            return scroll;
        }

        public void ApplyZoomScroll(float rawScroll)
        {
            float deltaSteps = NormalizeScrollDelta(rawScroll);
            if (Mathf.Abs(deltaSteps) > 0.001f)
            {
                deltaSteps = Mathf.Clamp(deltaSteps, -3f, 3f);
                TargetFov = Mathf.Clamp(TargetFov - deltaSteps * ZoomStep, MinZoomFov, MaxZoomFov);
            }
        }

        public void ResetZoom()
        {
            TargetFov = DefaultZoomFov;
        }

        public void SetZoom(float fov)
        {
            TargetFov = Mathf.Clamp(fov, MinZoomFov, MaxZoomFov);
            if (WorldCamera != null) WorldCamera.fieldOfView = TargetFov;
        }

        readonly Color gold = new(1f, .72f, .27f);
        readonly Color mint = new(.46f, .91f, .72f);

        public bool TryAddStat(CombatRules.StatKind stat)
        {
            if (Phase != RunPhase.Camp || StatPoints <= 0 || GetStat(stat) >= 99 || !Enum.IsDefined(typeof(CombatRules.StatKind), stat)) return false;
            StatPoints--;
            switch (stat)
            {
                case CombatRules.StatKind.STR: STR++; break;
                case CombatRules.StatKind.AGI: AGI++; break;
                case CombatRules.StatKind.VIT: VIT++; Health = Mathf.Min(MaxHealth, Health + 8); break;
                case CombatRules.StatKind.INT: INT++; break;
                case CombatRules.StatKind.DEX: DEX++; break;
                case CombatRules.StatKind.LUK: LUK++; break;
            }
            SaveProfile();
            Notify(Loc.StatName(stat) + Loc.T(" increased to ", " เพิ่มขึ้นเป็น ") + GetStat(stat));
            return true;
        }

        public void ResetStats()
        {
            if (Phase != RunPhase.Camp) return;
            int spent = (STR - 1) + (AGI - 1) + (VIT - 1) + (INT - 1) + (DEX - 1) + (LUK - 1);
            if (spent <= 0) return;
            StatPoints += spent;
            STR = AGI = VIT = INT = DEX = LUK = 1;
            Health = Mathf.Min(MaxHealth, Health);
            SaveProfile();
            Notify(Loc.T("Stats reset! ", "รีเซ็ตสเตตัสแล้ว! ได้รับคืน ") + StatPoints + Loc.T(" points refunded.", " แต้ม"));
        }

        public int GetStat(CombatRules.StatKind stat) => stat switch
        {
            CombatRules.StatKind.STR => STR,
            CombatRules.StatKind.AGI => AGI,
            CombatRules.StatKind.VIT => VIT,
            CombatRules.StatKind.INT => INT,
            CombatRules.StatKind.DEX => DEX,
            _ => LUK
        };

        public void LoadProfile()
        {
            Shards = Mathf.Max(0, PlayerPrefs.GetInt("Umbra.Shards", 0));
            Rune = (RuneKind)Mathf.Clamp(PlayerPrefs.GetInt("Umbra.Rune", 0), 0, 2);
            BaseLevel = Mathf.Clamp(PlayerPrefs.GetInt("Umbra.BaseLevel", 1), 1, 99);
            BaseExperience = Mathf.Clamp(PlayerPrefs.GetInt("Umbra.BaseExperience", 0), 0, CombatRules.ExperienceToLevel(BaseLevel)-1);
            STR = Mathf.Clamp(PlayerPrefs.GetInt("Umbra.STR", 1), 1, 99);
            AGI = Mathf.Clamp(PlayerPrefs.GetInt("Umbra.AGI", 1), 1, 99);
            VIT = Mathf.Clamp(PlayerPrefs.GetInt("Umbra.VIT", 1), 1, 99);
            INT = Mathf.Clamp(PlayerPrefs.GetInt("Umbra.INT", 1), 1, 99);
            DEX = Mathf.Clamp(PlayerPrefs.GetInt("Umbra.DEX", 1), 1, 99);
            LUK = Mathf.Clamp(PlayerPrefs.GetInt("Umbra.LUK", 1), 1, 99);
            int totalEarned = (BaseLevel - 1) * CombatRules.StatPointsPerLevel;
            int spent = (STR - 1) + (AGI - 1) + (VIT - 1) + (INT - 1) + (DEX - 1) + (LUK - 1);
            if(spent > totalEarned) { STR=AGI=VIT=INT=DEX=LUK=1; spent=0; }
            StatPoints = totalEarned - spent;
            LoadCampaignProfile();
            Health = MaxHealth;
        }

        public void SaveProfile()
        {
            if(suppressSave) return;
            SaveCampaignProfile();
            PlayerPrefs.SetInt("Umbra.Shards", Shards);
            PlayerPrefs.SetInt("Umbra.Rune", (int)(Phase==RunPhase.Running?campRune:Rune));
            PlayerPrefs.SetInt("Umbra.BaseLevel", BaseLevel);
            PlayerPrefs.SetInt("Umbra.BaseExperience", BaseExperience);
            PlayerPrefs.SetInt("Umbra.STR", STR);
            PlayerPrefs.SetInt("Umbra.AGI", AGI);
            PlayerPrefs.SetInt("Umbra.VIT", VIT);
            PlayerPrefs.SetInt("Umbra.INT", INT);
            PlayerPrefs.SetInt("Umbra.DEX", DEX);
            PlayerPrefs.SetInt("Umbra.LUK", LUK);
            PlayerPrefs.Save();
        }

        void Start()
        {
            Application.targetFrameRate = 60; Application.runInBackground = true;
            int mobilePref = PlayerPrefs.GetInt("Umbra.MobileUI", -1);
            IsMobile = mobilePref != -1 ? (mobilePref == 1) : (Application.isMobilePlatform || SystemInfo.deviceType == DeviceType.Handheld);
            LoadProfile();
            BuildWorld();
            gameObject.AddComponent<UmbraHud>().Game = this;
            Ready = true;
            EnterCamp();
        }

        public void ToggleMobileInput()
        {
            IsMobile = !IsMobile;
            PlayerPrefs.SetInt("Umbra.MobileUI", IsMobile ? 1 : 0);
            PlayerPrefs.Save();
            Notify(IsMobile ? Loc.T("Mobile Touch Controls Enabled", "เปิดใช้งานการควบคุมแบบสัมผัส") : Loc.T("Desktop Keyboard Controls Enabled", "เปิดใช้งานการควบคุมด้วยคีย์บอร์ด"));
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

        Material TransparentMat(string key, Color color)
        {
            if (materials.TryGetValue(key, out Material existing)) return existing;
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (!shader) shader = Shader.Find("Unlit/Color");
            var material = new Material(shader);
            material.name = key;
            material.SetFloat("_Surface", 1);
            material.SetFloat("_Blend", 0);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.SetFloat("_Cull", 0);
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            material.SetColor("_BaseColor", color);
            material.color = color;
            materials.Add(key, material);
            owned.Add(material);
            return material;
        }

        GameObject Shape(string label, PrimitiveType type, Vector3 position, Vector3 scale, Material material, Transform parent = null, int pointerLayer = 2)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = label;go.layer=pointerLayer;
            go.transform.SetParent(parent == null ? world : parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            var collider = go.GetComponent<Collider>();
            if (collider && pointerLayer != GroundPointerLayer && pointerLayer != BlockerPointerLayer) {collider.enabled=false;Destroy(collider);}
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        void BuildWorld()
        {
            Random.InitState(24816);
            world = new GameObject("Amberfall • environment").transform;
            world.SetParent(transform);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.66f, .62f, .52f);
            RenderSettings.ambientEquatorColor = new Color(.46f, .42f, .28f);
            RenderSettings.ambientGroundColor = new Color(.20f, .18f, .14f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(.48f, .42f, .32f);
            RenderSettings.fogDensity = .011f;
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(UniversalAdditionalCameraData));
            cameraObject.tag = "MainCamera";
            WorldCamera = cameraObject.GetComponent<Camera>();
            WorldCamera.orthographic = false;
            WorldCamera.fieldOfView = DefaultZoomFov;
            TargetFov = DefaultZoomFov;
            WorldCamera.nearClipPlane = .3f;
            WorldCamera.farClipPlane = 120;
            WorldCamera.backgroundColor = RenderSettings.fogColor;
            WorldCamera.clearFlags = CameraClearFlags.SolidColor;
            cameraObject.transform.position = cameraOffset;
            cameraObject.transform.rotation = Quaternion.Euler(49.8f, 0, 0);
            var cameraData = cameraObject.GetComponent<UniversalAdditionalCameraData>();
            cameraData.renderPostProcessing = true;
            cameraData.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
            cameraData.volumeLayerMask = ~0;
            cameraData.volumeTrigger = cameraObject.transform;
            var sunlight = new GameObject("Late afternoon sun", typeof(Light)).GetComponent<Light>();
            sunlight.type = LightType.Directional;
            sunlight.color = new Color(1f, .85f, .60f);
            sunlight.intensity = 2.2f;
            sunlight.shadows = LightShadows.Soft;
            sunlight.shadowStrength = .80f;
            sunlight.transform.rotation = Quaternion.Euler(38f, -44f, 0);
            var volume = new GameObject("Amber atmosphere", typeof(Volume)).GetComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 100f;
            volume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
            owned.Add(volume.profile);

            var tone = volume.profile.Add<Tonemapping>();
            tone.mode.Override(TonemappingMode.ACES);

            var wb = volume.profile.Add<WhiteBalance>();
            wb.temperature.Override(20f);
            wb.tint.Override(-2f);

            var grading = volume.profile.Add<ColorAdjustments>();
            grading.postExposure.Override(.22f);
            grading.contrast.Override(26f);
            grading.saturation.Override(24f);
            grading.colorFilter.Override(new Color(1f, .96f, .89f));

            var smh = volume.profile.Add<ShadowsMidtonesHighlights>();
            smh.shadows.Override(new Vector4(.22f, .18f, .14f, 0f));
            smh.midtones.Override(new Vector4(1f, .95f, .86f, 0f));
            smh.highlights.Override(new Vector4(1.06f, .95f, .78f, 0f));

            var bloom = volume.profile.Add<Bloom>();
            bloom.intensity.Override(.48f);
            bloom.threshold.Override(1.05f);
            bloom.scatter.Override(.70f);
            bloom.tint.Override(new Color(1f, .88f, .65f));

            dof = volume.profile.Add<DepthOfField>();
            dof.mode.Override(DepthOfFieldMode.Bokeh);
            dof.focusDistance.Override(24f);
            dof.focalLength.Override(65f);
            dof.aperture.Override(2.8f);

            var vignette = volume.profile.Add<Vignette>();
            vignette.intensity.Override(.28f);
            vignette.smoothness.Override(.70f);
            vignette.color.Override(new Color(.05f, .08f, .06f));

            var grain = volume.profile.Add<FilmGrain>();
            grain.type.Override(FilmGrainLookup.Medium1);
            grain.intensity.Override(.08f);
            crownMesh=MakeCrownMesh();
            var ground=Mat("moss",new(.44f,.48f,.34f));
            ground.mainTexture=Resources.Load<Texture2D>("Umbra/ground");ground.mainTextureScale=new Vector2(40,40);
            Shape("Forest floor", PrimitiveType.Cube, new(0, -.27f, 0), new(CombatRules.FieldHalfSize*2+40, .5f, CombatRules.FieldHalfSize*2+40), ground, pointerLayer:GroundPointerLayer);
            BuildGround();
            var bark = Mat("bark", new(.23f, .15f, .095f));
            Color[] leaves = { new(.88f, .44f, .12f), new(.96f, .58f, .14f), new(1.0f, .72f, .18f), new(.46f, .50f, .18f), new(.28f, .38f, .19f) };
            // Keep tall scenery outside the playable meadow so hordes remain readable.
            for (int i = 0; i < 80; i++)
            {
                float edge=CombatRules.FieldHalfSize+Random.Range(4f,9f);
                float along=Random.Range(-CombatRules.FieldHalfSize,CombatRules.FieldHalfSize);
                Vector3 p=i%4==0?new(edge,0,along):i%4==1?new(-edge,0,along):i%4==2?new(along,0,edge):new(along,0,-edge);
                Tree(p,Random.Range(2.8f,5.7f),bark,Mat("canopy"+i%5,leaves[i%5]));
            }
            // Low decorative stones never obstruct movement or conceal enemies.
            for (int i = 0; i < 60; i++)
            {
                Vector3 p=new(Random.Range(-78f,78f),.025f,Random.Range(-78f,78f));
                float size=Random.Range(.15f,.35f);
                Shape("Meadow pebble",PrimitiveType.Sphere,p,new(size,.08f,size),Mat("meadow stone",new(.38f,.41f,.30f)));
            }
            var border=Mat("meadow boundary",new(.62f,.52f,.29f));
            float half=CombatRules.FieldHalfSize;
            for(int side=-1;side<=1;side+=2)
            {
                Shape("Field boundary",PrimitiveType.Cube,new(side*half,.005f,0),new(.35f,.02f,half*2),border);
                Shape("Field boundary",PrimitiveType.Cube,new(0,.005f,side*half),new(half*2,.02f,.35f),border);
            }
            StaticBatchingUtility.Combine(world.gameObject);
            BuildMotes();
            BuildActors();
        }

        void BuildGround()
        {
            // Thousands of colored triangles in a few meshes rather than thousands of renderers.
            List<Vector3> verts = new(); List<int> tris = new(); List<Color> colors = new();
            Color[] palette = { new(.32f,.42f,.18f), new(.46f,.46f,.20f), new(.24f,.34f,.15f), new(.68f,.45f,.16f), new(.78f,.52f,.18f), new(.58f,.32f,.14f) };
            for (int i = 0; i < 16000; i++)
            {
                float x = Random.Range(-CombatRules.FieldHalfSize,CombatRules.FieldHalfSize), z = Random.Range(-CombatRules.FieldHalfSize,CombatRules.FieldHalfSize);
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

        }

        void Tree(Vector3 p, float height, Material bark, Material foliage)
        {
            var root = new GameObject("Amber oak").transform; root.SetParent(world); root.position = p;
            Shape("Trunk",PrimitiveType.Cylinder,new(0,height*.35f,0),new(.36f,height*.35f,.36f),bark,root,pointerLayer:BlockerPointerLayer);
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
            classFrames[HeroClass.Archer]=rangerFrames;
            classFrames[HeroClass.Novice]=LoadClassSheet("novice_sheet");
            classFrames[HeroClass.Mage]=LoadClassSheet("mage_sheet");
            classFrames[HeroClass.Swordsman]=LoadClassSheet("swordsman_sheet");
            sproutSprite=LoadSprite("sprout"); mushroomSprite=LoadSprite("mushroom");
            Player=new GameObject("Wanderer • player").transform; Player.SetParent(transform); Player.position=new(0,0,-2);
            Shadow(Player,.75f); playerSprite=SpriteActor(Player,rangerFrames[0]); actorVisual=playerSprite.transform; RefreshClassSprite();
            Vector3[] homes={new(3,0,2),new(-3,0,3),new(4,0,6),new(-3,0,8),new(1,0,13),new(6,0,-3),new(-5,0,-5),new(5,0,12)};
            for(int i=0;i<CombatRules.EnemyPoolSize;i++)
            {
                bool isElite = i % 8 == 7;
                Vector3 p = i < homes.Length ? homes[i] : new Vector3(Random.Range(-18f,18f),0,Random.Range(-16f,18f));
                var enemy=new Enemy{home=p,maxHp=isElite?180:55,elite=isElite,phase=Random.value*6.28f};
                enemy.hp = i < 8 ? enemy.maxHp : 0;
                enemy.root=new GameObject(enemy.elite?"Elder bloom":"Grove creature").transform; enemy.root.SetParent(transform); enemy.root.position=enemy.home;
                Shadow(enemy.root,enemy.elite?1.1f:.6f);
                enemy.sprite=SpriteActor(enemy.root,i%2==0?mushroomSprite:sproutSprite,enemy.elite?1.5f:1);
                enemy.visual=enemy.sprite.transform; enemies.Add(enemy);
                if(i >= 8) enemy.root.gameObject.SetActive(false);
            }
        }

        void UpdateMobileInput()
        {
            if (!IsMobile)
            {
                var ts = Touchscreen.current;
                if (ts != null)
                {
                    foreach (var t in ts.touches)
                    {
                        if (t.press.wasPressedThisFrame)
                        {
                            IsMobile = true;
                            break;
                        }
                    }
                }
            }

            if (!IsMobile) return;
            if(Phase!=RunPhase.Running||IsModal)
            {
                IsJoystickActive=false;joystickFingerId=-1;MobileMoveInput=Vector2.zero;
                return;
            }

            bool dashPressed = false;
            bool volleyPressed = false;
            bool healPressed = false;
            bool aimPressed = false;

            var layout = new HudLayout(Screen.width, Screen.height);
            Vector2 ScreenToCanvas(Vector2 sp) => layout.FromScreen(sp);
            Vector2 dodgeCenter = layout.Point(new(1430, 730), 1, 1); float dodgeR = 60f;
            Vector2 novaCenter = layout.Point(new(1295, 755), 1, 1); float novaR = 48f;
            Vector2 healCenter = layout.Point(new(1430, 585), 1, 1); float healR = 48f;
            Vector2 aimCenter = layout.Point(new(1295, 635), 1, 1); float aimR = 40f;

            var tsCur = Touchscreen.current;
            bool hasTouch = false;

            if (tsCur != null)
            {
                for (int i = 0; i < tsCur.touches.Count; i++)
                {
                    var touch = tsCur.touches[i];
                    if (!touch.press.isPressed)
                    {
                        if (touch.touchId.ReadValue() == joystickFingerId)
                        {
                            joystickFingerId = -1;
                            IsJoystickActive = false;
                            MobileMoveInput = Vector2.zero;
                        }
                        continue;
                    }

                    hasTouch = true;
                    int fingerId = touch.touchId.ReadValue();
                    Vector2 sp = touch.position.ReadValue();
                    Vector2 cp = ScreenToCanvas(sp);
                    bool isDown = touch.press.wasPressedThisFrame;

                    if (isDown && Phase == RunPhase.Running && !IsModal && !Drafting)
                    {
                        if (Vector2.Distance(cp, dodgeCenter) <= dodgeR) { dashPressed = true; continue; }
                        if (Vector2.Distance(cp, novaCenter) <= novaR) { volleyPressed = true; continue; }
                        if (Vector2.Distance(cp, healCenter) <= healR) { healPressed = true; continue; }
                        if (Vector2.Distance(cp, aimCenter) <= aimR) { aimPressed = true; continue; }
                    }

                    if (sp.x < Screen.width * 0.48f && sp.y < Screen.height * 0.80f)
                    {
                        if (isDown && joystickFingerId == -1 && !layout.OverHud(cp, true))
                        {
                            joystickFingerId = fingerId;
                            IsJoystickActive = true;
                            JoystickOrigin = cp;
                            JoystickCurrent = cp;
                        }
                        else if (fingerId == joystickFingerId)
                        {
                            JoystickCurrent = cp;
                        }
                    }
                }
            }

            var mouse = Mouse.current;
            if (!hasTouch && mouse != null && mouse.leftButton.isPressed)
            {
                Vector2 mp = mouse.position.ReadValue();
                Vector2 cp = ScreenToCanvas(mp);
                bool isDown = mouse.leftButton.wasPressedThisFrame;

                if (Phase == RunPhase.Running && !IsModal && !Drafting)
                {
                    if (isDown)
                    {
                        if (Vector2.Distance(cp, dodgeCenter) <= dodgeR) dashPressed = true;
                        else if (Vector2.Distance(cp, novaCenter) <= novaR) volleyPressed = true;
                        else if (Vector2.Distance(cp, healCenter) <= healR) healPressed = true;
                        else if (Vector2.Distance(cp, aimCenter) <= aimR) aimPressed = true;
                    }

                    if (mp.x < Screen.width * 0.48f && mp.y < Screen.height * 0.80f)
                    {
                        if (isDown && !layout.OverHud(cp, true))
                        {
                            IsJoystickActive = true;
                            JoystickOrigin = cp;
                            JoystickCurrent = cp;
                        }
                        else if (IsJoystickActive)
                        {
                            JoystickCurrent = cp;
                        }
                    }
                }
            }
            else if (!hasTouch && (mouse == null || !mouse.leftButton.isPressed))
            {
                if (IsJoystickActive && joystickFingerId == -1)
                {
                    IsJoystickActive = false;
                    MobileMoveInput = Vector2.zero;
                }
            }

            if (IsJoystickActive)
            {
                Vector2 delta = JoystickCurrent - JoystickOrigin;
                float maxRadius = 75f;
                if (delta.magnitude > maxRadius)
                {
                    delta = delta.normalized * maxRadius;
                    JoystickCurrent = JoystickOrigin + delta;
                }
                float dist = delta.magnitude;
                if (dist > 8f)
                {
                    MobileMoveInput = new Vector2(delta.x / maxRadius, -delta.y / maxRadius);
                }
                else
                {
                    MobileMoveInput = Vector2.zero;
                }
            }
            else
            {
                MobileMoveInput = Vector2.zero;
            }

            if (dashPressed) TryDash();
            if (volleyPressed) TryVolley();
            if (healPressed) TryHeal();
            if (aimPressed)
            {
                AutoAim = !AutoAim;
                Notify(AutoAim ? Loc.T("AUTO-AIM: ON", "เล็งอัตโนมัติ: เปิด") : Loc.T("AUTO-AIM: OFF", "เล็งอัตโนมัติ: ปิด"));
            }
        }

        void Update()
        {
            if(!Ready) return;
            var pointer=Mouse.current;
            if(pointer!=null)
            {
                if(!pointer.leftButton.isPressed&&!pointer.rightButton.isPressed)pointerCapturedByUi=false;
                else if((pointer.leftButton.wasPressedThisFrame||pointer.rightButton.wasPressedThisFrame)&&PointerOverHud())pointerCapturedByUi=true;
            }
            UpdateMobileInput();
            var keyboard=Keyboard.current;
            if(keyboard!=null)
            {
                if(keyboard.escapeKey.wasPressedThisFrame) TogglePause();
                if(!Drafting&&!Paused)
                {
                    if(keyboard.tabKey.wasPressedThisFrame){RunePanel=!RunePanel;HelpPanel=StatsPanel=false;}
                    if(keyboard.hKey.wasPressedThisFrame){HelpPanel=!HelpPanel;RunePanel=StatsPanel=false;}
                    if(keyboard.cKey.wasPressedThisFrame){StatsPanel=!StatsPanel;RunePanel=HelpPanel=false;}
                }
            }
            SyncPause();
            if(!CanAct){ClearIncomingDamage();return;}
            float now=Time.time; float dt=Time.deltaTime; elapsed+=dt;
            BeginIncomingFrame();
            RunTimer+=dt;
            UpdateRunDirector(dt);
            if(Phase!=RunPhase.Running||Drafting){ClearIncomingDamage();return;}
            if(HasNovaPulse && Time.time >= nextNovaPulse)
            {
                nextNovaPulse = Time.time + 10f;
                CastNova();
            }
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
            if(IsMobile && MobileMoveInput.sqrMagnitude > 0.001f) input = MobileMoveInput;
            var mouse=Mouse.current;
            bool manual = mouse!=null && mouse.leftButton.isPressed && !PointerOverHud() && !pointerCapturedByUi;
            if(mouse!=null&&!PointerOverHud()&&!pointerCapturedByUi&&TryMousePoint(out Vector3 mousePoint))
            {
                if(mouse.rightButton.wasPressedThisFrame){moveTarget=mousePoint;walkingTo=true;}
                if(manual)TryAttack(mousePoint);
            }
            if(!manual && AutoAim)
            {
                Enemy target = FindNearestEnemy(Player.position, 12f);
                if(target != null) TryAttack(target.root.position);
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
            RefreshClassSprite(moving);
            actorVisual.localPosition=new(0,moving?Mathf.Abs(Mathf.Sin(elapsed*12))*.05f:0,0);
            playerSprite.color=IsPlayerProtected(now)?new Color(.62f,1,1):Color.white;
            UpdateHorde(dt); UpdateEnemies(dt);
            if(Phase!=RunPhase.Running){ClearIncomingDamage();return;}
            UpdateHazards(dt);
            ResolveIncomingDamage(now);
            if(Phase!=RunPhase.Running)return;
            UpdateShots(dt);
            if(Phase!=RunPhase.Running)return;
            UpdateLoot(dt); UpdateRings(); UpdateLightning();
            regenBank += CombatRules.HealthRegenPerSecond(VIT)*dt;
            int regen = Mathf.FloorToInt(regenBank);
            if(regen>0){Health=Mathf.Min(MaxHealth,Health+regen);regenBank-=regen;}
        }

        void LateUpdate()
        {
            if(!Ready)return;
            WorldCamera.transform.position=Vector3.Lerp(WorldCamera.transform.position,Player.position+cameraOffset,1-Mathf.Exp(-6*Time.deltaTime));
            UpdateCameraZoom();
            if(dof!=null)dof.focusDistance.Override(Vector3.Distance(WorldCamera.transform.position,Player.position));
        }

        void UpdateCameraZoom()
        {
            if (WorldCamera == null) return;
            if (Phase == RunPhase.Running && !IsModal)
            {
                var mouse = Mouse.current;
                if (mouse != null)
                {
                    Vector2 mousePos = mouse.position.ReadValue();
                    if (new Rect(0, 0, Screen.width, Screen.height).Contains(mousePos))
                    {
                        float scroll = mouse.scroll.ReadValue().y;
                        if (Mathf.Abs(scroll) > 0.01f)
                        {
                            ApplyZoomScroll(scroll);
                        }
                        if (mouse.middleButton.wasPressedThisFrame)
                        {
                            ResetZoom();
                        }
                    }
                }

                var kb = Keyboard.current;
                if (kb != null)
                {
                    float keyZoom = 0f;
                    if (kb.equalsKey.isPressed || (kb.numpadPlusKey != null && kb.numpadPlusKey.isPressed)) keyZoom += 1f;
                    if (kb.minusKey.isPressed || (kb.numpadMinusKey != null && kb.numpadMinusKey.isPressed)) keyZoom -= 1f;
                    if (keyZoom != 0f)
                    {
                        TargetFov = Mathf.Clamp(TargetFov - keyZoom * 14f * Time.unscaledDeltaTime, MinZoomFov, MaxZoomFov);
                    }
                }
            }

            // Smoothly glide toward TargetFov independent of framerate and time scale
            float t = 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime);
            WorldCamera.fieldOfView = Mathf.Lerp(WorldCamera.fieldOfView, TargetFov, t);
        }
        bool PointerOverHud()
        {
            if(Phase!=RunPhase.Running||IsModal)return true;
            Vector2 pos = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
            var ts = Touchscreen.current;
            if (IsMobile && ts != null && ts.primaryTouch.press.isPressed) pos = ts.primaryTouch.position.ReadValue();
            if(!new Rect(0,0,Screen.width,Screen.height).Contains(pos))return true;
            var layout = new HudLayout(Screen.width, Screen.height);
            if (IsMobile && pos.x < Screen.width * .48f && pos.y < Screen.height * .80f) return true;
            return layout.OverHud(layout.FromScreen(pos), IsMobile);
        }

        bool TryMousePoint(out Vector3 point)
        {
            point=default;
            if(Mouse.current==null||PointerOverHud()||pointerCapturedByUi)return false;
            return TryWorldPoint(WorldCamera.ScreenPointToRay(Mouse.current.position.ReadValue()),out point);
        }
        bool TryWorldPoint(Ray ray,out Vector3 point)
        {
            point=default;
            if(!Physics.Raycast(ray,out RaycastHit hit,WorldCamera.farClipPlane,pointerMask,QueryTriggerInteraction.Ignore))return false;
            // Nearest solid collider wins. Never fall through a blocker to the ground behind it.
            if(hit.collider.gameObject.layer!=GroundPointerLayer)return false;
            point=hit.point;point.y=0;
            return IsWalkable(point);
        }
        public bool IsWalkable(Vector3 p)
        {
            if(BossActive&&(Mathf.Abs(p.x)>4.8f||p.z< -7||p.z>7))return false;
            if(Mathf.Abs(p.x)>CombatRules.FieldHalfSize||Mathf.Abs(p.z)>CombatRules.FieldHalfSize)return false;
            return true;
        }
        public void MovePlayer(Vector3 delta)
        {
            delta *= MoveSpeedMultiplier * (1f + CombatRules.MoveSpeedBonus(AGI));
            Vector3 p=Player.position;
            Vector3 x=p+new Vector3(delta.x,0,0); if(IsWalkable(x))p=x;
            Vector3 z=p+new Vector3(0,0,delta.z); if(IsWalkable(z))p=z;
            Player.position=p;
        }
        public void SetRune(RuneKind rune){if(Phase!=RunPhase.Camp)return;Rune=rune;SaveProfile();Notify(Loc.RuneName(rune)+" "+Loc.T("equipped","ติดตั้งแล้ว"));}
        public bool TryAttack(Vector3 point)
        {
            if(!CanAct||Time.time<attackAt)return false;
            if(Class==HeroClass.Mage)return CastLightning(point);
            float speedMod = AttackSpeedMultiplier * (1f + CombatRules.AttackSpeedBonus(AGI)) * (Class==HeroClass.Archer?1.20f:1f);
            attackAt=Time.time+(CombatRules.AttackCooldown / speedMod);
            Vector3 dir=point-Player.position;dir.y=0;if(dir.sqrMagnitude<.01f)dir=facing;dir.Normalize();
            facing=dir;playerSprite.flipX=dir.x<0;
            int count=CombatRules.ProjectileCount(Rune) + BonusProjectiles;
            int baseDamage = AttackDamage;
            bool isCrit = Random.value < CombatRules.CritChance(LUK);
            int finalDamage = isCrit ? Mathf.RoundToInt(baseDamage * CombatRules.CritMultiplier) : baseDamage;
            if(Class==HeroClass.Swordsman) Slash(dir,finalDamage,isCrit);
            else for(int i=0;i<count;i++) Fire(Quaternion.Euler(0,(i-(count-1)*.5f)*12,0)*dir,Rune,finalDamage,isCrit);
            PlayCue(0);
            return true;
        }
        void Fire(Vector3 direction,RuneKind rune,int damage,bool isCrit=false)
        {
            float speed = 16f * (1f + CombatRules.ProjectileSpeedBonus(DEX));
            var shot=new Shot{velocity=direction*speed,damage=damage,rune=rune,isCrit=isCrit,remaining=(rune==RuneKind.Pierce?3:1)+(HasSproutCard?1:0),expiry=Time.time+1.1f};
            Color c=rune==RuneKind.Ember?new(1.8f,.55f,.12f):(isCrit?new(1.8f,1.3f,.3f):new(.6f,1.6f,1.2f));
            var go=Shape("Spirit arrow",PrimitiveType.Cube,Player.position+Vector3.up*.6f,new(.055f,.055f,.8f),Mat("arrow"+rune+(isCrit?"crit":""),c,true),transform);
            go.transform.rotation=Quaternion.LookRotation(direction);
            shot.root=go.transform; shots.Add(shot);
        }
        public bool TryVolley()
        {
            if(!CanAct||Time.time<volleyAt)return false;
            float cdr = CombatRules.CooldownReduction(INT);
            volleyAt=Time.time+CombatRules.VolleyCooldown*(1f-cdr);
            CastNova();return true;
        }

        public bool TryDash()
        {
            if(!CanAct||Time.time<dashAt)return false;
            float cdr = CombatRules.CooldownReduction(INT);
            dashAt=Time.time+CombatRules.DashCooldown*(1f-cdr);
            dashUntil=Time.time+.18f;dashImmuneUntil=Mathf.Max(dashImmuneUntil,Time.time+.28f);dashDirection=facing;
            Pulse(Player.position,1,mint,.28f);return true;
        }
        public bool TryHeal()
        {
            if(!CanAct||Time.time<healAt||Health>=MaxHealth)return false;
            float cdr = CombatRules.CooldownReduction(INT);
            healAt=Time.time+CombatRules.HealCooldown*(1f-cdr);
            int restored=Mathf.Min(MaxHealth-Health,Mathf.RoundToInt(MaxHealth*.30f));
            Health+=restored;PlayCue(2);
            Popup(Player.position+Vector3.up,"+"+restored,mint);Pulse(Player.position,2,mint,.6f);return true;
        }
        public Enemy FindNearestEnemy(Vector3 origin, float maxDistance)
        {
            Enemy nearest = null;
            float bestSq = maxDistance * maxDistance;
            foreach(var e in enemies)
            {
                if(e.hp <= 0 || !e.root.gameObject.activeSelf) continue;
                float sq = (e.root.position - origin).sqrMagnitude;
                if(sq < bestSq)
                {
                    bestSq = sq;
                    nearest = e;
                }
            }
            return nearest;
        }

        void UpdateHorde(float dt)
        {
            if(BossActive||Time.time<nextHordeSpawn)return;
            var wave=CombatRules.WaveAt(RunTimer);
            // Never recycle visible actors, even when zoom/aspect makes the view wider than 40m.
            for(int i=0;i<enemies.Count-1;i++)
            {
                var enemy=enemies[i];
                if(enemy.hp>0&&!enemy.boss&&!InSpawnView(enemy.root.position,.25f)&&
                    (enemy.root.position-Player.position).sqrMagnitude>CombatRules.EnemyRecycleDistance*CombatRules.EnemyRecycleDistance)
                {enemy.hp=0;enemy.root.gameObject.SetActive(false);}
            }
            int active=0,elites=0;
            for(int i=0;i<enemies.Count-1;i++)
                if(enemies[i].hp>0&&enemies[i].root.gameObject.activeSelf){active++;if(enemies[i].elite)elites++;}
            // Refill an empty field promptly, but never burst-spawn an entire target population.
            bool depleted=active<wave.Target/2;
            nextHordeSpawn=Time.time+(depleted?wave.Interval*.5f:wave.Interval);
            int batch=wave.Batch,available=enemies.Count-1;
            int eliteTarget=RunTimer>=CombatRules.EliteArrival?wave.EliteCap:0;
            if(elites<eliteTarget)
            {
                int eliteSlot=enemies.FindIndex(enemy=>enemy.elite&&enemy.hp<=0&&!enemy.boss);
                if(eliteSlot>=0&&eliteSlot<available)hordeCursor=eliteSlot;
            }
            for(int checkedSlots=0;checkedSlots<available&&active<wave.Target&&batch>0;checkedSlots++)
            {
                var e=enemies[hordeCursor];hordeCursor=(hordeCursor+1)%available;
                if(e.hp>0||e.boss||(e.elite&&elites>=eliteTarget))continue;
                int side=wave.Front?((int)(RunTimer/60f)/4)%4:-1;
                if(!TrySpawnPosition(out Vector3 pos,side))continue;
                SpawnEnemy(e,pos,CombatRules.EnemyHealth(e.elite,SelectedMap,RunTimer));
                active++;batch--;if(e.elite)elites++;
            }
        }

        void UpdateEnemies(float dt)
        {
            float now = Time.time;
            for(int i = 0; i < enemies.Count; i++)
            {
                var e = enemies[i];
                if(e.hp <= 0 || !e.root.gameObject.activeSelf) continue;
                if(e.burnUntil > now && now >= e.burnTick){e.burnTick = now + 1; DamageEnemy(e, e.burnDamage); if(e.hp <= 0) continue;}
                if(e.boss){UpdateBoss(e, dt); continue;}
                Vector3 delta = Player.position - e.root.position; delta.y = 0; float distance = delta.magnitude;
                float stopDist = 0.8f * (DamageRules.PlayerRadius + e.contactRadius);
                if(distance > stopDist)
                {
                    Vector3 motion = delta.normalized * e.moveSpeed;
                    foreach(var other in enemies) if(other != e && other.hp > 0)
                    {
                        Vector3 away = e.root.position - other.root.position; away.y = 0; float sq = away.sqrMagnitude;
                        if(sq > .001f && sq < .8f) motion += away.normalized * (.8f - sq) * 2;
                    }
                    SlideEnemy(e, motion * dt);
                }
                TryContactAttack(e, i, now);
                e.visual.localPosition = new(0, Mathf.Abs(Mathf.Sin(elapsed * 4 + e.phase)) * .10f, 0);
                e.sprite.flipX = delta.x < 0;
                e.sprite.color = now < e.flashUntil ? new(1, .52f, .36f) : Color.white;
            }
        }
        void UpdateShots(float dt)
        {
            for(int i=shots.Count-1;i>=0;i--)
            {
                Shot s=shots[i];Vector3 before=s.root.position;Vector3 after=before+s.velocity*dt;s.root.position=after;
                foreach(var e in enemies)
                {
                    if(e.hp<=0||!e.root.gameObject.activeSelf||s.hit.Contains(e))continue;
                    Vector3 p=e.root.position+Vector3.up*.6f;
                    Vector3 segment=after-before;
                    float t=Mathf.Clamp01(Vector3.Dot(p-before,segment)/Mathf.Max(.0001f,segment.sqrMagnitude));
                    if((before+segment*t-p).sqrMagnitude>(e.elite?.75f:.4f))continue;
                    s.hit.Add(e);DamageEnemy(e,s.damage,s.isCrit);s.remaining--;
                    ApplyHitSupports(e,s.damage,s.hit.Count==1);
                    if(s.rune==RuneKind.Ember){Pulse(e.root.position,1.8f,new(1,.5f,.15f),.45f);foreach(var other in enemies)if(other!=e&&other.hp>0&&other.root.gameObject.activeSelf&&Vector3.Distance(other.root.position,e.root.position)<1.8f)DamageEnemy(other,s.damage/2);}
                    if(s.remaining<=0)break;
                }
                if(s.remaining<=0||Time.time>s.expiry){Destroy(s.root.gameObject);shots.RemoveAt(i);}
            }
        }
        public void DamageEnemy(Enemy e,int damage,bool isCrit=false)
        {
            if(Phase!=RunPhase.Running||e.hp<=0)return;e.hp-=Mathf.Max(0,damage);e.flashUntil=Time.time+.12f;
            Popup(e.root.position+Vector3.up*1.2f,isCrit?"CRIT! "+damage:damage.ToString(),isCrit?new Color(1f,.88f,.25f):gold);
            if(e.hp>0)return;
            e.hp=0;
            e.root.gameObject.SetActive(false);e.windupUntil=0;Kills++;
            if(e.boss){FinishRun(true,"The guardian has fallen");return;}

            if(HasMushroomCard) Health=Mathf.Min(MaxHealth,Health+1);
            if(SelectedMap>0) AddHazard(e.root.position,SelectedMap==1?1.5f:2.2f,SelectedMap==1?1f:1.5f,SelectedMap==1?4f:.35f,SelectedMap==1?8:24);

            var item=Shape("Amber shard",PrimitiveType.Cube,e.root.position+Vector3.up*.35f,Vector3.one*.22f,Mat("loot amber",new(1.6f,.8f,.19f),true),transform);
            loot.Add(new Loot{root=item.transform,phase=Random.value*5,shards=e.elite?9:3,exp=e.elite?30:10});
        }
        void ApplyLevelUpKnockback()
        {
            foreach(var e in enemies)
            {
                if(e.hp<=0||!e.root.gameObject.activeSelf)continue;
                Vector3 diff=e.root.position-Player.position;
                diff.y=0;
                float dist=diff.magnitude;
                if(dist<3.0f)
                {
                    Vector3 dir=dist>0.01f?diff/dist:Vector3.back;
                    e.root.position=Player.position+dir*3.5f;
                }
            }
        }

        void SpawnLevelUpVfx()
        {
            if(activePillar!=null)Destroy(activePillar);
            if(activePillarLight!=null)Destroy(activePillarLight.gameObject);

            Pulse(Player.position, 3.8f, new Color(1f, 0.85f, 0.32f), 0.6f);
            Popup(Player.position + Vector3.up * 2.2f, "★ LEVEL UP! ★", new Color(1f, 0.92f, 0.38f));

            var pillarMat = TransparentMat("vfx_pillar", new Color(1f, 0.88f, 0.35f, 0.28f));
            activePillar = Shape("Golden Pillar", PrimitiveType.Cylinder, Player.position + Vector3.up * 5.5f, new Vector3(1.6f, 5.5f, 1.6f), pillarMat, transform);

            var lightGo = new GameObject("LevelUp Light", typeof(Light));
            lightGo.transform.SetParent(transform);
            lightGo.transform.position = Player.position + Vector3.up * 2f;
            activePillarLight = lightGo.GetComponent<Light>();
            activePillarLight.type = LightType.Point;
            activePillarLight.range = 7f;
            activePillarLight.color = new Color(1f, 0.85f, 0.45f);
            activePillarLight.intensity = 2.4f;

            StartCoroutine(AnimateLevelUpPillar(activePillar, activePillarLight, 0.5f));
        }

        IEnumerator AnimateLevelUpPillar(GameObject pillar, Light light, float duration)
        {
            float elapsed = 0f;
            var renderer = pillar != null ? pillar.GetComponent<Renderer>() : null;
            Material instMat = renderer != null ? renderer.material : null;
            Color initColor = instMat != null ? instMat.GetColor("_BaseColor") : new Color(1f, 0.88f, 0.35f, 0.28f);
            float initIntensity = light != null ? light.intensity : 2.4f;

            while (elapsed < duration)
            {
                if (pillar == null) yield break;
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                if (instMat != null)
                {
                    Color c = initColor;
                    c.a = Mathf.Lerp(initColor.a, 0f, t);
                    instMat.SetColor("_BaseColor", c);
                    instMat.color = c;
                }
                if (light != null)
                {
                    light.intensity = Mathf.Lerp(initIntensity, 0f, t);
                }
                yield return null;
            }

            if (instMat != null) Destroy(instMat);
            if (pillar != null) Destroy(pillar);
            if (light != null) Destroy(light.gameObject);
        }

        public void TriggerLevelUp()
        {
            if(Phase!=RunPhase.Running||BossActive)return;
            ApplyLevelUpKnockback();
            SpawnLevelUpVfx();
            DraftOpenedAt = Time.unscaledTime;
            ActiveDraft = RollDraft();
            Drafting = true;
            SyncPause();
            PlayCue(2);
        }
        public void ApplyPerk(CombatRules.Perk perk)
        {
            if(!Drafting||perk==null||!ActiveDraft.Contains(perk))return;
            perkRanks[perk.Kind]=Rank(perk.Kind)+1;
            switch(perk.Kind)
            {
                case CombatRules.PerkKind.RunePierce:Rune=RuneKind.Pierce;break;
                case CombatRules.PerkKind.RuneScatter:Rune=RuneKind.Scatter;break;
                case CombatRules.PerkKind.RuneEmber:Rune=RuneKind.Ember;break;
                case CombatRules.PerkKind.RapidFire:AttackSpeedMultiplier+=.15f;break;
                case CombatRules.PerkKind.SwiftBoots:MoveSpeedMultiplier+=.08f;break;
                case CombatRules.PerkKind.Multishot:BonusProjectiles++;break;
                case CombatRules.PerkKind.Vitality:MaxHealthBonus+=20;Health=Mathf.Min(MaxHealth,Health+30);break;
                case CombatRules.PerkKind.Magnetism:PickupRadius+=2;break;
                case CombatRules.PerkKind.WindNovaPulse:HasNovaPulse=true;nextNovaPulse=Time.time+10;break;
            }
            PlayCue(0);
            Notify(Loc.PerkTitle(perk.Kind)+" "+Loc.T("acquired","ได้รับแล้ว"));Drafting=false;ActiveDraft.Clear();
            if(pendingDrafts>0){pendingDrafts--;TriggerLevelUp();}
            SyncPause();
        }


        void UpdateLoot(float dt)
        {
            for(int i=loot.Count-1;i>=0;i--)
            {
                var l=loot[i];l.root.Rotate(0,80*dt,0);var p=l.root.position;p.y=.4f+Mathf.Sin(elapsed*3+l.phase)*.10f;
                float dist = Vector2.Distance(new(p.x,p.z),new(Player.position.x,Player.position.z));
                if(dist < PickupRadius)
                {
                    Vector3 target = Player.position + Vector3.up * .35f;
                    p = Vector3.MoveTowards(p, target, dt * (12f + (PickupRadius - dist) * 4f));
                }
                l.root.position=p;
                if(dist<1.35f){int s=l.shards>0?l.shards:3;int x=l.exp>0?l.exp:10;RunShards+=s;AwardRunExperience(x);Collected++;Popup(Player.position+Vector3.up,"+"+s+" amber",gold);Destroy(l.root.gameObject);loot.RemoveAt(i);}
            }
        }
        public void Notify(string message){Notice=message;NoticeUntil=Time.unscaledTime+4;}
        void Popup(Vector3 p,string message,Color color){floats.Add(new FloatText{point=p,text=message,color=color,until=Time.time+1.1f});}
        public void DrawWorldLabels(GUIStyle style)
        {
            foreach(var e in enemies)
            {
                if(e.hp<=0||!e.root.gameObject.activeSelf||e.hp==e.maxHp)continue;
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
            var line=go.GetComponent<LineRenderer>();line.sharedMaterial=RingMaterial();line.startColor=line.endColor=color;line.positionCount=49;line.widthMultiplier=.055f;line.loop=false;
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
        void SaveCollection() => SaveProfile();
        void OnDestroy()
        {
            Time.timeScale=1;
            if(activePillar)Destroy(activePillar);
            if(activePillarLight)Destroy(activePillarLight.gameObject);
            foreach(var o in owned)if(o)Destroy(o);
        }

        public IEnumerator SmokeTest(Action<string> complete)
        {
            yield return null;
            RunCampaignTests(complete);
        }

    }
}
