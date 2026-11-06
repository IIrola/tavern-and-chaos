using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace TavernChaos.Editor
{
    public static class DemoBuild
    {
        private const string ScenePath="Assets/Scenes/Tavern.unity";
        [MenuItem("Tavern/Run first-person checks")]
        public static void RunFirstPersonChecks()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) return;
            SessionState.SetBool("Tavern.FirstPersonChecks",true);
            EditorApplication.EnterPlaymode();
        }
        [InitializeOnLoadMethod]
        private static void RegisterChecks()
        {
            EditorApplication.playModeStateChanged-=StartChecks;
            EditorApplication.playModeStateChanged+=StartChecks;
        }
        private static void StartChecks(PlayModeStateChange state)
        {
            if(state!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool("Tavern.FirstPersonChecks",false)) return;
            SessionState.SetBool("Tavern.FirstPersonChecks",false);
            EditorApplication.delayCall+=()=> {
                var game=UnityEngine.Object.FindAnyObjectByType<DemoGame>();
                if(game) game.RunFirstPersonChecks();
                else { Debug.LogError("Open Tavern.unity before running demo checks."); EditorApplication.isPlaying=false; }
            };
        }
        [MenuItem("Tavern/Prepare demo scene")]
        public static void Prepare()
        {
            if(File.Exists(ScenePath)) { Debug.Log("Demo scene already exists; it was not regenerated."); return; }
            Directory.CreateDirectory("Assets/Scenes");
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            new GameObject("Tavern demo | runtime composition").AddComponent<DemoGame>();
            EditorSceneManager.SaveScene(scene,ScenePath);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
        }
        private static void Configure()
        {
            if(!File.Exists(ScenePath)) throw new FileNotFoundException("The saved demo scene is missing.",ScenePath);
            if(EditorSceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Save scene changes before building the demo.");
            PlayerSettings.companyName="Tavern Workshop"; PlayerSettings.productName="Tavern and Chaos"; PlayerSettings.bundleVersion="0.9.0";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone,"com.tavernworkshop.tavernchaos");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Standalone,ManagedStrippingLevel.Disabled);
            PlayerSettings.defaultScreenWidth=1440; PlayerSettings.defaultScreenHeight=900;
            PlayerSettings.fullScreenMode=FullScreenMode.Windowed; PlayerSettings.resizableWindow=true; PlayerSettings.runInBackground=false;
            PlayerSettings.colorSpace=ColorSpace.Linear; PlayerSettings.usePlayerLog=true;
            var so=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            var active=so.FindProperty("activeInputHandler"); if(active!=null) active.intValue=1; so.ApplyModifiedPropertiesWithoutUndo();
            var tags=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers=tags.FindProperty("layers"); layers.GetArrayElementAtIndex(PoolPhysics.BallLayer).stringValue="Pool Balls"; layers.GetArrayElementAtIndex(PoolPhysics.CushionLayer).stringValue="Pool Cushions"; tags.ApplyModifiedPropertiesWithoutUndo();
            QualitySettings.SetQualityLevel(2,true); QualitySettings.shadows=ShadowQuality.All; QualitySettings.shadowResolution=ShadowResolution.Medium;
            QualitySettings.shadowDistance=22; QualitySettings.antiAliasing=2; QualitySettings.pixelLightCount=6;
            // Runtime-created materials must keep their shaders in a stripped player.
            var graphics=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
            var shaders=graphics.FindProperty("m_AlwaysIncludedShaders");
            // Internal font shaders come from the built-in font, not the player shader list.
            for(int i=shaders.arraySize-1;i>=0;i--) {
                var existing=shaders.GetArrayElementAtIndex(i).objectReferenceValue as Shader;
                if(existing && existing.name=="GUI/Text Shader") { shaders.GetArrayElementAtIndex(i).objectReferenceValue=null; shaders.DeleteArrayElementAtIndex(i); }
            }
            foreach(string name in new[]{"Standard","Sprites/Default"}) {
                var shader=Shader.Find(name); bool found=false;
                for(int i=0;i<shaders.arraySize;i++) if(shaders.GetArrayElementAtIndex(i).objectReferenceValue==shader) found=true;
                if(shader&&!found) { shaders.InsertArrayElementAtIndex(shaders.arraySize); shaders.GetArrayElementAtIndex(shaders.arraySize-1).objectReferenceValue=shader; }
            }
            graphics.ApplyModifiedPropertiesWithoutUndo(); AssetDatabase.SaveAssets();
        }
        [MenuItem("Tavern/Build macOS demo")]
        public static void BuildMac()
        {
            RejectDuplicateTypeMetadata();
            BarValidation.Run();
            ArmValidation.Run();
            SessionValidation.Run();
            EconomyValidation.Run();
            PoolRulesValidation.Run();
            BlackjackValidation.Run();
            ConfigureMacArchitecture(); Configure(); Directory.CreateDirectory("../Builds/macOS");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes=new[]{ScenePath},locationPathName="../Builds/macOS/Tavern and Chaos.app",target=BuildTarget.StandaloneOSX,options=BuildOptions.CleanBuildCache });
            var summary=report.summary;
            Directory.CreateDirectory("../Evidence/effects");
            File.WriteAllText("../Evidence/effects/build-macos.json",JsonUtility.ToJson(new BuildEvidence { result=summary.result.ToString(),unity=Application.unityVersion,platform="macOS Universal (Intel x64 + Apple Silicon arm64)",backend="Mono",version=PlayerSettings.bundleVersion,bytes=summary.totalSize,seconds=summary.totalTime.TotalSeconds,errors=summary.totalErrors,warnings=summary.totalWarnings },true));
            InspectSteps(report,"macos");
            if(summary.result!=BuildResult.Succeeded||summary.totalErrors>0) throw new Exception($"Build failed validation: {summary.totalErrors} errors.");
            Debug.Log($"DEMO_BUILD_OK: {summary.totalSize} bytes, {summary.totalTime}");
        }
        private static void ConfigureMacArchitecture()
        {
#if UNITY_STANDALONE_OSX
            UnityEditor.OSXStandalone.UserBuildSettings.architecture=OSArchitecture.x64ARM64;
#else
            throw new InvalidOperationException("Switch the active build platform to macOS first, or use -buildTarget osx in batch mode.");
#endif
        }
        [MenuItem("Tavern/Build Windows LAN demo")]
        public static void BuildWindows()
        {
            RejectDuplicateTypeMetadata();
            BarValidation.Run();
            ArmValidation.Run();
            SessionValidation.Run();
            EconomyValidation.Run();
            PoolRulesValidation.Run();
            BlackjackValidation.Run();
            Configure(); Directory.CreateDirectory("../Builds/Windows");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes=new[]{ScenePath},locationPathName="../Builds/Windows/Tavern and Chaos.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.CleanBuildCache });
            var summary=report.summary;
            Directory.CreateDirectory("../Evidence/effects");
            File.WriteAllText("../Evidence/effects/build-windows.json",JsonUtility.ToJson(new BuildEvidence { result=summary.result.ToString(),unity=Application.unityVersion,platform="Windows x64",backend="Mono",version=PlayerSettings.bundleVersion,bytes=summary.totalSize,seconds=summary.totalTime.TotalSeconds,errors=summary.totalErrors,warnings=summary.totalWarnings },true));
            InspectSteps(report,"windows");
            if(summary.result!=BuildResult.Succeeded||summary.totalErrors>0) throw new Exception($"Build failed validation: {summary.totalErrors} errors.");
            Debug.Log($"WINDOWS_LAN_BUILD_OK: {summary.totalSize} bytes, {summary.totalTime}");
        }
        private static void RejectDuplicateTypeMetadata()
        {
            const string path="Library/BuildPlayerData";
            if(!Directory.Exists(path)) return;
            foreach(string file in Directory.EnumerateFiles(path,"*.json",SearchOption.AllDirectories))
                if(Regex.IsMatch(Path.GetFileName(file),@"^(TypeDb-All|RuntimeInitializeOnLoads) [1-9][0-9]*\.json$"))
                    throw new InvalidOperationException("Duplicate generated script metadata: "+file+". Close Unity and run build-demo.sh to regenerate Library/BuildPlayerData. Source assets are unaffected.");
        }
        private static void InspectSteps(BuildReport report,string platform)
        {
            var evidence=new StepEvidence();
            foreach(var step in report.steps) foreach(var message in step.messages) {
                if(message.type==LogType.Error||message.type==LogType.Exception||message.type==LogType.Assert) evidence.errors.Add(step.name+": "+message.content);
                if(message.type==LogType.Warning) evidence.warnings.Add(step.name+": "+message.content);
                evidence.messages++;
            }
            File.WriteAllText("../Evidence/effects/build-"+platform+"-steps.json",JsonUtility.ToJson(evidence,true));
            if(evidence.errors.Count>0) throw new Exception("Build steps contain "+evidence.errors.Count+" errors; inspect build evidence.");
        }
        [Serializable] private sealed class StepEvidence { public int messages; public List<string> errors=new(),warnings=new(); }
        [Serializable] private sealed class BuildEvidence { public string result,unity,platform,backend,version; public ulong bytes; public double seconds; public int errors,warnings; }
    }
}
