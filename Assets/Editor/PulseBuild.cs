using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Pulse.EditorTools
{
    /// <summary>
    /// One-click / command-line builds for Android and iOS.
    ///
    /// Editor: menu Pulse ▸ Build ▸ …
    /// CLI:    Unity -batchmode -quit -projectPath . -executeMethod Pulse.EditorTools.PulseBuild.AndroidAab
    ///         Unity -batchmode -quit -projectPath . -executeMethod Pulse.EditorTools.PulseBuild.IOS
    ///
    /// Android release signing reads these environment variables (never commit keystores):
    ///   PULSE_KEYSTORE_PATH, PULSE_KEYSTORE_PASS, PULSE_KEY_ALIAS, PULSE_KEY_PASS
    /// iOS signing (optional): PULSE_APPLE_TEAM_ID enables automatic signing with that team.
    /// Build numbers (optional, e.g. from CI): PULSE_BUILD_NUMBER.
    /// </summary>
    public static class PulseBuild
    {
        private const string OutputRoot = "Builds";

        [MenuItem("Pulse/Build/Android APK (development)")]
        public static void AndroidApkDev() => BuildAndroid(appBundle: false, development: true);

        [MenuItem("Pulse/Build/Android APK (release)")]
        public static void AndroidApk() => BuildAndroid(appBundle: false, development: false);

        [MenuItem("Pulse/Build/Android App Bundle (Google Play)")]
        public static void AndroidAab() => BuildAndroid(appBundle: true, development: false);

        [MenuItem("Pulse/Build/iOS Xcode Project")]
        public static void IOS() => BuildIOS(development: false);

        [MenuItem("Pulse/Build/iOS Xcode Project (development)")]
        public static void IOSDev() => BuildIOS(development: true);

        private static void BuildAndroid(bool appBundle, bool development)
        {
            Prepare();
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            EditorUserBuildSettings.buildAppBundle = appBundle;

            ApplyBuildNumber();
            bool signed = ConfigureAndroidSigning();
            if (!signed && !development)
                Debug.LogWarning("[Pulse] No PULSE_KEYSTORE_* env vars: building with the debug keystore. " +
                                 "Google Play will reject debug-signed uploads.");

            string file = appBundle ? "Pulse.aab" : (development ? "Pulse-dev.apk" : "Pulse.apk");
            Run(BuildTarget.Android, Path.Combine(OutputRoot, "Android", file), development);
        }

        private static void BuildIOS(bool development)
        {
            Prepare();
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS);

            ApplyBuildNumber();
            string team = Environment.GetEnvironmentVariable("PULSE_APPLE_TEAM_ID");
            if (!string.IsNullOrEmpty(team))
            {
                PlayerSettings.iOS.appleDeveloperTeamID = team;
                PlayerSettings.iOS.appleEnableAutomaticSigning = true;
            }

            // Produces an Xcode project; open it on a Mac, then Product ▸ Archive to ship.
            Run(BuildTarget.iOS, Path.Combine(OutputRoot, "iOS"), development);
        }

        private static void Prepare()
        {
            if (!File.Exists(PulseProjectSetup.ScenePath)) PulseProjectSetup.CreateMainScene(openIt: false);
            PulseProjectSetup.EnsureSceneInBuildSettings();
            PulseProjectSetup.ApplyPlayerSettings();
        }

        private static void ApplyBuildNumber()
        {
            string n = Environment.GetEnvironmentVariable("PULSE_BUILD_NUMBER");
            if (int.TryParse(n, out int build) && build > 0)
            {
                PlayerSettings.Android.bundleVersionCode = build;
                PlayerSettings.iOS.buildNumber = build.ToString();
            }
        }

        private static bool ConfigureAndroidSigning()
        {
            string path = Environment.GetEnvironmentVariable("PULSE_KEYSTORE_PATH");
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                PlayerSettings.Android.useCustomKeystore = false;
                return false;
            }
            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = path;
            PlayerSettings.Android.keystorePass = Environment.GetEnvironmentVariable("PULSE_KEYSTORE_PASS");
            PlayerSettings.Android.keyaliasName = Environment.GetEnvironmentVariable("PULSE_KEY_ALIAS");
            PlayerSettings.Android.keyaliasPass = Environment.GetEnvironmentVariable("PULSE_KEY_PASS");
            return true;
        }

        private static void Run(BuildTarget target, string location, bool development)
        {
            Directory.CreateDirectory(target == BuildTarget.iOS ? location : Path.GetDirectoryName(location));

            var options = new BuildPlayerOptions
            {
                scenes = new[] { PulseProjectSetup.ScenePath },
                locationPathName = location,
                target = target,
                options = development ? BuildOptions.Development : BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[Pulse] {target} build succeeded: {location} ({summary.totalSize / (1024f * 1024f):F1} MB, {summary.totalTime.TotalSeconds:F0}s)");
                if (!Application.isBatchMode) EditorUtility.RevealInFinder(location);
            }
            else
            {
                Debug.LogError($"[Pulse] {target} build {summary.result} with {summary.totalErrors} error(s).");
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }
    }
}
