#if UNITY_IOS
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

namespace BG_Library.NET.AdCore.MainIOS
{
    public static class IOSKMPPostprocess
    {
        private const string FallbackIosDeploymentTarget = "15.0";
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
        private static readonly string[] DynamicPodFrameworkSubpaths =
        {
            "AppLovinSDK/AppLovinSDK.framework",
            "AdjustSignature/AdjustSigSdk.framework"
        };

        private static readonly string[] DynamicPodFrameworkEmbedNames =
        {
            "AppLovinSDK.xcframework in Embed Frameworks",
            "AdjustSigSdk.xcframework in Embed Frameworks"
        };

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
            RemoveExistingBuildPhase(projectPath, CopyComposeResourcesPhaseName);
            var project = new PBXProject();
            project.ReadFromFile(projectPath);
            bool isSimulatorExport = IsSimulatorExport(projectPath);
            string deploymentTarget = ResolveIosDeploymentTarget();

            ApplyIosOnlyBuildSettings(project, project.GetUnityMainTargetGuid(), deploymentTarget);
            ApplyIosOnlyBuildSettings(project, project.GetUnityFrameworkTargetGuid(), deploymentTarget);
            AddDynamicPodFrameworkEmbedPhase(project, projectPath, project.GetUnityMainTargetGuid());
            AddKmpComposeResourcesPhase(project, projectPath, project.GetUnityMainTargetGuid());

            string gameAssemblyTarget = project.TargetGuidByName("GameAssembly");
            if (!string.IsNullOrEmpty(gameAssemblyTarget))
                ApplyIosOnlyBuildSettings(project, gameAssemblyTarget, deploymentTarget);

            project.WriteToFile(projectPath);
            RemoveRedundantDynamicPodFrameworkEmbedPhase(projectPath);
            RemoveAllLoadLinkerFlag(projectPath);

            if (isSimulatorExport)
            {
                ReplaceGoogleMobileAdsUnityPluginForSimulator(buildPath);
                ReplaceFirebaseUnityPluginLibrariesForSimulator(buildPath);
            }
        }

        private static void ApplyIosOnlyBuildSettings(PBXProject project, string targetGuid, string deploymentTarget)
        {
            if (string.IsNullOrEmpty(targetGuid))
                return;

            project.SetBuildProperty(targetGuid, "IPHONEOS_DEPLOYMENT_TARGET", deploymentTarget);
            project.SetBuildProperty(targetGuid, "SUPPORTED_PLATFORMS", SupportedPlatforms);
            project.SetBuildProperty(targetGuid, "SUPPORTS_MACCATALYST", "NO");
            project.SetBuildProperty(targetGuid, "SUPPORTS_MAC_DESIGNED_FOR_IPHONE_IPAD", "NO");
        }

        private static string ResolveIosDeploymentTarget()
        {
            string configured = PlayerSettings.iOS.targetOSVersionString;
            return string.IsNullOrWhiteSpace(configured)
                ? FallbackIosDeploymentTarget
                : configured.Trim();
        }

        private static void AddDynamicPodFrameworkEmbedPhase(PBXProject project, string projectPath, string mainTargetGuid)
        {
            if (string.IsNullOrEmpty(mainTargetGuid))
                return;

            string[] frameworkSubpaths = ResolveDynamicPodFrameworksToEmbed(projectPath);
            if (frameworkSubpaths.Length == 0)
            {
                UnityEngine.Debug.Log(
                    "Skipped BG dynamic pod framework embed phase because Xcode Embed Frameworks already handles AppLovinSDK and AdjustSigSdk.");
                return;
            }

            if (File.Exists(projectPath) && File.ReadAllText(projectPath).Contains(EmbedDynamicPodsPhaseName))
                return;

            project.AddShellScriptBuildPhase(
                mainTargetGuid,
                EmbedDynamicPodsPhaseName,
                "/bin/sh",
                BuildDynamicPodFrameworkEmbedScript(frameworkSubpaths));
        }

        private static void AddKmpComposeResourcesPhase(PBXProject project, string projectPath, string mainTargetGuid)
        {
            if (string.IsNullOrEmpty(mainTargetGuid))
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

        private static string[] ResolveDynamicPodFrameworksToEmbed(string projectPath)
        {
            string projectText = File.Exists(projectPath) ? File.ReadAllText(projectPath) : string.Empty;
            var frameworkSubpaths = new List<string>(DynamicPodFrameworkSubpaths.Length);

            for (int i = 0; i < DynamicPodFrameworkSubpaths.Length; i++)
            {
                string embedName = i < DynamicPodFrameworkEmbedNames.Length ? DynamicPodFrameworkEmbedNames[i] : string.Empty;
                if (!string.IsNullOrEmpty(embedName) && projectText.Contains(embedName))
                    continue;

                frameworkSubpaths.Add(DynamicPodFrameworkSubpaths[i]);
            }

            return frameworkSubpaths.ToArray();
        }

        private static void RemoveRedundantDynamicPodFrameworkEmbedPhase(string projectPath)
        {
            if (!File.Exists(projectPath))
                return;

            string projectText = File.ReadAllText(projectPath);
            if (!projectText.Contains(EmbedDynamicPodsPhaseName) || !ProjectEmbedsAllDynamicPodFrameworks(projectText))
                return;

            string updatedProjectText = RemoveBuildPhaseReferenceLine(projectText, EmbedDynamicPodsPhaseName);
            updatedProjectText = RemoveBuildPhaseBlock(updatedProjectText, EmbedDynamicPodsPhaseName);
            if (updatedProjectText == projectText)
                return;

            File.WriteAllText(projectPath, updatedProjectText);
            UnityEngine.Debug.Log("Removed redundant BG dynamic pod framework embed phase from iOS Xcode project.");
        }

        private static bool ProjectEmbedsAllDynamicPodFrameworks(string projectText)
        {
            foreach (string embedName in DynamicPodFrameworkEmbedNames)
            {
                if (!projectText.Contains(embedName))
                    return false;
            }

            return true;
        }

        private static string RemoveBuildPhaseReferenceLine(string projectText, string phaseName)
        {
            string[] lines = projectText.Split('\n');
            var keptLines = new List<string>(lines.Length);
            string marker = $"/* {phaseName} */";

            foreach (string line in lines)
            {
                if (line.Contains(marker) && !line.Contains($"{marker} = {{"))
                    continue;

                keptLines.Add(line);
            }

            return string.Join("\n", keptLines);
        }

        private static string RemoveBuildPhaseBlock(string projectText, string phaseName)
        {
            string marker = $"/* {phaseName} */ = {{";
            int markerIndex = projectText.IndexOf(marker, StringComparison.Ordinal);
            if (markerIndex < 0)
                return projectText;

            int blockStart = projectText.LastIndexOf('\n', markerIndex);
            blockStart = blockStart < 0 ? markerIndex : blockStart + 1;

            string endMarker = "\n\t\t};";
            int blockEnd = projectText.IndexOf(endMarker, markerIndex, StringComparison.Ordinal);
            if (blockEnd < 0)
                return projectText;

            blockEnd += endMarker.Length;
            if (blockEnd < projectText.Length && projectText[blockEnd] == '\n')
                blockEnd++;

            return projectText.Remove(blockStart, blockEnd - blockStart);
        }

        private static string BuildDynamicPodFrameworkArguments(string[] frameworkSubpaths)
        {
            string arguments = string.Empty;
            foreach (string frameworkSubpath in frameworkSubpaths)
                arguments += $" \"{frameworkSubpath}\"";

            return arguments;
        }

        private static string BuildDynamicPodFrameworkEmbedScript(string[] frameworkSubpaths)
        {
            return string.Join("\n", new[]
            {
                "set -e",
                "",
                "BG_XCFRAMEWORKS_BUILD_DIR=\"${PODS_XCFRAMEWORKS_BUILD_DIR:-${BUILT_PRODUCTS_DIR}/XCFrameworkIntermediates}\"",
                "BG_FRAMEWORKS_DIR=\"${TARGET_BUILD_DIR}/${FRAMEWORKS_FOLDER_PATH}\"",
                "mkdir -p \"${BG_FRAMEWORKS_DIR}\"",
                "",
                "for BG_FRAMEWORK_SUBPATH in" + BuildDynamicPodFrameworkArguments(frameworkSubpaths) + "; do",
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
                "    \"${PROJECT_DIR}/Libraries/BG Lib/IOS-Core-Mediation/Assets/Plugins/iOS/Shared.xcframework\" \\",
                "    \"${PROJECT_DIR}/Frameworks/Plugins/iOS/Shared.xcframework\" \\",
                "    \"${PROJECT_DIR}/Libraries/Plugins/iOS/Shared.xcframework\" \\",
                "    \"${PROJECT_DIR}/Frameworks/AdsMultiplatform/Plugins/iOS/Shared.xcframework\" \\",
                "    \"${PROJECT_DIR}/Libraries/AdsMultiplatform/Plugins/iOS/Shared.xcframework\"; do",
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
                "BG_APP_RESOURCES=\"${TARGET_BUILD_DIR}/${UNLOCALIZED_RESOURCES_FOLDER_PATH}/compose-resources/composeResources\"",
                "BG_CTA_RESOURCE=\"adsmultiplatform.shared.generated.resources/drawable/bg_btn_bg005_02.png\"",
                "if [ ! -d \"${BG_SOURCE_RESOURCES}\" ]; then",
                "    echo \"${BG_SOURCE_RESOURCES} not found; skip KMP compose resources\"",
                "    exit 0",
                "fi",
                "",
                "copy_bg_compose_resources() {",
                "    BG_DEST_RESOURCES=\"$1\"",
                "    mkdir -p \"${BG_DEST_RESOURCES}\"",
                "    echo \"Copying KMP compose resources from ${BG_SOURCE_RESOURCES} to ${BG_DEST_RESOURCES}\"",
                "    rsync -a --delete \"${BG_SOURCE_RESOURCES}/\" \"${BG_DEST_RESOURCES}/\"",
                "}",
                "",
                "copy_bg_compose_resources \"${BG_APP_RESOURCES}\"",
                "for BG_FRAMEWORK_RESOURCES in \\",
                "    \"${TARGET_BUILD_DIR}/${FRAMEWORKS_FOLDER_PATH}/Shared.framework/composeResources\" \\",
                "    \"${TARGET_BUILD_DIR}/${UNLOCALIZED_RESOURCES_FOLDER_PATH}/Frameworks/Shared.framework/composeResources\" \\",
                "    \"${BUILT_PRODUCTS_DIR}/Shared.framework/composeResources\"; do",
                "    BG_FRAMEWORK_DIR=\"$(dirname \"${BG_FRAMEWORK_RESOURCES}\")\"",
                "    if [ -d \"${BG_FRAMEWORK_DIR}\" ]; then",
                "        copy_bg_compose_resources \"${BG_FRAMEWORK_RESOURCES}\"",
                "    fi",
                "done",
                "",
                "if [ ! -f \"${BG_APP_RESOURCES}/${BG_CTA_RESOURCE}\" ]; then",
                "    echo \"Required KMP CTA drawable missing after copy: ${BG_APP_RESOURCES}/${BG_CTA_RESOURCE}\"",
                "    exit 1",
                "fi",
                "",
                "echo \"Verified KMP CTA drawable: ${BG_APP_RESOURCES}/${BG_CTA_RESOURCE}\"",
                ""
            });
        }

        private static void RemoveExistingBuildPhase(string projectPath, string phaseName)
        {
            if (!File.Exists(projectPath))
                return;

            string projectText = File.ReadAllText(projectPath);
            if (!projectText.Contains(phaseName))
                return;

            string updatedProjectText = RemoveBuildPhaseReferenceLine(projectText, phaseName);
            updatedProjectText = RemoveBuildPhaseBlock(updatedProjectText, phaseName);
            if (updatedProjectText == projectText)
                return;

            File.WriteAllText(projectPath, updatedProjectText);
            UnityEngine.Debug.Log($"Removed stale {phaseName} build phase from iOS Xcode project.");
        }
    }
}
#endif
