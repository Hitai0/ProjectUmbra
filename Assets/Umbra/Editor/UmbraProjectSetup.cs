using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor.Build.Reporting;

namespace Umbra.Editor
{
    [InitializeOnLoad]
    public static class UmbraProjectSetup
    {
        const string ScenePath="Assets/Umbra/Scenes/Amberfall.unity";
        static double nextPoll;
        static UmbraProjectSetup(){EditorApplication.update+=Poll;EditorApplication.delayCall+=FirstSetup;}
        static void FirstSetup()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating){EditorApplication.delayCall+=FirstSetup;return;}
            if(!File.Exists(ScenePath))CreatePrototype();
        }
        static void Poll()
        {
            if(EditorApplication.timeSinceStartup<nextPoll||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
            nextPoll=EditorApplication.timeSinceStartup+1;
            if(EditorApplication.isPlaying&&SessionState.GetBool("Umbra.PendingSmoke",false))
            {
                var ready=UnityEngine.Object.FindFirstObjectByType<UmbraPrototype>();
                if(ready&&ready.Ready){SessionState.SetBool("Umbra.PendingSmoke",false);ExecuteSmokeTests();}
            }
            string path=".umbra-command";
            if(!File.Exists(path))return;
            string command=File.ReadAllText(path).Trim();File.Delete(path);
            try
            {
                if(command=="play")EditorApplication.isPlaying=true;
                else if(command=="stop")EditorApplication.isPlaying=false;
                else if(command=="setup")CreatePrototype();
                else if(command=="open")Open();
                else if(command=="web")BuildWeb();
                else if(command=="test")RunSmokeTests();
                else if(command=="capture")Capture();
                else if(command=="diag")Diag();
                else if(command=="boss-preview")UnityEngine.Object.FindFirstObjectByType<UmbraPrototype>()?.PreviewGuardian();
                else if(command=="refresh")AssetDatabase.Refresh();
            }
            catch(Exception e){File.WriteAllText(".umbra-error",e.ToString());Debug.LogException(e);}
        }
        [MenuItem("Umbra/Open Amberfall")]
        public static void Open(){EditorSceneManager.OpenScene(ScenePath);}
        [MenuItem("Umbra/Create or configure prototype")]
        public static void CreatePrototype()
        {
            if(EditorApplication.isPlaying)return;
            Directory.CreateDirectory("Assets/Umbra/Settings");Directory.CreateDirectory("Assets/Umbra/Scenes");
            const string rendererPath="Assets/Umbra/Settings/UmbraRenderer.asset";
            const string pipelinePath="Assets/Umbra/Settings/UmbraURP.asset";
            var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            if(!renderer){renderer=ScriptableObject.CreateInstance<UniversalRendererData>();renderer.name="UmbraRenderer";AssetDatabase.CreateAsset(renderer,rendererPath);}
            EnsureSSAO(renderer);
            var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
            if(!pipeline)
            {
                pipeline=ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();pipeline.name="UmbraURP";
                var serialized=new SerializedObject(pipeline);serialized.FindProperty("m_RendererDataList").GetArrayElementAtIndex(0).objectReferenceValue=renderer;serialized.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.CreateAsset(pipeline,pipelinePath);
            }
            pipeline.renderScale=1;pipeline.msaaSampleCount=2;pipeline.shadowDistance=45;pipeline.supportsHDR=true;
            GraphicsSettings.defaultRenderPipeline=pipeline;
            int old=QualitySettings.GetQualityLevel();
            for(int i=0;i<QualitySettings.names.Length;i++){QualitySettings.SetQualityLevel(i);QualitySettings.renderPipeline=pipeline;}
            QualitySettings.SetQualityLevel(old);
            QualitySettings.vSyncCount=0;
            PlayerSettings.companyName="Project Umbra";PlayerSettings.productName="Project Umbra";
            PlayerSettings.bundleVersion=UmbraPrototype.Version;
            PlayerSettings.defaultScreenWidth=1600;PlayerSettings.defaultScreenHeight=900;
            PlayerSettings.runInBackground=true;
            PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback=false;
            // Shader.Find paths used at runtime must survive build stripping.
            var graphics=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
            var shaders=graphics.FindProperty("m_AlwaysIncludedShaders");
            for(int i=shaders.arraySize-1;i>=0;i--)
            {
                var s=shaders.GetArrayElementAtIndex(i).objectReferenceValue as Shader;
                if(s&&s.name=="Universal Render Pipeline/Lit"){shaders.GetArrayElementAtIndex(i).objectReferenceValue=null;shaders.DeleteArrayElementAtIndex(i);}
            }
            Directory.CreateDirectory("Assets/Umbra/Resources/Umbra/Materials");
            foreach(string kind in new[]{"Lit","Unlit"})
            {
                string materialPath="Assets/Umbra/Resources/Umbra/Materials/"+kind+".mat";
                if(!AssetDatabase.LoadAssetAtPath<Material>(materialPath))AssetDatabase.CreateAsset(new Material(Shader.Find("Universal Render Pipeline/"+kind)){enableInstancing=true},materialPath);
            }
            foreach(string name in new[]{"Umbra/VertexColor","Universal Render Pipeline/Unlit","Universal Render Pipeline/2D/Sprite-Unlit-Default"})
            {
                Shader shader=Shader.Find(name);if(!shader)continue;bool found=false;
                for(int i=0;i<shaders.arraySize;i++)if(shaders.GetArrayElementAtIndex(i).objectReferenceValue==shader)found=true;
                if(!found){int i=shaders.arraySize;shaders.InsertArrayElementAtIndex(i);shaders.GetArrayElementAtIndex(i).objectReferenceValue=shader;}
            }
            graphics.ApplyModifiedPropertiesWithoutUndo();
            if(!File.Exists(ScenePath))
            {
                // Additive creation preserves any existing user's open scene and unsaved edits.
                var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
                var root=new GameObject("Project Umbra • Play to enter Amberfall");root.AddComponent<UmbraPrototype>();
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);
                UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
                EditorSceneManager.SaveScene(scene,ScenePath);
            }
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
            EditorUtility.SetDirty(pipeline);AssetDatabase.SaveAssets();
            File.WriteAllText(".umbra-ready","URP configured. Amberfall scene created.");
            Debug.Log("UMBRA READY: Open Assets/Umbra/Scenes/Amberfall.unity and press Play.");
        }
        [MenuItem("Umbra/Build Web prototype")]
        public static void BuildWeb()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode before building.");
            PlayerSettings.bundleVersion=UmbraPrototype.Version;
            Directory.CreateDirectory("Builds/Web");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},target=BuildTarget.WebGL,locationPathName="Builds/Web",options=BuildOptions.None});
            File.WriteAllText(".umbra-build",report.summary.result+" | "+report.summary.totalSize+" bytes | "+report.summary.totalTime);
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Web build failed: "+report.summary.result);
            if(File.Exists("Tools/web_shell.html"))
            {
                string shell=File.ReadAllText("Tools/web_shell.html").Replace("@@BUILD_NAME@@","Web");
                File.WriteAllText("Builds/Web/index.html",shell);
            }
            if(File.Exists("Tools/vercel.json"))File.Copy("Tools/vercel.json","Builds/Web/vercel.json",true);
        }
        [MenuItem("Umbra/Run gameplay smoke tests (Play Mode)")]
        public static void RunSmokeTests()
        {
            if(!EditorApplication.isPlaying)
            {
                SessionState.SetBool("Umbra.PendingSmoke",true);
                EditorApplication.isPlaying = true;
                return;
            }
            ExecuteSmokeTests();
        }
        static void OnPlayStateChangedForTest(PlayModeStateChange state)
        {
            if(state == PlayModeStateChange.EnteredPlayMode)
            {
                EditorApplication.playModeStateChanged -= OnPlayStateChangedForTest;
                EditorApplication.delayCall += ExecuteSmokeTests;
            }
        }
        static void ExecuteSmokeTests()
        {
            var game = UnityEngine.Object.FindFirstObjectByType<UmbraPrototype>();
            if(!game)
            {
                EditorApplication.delayCall += ExecuteSmokeTests;
                return;
            }
            game.StartCoroutine(game.SmokeTest(result => { File.WriteAllText(".umbra-test", result); Debug.Log(result); }));
        }
        [MenuItem("Umbra/Capture prototype (Play Mode)")]
        public static void Capture()
        {
            Directory.CreateDirectory("Recordings");
            var game = UnityEngine.Object.FindFirstObjectByType<UmbraPrototype>();
            if (game && game.WorldCamera)
            {
                var cam = game.WorldCamera;
                var rt = new RenderTexture(1600, 900, 24);
                var prevRt = cam.targetTexture;
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(1600, 900, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
                tex.Apply();
                cam.targetTexture = prevRt;
                RenderTexture.active = null;
                UnityEngine.Object.DestroyImmediate(rt);
                File.WriteAllBytes("Recordings/Amberfall.png", tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);
            }
            else ScreenCapture.CaptureScreenshot("Recordings/Amberfall.png");
        }
        public static void Diag()
        {
            var game = UnityEngine.Object.FindFirstObjectByType<UmbraPrototype>();
            if (!game) { File.WriteAllText(".umbra-diag", "No game found"); return; }
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Enemies count: " + game.Enemies.Count);
            int active = 0, inactiveWithHp = 0;
            for (int i = 0; i < game.Enemies.Count; i++)
            {
                var e = game.Enemies[i];
                bool act = e.root.gameObject.activeSelf;
                if (act) active++;
                else if (e.hp > 0) inactiveWithHp++;
                if (i < 5 || act)
                {
                    sb.AppendLine($"Enemy {i}: active={act}, hp={e.hp}/{e.maxHp}, pos={e.root.position}, sprite={e.sprite != null}, srEnabled={(e.sprite ? e.sprite.enabled : false)}, srMat={(e.sprite && e.sprite.sharedMaterial ? e.sprite.sharedMaterial.name : "null")}, shader={(e.sprite && e.sprite.sharedMaterial && e.sprite.sharedMaterial.shader ? e.sprite.sharedMaterial.shader.name : "null")}");
                }
            }
            sb.AppendLine($"Summary: active={active}, inactiveWithHp={inactiveWithHp}");
            File.WriteAllText(".umbra-diag", sb.ToString());
        }
        public static void EnsureSSAO(UniversalRendererData renderer)
        {
            if(!renderer)return;
            foreach(var f in renderer.rendererFeatures)if(f is ScreenSpaceAmbientOcclusion)return;
            var ssao=ScriptableObject.CreateInstance<ScreenSpaceAmbientOcclusion>();
            ssao.name="ScreenSpaceAmbientOcclusion";
            AssetDatabase.AddObjectToAsset(ssao,renderer);
            var serialized=new SerializedObject(renderer);
            var prop=serialized.FindProperty("m_RendererFeatures");
            int idx=prop.arraySize++;
            prop.GetArrayElementAtIndex(idx).objectReferenceValue=ssao;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(renderer);
            AssetDatabase.SaveAssets();
        }
    }

    public sealed class UmbraTextureImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if(!assetPath.StartsWith("Assets/Umbra/Resources/Umbra/"))return;
            var importer=(TextureImporter)assetImporter;importer.textureType=TextureImporterType.Default;
            importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.npotScale=TextureImporterNPOTScale.None;
        }
    }
}
