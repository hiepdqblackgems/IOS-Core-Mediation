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
        private const string SupportedPlatforms = "iphoneos";
        private const string EmbedDynamicPodsPhaseName = "BG Embed iOS Dynamic Pod Frameworks";

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

            ApplyIosOnlyBuildSettings(project, project.GetUnityMainTargetGuid());
            ApplyIosOnlyBuildSettings(project, project.GetUnityFrameworkTargetGuid());
            AddDynamicPodFrameworkEmbedPhase(project, projectPath, project.GetUnityMainTargetGuid());

            string gameAssemblyTarget = project.TargetGuidByName("GameAssembly");
            if (!string.IsNullOrEmpty(gameAssemblyTarget))
                ApplyIosOnlyBuildSettings(project, gameAssemblyTarget);

            project.WriteToFile(projectPath);
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
    }
}
#endif
