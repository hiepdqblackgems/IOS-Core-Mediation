#import <Foundation/Foundation.h>
#import <UIKit/UIKit.h>
#import <objc/message.h>
#import <objc/runtime.h>
#import <Shared/Shared.h>

extern void UnitySendMessage(const char *obj, const char *method, const char *msg);

extern "C" void UnityPause(int pause) __attribute__((weak_import));

static const char *AdsMultiplatformCallbackObject = "AdsMultiplatformCallbacks";
static const char *AdsMultiplatformCallbackMethod = "OnNativeAdEvent";

static void AdsMultiplatformSendEvent(NSString *instanceId, NSString *stateName) {
    NSString *payload = [NSString stringWithFormat:@"%@|%@", instanceId ?: @"", stateName ?: @""];
    UnitySendMessage(AdsMultiplatformCallbackObject, AdsMultiplatformCallbackMethod, payload.UTF8String);
}

static NSString *AdsMultiplatformString(const char *value) {
    return value == nullptr ? @"" : [NSString stringWithUTF8String:value];
}

static NSString *AdsMultiplatformStateName(SharedNativeAdState *state) {
    return state == nil ? @"" : state.name;
}

static UIWindow *AdsMultiplatformActiveWindow(void) {
    UIWindow *candidate = nil;
    for (UIScene *scene in UIApplication.sharedApplication.connectedScenes) {
        if (![scene isKindOfClass:[UIWindowScene class]]) {
            continue;
        }
        for (UIWindow *window in ((UIWindowScene *)scene).windows) {
            if (candidate == nil) {
                candidate = window;
            }
            if (window.isKeyWindow) {
                return window;
            }
        }
    }
    return candidate;
}

static UIViewController *AdsMultiplatformPresenter(void) {
    UIWindow *window = AdsMultiplatformActiveWindow();
    UIViewController *presenter = window.rootViewController;
    while (presenter.presentedViewController != nil) {
        presenter = presenter.presentedViewController;
    }
    return presenter;
}

static void AdsMultiplatformPrepareModal(UIViewController *controller) {
    controller.modalPresentationStyle = UIModalPresentationFullScreen;
    controller.modalPresentationCapturesStatusBarAppearance = YES;
    if (@available(iOS 13.0, *)) {
        controller.modalInPresentation = YES;
    }
}

static NSMutableSet<NSString *> *AdsMultiplatformPausedFullscreenInstances(void) {
    static NSMutableSet<NSString *> *instances;
    static dispatch_once_t onceToken;
    dispatch_once(&onceToken, ^{
        instances = [NSMutableSet set];
    });
    return instances;
}

static void AdsMultiplatformSetUnityPaused(BOOL paused) {
    if (UnityPause != NULL) {
        UnityPause(paused ? 1 : 0);
        return;
    }

    id controller = UIApplication.sharedApplication.delegate;
    SEL setPausedSelector = sel_registerName("setPaused:");
    if ([controller respondsToSelector:setPausedSelector]) {
        ((void (*)(id, SEL, BOOL))objc_msgSend)(controller, setPausedSelector, paused);
        return;
    }

    SEL pauseSelector = sel_registerName("pause:");
    if ([controller respondsToSelector:pauseSelector]) {
        ((void (*)(id, SEL, BOOL))objc_msgSend)(controller, pauseSelector, paused);
    }
}

static void AdsMultiplatformPauseUnityForFullscreen(NSString *instanceId) {
    NSString *safeInstanceId = instanceId.length == 0 ? @"fullscreen_native_default" : instanceId;
    NSMutableSet<NSString *> *instances = AdsMultiplatformPausedFullscreenInstances();
    if ([instances containsObject:safeInstanceId]) {
        return;
    }

    [instances addObject:safeInstanceId];
    AdsMultiplatformSetUnityPaused(YES);
}

static void AdsMultiplatformResumeUnityForFullscreen(NSString *instanceId) {
    NSString *safeInstanceId = instanceId.length == 0 ? @"fullscreen_native_default" : instanceId;
    NSMutableSet<NSString *> *instances = AdsMultiplatformPausedFullscreenInstances();
    if (![instances containsObject:safeInstanceId]) {
        return;
    }

    [instances removeObject:safeInstanceId];
    if (instances.count == 0) {
        AdsMultiplatformSetUnityPaused(NO);
    }
}

extern "C" {
    void AdsMultiplatform_LoadNativeAd(const char *adUnitId) {
        NSString *unitId = adUnitId == nullptr ? @"" : [NSString stringWithUTF8String:adUnitId];
        dispatch_async(dispatch_get_main_queue(), ^{
            UIViewController *presenter = AdsMultiplatformPresenter();
            SharedNativeAdIosBridge *bridge = [[SharedNativeAdIosBridge alloc] init];
            [bridge loadAdUnitId:unitId rootViewController:presenter];
        });
    }

    void AdsMultiplatform_LoadFullscreenNativeAdWithReload(
        const char *instanceId,
        const char *adUnitIdsCsv,
        int enableReloadAfterShow
    ) {
        NSString *nativeInstanceId = instanceId == nullptr ? @"" : [NSString stringWithUTF8String:instanceId];
        NSString *unitIds = adUnitIdsCsv == nullptr ? @"" : [NSString stringWithUTF8String:adUnitIdsCsv];
        dispatch_async(dispatch_get_main_queue(), ^{
            UIViewController *presenter = AdsMultiplatformPresenter();
            SharedNativeAdIosBridge *bridge = [[SharedNativeAdIosBridge alloc] init];
            [bridge loadFullscreenInstanceId:nativeInstanceId
                                adUnitIdsCsv:unitIds
                          rootViewController:presenter
                        enableReloadAfterShow:enableReloadAfterShow != 0
                              onStateChanged:^(SharedNativeAdState *state) {
                AdsMultiplatformSendEvent(nativeInstanceId, state.name);
            }];
        });
    }

    void AdsMultiplatform_LoadFullscreenNativeAd(const char *instanceId, const char *adUnitIdsCsv) {
        AdsMultiplatform_LoadFullscreenNativeAdWithReload(instanceId, adUnitIdsCsv, 0);
    }

    void AdsMultiplatform_LoadBannerNativeAdWithConfig(
        const char *instanceId,
        const char *adUnitIdsCsv,
        const char *layoutNamesCsv,
        int timeReloadSeconds,
        int timeCountdownSeconds,
        int timeCollapseSeconds
    ) {
        NSString *nativeInstanceId = AdsMultiplatformString(instanceId);
        NSString *unitIds = AdsMultiplatformString(adUnitIdsCsv);
        NSString *layoutNames = AdsMultiplatformString(layoutNamesCsv);
        dispatch_async(dispatch_get_main_queue(), ^{
            SharedIosBannerNativeAdSdk *sdk = [[SharedIosBannerNativeAdSdk alloc] init];
            [sdk loadWithConfigRootViewController:AdsMultiplatformPresenter()
                                            alias:nativeInstanceId
                                     adUnitIdsCsv:unitIds
                                   layoutNamesCsv:layoutNames
                                timeReloadSeconds:timeReloadSeconds
                             timeCountdownSeconds:timeCountdownSeconds
                              timeCollapseSeconds:timeCollapseSeconds
                                   onStateChanged:^(SharedNativeAdState *state) {
                AdsMultiplatformSendEvent(nativeInstanceId, AdsMultiplatformStateName(state));
            }];
        });
    }

    void AdsMultiplatform_ShowBannerNativeAd(const char *instanceId) {
        NSString *nativeInstanceId = AdsMultiplatformString(instanceId);
        dispatch_async(dispatch_get_main_queue(), ^{
            SharedIosBannerNativeAdSdk *sdk = [[SharedIosBannerNativeAdSdk alloc] init];
            [sdk showRootViewController:AdsMultiplatformPresenter() alias:nativeInstanceId];
        });
    }

    int AdsMultiplatform_ExpandBannerNativeAd(const char *instanceId, int enableClick) {
        NSString *nativeInstanceId = AdsMultiplatformString(instanceId);
        __block BOOL result = NO;
        if ([NSThread isMainThread]) {
            SharedIosBannerNativeAdSdk *sdk = [[SharedIosBannerNativeAdSdk alloc] init];
            result = [sdk expandAlias:nativeInstanceId enableClick:enableClick != 0];
        } else {
            dispatch_sync(dispatch_get_main_queue(), ^{
                SharedIosBannerNativeAdSdk *sdk = [[SharedIosBannerNativeAdSdk alloc] init];
                result = [sdk expandAlias:nativeInstanceId enableClick:enableClick != 0];
            });
        }
        return result ? 1 : 0;
    }

    void AdsMultiplatform_CollapseBannerNativeAd(const char *instanceId) {
        NSString *nativeInstanceId = AdsMultiplatformString(instanceId);
        dispatch_async(dispatch_get_main_queue(), ^{
            SharedIosBannerNativeAdSdk *sdk = [[SharedIosBannerNativeAdSdk alloc] init];
            [sdk collapseAlias:nativeInstanceId];
        });
    }

    void AdsMultiplatform_HideBannerNativeAd(const char *instanceId) {
        NSString *nativeInstanceId = AdsMultiplatformString(instanceId);
        dispatch_async(dispatch_get_main_queue(), ^{
            SharedIosBannerNativeAdSdk *sdk = [[SharedIosBannerNativeAdSdk alloc] init];
            [sdk hideAlias:nativeInstanceId];
        });
    }

    void AdsMultiplatform_DestroyBannerNativeAd(const char *instanceId) {
        NSString *nativeInstanceId = AdsMultiplatformString(instanceId);
        dispatch_async(dispatch_get_main_queue(), ^{
            SharedIosBannerNativeAdSdk *sdk = [[SharedIosBannerNativeAdSdk alloc] init];
            [sdk destroyAlias:nativeInstanceId];
        });
    }

    void AdsMultiplatform_LoadPopupNativeAdWithConfig(
        const char *instanceId,
        const char *adUnitIdsCsv,
        const char *layoutName,
        int timeShowSeconds,
        int timeReloadSeconds,
        float xDp,
        float yDp,
        float adWidthDp,
        float adHeightDp,
        int autoClose,
        int enableCtrOverlay
    ) {
        NSString *nativeInstanceId = AdsMultiplatformString(instanceId);
        NSString *unitIds = AdsMultiplatformString(adUnitIdsCsv);
        NSString *nativeLayoutName = AdsMultiplatformString(layoutName);
        dispatch_async(dispatch_get_main_queue(), ^{
            SharedIosPopupNativeAdSdk *sdk = [[SharedIosPopupNativeAdSdk alloc] init];
            [sdk loadWithConfigRootViewController:AdsMultiplatformPresenter()
                                            alias:nativeInstanceId
                                     adUnitIdsCsv:unitIds
                                       layoutName:nativeLayoutName
                                  timeShowSeconds:timeShowSeconds
                                timeReloadSeconds:timeReloadSeconds
                                              xDp:xDp
                                              yDp:yDp
                                        adWidthDp:adWidthDp
                                       adHeightDp:adHeightDp
                                        autoClose:autoClose != 0
                                 enableCtrOverlay:enableCtrOverlay != 0
                                   onStateChanged:^(SharedNativeAdState *state) {
                AdsMultiplatformSendEvent(nativeInstanceId, AdsMultiplatformStateName(state));
            }];
        });
    }

    void AdsMultiplatform_UpdatePopupNativeAdPlacement(
        const char *instanceId,
        float xDp,
        float yDp,
        float adWidthDp,
        float adHeightDp
    ) {
        NSString *nativeInstanceId = AdsMultiplatformString(instanceId);
        dispatch_async(dispatch_get_main_queue(), ^{
            SharedIosPopupNativeAdSdk *sdk = [[SharedIosPopupNativeAdSdk alloc] init];
            [sdk updatePlacementAlias:nativeInstanceId
                                  xDp:xDp
                                  yDp:yDp
                            adWidthDp:adWidthDp
                           adHeightDp:adHeightDp];
        });
    }

    void AdsMultiplatform_ShowPopupNativeAd(const char *instanceId) {
        NSString *nativeInstanceId = AdsMultiplatformString(instanceId);
        dispatch_async(dispatch_get_main_queue(), ^{
            SharedIosPopupNativeAdSdk *sdk = [[SharedIosPopupNativeAdSdk alloc] init];
            [sdk showRootViewController:AdsMultiplatformPresenter() alias:nativeInstanceId];
        });
    }

    void AdsMultiplatform_ClosePopupNativeAd(const char *instanceId) {
        NSString *nativeInstanceId = AdsMultiplatformString(instanceId);
        dispatch_async(dispatch_get_main_queue(), ^{
            SharedIosPopupNativeAdSdk *sdk = [[SharedIosPopupNativeAdSdk alloc] init];
            [sdk closeAlias:nativeInstanceId];
        });
    }

    void AdsMultiplatform_HidePopupNativeAd(const char *instanceId) {
        NSString *nativeInstanceId = AdsMultiplatformString(instanceId);
        dispatch_async(dispatch_get_main_queue(), ^{
            SharedIosPopupNativeAdSdk *sdk = [[SharedIosPopupNativeAdSdk alloc] init];
            [sdk hideAlias:nativeInstanceId];
        });
    }

    void AdsMultiplatform_StopPopupNativeAd(const char *instanceId) {
        NSString *nativeInstanceId = AdsMultiplatformString(instanceId);
        dispatch_async(dispatch_get_main_queue(), ^{
            SharedIosPopupNativeAdSdk *sdk = [[SharedIosPopupNativeAdSdk alloc] init];
            [sdk stopAlias:nativeInstanceId];
        });
    }

    void AdsMultiplatform_DestroyPopupNativeAd(const char *instanceId) {
        NSString *nativeInstanceId = AdsMultiplatformString(instanceId);
        dispatch_async(dispatch_get_main_queue(), ^{
            SharedIosPopupNativeAdSdk *sdk = [[SharedIosPopupNativeAdSdk alloc] init];
            [sdk destroyAlias:nativeInstanceId];
        });
    }

    void AdsMultiplatform_ShowNativeAd(const char *adUnitId) {
        NSString *unitId = adUnitId == nullptr ? @"" : [NSString stringWithUTF8String:adUnitId];
        dispatch_async(dispatch_get_main_queue(), ^{
            SharedNativeAdIosBridge *bridge = [[SharedNativeAdIosBridge alloc] init];
            UIViewController *controller = [bridge viewControllerAdUnitId:unitId];
            AdsMultiplatformPrepareModal(controller);
            [AdsMultiplatformPresenter() presentViewController:controller animated:YES completion:nil];
        });
    }

    void AdsMultiplatform_ShowFullscreenNativeAd(const char *instanceId, const char *layoutName, double durationSeconds) {
        NSString *nativeInstanceId = instanceId == nullptr ? @"" : [NSString stringWithUTF8String:instanceId];
        NSString *nativeLayoutName = layoutName == nullptr ? @"" : [NSString stringWithUTF8String:layoutName];
        dispatch_async(dispatch_get_main_queue(), ^{
            if (![SharedFullscreenNativeAdRegistry.shared isReadyAlias:nativeInstanceId]) {
                AdsMultiplatformSendEvent(nativeInstanceId, @"Failed");
                return;
            }
            SharedNativeAdIosBridge *bridge = [[SharedNativeAdIosBridge alloc] init];
            AdsMultiplatformSendEvent(nativeInstanceId, @"Shown");
            UIViewController *controller = [bridge fullscreenViewControllerForUnityInstanceId:nativeInstanceId
                                                                                    layoutName:nativeLayoutName
                                                                              durationSeconds:durationSeconds
                                                                             fallbackAdUnitId:@""
                                                                                     onClosed:^{
                AdsMultiplatformResumeUnityForFullscreen(nativeInstanceId);
                AdsMultiplatformSendEvent(nativeInstanceId, @"onClosed");
            }];
            AdsMultiplatformPrepareModal(controller);
            AdsMultiplatformPauseUnityForFullscreen(nativeInstanceId);
            [AdsMultiplatformPresenter() presentViewController:controller animated:YES completion:nil];
        });
    }

    void AdsMultiplatform_ShowFullscreenNativeAdWithOptions(
        const char *instanceId,
        const char *mode,
        const char *layoutNamesCsv,
        double durationSeconds,
        const char *durationsSecondsCsv,
        const char *orientation,
        int autoClose,
        int pauseGameplay,
        int enableAdComeback,
        int showTCD,
        double delaySeconds,
        int timeUpCSeconds,
        const char *fallbackAdUnitId,
        int cta,
        int headline,
        int body,
        int description,
        int icon,
        int advertiser,
        int media,
        int mediaImage,
        int mediaVideo
    ) {
        NSString *nativeInstanceId = instanceId == nullptr ? @"" : [NSString stringWithUTF8String:instanceId];
        NSString *nativeMode = mode == nullptr ? @"" : [NSString stringWithUTF8String:mode];
        NSString *nativeLayoutNamesCsv = layoutNamesCsv == nullptr ? @"" : [NSString stringWithUTF8String:layoutNamesCsv];
        NSString *nativeDurationsSecondsCsv = durationsSecondsCsv == nullptr ? @"" : [NSString stringWithUTF8String:durationsSecondsCsv];
        NSString *nativeOrientation = orientation == nullptr ? @"auto" : [NSString stringWithUTF8String:orientation];
        NSString *nativeFallbackAdUnitId = fallbackAdUnitId == nullptr ? @"" : [NSString stringWithUTF8String:fallbackAdUnitId];
        dispatch_async(dispatch_get_main_queue(), ^{
            if (![SharedFullscreenNativeAdRegistry.shared isReadyAlias:nativeInstanceId]) {
                AdsMultiplatformSendEvent(nativeInstanceId, @"Failed");
                return;
            }
            SharedNativeAdIosBridge *bridge = [[SharedNativeAdIosBridge alloc] init];
            AdsMultiplatformSendEvent(nativeInstanceId, @"Shown");
            UIViewController *controller = [bridge fullscreenViewControllerForUnityWithOptionsInstanceId:nativeInstanceId
                                                                                                    mode:nativeMode
                                                                                          layoutNamesCsv:nativeLayoutNamesCsv
                                                                                         durationSeconds:durationSeconds
                                                                                      durationsSecondsCsv:nativeDurationsSecondsCsv
                                                                                             orientation:nativeOrientation
                                                                                               autoClose:autoClose != 0
                                                                                           pauseGameplay:pauseGameplay != 0
                                                                                       enableAdComeback:enableAdComeback != 0
                                                                                                  showTCD:showTCD != 0
                                                                                             delaySeconds:delaySeconds
                                                                                          timeUpCSeconds:timeUpCSeconds
                                                                                         fallbackAdUnitId:nativeFallbackAdUnitId
                                                                                                      cta:cta != 0
                                                                                                 headline:headline != 0
                                                                                                     body:body != 0
                                                                                              description:description != 0
                                                                                                     icon:icon != 0
                                                                                               advertiser:advertiser != 0
                                                                                                    media:media != 0
                                                                                                mediaImage:mediaImage != 0
                                                                                                mediaVideo:mediaVideo != 0
                                                                                                onClosed:^{
                AdsMultiplatformResumeUnityForFullscreen(nativeInstanceId);
                AdsMultiplatformSendEvent(nativeInstanceId, @"onClosed");
            }];
            AdsMultiplatformPrepareModal(controller);
            AdsMultiplatformPauseUnityForFullscreen(nativeInstanceId);
            [AdsMultiplatformPresenter() presentViewController:controller animated:YES completion:nil];
        });
    }

    void AdsMultiplatform_HideNativeAd(void) {
        dispatch_async(dispatch_get_main_queue(), ^{
            [AdsMultiplatformPresenter() dismissViewControllerAnimated:YES completion:nil];
        });
    }

    void AdsMultiplatform_HideFullscreenNativeAd(const char *instanceId) {
        NSString *nativeInstanceId = instanceId == nullptr ? @"" : [NSString stringWithUTF8String:instanceId];
        dispatch_async(dispatch_get_main_queue(), ^{
            AdsMultiplatformResumeUnityForFullscreen(nativeInstanceId);
            AdsMultiplatformSendEvent(nativeInstanceId, @"onClosed");
            [AdsMultiplatformPresenter() dismissViewControllerAnimated:YES completion:nil];
        });
    }

    void AdsMultiplatform_DestroyFullscreenNativeAd(const char *instanceId) {
        NSString *nativeInstanceId = instanceId == nullptr ? @"" : [NSString stringWithUTF8String:instanceId];
        dispatch_async(dispatch_get_main_queue(), ^{
            SharedIosFullscreenNativeAdSdk *sdk = [[SharedIosFullscreenNativeAdSdk alloc] init];
            [sdk destroyAlias:nativeInstanceId];
        });
    }

    void loadNativeAdIOS(const char *adUnitId) {
        AdsMultiplatform_LoadNativeAd(adUnitId);
    }

    void showNativeAdIOS(const char *adUnitId) {
        AdsMultiplatform_ShowNativeAd(adUnitId);
    }
}
