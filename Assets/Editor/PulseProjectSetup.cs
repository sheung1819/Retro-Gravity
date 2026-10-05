using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Pulse.EditorTools
{
    /// <summary>
    /// First-open setup: creates Assets/Scenes/Main.unity (camera + GameBootstrap),
    /// registers it in Build Settings, and applies mobile Player Settings for
    /// Android and iOS. Safe to re-run from the "Pulse" menu.
    /// </summary>
    [InitializeOnLoad]
    public static class PulseProjectSetup
    {
        public const string ScenePath = "Assets/Scenes/Main.unity";
        public const string ProductName = "Pulse";
        public const string CompanyName = "Retro Gravity";
        public const string BundleId = "com.retrogravity.pulse";
        public const string Version = "0.1.0";

        static PulseProjectSetup()
        {
            EditorApplication.delayCall += AutoSetup;
        }

        private static void AutoSetup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || Application.isBatchMode) return;

            if (!File.Exists(ScenePath))
            {
                CreateMainScene(openIt: true);
                ApplyPlayerSettings();
                Debug.Log("[Pulse] Project set up: created " + ScenePath + " and applied mobile Player Settings. Press Play.");
            }
            EnsureSceneInBuildSettings();
        }

        [MenuItem("Pulse/Setup/Recreate Main Scene")]
        public static void RecreateMainSceneMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            CreateMainScene(openIt: true);
            EnsureSceneInBuildSettings();
        }

        [MenuItem("Pulse/Setup/Apply Player Settings")]
        public static void ApplyPlayerSettingsMenu()
        {
            ApplyPlayerSettings();
            Debug.Log("[Pulse] Player Settings applied for Android + iOS.");
        }

        public static void CreateMainScene(bool openIt)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 8f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.012f, 0.06f);
            camGo.transform.position = new Vector3(0f, 0f, -10f);
            camGo.AddComponent<AudioListener>();

            new GameObject("Pulse").AddComponent<GameBootstrap>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
            if (openIt) EditorSceneManager.OpenScene(ScenePath);
        }

        public static void EnsureSceneInBuildSettings()
        {
            if (!File.Exists(ScenePath)) return;
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(s => s.path == ScenePath && s.enabled)) return;
            scenes.RemoveAll(s => s.path == ScenePath);
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        public static void ApplyPlayerSettings()
        {
            PlayerSettings.productName = ProductName;
            PlayerSettings.companyName = CompanyName;
            PlayerSettings.bundleVersion = Version;

            // Portrait-only arcade game.
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.statusBarHidden = true;
            PlayerSettings.runInBackground = false;

            // Android — IL2CPP + ARM64 is required for Google Play.
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, BundleId);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel23;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            if (PlayerSettings.Android.bundleVersionCode < 1) PlayerSettings.Android.bundleVersionCode = 1;

            // iOS
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, BundleId);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.iOS.targetOSVersionString = "13.0";
            PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneAndiPad;
            PlayerSettings.iOS.requiresFullScreen = true;
            PlayerSettings.iOS.hideHomeButton = false;
            if (string.IsNullOrEmpty(PlayerSettings.iOS.buildNumber) || PlayerSettings.iOS.buildNumber == "0")
                PlayerSettings.iOS.buildNumber = "1";

            AssetDatabase.SaveAssets();
        }
    }
}
