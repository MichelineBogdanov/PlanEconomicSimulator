using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace PlanEconomicSimulator.Editor
{
    public static class ProjectBuild
    {
        public const string ScenePath = "Assets/Game/Scenes/Bootstrap.unity";

        public static void Configure()
        {
            PlayerSettings.companyName = "MichelineBogdanov";
            PlayerSettings.productName = "PlanEconomicSimulator";
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            PlayerSettings.SetApiCompatibilityLevel(NamedBuildTarget.Standalone, ApiCompatibilityLevel.NET_Standard);
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            EditorSettings.serializationMode = SerializationMode.ForceText;

            if (!File.Exists(ScenePath))
            {
                Directory.CreateDirectory("Assets/Game/Scenes");
                var panel = ScriptableObject.CreateInstance<PanelSettings>();
                panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                panel.referenceResolution = new Vector2Int(1920, 1080);
                panel.themeStyleSheet = RequireAsset<ThemeStyleSheet>("Assets/Game/UI/Office.tss");
                AssetDatabase.CreateAsset(panel, "Assets/Game/UI/OfficePanel.asset");
                AssetDatabase.SaveAssets();
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var camera = new GameObject("Main Camera").AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color32(233, 223, 199, 255);
                camera.orthographic = true;
                var document = new GameObject("Planning Office").AddComponent<UIDocument>();
                document.visualTreeAsset = RequireAsset<VisualTreeAsset>("Assets/Game/UI/PlanningOffice.uxml");
                document.panelSettings = RequireAsset<PanelSettings>("Assets/Game/UI/OfficePanel.asset");
                EditorUtility.SetDirty(document);
                if (!EditorSceneManager.SaveScene(scene, ScenePath))
                    throw new InvalidOperationException("Could not save the bootstrap scene.");
            }

            var bootstrapScene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var bootstrapDocument = UnityEngine.Object.FindFirstObjectByType<UIDocument>();
            if (bootstrapDocument == null)
                throw new InvalidOperationException("The bootstrap scene has no UI document.");
            bootstrapDocument.panelSettings = RequireAsset<PanelSettings>("Assets/Game/UI/OfficePanel.asset");
            EditorUtility.SetDirty(bootstrapDocument);
            EditorSceneManager.MarkSceneDirty(bootstrapScene);
            if (!EditorSceneManager.SaveScene(bootstrapScene))
                throw new InvalidOperationException("Could not save the bootstrap panel reference.");

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
        }

        public static void BuildWindows()
        {
            var scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
            if (scenes.Length == 0 || scenes.Any(scene => !File.Exists(scene)))
                throw new InvalidOperationException("A build requires existing enabled scenes.");

            Directory.CreateDirectory("Builds/Windows");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = "Builds/Windows/PlanEconomicSimulator.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });

            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException($"Windows build failed: {report.summary.result}, errors: {report.summary.totalErrors}.");
        }

        private static T RequireAsset<T>(string path) where T : UnityEngine.Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(path)
                ?? throw new InvalidOperationException($"Required asset could not be imported: {path}.");
        }
    }
}
