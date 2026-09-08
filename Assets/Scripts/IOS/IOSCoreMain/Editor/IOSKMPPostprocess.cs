#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

namespace BG_Library.NET.AdCore.MainIOS
{
    public static class IOSKMPPostprocess
    {
        private const string IosDeploymentTarget = "18.5";
        private const string SupportedPlatforms = "iphoneos iphonesimulator";
        private const string EmbedDynamicPodsPhaseName = "BG Embed iOS Dynamic Pod Frameworks";
        private const string CopyComposeResourcesPhaseName = "BG Copy KMP Compose Resources";
        private const string GoogleMobileAdsUnityPluginLibraryPath = "Libraries/Plugins/iOS/unity-plugin-library.a";
        private const string SimulatorGoogleMobileAdsUnityPluginAssetPath =
            "BG Lib/IOS-Core-Mediation/Assets/Plugins/iOS/Simulator/unity-plugin-library-arm64-simulator.a.bytes";
        private const string FirebaseUnityPluginLibraryDirectoryPath = "Libraries/Plugins/iOS/Firebase";
        private const string SimulatorFirebaseUnityPluginAssetDirectoryPath =
            "BG Lib/IOS-Core-Mediation/Assets/Plugins/iOS/Simulator/Firebase";
        private const string AllLoadDynamicLookupFlag = "-Wl,-undefined,dynamic_lookup,-all_load";
        private const string DynamicLookupFlag = "-Wl,-undefined,dynamic_lookup";
        private static readonly string[] FirebaseUnityPluginLibraries =
        {
            "libFirebaseCppApp.a",
            "libFirebaseCppAnalytics.a",
            "libFirebaseCppRemoteConfig.a"
        };

        [PostProcessBuild(101)]
        public static void ApplyKMPBuildSettings(BuildTarget target, string buildPath)
        {
            if (target != BuildTarget.iOS)
                return;

            string plistPath = Path.Combine(buildPath, "Info.plist");
            if (File.Exists(plistPath))
            {
                var plist = new PlistDocument();
                plist.ReadFromFile(plistPath);
                plist.root.SetBoolean("CADisableMinimumFrameDurationOnPhone", true);
                plist.WriteToFile(plistPath);
            }

            string projectPath = PBXProject.GetPBXProjectPath(buildPath);
            var project = new PBXProject();
            project.ReadFromFile(projectPath);
            bool isSimulatorExport = IsSimulatorExport(projectPath);

            ApplyIosOnlyBuildSettings(project, project.GetUnityMainTargetGuid());
            ApplyIosOnlyBuildSettings(project, project.GetUnityFrameworkTargetGuid());
            AddDynamicPodFrameworkEmbedPhase(project, projectPath, project.GetUnityMainTargetGuid());
            AddKmpComposeResourcesPhase(project, projectPath, project.GetUnityMainTargetGuid());

            string gameAssemblyTarget = project.TargetGuidByName("GameAssembly");
            if (!string.IsNullOrEmpty(gameAssemblyTarget))
                ApplyIosOnlyBuildSettings(project, gameAssemblyTarget);

            project.WriteToFile(projectPath);
            RemoveAllLoadLinkerFlag(projectPath);

            if (isSimulatorExport)
            {
                ReplaceGoogleMobileAdsUnityPluginForSimulator(buildPath);
                ReplaceFirebaseUnityPluginLibrariesForSimulator(buildPath);
            }
        }

        private static void ApplyIosOnlyBuildSettings(PBXProject project, string targetGuid)
        {
            if (string.IsNullOrEmpty(targetGuid))
                return;

            project.SetBuildProperty(targetGuid, "IPHONEOS_DEPLOYMENT_TARGET", IosDeploymentTarget);
            project.SetBuildProperty(targetGuid, "SUPPORTED_PLATFORMS", SupportedPlatforms);
            project.SetBuildProperty(targetGuid, "SUPPORTS_MACCATALYST", "NO");
            project.SetBuildProperty(targetGuid, "SUPPORTS_MAC_DESIGNED_FOR_IPHONE_IPAD", "NO");
        }

        private static void AddDynamicPodFrameworkEmbedPhase(PBXProject project, string projectPath, string mainTargetGuid)
        {
            if (string.IsNullOrEmpty(mainTargetGuid))
                return;

            if (File.Exists(projectPath) && File.ReadAllText(projectPath).Contains(EmbedDynamicPodsPhaseName))
                return;

            project.AddShellScriptBuildPhase(
                mainTargetGuid,
                EmbedDynamicPodsPhaseName,
                "/bin/sh",
                BuildDynamicPodFrameworkEmbedScript());
        }

        private static void AddKmpComposeResourcesPhase(PBXProject project, string projectPath, string mainTargetGuid)
        {
            if (string.IsNullOrEmpty(mainTargetGuid))
                return;

            if (File.Exists(projectPath) && File.ReadAllText(projectPath).Contains(CopyComposeResourcesPhaseName))
                return;

            project.AddShellScriptBuildPhase(
                mainTargetGuid,
                CopyComposeResourcesPhaseName,
                "/bin/sh",
                BuildKmpComposeResourcesScript());
        }

        private static bool IsSimulatorExport(string projectPath)
        {
            if (!File.Exists(projectPath))
                return false;

            return File.ReadAllText(projectPath).Contains("SUPPORTED_PLATFORMS = iphonesimulator");
        }

        private static void ReplaceGoogleMobileAdsUnityPluginForSimulator(string buildPath)
        {
            string sourcePath = Path.Combine(UnityEngine.Application.dataPath, SimulatorGoogleMobileAdsUnityPluginAssetPath);
            string destinationPath = Path.Combine(buildPath, GoogleMobileAdsUnityPluginLibraryPath);

            if (!File.Exists(sourcePath))
            {
                UnityEngine.Debug.LogWarning(
                    $"Missing simulator Google Mobile Ads Unity plugin library at {sourcePath}.");
                return;
            }

            if (!File.Exists(destinationPath))
            {
                UnityEngine.Debug.LogWarning(
                    $"Missing exported Google Mobile Ads Unity plugin library at {destinationPath}.");
                return;
            }

            File.Copy(sourcePath, destinationPath, true);
            UnityEngine.Debug.Log(
                $"Replaced Google Mobile Ads Unity plugin library with arm64 simulator build: {destinationPath}");
        }

        private static void ReplaceFirebaseUnityPluginLibrariesForSimulator(string buildPath)
        {
            foreach (string libraryName in FirebaseUnityPluginLibraries)
            {
                string sourcePath = Path.Combine(
                    UnityEngine.Application.dataPath,
                    SimulatorFirebaseUnityPluginAssetDirectoryPath,
                    $"{libraryName}.bytes");
                string destinationPath = Path.Combine(
                    buildPath,
                    FirebaseUnityPluginLibraryDirectoryPath,
                    libraryName);

                if (!File.Exists(sourcePath))
                {
                    UnityEngine.Debug.LogWarning($"Missing simulator Firebase Unity plugin library at {sourcePath}.");
                    continue;
                }

                if (!File.Exists(destinationPath))
                {
                    UnityEngine.Debug.LogWarning($"Missing exported Firebase Unity plugin library at {destinationPath}.");
                    continue;
                }

                File.Copy(sourcePath, destinationPath, true);
                UnityEngine.Debug.Log($"Replaced Firebase Unity plugin library with arm64 simulator build: {destinationPath}");
            }
        }

        private static void RemoveAllLoadLinkerFlag(string projectPath)
        {
            if (!File.Exists(projectPath))
                return;

            string projectText = File.ReadAllText(projectPath);
            string updatedProjectText = projectText.Replace(AllLoadDynamicLookupFlag, DynamicLookupFlag);

            if (updatedProjectText == projectText)
                return;

            File.WriteAllText(projectPath, updatedProjectText);
            UnityEngine.Debug.Log("Removed -all_load from iOS linker flags to avoid static framework duplicate symbols.");
        }

        private static string BuildDynamicPodFrameworkEmbedScript()
        {
            return string.Join("\n", new[]
            {
                "set -e",
                "",
                "BG_XCFRAMEWORKS_BUILD_DIR=\"${PODS_XCFRAMEWORKS_BUILD_DIR:-${BUILT_PRODUCTS_DIR}/XCFrameworkIntermediates}\"",
                "BG_FRAMEWORKS_DIR=\"${TARGET_BUILD_DIR}/${FRAMEWORKS_FOLDER_PATH}\"",
                "mkdir -p \"${BG_FRAMEWORKS_DIR}\"",
                "",
                "for BG_FRAMEWORK_SUBPATH in \"AppLovinSDK/AppLovinSDK.framework\" \"AdjustSignature/AdjustSigSdk.framework\"; do",
                "    BG_SOURCE_FRAMEWORK=\"${BG_XCFRAMEWORKS_BUILD_DIR}/${BG_FRAMEWORK_SUBPATH}\"",
                "    if [ ! -d \"${BG_SOURCE_FRAMEWORK}\" ]; then",
                "        echo \"${BG_FRAMEWORK_SUBPATH} not found; skip embed\"",
                "        continue",
                "    fi",
                "",
                "    BG_DEST_FRAMEWORK=\"${BG_FRAMEWORKS_DIR}/$(basename \"${BG_SOURCE_FRAMEWORK}\")\"",
                "    echo \"Embedding ${BG_SOURCE_FRAMEWORK}\"",
                "    rsync -av --delete --exclude \"Headers\" --exclude \"PrivateHeaders\" \"${BG_SOURCE_FRAMEWORK}\" \"${BG_FRAMEWORKS_DIR}/\"",
                "",
                "    if [ \"${CODE_SIGNING_ALLOWED:-NO}\" = \"YES\" ] && [ -n \"${EXPANDED_CODE_SIGN_IDENTITY:-}\" ]; then",
                "        /usr/bin/codesign --force --sign \"${EXPANDED_CODE_SIGN_IDENTITY}\" --preserve-metadata=identifier,entitlements,flags --timestamp=none \"${BG_DEST_FRAMEWORK}\"",
                "    fi",
                "done",
                ""
            });
        }

        private static string BuildKmpComposeResourcesScript()
        {
            return string.Join("\n", new[]
            {
                "set -e",
                "",
                "BG_SHARED_XCFRAMEWORK=\"\"",
                "for BG_CANDIDATE in \\",
                "    \"${PROJECT_DIR}/Frameworks/BG Lib/IOS-Core-Mediation/Assets/Plugins/iOS/Shared.xcframework\" \\",
                "    \"${PROJECT_DIR}/Frameworks/Plugins/iOS/Shared.xcframework\" \\",
                "    \"${PROJECT_DIR}/Frameworks/AdsMultiplatform/Plugins/iOS/Shared.xcframework\"; do",
                "    if [ -d \"${BG_CANDIDATE}\" ]; then",
                "        BG_SHARED_XCFRAMEWORK=\"${BG_CANDIDATE}\"",
                "        break",
                "    fi",
                "done",
                "",
                "if [ -z \"${BG_SHARED_XCFRAMEWORK}\" ]; then",
                "    echo \"Shared.xcframework not found; skip KMP compose resources\"",
                "    exit 0",
                "fi",
                "",
                "if [[ \"${PLATFORM_NAME}\" == *simulator* ]]; then",
                "    BG_SHARED_IDENTIFIER=\"ios-arm64-simulator\"",
                "else",
                "    BG_SHARED_IDENTIFIER=\"ios-arm64\"",
                "fi",
                "",
                "BG_SOURCE_RESOURCES=\"${BG_SHARED_XCFRAMEWORK}/${BG_SHARED_IDENTIFIER}/Shared.framework/composeResources\"",
                "BG_DEST_RESOURCES=\"${TARGET_BUILD_DIR}/${UNLOCALIZED_RESOURCES_FOLDER_PATH}/compose-resources/composeResources\"",
                "if [ ! -d \"${BG_SOURCE_RESOURCES}\" ]; then",
                "    echo \"${BG_SOURCE_RESOURCES} not found; skip KMP compose resources\"",
                "    exit 0",
                "fi",
                "",
                "mkdir -p \"${BG_DEST_RESOURCES}\"",
                "echo \"Copying KMP compose resources from ${BG_SOURCE_RESOURCES}\"",
                "rsync -a --delete \"${BG_SOURCE_RESOURCES}/\" \"${BG_DEST_RESOURCES}/\"",
                ""
            });
        }
    }
}
#endif
