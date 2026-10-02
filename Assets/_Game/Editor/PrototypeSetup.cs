using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Starfall.Editor
{
    public static class PrototypeSetup
    {
        public const string EntryScene = "Assets/_Game/Scenes/Boot.unity";
        [MenuItem("Starfall/Setup M2b")]
        public static void SetupM2b()
        {
            SetupM2a();
            if (AssetDatabase.LoadAssetAtPath<FirstLevelConfig>("Assets/_Game/Resources/FirstLevelConfig.asset") == null)
                AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<FirstLevelConfig>(), "Assets/_Game/Resources/FirstLevelConfig.asset");
            EditorUtility.SetDirty(AssetDatabase.LoadAssetAtPath<FirstLevelConfig>("Assets/_Game/Resources/FirstLevelConfig.asset"));
            AssetDatabase.SaveAssets(); ValidateM2b(); Debug.Log("STARFALL_M2B_SETUP_OK");
        }
        [MenuItem("Starfall/Validate M2b Assets")]
        public static void ValidateM2b()
        {
            Validate();
            var config = AssetDatabase.LoadAssetAtPath<FirstLevelConfig>("Assets/_Game/Resources/FirstLevelConfig.asset");
            if (config == null || config.bossHealth <= 0 || config.bossWarning <= 0 || config.bossRecovery <= 0 || config.challengeSeconds <= 0 || string.IsNullOrEmpty(config.timingVersion) || string.IsNullOrEmpty(config.balanceVersion))
                throw new System.InvalidOperationException("Invalid first-level config");
            var items = AssetDatabase.LoadAssetAtPath<ItemCatalog>("Assets/_Game/Resources/ItemCatalog.asset");
            if (items == null || items.items == null || items.items.Select(item => item.id).Distinct().Count() != items.items.Length)
                throw new System.InvalidOperationException("Missing or duplicate item definitions");
            Debug.Log("STARFALL_M2B_ASSETS_OK");
        }
        [MenuItem("Starfall/Build M2b Windows")]
        public static void BuildM2bWindows()
        {
            SetupM2b();
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { EntryScene }, locationPathName = "Builds/M2b/StarcoreLabyrinth.exe", target = BuildTarget.StandaloneWindows64, options = BuildOptions.None });
            if (report.summary.result != BuildResult.Succeeded) throw new System.InvalidOperationException("M2b Windows build failed: " + report.summary.result);
            Debug.Log("STARFALL_M2B_BUILD_OK: " + report.summary.totalSize + " bytes, " + report.summary.totalErrors + " errors");
        }
        [MenuItem("Starfall/Setup M2a")]
        public static void SetupM2a()
        {
            Setup();
            if (AssetDatabase.LoadAssetAtPath<ItemCatalog>("Assets/_Game/Resources/ItemCatalog.asset") == null)
            {
                var catalog = ItemCatalog.Defaults();
                var previous = AssetDatabase.LoadAssetAtPath<PrototypeConfig>("Assets/_Game/Resources/PrototypeConfig.asset");
                catalog.Find("pistol").damage = previous.bulletDamage; catalog.Find("pistol").interval = previous.shotInterval;
                AssetDatabase.CreateAsset(catalog, "Assets/_Game/Resources/ItemCatalog.asset");
            }
            AssetDatabase.SaveAssets(); Validate(); Debug.Log("STARFALL_M2A_SETUP_OK");
        }
        [MenuItem("Starfall/Setup M1 Prototype")]
        public static void Setup()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory("Assets/_Game/Scenes"); AssetDatabase.Refresh();
            var config = AssetDatabase.LoadAssetAtPath<PrototypeConfig>("Assets/_Game/Resources/PrototypeConfig.asset");
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<PrototypeConfig>(); AssetDatabase.CreateAsset(config, "Assets/_Game/Resources/PrototypeConfig.asset");
            }
            if (AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Resources/PrototypeSprite.mat") == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                if (shader == null) shader = Shader.Find("Sprites/Default");
                if (shader == null) throw new System.InvalidOperationException("Missing sprite shader");
                AssetDatabase.CreateAsset(new Material(shader), "Assets/_Game/Resources/PrototypeSprite.mat");
            }
            if (!File.Exists(EntryScene))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var root = new GameObject("Starcore M1 Bootstrap"); root.AddComponent<StarfallGame>().config = config;
                EditorSceneManager.SaveScene(scene, EntryScene);
            }
            // Existing authored scenes and config assets are never rebuilt or overwritten.
            var previous = EditorBuildSettings.scenes.Where(s => s.path != EntryScene);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(EntryScene, true) }.Concat(previous).ToArray();
            AssetDatabase.SaveAssets(); EditorSceneManager.OpenScene(EntryScene);
            Validate(); Debug.Log("STARFALL_M1_SETUP_OK: " + EntryScene);
        }
        [MenuItem("Starfall/Validate M1 Assets")]
        public static void Validate()
        {
            var locale = new LocalizationService("en"); var errors = locale.Validate();
            if (errors.Count > 0) throw new System.InvalidOperationException(string.Join("\n", errors));
            if (AssetDatabase.LoadAssetAtPath<PrototypeConfig>("Assets/_Game/Resources/PrototypeConfig.asset") == null)
                throw new System.InvalidOperationException("Missing prototype config");
            if (File.Exists(EntryScene))
            {
                var scene = EditorSceneManager.OpenScene(EntryScene);
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var child in root.GetComponentsInChildren<Transform>(true))
                        if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) != 0)
                            throw new System.InvalidOperationException("Missing scene script: " + child.name);
                var bootstrap = Object.FindFirstObjectByType<StarfallGame>();
                if (bootstrap == null || bootstrap.config == null) throw new System.InvalidOperationException("Missing bootstrap config reference");
            }
            Debug.Log("STARFALL_M1_ASSETS_OK");
        }
        [MenuItem("Starfall/Build M2a Windows")]
        public static void BuildM2aWindows()
        {
            SetupM2a();
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { EntryScene }, locationPathName = "Builds/M2a/StarcoreLabyrinth.exe",
                target = BuildTarget.StandaloneWindows64, options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded) throw new System.InvalidOperationException("M2a Windows build failed: " + report.summary.result);
            Debug.Log("STARFALL_M2A_BUILD_OK: " + report.summary.totalSize + " bytes, " + report.summary.totalErrors + " errors");
        }
        [MenuItem("Starfall/Build M1 Windows")]
        public static void BuildWindows()
        {
            Setup();
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { EntryScene }, locationPathName = "Builds/M1/StarcoreLabyrinth.exe",
                target = BuildTarget.StandaloneWindows64, options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded) throw new System.InvalidOperationException("M1 Windows build failed: " + report.summary.result);
            Debug.Log("STARFALL_M1_BUILD_OK: " + report.summary.totalSize + " bytes, " + report.summary.totalErrors + " errors");
        }
    }
}
