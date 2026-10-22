using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace Checkpoint.Editor
{
    [InitializeOnLoad]
    public static class CheckpointBootstrap
    {
        public const string ScenePath = "Assets/Checkpoint/Scenes/Checkpoint.unity";
        const string PanelPath = "Assets/Checkpoint/Resources/Checkpoint/CheckpointPanel.asset";
        static CheckpointBootstrap()
        {
            EditorApplication.delayCall += () => { if (!EditorApplication.isPlayingOrWillChangePlaymode) EnsureAssets(); };
        }
        [MenuItem("Checkpoint/1. Create or repair scene assets")]
        public static void EnsureAssets()
        {
            Directory.CreateDirectory("Assets/Checkpoint/Scenes");
            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelPath);
            if (panel == null)
            {
                panel = ScriptableObject.CreateInstance<PanelSettings>();
                panel.name = "CheckpointPanel";
                panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                panel.referenceResolution = new Vector2Int(1600, 900);
                panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
                panel.match = .5f;
                panel.clearColor = true;
                panel.colorClearValue = new Color(.06f, .14f, .16f);
                panel.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>("Assets/Checkpoint/Resources/Checkpoint/CheckpointTheme.tss");
                AssetDatabase.CreateAsset(panel, PanelPath);
            }
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                var previous = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                bool untitled = string.IsNullOrEmpty(previous.path);
                if (untitled && !Application.isBatchMode)
                { Debug.Log("Checkpoint: save or close the untitled scene, then use Checkpoint > Create or repair scene assets."); return; }
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, untitled ? NewSceneMode.Single : NewSceneMode.Additive);
                UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
                var go = new GameObject("Checkpoint Runtime");
                var doc = go.AddComponent<UIDocument>();
                doc.panelSettings = panel;
                go.AddComponent<GameManager>();
                var cameraGo = new GameObject("Checkpoint Camera");
                var camera = cameraGo.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.06f,.14f,.16f);
                camera.orthographic = true;
                cameraGo.transform.position = new Vector3(0,0,-10);
                cameraGo.AddComponent<AudioListener>();
                EditorSceneManager.SaveScene(scene, ScenePath);
                if (!untitled)
                {
                    if (previous.IsValid()) UnityEngine.SceneManagement.SceneManager.SetActiveScene(previous);
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            if (!EditorBuildSettings.scenes.Select(s => s.path).SequenceEqual(scenes.Select(s => s.path)))
                EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
        }
        [MenuItem("Checkpoint/2. Open starting scene")]
        public static void OpenScene()
        {
            EnsureAssets();
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(ScenePath);
        }
        [MenuItem("Checkpoint/3. Build Windows prototype")]
        public static void BuildWindows()
        {
            EnsureAssets();
            CaseRepository.Load();
            PlayerSettings.companyName = "Merrow Studio";
            PlayerSettings.productName = "Not Just Accept, But Deny";
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            Directory.CreateDirectory("Builds/Windows");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "Builds/Windows/NotJustAcceptButDeny.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Windows build failed: " + report.summary.result);
            Debug.Log("CHECKPOINT_BUILD_SUCCEEDED " + report.summary.totalSize + " bytes");
        }
    }
}
