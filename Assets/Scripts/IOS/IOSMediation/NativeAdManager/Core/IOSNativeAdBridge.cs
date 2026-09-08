using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace BG_Library.NET.IOSSDK
{
	public static class IOSNativeAdCallbackNames
	{
		public const string Idle = "Idle";
		public const string Loading = "Loading";
		public const string Loaded = "Loaded";
		public const string Displayable = "Displayable";
		public const string Failed = "Failed";
		public const string Shown = "Shown";
		public const string OnClosed = "onClosed";
	}

	public interface IOSNativeAdCallbackTarget
	{
		void HandleNativeCallback(string callbackName);
	}

	public static class IOSNativeAdBridge
	{
		public const string DefaultFullscreenInstanceId = "fullscreen_native_default";
		public const string DefaultLayoutName = "fs_single_universal_01";
		public const string FullscreenOverlayClsMode = "OVERLAY_CLS";
		public const string FullscreenOverlayNavMode = "OVERLAY_NAV";
		public const string FullscreenCls01 = "fs_single_cls_01";
		public const string FullscreenCls02 = "fs_single_cls_02";
		public const string FullscreenCls03Left = "fs_single_cls_03_left";
		public const string FullscreenCls03Right = "fs_single_cls_03_right";
		public const string FullscreenNav01Left = "fs_single_nav_01_left";
		public const string FullscreenNav01Right = "fs_single_nav_01_right";
		public const string FullscreenNav02Left = "fs_single_nav_02_left";
		public const string FullscreenNav02Right = "fs_single_nav_02_right";
		public const string FullscreenNav03Left = "fs_single_nav_03_left";
		public const string FullscreenNav03Right = "fs_single_nav_03_right";
		public const string DefaultBannerInstanceId = "banner_native_default";
		public const string DefaultPopupInstanceId = "popup_native_default";
		public const string DefaultInterstitialInstanceId = "interstitial";
		public const string DefaultBannerLayoutName = "bn_single_transparent_01";
		public const string DefaultPopupLayoutName = "mrec_single_manual_01";

		private const string IosTestAdUnitId = "ca-app-pub-3940256099942544/3986624511";
		private const string IosTestInterstitialAdUnitId = "ca-app-pub-3940256099942544/4411468910";
		private const int DefaultBannerCountdownSeconds = 5;
		private const int DefaultBannerCollapseSeconds = 5;

		private static readonly Dictionary<string, IOSNativeAdCallbackTarget> Instances =
			new Dictionary<string, IOSNativeAdCallbackTarget>();

#if UNITY_IOS && !UNITY_EDITOR
		[DllImport("__Internal")]
		private static extern void AdsMultiplatform_LoadFullscreenNativeAdWithReload(
			string instanceId,
			string adUnitIdsCsv,
			int enableReloadAfterShow);

		[DllImport("__Internal")]
		private static extern void AdsMultiplatform_ShowFullscreenNativeAd(
			string instanceId,
			string layoutName,
			double durationSeconds);

		[DllImport("__Internal")]
		private static extern void AdsMultiplatform_ShowFullscreenNativeAdWithOptions(
			string instanceId,
			string mode,
			string layoutNamesCsv,
			double durationSeconds,
			string durationsSecondsCsv,
			string orientation,
			int autoClose,
			int pauseGameplay,
			int enableAdComeback,
			int showTCD,
			double delaySeconds,
			int timeUpCSeconds,
			string fallbackAdUnitId,
			int cta,
			int headline,
			int body,
			int description,
			int icon,
			int advertiser,
			int media,
			int mediaImage,
			int mediaVideo);

		[DllImport("__Internal")]
		private static extern void AdsMultiplatform_HideFullscreenNativeAd(string instanceId);

		[DllImport("__Internal")]
		private static extern void AdsMultiplatform_DestroyFullscreenNativeAd(string instanceId);

		[DllImport("__Internal")]
		private static extern void AdsMultiplatform_LoadBannerNativeAdWithConfig(
			string instanceId,
			string adUnitIdsCsv,
			string layoutNamesCsv,
			int timeReloadSeconds,
			int timeCountdownSeconds,
			int timeCollapseSeconds);

		[DllImport("__Internal")]
		private static extern void AdsMultiplatform_ShowBannerNativeAd(string instanceId);

		[DllImport("__Internal")]
		private static extern int AdsMultiplatform_ExpandBannerNativeAd(string instanceId, int enableClick);

		[DllImport("__Internal")]
		private static extern void AdsMultiplatform_CollapseBannerNativeAd(string instanceId);

		[DllImport("__Internal")]
		private static extern void AdsMultiplatform_HideBannerNativeAd(string instanceId);

		[DllImport("__Internal")]
		private static extern void AdsMultiplatform_DestroyBannerNativeAd(string instanceId);

		[DllImport("__Internal")]
		private static extern void AdsMultiplatform_LoadPopupNativeAdWithConfig(
			string instanceId,
			string adUnitIdsCsv,
			string layoutName,
			int timeShowSeconds,
			int timeReloadSeconds,
			float xDp,
			float yDp,
			float adWidthDp,
			float adHeightDp,
			int autoClose,
			int enableCtrOverlay);

		[DllImport("__Internal")]
		private static extern void AdsMultiplatform_UpdatePopupNativeAdPlacement(
			string instanceId,
			float xDp,
			float yDp,
			float adWidthDp,
			float adHeightDp);

		[DllImport("__Internal")]
		private static extern void AdsMultiplatform_ShowPopupNativeAd(string instanceId);

		[DllImport("__Internal")]
		private static extern void AdsMultiplatform_ClosePopupNativeAd(string instanceId);

		[DllImport("__Internal")]
		private static extern void AdsMultiplatform_HidePopupNativeAd(string instanceId);

		[DllImport("__Internal")]
		private static extern void AdsMultiplatform_StopPopupNativeAd(string instanceId);

		[DllImport("__Internal")]
		private static extern void AdsMultiplatform_DestroyPopupNativeAd(string instanceId);

		[DllImport("__Internal")]
		private static extern int AdsMultiplatform_IsPopupNativeAdReady(string instanceId);

		[DllImport("__Internal")]
		private static extern int AdsMultiplatform_IsPopupNativeAdDisplayable(string instanceId);

		[DllImport("__Internal")]
		private static extern System.IntPtr AdsMultiplatform_PopupNativeAdStateFor(string instanceId);

		[DllImport("__Internal")]
		private static extern void AdsMultiplatform_FreeCString(System.IntPtr value);

		[DllImport("__Internal")]
		private static extern int AdsMultiplatform_CreateInterstitial(string instanceId, string configJson);

		[DllImport("__Internal")]
		private static extern int AdsMultiplatform_LoadInterstitialWithConfig(
			string instanceId,
			string adUnitIdsCsv,
			int preloadBufferSize,
			int autoReload);

		[DllImport("__Internal")]
		private static extern int AdsMultiplatform_ShowInterstitial(string instanceId, string optionsJson);

		[DllImport("__Internal")]
		private static extern int AdsMultiplatform_DestroyInterstitial(string instanceId);

		[DllImport("__Internal")]
		private static extern int AdsMultiplatform_IsInterstitialReady(string instanceId);
#endif

		public static void Register(string instanceId, IOSNativeAdCallbackTarget target)
		{
			if (target == null)
				return;

			string safeInstanceId = SafeInstanceId(instanceId);
			IOSNativeAdCallbackReceiver.EnsureReceiver();
			Instances[safeInstanceId] = target;
		}

		public static void Unregister(string instanceId)
		{
			if (string.IsNullOrEmpty(instanceId))
				return;

			Instances.Remove(SafeInstanceId(instanceId));
		}

		public static void LoadFullscreen(string instanceId, bool enableReloadAfterShow, string[] adUnitIds)
		{
			string safeInstanceId = SafeInstanceId(instanceId);
			string adUnitIdsCsv = JoinAdUnitIds(adUnitIds);

#if UNITY_IOS && !UNITY_EDITOR
			AdsMultiplatform_LoadFullscreenNativeAdWithReload(
				safeInstanceId,
				adUnitIdsCsv,
				enableReloadAfterShow ? 1 : 0);
#else
			UnityEngine.Debug.Log("[IOSNativeAdBridge] LoadFullscreen is only executed in an iOS player build. instance=" + safeInstanceId);
#endif
		}

		public static void ShowFullscreen(string instanceId, string layoutName = DefaultLayoutName, double durationSeconds = 0.0)
		{
			string safeInstanceId = SafeInstanceId(instanceId);
			string safeLayoutName = NormalizeLayoutName(layoutName);

#if UNITY_IOS && !UNITY_EDITOR
			AdsMultiplatform_ShowFullscreenNativeAd(safeInstanceId, safeLayoutName, durationSeconds);
#else
			UnityEngine.Debug.Log("[IOSNativeAdBridge] ShowFullscreen is only executed in an iOS player build. instance=" + safeInstanceId);
#endif
		}

		public static void ShowFullscreenWithOptions(
			string instanceId,
			string mode,
			string[] layoutNames,
			string fallbackLayoutName,
			double durationSeconds,
			string orientation,
			bool autoClose,
			bool pauseGameplay,
			bool enableAdComeback,
			bool showTCD,
			double delaySeconds,
			int timeUpCSeconds,
			string fallbackAdUnitId,
			global::NativeAssetVisibilityOptions assetVisibility)
		{
			string safeInstanceId = SafeInstanceId(instanceId);
			string safeMode = string.IsNullOrEmpty(mode) ? "" : mode.Trim();
			string layoutNamesCsv = JoinLayoutNames(layoutNames, fallbackLayoutName);
			string safeOrientation = SafeOrientation(orientation);
			string safeFallbackAdUnitId = string.IsNullOrEmpty(fallbackAdUnitId) ? IosTestAdUnitId : fallbackAdUnitId;

#if UNITY_IOS && !UNITY_EDITOR
			AdsMultiplatform_ShowFullscreenNativeAdWithOptions(
				safeInstanceId,
				safeMode,
				layoutNamesCsv,
				durationSeconds,
				string.Empty,
				safeOrientation,
				autoClose ? 1 : 0,
				pauseGameplay ? 1 : 0,
				enableAdComeback ? 1 : 0,
				showTCD ? 1 : 0,
				delaySeconds,
				timeUpCSeconds,
				safeFallbackAdUnitId,
				Flag(assetVisibility, x => x.cta),
				Flag(assetVisibility, x => x.headline),
				Flag(assetVisibility, x => x.body),
				Flag(assetVisibility, x => x.description),
				Flag(assetVisibility, x => x.icon),
				Flag(assetVisibility, x => x.advertiser),
				Flag(assetVisibility, x => x.media),
				Flag(assetVisibility, x => x.mediaImage),
				Flag(assetVisibility, x => x.mediaVideo));
#else
			UnityEngine.Debug.Log("[IOSNativeAdBridge] ShowFullscreenWithOptions is only executed in an iOS player build. instance=" + safeInstanceId);
#endif
		}

		public static void HideFullscreen(string instanceId)
		{
			string safeInstanceId = SafeInstanceId(instanceId);

#if UNITY_IOS && !UNITY_EDITOR
			AdsMultiplatform_HideFullscreenNativeAd(safeInstanceId);
#else
			UnityEngine.Debug.Log("[IOSNativeAdBridge] HideFullscreen is only executed in an iOS player build. instance=" + safeInstanceId);
#endif
		}

		public static void DestroyFullscreen(string instanceId)
		{
			string safeInstanceId = SafeInstanceId(instanceId);

#if UNITY_IOS && !UNITY_EDITOR
			AdsMultiplatform_DestroyFullscreenNativeAd(safeInstanceId);
#else
			UnityEngine.Debug.Log("[IOSNativeAdBridge] DestroyFullscreen is only executed in an iOS player build. instance=" + safeInstanceId);
#endif
		}

		public static bool CreateInterstitial(
			string instanceId = DefaultInterstitialInstanceId,
			string adUnitIdsCsv = null,
			int preloadBufferSize = 1,
			bool autoReload = true)
		{
			string safeInstanceId = SafeAlias(instanceId, DefaultInterstitialInstanceId);
			string safeAdUnitIds = SafeCsv(adUnitIdsCsv, IosTestInterstitialAdUnitId);
			string configJson = InterstitialConfigJson(safeAdUnitIds, preloadBufferSize, autoReload);

#if UNITY_IOS && !UNITY_EDITOR
			return AdsMultiplatform_CreateInterstitial(safeInstanceId, configJson) != 0;
#else
			UnityEngine.Debug.Log("[IOSNativeAdBridge] CreateInterstitial is only executed in an iOS player build. instance=" + safeInstanceId);
			return false;
#endif
		}

		public static bool LoadInterstitial(
			string instanceId = DefaultInterstitialInstanceId,
			string adUnitIdsCsv = null,
			int preloadBufferSize = 1,
			bool autoReload = true)
		{
			string safeInstanceId = SafeAlias(instanceId, DefaultInterstitialInstanceId);
			string safeAdUnitIds = SafeCsv(adUnitIdsCsv, IosTestInterstitialAdUnitId);

#if UNITY_IOS && !UNITY_EDITOR
			return AdsMultiplatform_LoadInterstitialWithConfig(
				safeInstanceId,
				safeAdUnitIds,
				preloadBufferSize < 1 ? 1 : preloadBufferSize,
				autoReload ? 1 : 0) != 0;
#else
			UnityEngine.Debug.Log("[IOSNativeAdBridge] LoadInterstitial is only executed in an iOS player build. instance=" + safeInstanceId);
			return false;
#endif
		}

		public static bool ShowInterstitial(string instanceId = DefaultInterstitialInstanceId, bool immersiveMode = true)
		{
			string safeInstanceId = SafeAlias(instanceId, DefaultInterstitialInstanceId);
			string optionsJson = InterstitialOptionsJson(immersiveMode);

#if UNITY_IOS && !UNITY_EDITOR
			return AdsMultiplatform_ShowInterstitial(safeInstanceId, optionsJson) != 0;
#else
			UnityEngine.Debug.Log("[IOSNativeAdBridge] ShowInterstitial is only executed in an iOS player build. instance=" + safeInstanceId);
			return false;
#endif
		}

		public static bool DestroyInterstitial(string instanceId = DefaultInterstitialInstanceId)
		{
			string safeInstanceId = SafeAlias(instanceId, DefaultInterstitialInstanceId);

#if UNITY_IOS && !UNITY_EDITOR
			return AdsMultiplatform_DestroyInterstitial(safeInstanceId) != 0;
#else
			UnityEngine.Debug.Log("[IOSNativeAdBridge] DestroyInterstitial is only executed in an iOS player build. instance=" + safeInstanceId);
			return false;
#endif
		}

		public static bool IsInterstitialReady(string instanceId = DefaultInterstitialInstanceId)
		{
			string safeInstanceId = SafeAlias(instanceId, DefaultInterstitialInstanceId);

#if UNITY_IOS && !UNITY_EDITOR
			return AdsMultiplatform_IsInterstitialReady(safeInstanceId) != 0;
#else
			return false;
#endif
		}

		public static void LoadBanner(
			string instanceId,
			string adUnitIdsCsv,
			string layoutNamesCsv,
			int timeReloadSeconds,
			int timeCountdownSeconds = DefaultBannerCountdownSeconds,
			int timeCollapseSeconds = DefaultBannerCollapseSeconds)
		{
			string safeInstanceId = SafeAlias(instanceId, DefaultBannerInstanceId);
			string safeAdUnitIds = SafeCsv(adUnitIdsCsv, IosTestAdUnitId);
			string safeLayoutNames = SafeCsv(layoutNamesCsv, DefaultBannerLayoutName);

#if UNITY_IOS && !UNITY_EDITOR
			AdsMultiplatform_LoadBannerNativeAdWithConfig(
				safeInstanceId,
				safeAdUnitIds,
				safeLayoutNames,
				NonNegative(timeReloadSeconds),
				NonNegative(timeCountdownSeconds),
				NonNegative(timeCollapseSeconds));
#else
			UnityEngine.Debug.Log("[IOSNativeAdBridge] LoadBanner is only executed in an iOS player build. instance=" + safeInstanceId);
#endif
		}

		public static void ShowBanner(string instanceId)
		{
			string safeInstanceId = SafeAlias(instanceId, DefaultBannerInstanceId);

#if UNITY_IOS && !UNITY_EDITOR
			AdsMultiplatform_ShowBannerNativeAd(safeInstanceId);
#else
			UnityEngine.Debug.Log("[IOSNativeAdBridge] ShowBanner is only executed in an iOS player build. instance=" + safeInstanceId);
#endif
		}

		public static bool ExpandBanner(string instanceId, bool enableClick)
		{
			string safeInstanceId = SafeAlias(instanceId, DefaultBannerInstanceId);

#if UNITY_IOS && !UNITY_EDITOR
			return AdsMultiplatform_ExpandBannerNativeAd(safeInstanceId, enableClick ? 1 : 0) != 0;
#else
			UnityEngine.Debug.Log("[IOSNativeAdBridge] ExpandBanner is only executed in an iOS player build. instance=" + safeInstanceId);
			return false;
#endif
		}

		public static void CollapseBanner(string instanceId)
		{
			string safeInstanceId = SafeAlias(instanceId, DefaultBannerInstanceId);

#if UNITY_IOS && !UNITY_EDITOR
			AdsMultiplatform_CollapseBannerNativeAd(safeInstanceId);
#else
			UnityEngine.Debug.Log("[IOSNativeAdBridge] CollapseBanner is only executed in an iOS player build. instance=" + safeInstanceId);
#endif
		}

		public static void HideBanner(string instanceId)
		{
			string safeInstanceId = SafeAlias(instanceId, DefaultBannerInstanceId);

#if UNITY_IOS && !UNITY_EDITOR
			AdsMultiplatform_HideBannerNativeAd(safeInstanceId);
#else
			UnityEngine.Debug.Log("[IOSNativeAdBridge] HideBanner is only executed in an iOS player build. instance=" + safeInstanceId);
#endif
		}

		public static void DestroyBanner(string instanceId)
		{
			string safeInstanceId = SafeAlias(instanceId, DefaultBannerInstanceId);

#if UNITY_IOS && !UNITY_EDITOR
			AdsMultiplatform_DestroyBannerNativeAd(safeInstanceId);
#else
			UnityEngine.Debug.Log("[IOSNativeAdBridge] DestroyBanner is only executed in an iOS player build. instance=" + safeInstanceId);
#endif
		}

		public static void LoadPopup(
			string instanceId,
			string adUnitIdsCsv,
			string layoutName,
			int timeShowSeconds,
			int timeReloadSeconds,
			float xDp,
			float yDp,
			float adWidthDp,
			float adHeightDp,
			bool autoClose,
			bool enableCtrOverlay)
		{
			string safeInstanceId = SafeAlias(instanceId, DefaultPopupInstanceId);
			string safeAdUnitIds = SafeCsv(adUnitIdsCsv, IosTestAdUnitId);
			string safeLayoutName = SafeLayoutName(layoutName, DefaultPopupLayoutName);

			if (!IsValidPopupLayout(xDp, yDp, adWidthDp, adHeightDp))
			{
				UnityEngine.Debug.LogWarning("[IOSNativeAdBridge] LoadPopup skipped. Invalid layout instance=" + safeInstanceId);
				DispatchSyntheticEvent(safeInstanceId, IOSNativeAdCallbackNames.Failed);
				return;
			}

#if UNITY_IOS && !UNITY_EDITOR
			AdsMultiplatform_LoadPopupNativeAdWithConfig(
				safeInstanceId,
				safeAdUnitIds,
				safeLayoutName,
				NonNegative(timeShowSeconds),
				NonNegative(timeReloadSeconds),
				xDp,
				yDp,
				adWidthDp,
				adHeightDp,
				autoClose ? 1 : 0,
				enableCtrOverlay ? 1 : 0);
#else
			UnityEngine.Debug.Log("[IOSNativeAdBridge] LoadPopup is only executed in an iOS player build. instance=" + safeInstanceId);
#endif
		}

		public static void UpdatePopupPlacement(
			string instanceId,
			float xDp,
			float yDp,
			float adWidthDp,
			float adHeightDp)
		{
			string safeInstanceId = SafeAlias(instanceId, DefaultPopupInstanceId);

			if (!IsValidPopupLayout(xDp, yDp, adWidthDp, adHeightDp))
			{
				UnityEngine.Debug.LogWarning("[IOSNativeAdBridge] UpdatePopupPlacement skipped. Invalid layout instance=" + safeInstanceId);
				DispatchSyntheticEvent(safeInstanceId, IOSNativeAdCallbackNames.Failed);
				return;
			}

#if UNITY_IOS && !UNITY_EDITOR
			AdsMultiplatform_UpdatePopupNativeAdPlacement(
				safeInstanceId,
				xDp,
				yDp,
				adWidthDp,
				adHeightDp);
#else
			UnityEngine.Debug.Log("[IOSNativeAdBridge] UpdatePopupPlacement is only executed in an iOS player build. instance=" + safeInstanceId);
#endif
		}

		public static void ShowPopup(string instanceId)
		{
			string safeInstanceId = SafeAlias(instanceId, DefaultPopupInstanceId);

#if UNITY_IOS && !UNITY_EDITOR
			if (!IsPopupDisplayable(safeInstanceId))
			{
				NotifyPopupShowSkipped(safeInstanceId, "state=" + PopupStateFor(safeInstanceId));
				return;
			}
			AdsMultiplatform_ShowPopupNativeAd(safeInstanceId);
#else
			UnityEngine.Debug.Log("[IOSNativeAdBridge] ShowPopup is only executed in an iOS player build. instance=" + safeInstanceId);
#endif
		}

		public static void ClosePopup(string instanceId)
		{
			string safeInstanceId = SafeAlias(instanceId, DefaultPopupInstanceId);

#if UNITY_IOS && !UNITY_EDITOR
			AdsMultiplatform_ClosePopupNativeAd(safeInstanceId);
#else
			UnityEngine.Debug.Log("[IOSNativeAdBridge] ClosePopup is only executed in an iOS player build. instance=" + safeInstanceId);
#endif
		}

		public static void HidePopup(string instanceId)
		{
			string safeInstanceId = SafeAlias(instanceId, DefaultPopupInstanceId);

#if UNITY_IOS && !UNITY_EDITOR
			AdsMultiplatform_HidePopupNativeAd(safeInstanceId);
#else
			UnityEngine.Debug.Log("[IOSNativeAdBridge] HidePopup is only executed in an iOS player build. instance=" + safeInstanceId);
#endif
		}

		public static void StopPopup(string instanceId)
		{
			string safeInstanceId = SafeAlias(instanceId, DefaultPopupInstanceId);

#if UNITY_IOS && !UNITY_EDITOR
			AdsMultiplatform_StopPopupNativeAd(safeInstanceId);
#else
			UnityEngine.Debug.Log("[IOSNativeAdBridge] StopPopup is only executed in an iOS player build. instance=" + safeInstanceId);
#endif
		}

		public static void DestroyPopup(string instanceId)
		{
			string safeInstanceId = SafeAlias(instanceId, DefaultPopupInstanceId);

#if UNITY_IOS && !UNITY_EDITOR
			AdsMultiplatform_DestroyPopupNativeAd(safeInstanceId);
#else
			UnityEngine.Debug.Log("[IOSNativeAdBridge] DestroyPopup is only executed in an iOS player build. instance=" + safeInstanceId);
#endif
		}

		public static bool IsPopupReady(string instanceId)
		{
			string safeInstanceId = SafeAlias(instanceId, DefaultPopupInstanceId);

#if UNITY_IOS && !UNITY_EDITOR
			return AdsMultiplatform_IsPopupNativeAdReady(safeInstanceId) != 0;
#else
			return false;
#endif
		}

		public static bool IsPopupDisplayable(string instanceId)
		{
			string safeInstanceId = SafeAlias(instanceId, DefaultPopupInstanceId);

#if UNITY_IOS && !UNITY_EDITOR
			return AdsMultiplatform_IsPopupNativeAdDisplayable(safeInstanceId) != 0;
#else
			return false;
#endif
		}

		public static string PopupStateFor(string instanceId)
		{
			string safeInstanceId = SafeAlias(instanceId, DefaultPopupInstanceId);

#if UNITY_IOS && !UNITY_EDITOR
			System.IntPtr statePtr = System.IntPtr.Zero;
			try
			{
				statePtr = AdsMultiplatform_PopupNativeAdStateFor(safeInstanceId);
				if (statePtr == System.IntPtr.Zero)
					return "NotLoaded";

				string state = Marshal.PtrToStringAnsi(statePtr);
				return string.IsNullOrEmpty(state) ? "NotLoaded" : state;
			}
			finally
			{
				if (statePtr != System.IntPtr.Zero)
					AdsMultiplatform_FreeCString(statePtr);
			}
#else
			return "NotLoaded";
#endif
		}

		public static void NotifyPopupShowSkipped(string instanceId, string reason)
		{
			string safeInstanceId = SafeAlias(instanceId, DefaultPopupInstanceId);
			UnityEngine.Debug.LogWarning("[IOSNativeAdBridge] ShowPopup skipped. instance=" + safeInstanceId + " " + reason);
			DispatchSyntheticEvent(safeInstanceId, IOSNativeAdCallbackNames.OnClosed);
		}

		internal static void DispatchNativeEvent(string payload)
		{
			int separator = string.IsNullOrEmpty(payload) ? -1 : payload.IndexOf('|');
			if (separator <= 0 || separator >= payload.Length - 1)
			{
				UnityEngine.Debug.LogWarning("[IOSNativeAdCallbackReceiver] Invalid callback payload: " + payload);
				return;
			}

			string instanceId = SafeInstanceId(payload.Substring(0, separator));
			string stateName = payload.Substring(separator + 1);
			if (!Instances.TryGetValue(instanceId, out var target))
			{
				UnityEngine.Debug.LogWarning("[IOSNativeAdCallbackReceiver] No registered native ad for callback: " + instanceId);
				return;
			}

			target.HandleNativeCallback(stateName);
		}

		private static void DispatchSyntheticEvent(string instanceId, string stateName)
		{
			DispatchNativeEvent(SafeAlias(instanceId, DefaultPopupInstanceId) + "|" + stateName);
		}

		public static string SafeInstanceId(string instanceId)
		{
			return string.IsNullOrEmpty(instanceId) ? DefaultFullscreenInstanceId : instanceId.Trim();
		}

		private static string SafeAlias(string instanceId, string fallback)
		{
			if (string.IsNullOrWhiteSpace(instanceId))
				return fallback;

			return instanceId.Trim();
		}

		private static string SafeCsv(string csv, string fallback)
		{
			if (string.IsNullOrWhiteSpace(csv))
				return fallback;

			var normalized = new List<string>();
			var parts = csv.Split(',');
			for (int i = 0; i < parts.Length; i++)
			{
				string value = parts[i]?.Trim();
				if (string.IsNullOrEmpty(value))
					continue;

				if (!normalized.Contains(value))
					normalized.Add(value);
			}

			return normalized.Count == 0 ? fallback : string.Join(",", normalized);
		}

		private static string SafeLayoutName(string layoutName, string fallbackLayoutName)
		{
			if (string.IsNullOrWhiteSpace(layoutName))
				return fallbackLayoutName;

			return layoutName.Trim().ToLowerInvariant();
		}

		private static string InterstitialConfigJson(string adUnitIdsCsv, int preloadBufferSize, bool autoReload)
		{
			string[] adUnitIds = SafeCsv(adUnitIdsCsv, IosTestInterstitialAdUnitId).Split(',');
			string idsJson = "";

			for (int i = 0; i < adUnitIds.Length; i++)
			{
				string id = adUnitIds[i]?.Trim();
				if (string.IsNullOrEmpty(id))
					continue;

				if (idsJson.Length > 0)
					idsJson += ",";

				idsJson += JsonString(id);
			}

			if (idsJson.Length == 0)
				idsJson = JsonString(IosTestInterstitialAdUnitId);

			return "{\"ids\":[" + idsJson + "],\"autoReload\":" +
			       (autoReload ? "true" : "false") +
			       ",\"preloadBufferSize\":" +
			       (preloadBufferSize < 1 ? 1 : preloadBufferSize) +
			       "}";
		}

		private static string InterstitialOptionsJson(bool immersiveMode)
		{
			return "{\"immersiveMode\":" + (immersiveMode ? "true" : "false") + "}";
		}

		private static string JsonString(string value)
		{
			if (string.IsNullOrEmpty(value))
				return "\"\"";

			return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
		}

		private static bool IsValidPopupLayout(float xDp, float yDp, float adWidthDp, float adHeightDp)
		{
			return IsFinite(xDp) &&
			       IsFinite(yDp) &&
			       IsFinite(adWidthDp) &&
			       IsFinite(adHeightDp) &&
			       adWidthDp > 0f &&
			       adHeightDp > 0f;
		}

		private static bool IsFinite(float value)
		{
			return !float.IsNaN(value) && !float.IsInfinity(value);
		}

		private static int NonNegative(int value) => value < 0 ? 0 : value;

		private static string JoinAdUnitIds(string[] adUnitIds)
		{
			if (adUnitIds == null || adUnitIds.Length == 0)
				return IosTestAdUnitId;

			var normalized = new List<string>();
			for (int i = 0; i < adUnitIds.Length; i++)
			{
				string id = adUnitIds[i];
				if (string.IsNullOrWhiteSpace(id))
					continue;

				string trimmed = id.Trim();
				if (!normalized.Contains(trimmed))
					normalized.Add(trimmed);
			}

			return normalized.Count == 0 ? IosTestAdUnitId : string.Join(",", normalized);
		}

		private static string JoinLayoutNames(string[] layoutNames, string fallbackLayoutName)
		{
			string fallback = NormalizeLayoutName(fallbackLayoutName);
			if (layoutNames == null || layoutNames.Length == 0)
				return fallback;

			var normalized = new List<string>();
			for (int i = 0; i < layoutNames.Length; i++)
			{
				string layout = NormalizeLayoutName(layoutNames[i], fallback);
				if (!normalized.Contains(layout))
					normalized.Add(layout);
			}

			return normalized.Count == 0 ? fallback : string.Join(",", normalized);
		}

		private static string SafeOrientation(string orientation)
		{
			return string.IsNullOrEmpty(orientation) ? "auto" : orientation.Trim();
		}

		private static string NormalizeLayoutName(string layoutName)
		{
			return NormalizeLayoutName(layoutName, DefaultLayoutName);
		}

		private static string NormalizeLayoutName(string layoutName, string fallbackLayoutName)
		{
			if (string.IsNullOrEmpty(layoutName))
				return string.IsNullOrEmpty(fallbackLayoutName) ? DefaultLayoutName : fallbackLayoutName;

			string trimmed = layoutName.Trim().ToLowerInvariant();
			for (int index = 1; index <= 5; index++)
			{
				string validName = "fs_single_universal_" + index.ToString("00");
				if (trimmed == validName)
					return trimmed;
			}

			switch (trimmed)
			{
				case FullscreenCls01:
				case FullscreenCls02:
				case FullscreenCls03Left:
				case FullscreenCls03Right:
				case FullscreenNav01Left:
				case FullscreenNav01Right:
				case FullscreenNav02Left:
				case FullscreenNav02Right:
				case FullscreenNav03Left:
				case FullscreenNav03Right:
					return trimmed;
				default:
					return string.IsNullOrEmpty(fallbackLayoutName) ? DefaultLayoutName : fallbackLayoutName;
			}
		}

		private static int Flag(global::NativeAssetVisibilityOptions assetVisibility, System.Func<global::NativeAssetVisibilityOptions, bool> selector)
		{
			return assetVisibility == null || selector(assetVisibility) ? 1 : 0;
		}
	}

	internal sealed class IOSNativeAdCallbackReceiver : MonoBehaviour
	{
		private const string GameObjectName = "AdsMultiplatformCallbacks";

		public static void EnsureReceiver()
		{
			GameObject receiver = GameObject.Find(GameObjectName);
			if (receiver == null)
			{
				receiver = new GameObject(GameObjectName);
				DontDestroyOnLoad(receiver);
			}

			if (receiver.GetComponent<IOSNativeAdCallbackReceiver>() == null)
				receiver.AddComponent<IOSNativeAdCallbackReceiver>();
		}

		public void OnNativeAdEvent(string payload)
		{
			IOSNativeAdBridge.DispatchNativeEvent(payload);
		}
	}
}
