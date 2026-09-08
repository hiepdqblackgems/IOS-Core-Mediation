# IOS-Core-Mediation

Version: 1.0.0

## Overview
`IOS-Core-Mediation` is the Unity iOS package that mirrors the existing Android CoreMain and Android Mediation layout for the AdsMultiplatform KMP flow.

It contains:
- Unity C# CoreMain entry points for iOS ad configuration and initialization.
- Unity C# iOS mediation controllers for fullscreen, banner native, and popup native ads.
- Objective-C++ bridge code that exposes Unity-callable native functions.
- The KMP `Shared.xcframework` build artifact used by the Objective-C++ bridge.

## Android to iOS Mapping
| Android package/file | iOS package/file | Purpose |
| --- | --- | --- |
| `AndroidCoreMain/AdCore_MainAndroid.cs` | `IOSCoreMain/AdCore_MainIOS.cs` | Platform AdCore runtime controller. |
| `AndroidCoreMain/AdCore_Configs.cs` | `IOSCoreMain/IOSConfigsAdapter.cs` | Maps Firebase/runtime config into iOS mediation config. |
| `AndroidMediation/Android_Info.cs` | `IOSMediation/IOS_Info.cs` | Runtime metadata and selected ad unit state. |
| `AndroidMediation/Android_FSGroupController.cs` | `IOSMediation/IOS_FSGroupController.cs` | Fullscreen group controller. |
| `AndroidMediation/Android_RectGroupController.cs` | `IOSMediation/IOS_RectGroupController.cs` | Rectangle/banner/popup group controller. |
| `AndroidMediation/Helper/Android_MediationManager.cs` | `IOSMediation/Helper/IOS_MediationManager.cs` | Platform mediation bootstrap and routing. |
| `AndroidMediation/NativeAdManager` | `IOSMediation/NativeAdManager` | Native ad instance, callback, load/show/hide lifecycle. |
| `AndroidMediation/Plugins` | `Assets/Plugins/iOS` | Native platform bridge and binary artifact. |

## Package Structure
```text
Assets/
  Scripts/
    IOS/
      IOSCoreMain/
      IOSMediation/
  Plugins/
    iOS/
      NativeAdBridge.mm
      Shared.xcframework/
```

## Build Artifact
`Assets/Plugins/iOS/Shared.xcframework` must be generated from the KMP `AdsMultiplatform` project, then copied into this package.

Build command in the KMP project:

```sh
./gradlew :shared:assembleSharedReleaseXCFramework
```

The expected artifact source is:

```text
shared/build/XCFrameworks/release/Shared.xcframework
```

The expected Unity package destination is:

```text
Assets/Plugins/iOS/Shared.xcframework
```

The checked-in framework includes:
- `ios-arm64`
- `ios-arm64-simulator`

The framework binary is larger than GitHub's normal file limit, so this repository tracks the framework executables with Git LFS.

## Unity Integration
- Import this repository as a Unity package/submodule into the project.
- Keep `Assets/Plugins/iOS/NativeAdBridge.mm` enabled only for iOS.
- Keep `Assets/Plugins/iOS/Shared.xcframework` enabled only for iOS.
- Firebase config key for this CoreMain should be `adcore_main_ios`.
- `NativeAdBridge.mm` imports the KMP framework with `#import <Shared/Shared.h>`.
- C# calls into native through `IOSNativeAdBridge.cs` using `DllImport("__Internal")`.

## Xcode and Dependency Notes
- The package postprocess uses Unity `PlayerSettings.iOS.targetOSVersionString`, falling back to `15.6`.
- The package postprocess adds a shell phase for dynamic pod frameworks used by current mediation dependencies:
  - `AppLovinSDK/AppLovinSDK.framework`
  - `AdjustSignature/AdjustSigSdk.framework`
- If the exported Xcode project includes Pods, run `pod install --repo-update` from the exported Xcode folder before building the workspace.
- There is no Swift Package setup in this repository; the Unity-side bridge uses Objective-C++ plus the KMP XCFramework.

## Compatibility Risks
- Device and simulator builds must match the Unity export SDK. If Unity exported a device build, build the generated Xcode project for a physical iPhone, not an iOS Simulator.
- `Shared.xcframework` contains a simulator slice, but Unity's own `libiPhone-lib.a` can still be device-only depending on export settings.
- iOS dynamic pod frameworks must be present in the Xcode build products before the embed script can copy/sign them.
- The KMP header surface must keep the methods used by `NativeAdBridge.mm`, especially banner/popup load, show, hide, destroy, and popup placement update APIs.

## Release Checklist
1. Rebuild `Shared.xcframework` from KMP when native iOS APIs change.
2. Copy the rebuilt framework into `Assets/Plugins/iOS/Shared.xcframework`.
3. Verify Unity plugin import settings still target iOS only.
4. Export Unity to Xcode and run `pod install --repo-update` when Pods are present.
5. Build on a physical iPhone for the default package configuration.
