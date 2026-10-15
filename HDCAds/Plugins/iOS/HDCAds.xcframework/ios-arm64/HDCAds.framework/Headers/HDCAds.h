#import <Foundation/NSArray.h>
#import <Foundation/NSDictionary.h>
#import <Foundation/NSError.h>
#import <Foundation/NSObject.h>
#import <Foundation/NSSet.h>
#import <Foundation/NSString.h>
#import <Foundation/NSValue.h>

@class HDCAAdsLogLevel, HDCAAdsLogging, HDCABannerCollapseCountdownStyle, HDCABannerNativeAdComposeCallbacks, HDCABannerNativeAdComposeState, HDCABannerNativeAdConfig, HDCABannerNativeAdError, HDCABannerNativeAdInfo, HDCABannerNativeAdLayoutCatalog, HDCABannerNativeAdReloadPolicy, HDCABannerNativeAdRequest, HDCABannerNativeAdShowOptions, HDCABannerNativeAdSizing, HDCAFullscreenLayoutCatalog, HDCAFullscreenNativeAdCallbacks, HDCAFullscreenNativeAdCloseStyle, HDCAFullscreenNativeAdConfig, HDCAFullscreenNativeAdControlPhase, HDCAFullscreenNativeAdControlState, HDCAFullscreenNativeAdError, HDCAFullscreenNativeAdInfo, HDCAFullscreenNativeAdLayoutCatalog, HDCAFullscreenNativeAdMode, HDCAFullscreenNativeAdModeCompanion, HDCAFullscreenNativeAdRegistry, HDCAFullscreenNativeAdRequest, HDCAFullscreenNativeAdShowOptions, HDCAFullscreenNativeAdShowOptionsCompanion, HDCAFullscreenNativeAdState, HDCAInterstitialAdConfig, HDCAInterstitialAdInfo, HDCAInterstitialAdPaidInfo, HDCAInterstitialAdRegistry, HDCAInterstitialShowOptions, HDCAIosAdEvent, HDCAIosAdEventCenter, HDCAKotlinArray<T>, HDCAKotlinEnum<E>, HDCAKotlinEnumCompanion, HDCANativeAdLayoutNames, HDCANativeAdLoadResult, HDCANativeAdLoadResultCompanion, HDCANativeAdPaidInfo, HDCANativeAdShowOptions, HDCANativeAdState, HDCANativeAssetVisibilityOptions, HDCANativeClickAssetOptions, HDCAPopupNativeAdComposeCallbacks, HDCAPopupNativeAdComposeState, HDCAPopupNativeAdConfig, HDCAPopupNativeAdError, HDCAPopupNativeAdInfo, HDCAPopupNativeAdLayoutCatalog, HDCAPopupNativeAdRequest, HDCUnityBridge, UIViewController;

@protocol HDCAFullscreenNativeAdCallback, HDCAInterstitialAdCallback, HDCAKotlinComparable, HDCAKotlinIterator, HDCAPlatform, HDCUnityListener;

NS_ASSUME_NONNULL_BEGIN
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wunknown-warning-option"
#pragma clang diagnostic ignored "-Wincompatible-property-type"
#pragma clang diagnostic ignored "-Wnullability"

#pragma push_macro("_Nullable_result")
#if !__has_feature(nullability_nullable_result)
#undef _Nullable_result
#define _Nullable_result _Nullable
#endif

__attribute__((swift_name("KotlinBase")))
@interface HDCABase : NSObject
- (instancetype)init __attribute__((unavailable));
+ (instancetype)new __attribute__((unavailable));
+ (void)initialize __attribute__((objc_requires_super));
@end

@interface HDCABase (HDCABaseCopying) <NSCopying>
@end

__attribute__((swift_name("KotlinMutableSet")))
@interface HDCAMutableSet<ObjectType> : NSMutableSet<ObjectType>
@end

__attribute__((swift_name("KotlinMutableDictionary")))
@interface HDCAMutableDictionary<KeyType, ObjectType> : NSMutableDictionary<KeyType, ObjectType>
@end

@interface NSError (NSErrorHDCAKotlinException)
@property (readonly) id _Nullable kotlinException;
@end

__attribute__((swift_name("KotlinNumber")))
@interface HDCANumber : NSNumber
- (instancetype)initWithChar:(char)value __attribute__((unavailable));
- (instancetype)initWithUnsignedChar:(unsigned char)value __attribute__((unavailable));
- (instancetype)initWithShort:(short)value __attribute__((unavailable));
- (instancetype)initWithUnsignedShort:(unsigned short)value __attribute__((unavailable));
- (instancetype)initWithInt:(int)value __attribute__((unavailable));
- (instancetype)initWithUnsignedInt:(unsigned int)value __attribute__((unavailable));
- (instancetype)initWithLong:(long)value __attribute__((unavailable));
- (instancetype)initWithUnsignedLong:(unsigned long)value __attribute__((unavailable));
- (instancetype)initWithLongLong:(long long)value __attribute__((unavailable));
- (instancetype)initWithUnsignedLongLong:(unsigned long long)value __attribute__((unavailable));
- (instancetype)initWithFloat:(float)value __attribute__((unavailable));
- (instancetype)initWithDouble:(double)value __attribute__((unavailable));
- (instancetype)initWithBool:(BOOL)value __attribute__((unavailable));
- (instancetype)initWithInteger:(NSInteger)value __attribute__((unavailable));
- (instancetype)initWithUnsignedInteger:(NSUInteger)value __attribute__((unavailable));
+ (instancetype)numberWithChar:(char)value __attribute__((unavailable));
+ (instancetype)numberWithUnsignedChar:(unsigned char)value __attribute__((unavailable));
+ (instancetype)numberWithShort:(short)value __attribute__((unavailable));
+ (instancetype)numberWithUnsignedShort:(unsigned short)value __attribute__((unavailable));
+ (instancetype)numberWithInt:(int)value __attribute__((unavailable));
+ (instancetype)numberWithUnsignedInt:(unsigned int)value __attribute__((unavailable));
+ (instancetype)numberWithLong:(long)value __attribute__((unavailable));
+ (instancetype)numberWithUnsignedLong:(unsigned long)value __attribute__((unavailable));
+ (instancetype)numberWithLongLong:(long long)value __attribute__((unavailable));
+ (instancetype)numberWithUnsignedLongLong:(unsigned long long)value __attribute__((unavailable));
+ (instancetype)numberWithFloat:(float)value __attribute__((unavailable));
+ (instancetype)numberWithDouble:(double)value __attribute__((unavailable));
+ (instancetype)numberWithBool:(BOOL)value __attribute__((unavailable));
+ (instancetype)numberWithInteger:(NSInteger)value __attribute__((unavailable));
+ (instancetype)numberWithUnsignedInteger:(NSUInteger)value __attribute__((unavailable));
@end

__attribute__((swift_name("KotlinByte")))
@interface HDCAByte : HDCANumber
- (instancetype)initWithChar:(char)value;
+ (instancetype)numberWithChar:(char)value;
@end

__attribute__((swift_name("KotlinUByte")))
@interface HDCAUByte : HDCANumber
- (instancetype)initWithUnsignedChar:(unsigned char)value;
+ (instancetype)numberWithUnsignedChar:(unsigned char)value;
@end

__attribute__((swift_name("KotlinShort")))
@interface HDCAShort : HDCANumber
- (instancetype)initWithShort:(short)value;
+ (instancetype)numberWithShort:(short)value;
@end

__attribute__((swift_name("KotlinUShort")))
@interface HDCAUShort : HDCANumber
- (instancetype)initWithUnsignedShort:(unsigned short)value;
+ (instancetype)numberWithUnsignedShort:(unsigned short)value;
@end

__attribute__((swift_name("KotlinInt")))
@interface HDCAInt : HDCANumber
- (instancetype)initWithInt:(int)value;
+ (instancetype)numberWithInt:(int)value;
@end

__attribute__((swift_name("KotlinUInt")))
@interface HDCAUInt : HDCANumber
- (instancetype)initWithUnsignedInt:(unsigned int)value;
+ (instancetype)numberWithUnsignedInt:(unsigned int)value;
@end

__attribute__((swift_name("KotlinLong")))
@interface HDCALong : HDCANumber
- (instancetype)initWithLongLong:(long long)value;
+ (instancetype)numberWithLongLong:(long long)value;
@end

__attribute__((swift_name("KotlinULong")))
@interface HDCAULong : HDCANumber
- (instancetype)initWithUnsignedLongLong:(unsigned long long)value;
+ (instancetype)numberWithUnsignedLongLong:(unsigned long long)value;
@end

__attribute__((swift_name("KotlinFloat")))
@interface HDCAFloat : HDCANumber
- (instancetype)initWithFloat:(float)value;
+ (instancetype)numberWithFloat:(float)value;
@end

__attribute__((swift_name("KotlinDouble")))
@interface HDCADouble : HDCANumber
- (instancetype)initWithDouble:(double)value;
+ (instancetype)numberWithDouble:(double)value;
@end

__attribute__((swift_name("KotlinBoolean")))
@interface HDCABoolean : HDCANumber
- (instancetype)initWithBool:(BOOL)value;
+ (instancetype)numberWithBool:(BOOL)value;
@end

__attribute__((swift_name("BannerNativeAdCallback")))
@protocol HDCABannerNativeAdCallback
@required
- (void)onClickedAd:(HDCABannerNativeAdInfo *)ad __attribute__((swift_name("onClicked(ad:)")));
- (void)onClosedAd:(HDCABannerNativeAdInfo *)ad __attribute__((swift_name("onClosed(ad:)")));
- (void)onDisplayableAlias:(NSString *)alias __attribute__((swift_name("onDisplayable(alias:)")));
- (void)onDisplayedAd:(HDCABannerNativeAdInfo *)ad __attribute__((swift_name("onDisplayed(ad:)")));
- (void)onFailedToLoadError:(HDCABannerNativeAdError *)error __attribute__((swift_name("onFailedToLoad(error:)")));
- (void)onLoadedAd:(HDCABannerNativeAdInfo *)ad __attribute__((swift_name("onLoaded(ad:)")));
- (void)onOpenedAd:(HDCABannerNativeAdInfo *)ad __attribute__((swift_name("onOpened(ad:)")));
@end


/**
 * Impressions and paid impressions of the banner native ads of an alias, for revenue reporting. It is apart from
 * [BannerNativeAdCallback] so code that implements that callback keeps working: on iOS every method of an exported
 * callback is required.
 */
__attribute__((swift_name("BannerNativeAdRevenueCallback")))
@protocol HDCABannerNativeAdRevenueCallback
@required
- (void)onImpressionAd:(HDCABannerNativeAdInfo *)ad __attribute__((swift_name("onImpression(ad:)")));
- (void)onPaidImpressionAd:(HDCABannerNativeAdInfo *)ad paidInfo:(HDCANativeAdPaidInfo *)paidInfo __attribute__((swift_name("onPaidImpression(ad:paidInfo:)")));
@end

__attribute__((objc_subclassing_restricted))
__attribute__((swift_name("BannerNativeAdConfig")))
@interface HDCABannerNativeAdConfig : HDCABase
- (instancetype)initWithIds:(NSArray<NSString *> *)ids layoutNames:(NSArray<NSString *> *)layoutNames timeReloadSeconds:(int32_t)timeReloadSeconds timeCountdownSeconds:(int32_t)timeCountdownSeconds timeCollapseSeconds:(int32_t)timeCollapseSeconds __attribute__((swift_name("init(ids:layoutNames:timeReloadSeconds:timeCountdownSeconds:timeCollapseSeconds:)"))) __attribute__((objc_designated_initializer));
- (HDCABannerNativeAdConfig *)doCopyIds:(NSArray<NSString *> *)ids layoutNames:(NSArray<NSString *> *)layoutNames timeReloadSeconds:(int32_t)timeReloadSeconds timeCountdownSeconds:(int32_t)timeCountdownSeconds timeCollapseSeconds:(int32_t)timeCollapseSeconds __attribute__((swift_name("doCopy(ids:layoutNames:timeReloadSeconds:timeCountdownSeconds:timeCollapseSeconds:)")));
- (BOOL)isEqual:(id _Nullable)other __attribute__((swift_name("isEqual(_:)")));
- (NSUInteger)hash __attribute__((swift_name("hash()")));
- (NSString *)description __attribute__((swift_name("description()")));
@property (readonly) NSArray<NSString *> *ids __attribute__((swift_name("ids")));
@property (readonly) NSArray<NSString *> *layoutNames __attribute__((swift_name("layoutNames")));
@property (readonly) int32_t timeCollapseSeconds __attribute__((swift_name("timeCollapseSeconds")));
@property (readonly) int32_t timeCountdownSeconds __attribute__((swift_name("timeCountdownSeconds")));
@property (readonly) int32_t timeReloadSeconds __attribute__((swift_name("timeReloadSeconds")));
@end

__attribute__((objc_subclassing_restricted))
__attribute__((swift_name("BannerNativeAdError")))
@interface HDCABannerNativeAdError : HDCABase
- (instancetype)initWithAlias:(NSString *)alias adUnitId:(NSString *)adUnitId code:(int32_t)code message:(NSString *)message __attribute__((swift_name("init(alias:adUnitId:code:message:)"))) __attribute__((objc_designated_initializer));
- (HDCABannerNativeAdError *)doCopyAlias:(NSString *)alias adUnitId:(NSString *)adUnitId code:(int32_t)code message:(NSString *)message __attribute__((swift_name("doCopy(alias:adUnitId:code:message:)")));
- (BOOL)isEqual:(id _Nullable)other __attribute__((swift_name("isEqual(_:)")));
- (NSUInteger)hash __attribute__((swift_name("hash()")));
- (NSString *)description __attribute__((swift_name("description()")));
@property (readonly) NSString *adUnitId __attribute__((swift_name("adUnitId")));
@property (readonly) NSString *alias __attribute__((swift_name("alias")));
@property (readonly) int32_t code __attribute__((swift_name("code")));
@property (readonly) NSString *message __attribute__((swift_name("message")));
@end

__attribute__((objc_subclassing_restricted))
__attribute__((swift_name("BannerNativeAdInfo")))
@interface HDCABannerNativeAdInfo : HDCABase
- (instancetype)initWithAlias:(NSString *)alias instanceId:(NSString *)instanceId adUnitId:(NSString *)adUnitId layoutName:(NSString *)layoutName __attribute__((swift_name("init(alias:instanceId:adUnitId:layoutName:)"))) __attribute__((objc_designated_initializer));
- (HDCABannerNativeAdInfo *)doCopyAlias:(NSString *)alias instanceId:(NSString *)instanceId adUnitId:(NSString *)adUnitId layoutName:(NSString *)layoutName __attribute__((swift_name("doCopy(alias:instanceId:adUnitId:layoutName:)")));
- (BOOL)isEqual:(id _Nullable)other __attribute__((swift_name("isEqual(_:)")));
- (NSUInteger)hash __attribute__((swift_name("hash()")));
- (NSString *)description __attribute__((swift_name("description()")));
@property (readonly) NSString *adUnitId __attribute__((swift_name("adUnitId")));
@property (readonly) NSString *alias __attribute__((swift_name("alias")));
@property (readonly) NSString *instanceId __attribute__((swift_name("instanceId")));
@property (readonly) NSString *layoutName __attribute__((swift_name("layoutName")));
@end

__attribute__((objc_subclassing_restricted))
__attribute__((swift_name("BannerNativeAdRequest")))
@interface HDCABannerNativeAdRequest : HDCABase
- (instancetype)initWithInstanceId:(NSString *)instanceId adUnitIds:(NSArray<NSString *> *)adUnitIds __attribute__((swift_name("init(instanceId:adUnitIds:)"))) __attribute__((objc_designated_initializer));
- (HDCABannerNativeAdRequest *)doCopyInstanceId:(NSString *)instanceId adUnitIds:(NSArray<NSString *> *)adUnitIds __attribute__((swift_name("doCopy(instanceId:adUnitIds:)")));
- (BOOL)isEqual:(id _Nullable)other __attribute__((swift_name("isEqual(_:)")));
- (NSUInteger)hash __attribute__((swift_name("hash()")));
- (NSString *)description __attribute__((swift_name("description()")));
@property (readonly) NSArray<NSString *> *adUnitIds __attribute__((swift_name("adUnitIds")));
@property (readonly) NSString *instanceId __attribute__((swift_name("instanceId")));
@end

__attribute__((objc_subclassing_restricted))
__attribute__((swift_name("BannerNativeAdShowOptions")))
@interface HDCABannerNativeAdShowOptions : HDCABase
- (instancetype)initWithLayoutName:(NSString *)layoutName timeReloadSeconds:(int32_t)timeReloadSeconds timeCountdownSeconds:(int32_t)timeCountdownSeconds timeCollapseSeconds:(int32_t)timeCollapseSeconds __attribute__((swift_name("init(layoutName:timeReloadSeconds:timeCountdownSeconds:timeCollapseSeconds:)"))) __attribute__((objc_designated_initializer));
- (HDCABannerNativeAdShowOptions *)doCopyLayoutName:(NSString *)layoutName timeReloadSeconds:(int32_t)timeReloadSeconds timeCountdownSeconds:(int32_t)timeCountdownSeconds timeCollapseSeconds:(int32_t)timeCollapseSeconds __attribute__((swift_name("doCopy(layoutName:timeReloadSeconds:timeCountdownSeconds:timeCollapseSeconds:)")));
- (BOOL)isEqual:(id _Nullable)other __attribute__((swift_name("isEqual(_:)")));
- (NSUInteger)hash __attribute__((swift_name("hash()")));
- (NSString *)description __attribute__((swift_name("description()")));
@property (readonly) NSString *layoutName __attribute__((swift_name("layoutName")));
@property (readonly) int32_t timeCollapseSeconds __attribute__((swift_name("timeCollapseSeconds")));
@property (readonly) int32_t timeCountdownSeconds __attribute__((swift_name("timeCountdownSeconds")));
@property (readonly) int32_t timeReloadSeconds __attribute__((swift_name("timeReloadSeconds")));
@end

__attribute__((objc_subclassing_restricted))
__attribute__((swift_name("BannerNativeAdReloadPolicy")))
@interface HDCABannerNativeAdReloadPolicy : HDCABase
+ (instancetype)alloc __attribute__((unavailable));
+ (instancetype)allocWithZone:(struct _NSZone *)zone __attribute__((unavailable));
+ (instancetype)bannerNativeAdReloadPolicy __attribute__((swift_name("init()")));
@property (class, readonly, getter=shared) HDCABannerNativeAdReloadPolicy *shared __attribute__((swift_name("shared")));
- (int64_t)intervalMillisTimeReloadSeconds:(int32_t)timeReloadSeconds __attribute__((swift_name("intervalMillis(timeReloadSeconds:)")));
- (BOOL)isEnabledTimeReloadSeconds:(int32_t)timeReloadSeconds __attribute__((swift_name("isEnabled(timeReloadSeconds:)")));
@end

__attribute__((objc_subclassing_restricted))
__attribute__((swift_name("IosBannerNativeAdSdk")))
@interface HDCAIosBannerNativeAdSdk : HDCABase
- (instancetype)init __attribute__((swift_name("init()"))) __attribute__((objc_designated_initializer));
+ (instancetype)new __attribute__((availability(swift, unavailable, message="use object initializers instead")));
- (void)collapseAlias:(NSString *)alias __attribute__((swift_name("collapse(alias:)")));
- (void)createAlias:(NSString *)alias adUnitIdsCsv:(NSString *)adUnitIdsCsv layoutNamesCsv:(NSString *)layoutNamesCsv timeReloadSeconds:(int32_t)timeReloadSeconds timeCountdownSeconds:(int32_t)timeCountdownSeconds timeCollapseSeconds:(int32_t)timeCollapseSeconds __attribute__((swift_name("create(alias:adUnitIdsCsv:layoutNamesCsv:timeReloadSeconds:timeCountdownSeconds:timeCollapseSeconds:)")));
- (void)destroyAlias:(NSString *)alias __attribute__((swift_name("destroy(alias:)")));
- (BOOL)expandAlias:(NSString *)alias enableClick:(BOOL)enableClick __attribute__((swift_name("expand(alias:enableClick:)")));
- (void)hideAlias:(NSString *)alias __attribute__((swift_name("hide(alias:)")));
- (void)loadRootViewController:(UIViewController *)rootViewController alias:(NSString *)alias __attribute__((swift_name("load(rootViewController:alias:)")));
- (void)loadWithConfigRootViewController:(UIViewController *)rootViewController alias:(NSString *)alias adUnitIdsCsv:(NSString *)adUnitIdsCsv layoutNamesCsv:(NSString *)layoutNamesCsv timeReloadSeconds:(int32_t)timeReloadSeconds timeCountdownSeconds:(int32_t)timeCountdownSeconds timeCollapseSeconds:(int32_t)timeCollapseSeconds onStateChanged:(void (^)(HDCANativeAdState *))onStateChanged __attribute__((swift_name("loadWithConfig(rootViewController:alias:adUnitIdsCsv:layoutNamesCsv:timeReloadSeconds:timeCountdownSeconds:timeCollapseSeconds:onStateChanged:)")));
- (void)setCallbackAlias:(NSString *)alias callback:(void (^)(HDCANativeAdState *))callback __attribute__((swift_name("setCallback(alias:callback:)")));
- (void)showRootViewController:(UIViewController *)rootViewController alias:(NSString *)alias __attribute__((swift_name("show(rootViewController:alias:)")));
@end


/** A paid impression of a native banner or popup: its value in micros of [currencyCode], and who served the ad. */
__attribute__((objc_subclassing_restricted))
__attribute__((swift_name("NativeAdPaidInfo")))
@interface HDCANativeAdPaidInfo : HDCABase
- (instancetype)initWithAdUnitId:(NSString *)adUnitId revenueMicros:(int64_t)revenueMicros currencyCode:(NSString *)currencyCode mediationAdapter:(NSString * _Nullable)mediationAdapter adSource:(NSString * _Nullable)adSource responseId:(NSString * _Nullable)responseId precision:(int32_t)precision __attribute__((swift_name("init(adUnitId:revenueMicros:currencyCode:mediationAdapter:adSource:responseId:precision:)"))) __attribute__((objc_designated_initializer));
- (HDCANativeAdPaidInfo *)doCopyAdUnitId:(NSString *)adUnitId revenueMicros:(int64_t)revenueMicros currencyCode:(NSString *)currencyCode mediationAdapter:(NSString * _Nullable)mediationAdapter adSource:(NSString * _Nullable)adSource responseId:(NSString * _Nullable)responseId precision:(int32_t)precision __attribute__((swift_name("doCopy(adUnitId:revenueMicros:currencyCode:mediationAdapter:adSource:responseId:precision:)")));

/** A paid impression of a native banner or popup: its value in micros of [currencyCode], and who served the ad. */
- (BOOL)isEqual:(id _Nullable)other __attribute__((swift_name("isEqual(_:)")));

/** A paid impression of a native banner or popup: its value in micros of [currencyCode], and who served the ad. */
- (NSUInteger)hash __attribute__((swift_name("hash()")));

/** A paid impression of a native banner or popup: its value in micros of [currencyCode], and who served the ad. */
- (NSString *)description __attribute__((swift_name("description()")));
@property (readonly) NSString * _Nullable adSource __attribute__((swift_name("adSource")));
@property (readonly) NSString *adUnitId __attribute__((swift_name("adUnitId")));
@property (readonly) NSString *currencyCode __attribute__((swift_name("currencyCode")));
@property (readonly) NSString * _Nullable mediationAdapter __attribute__((swift_name("mediationAdapter")));

/** How exact the value is, as the network says: 0 unknown, 1 estimated, 2 publisher provided, 3 precise. */
@property (readonly) int32_t precision __attribute__((swift_name("precision")));
@property (readonly) NSString * _Nullable responseId __attribute__((swift_name("responseId")));
@property (readonly) int64_t revenueMicros __attribute__((swift_name("revenueMicros")));
@end


/** Forwards ad tracking events to the host app; the Unity bridge subscribes once at startup. */
__attribute__((objc_subclassing_restricted))
__attribute__((swift_name("IosAdEventCenter")))
@interface HDCAIosAdEventCenter : HDCABase
+ (instancetype)alloc __attribute__((unavailable));

/** Forwards ad tracking events to the host app; the Unity bridge subscribes once at startup. */
+ (instancetype)allocWithZone:(struct _NSZone *)zone __attribute__((unavailable));
+ (instancetype)iosAdEventCenter __attribute__((swift_name("init()")));
@property (class, readonly, getter=shared) HDCAIosAdEventCenter *shared __attribute__((swift_name("shared")));
- (void)subscribeListener:(void (^ _Nullable)(HDCAIosAdEvent *))listener __attribute__((swift_name("subscribe(listener:)")));
@end


/**
 * A tracking event (impression, click, paid revenue) for one ad instance.
 * [instanceId] is the alias the host (Unity) used when it loaded the ad.
 */
__attribute__((objc_subclassing_restricted))
__attribute__((swift_name("IosAdEvent")))
@interface HDCAIosAdEvent : HDCABase

/** JSON payload whose field names match Unity's `IOSNativeAdEventData`. */
- (NSString *)toUnityJson __attribute__((swift_name("toUnityJson()")));
@property (readonly) NSString *adSourceId __attribute__((swift_name("adSourceId")));
@property (readonly) NSString *adSourceInstanceId __attribute__((swift_name("adSourceInstanceId")));
@property (readonly) NSString *adSourceInstanceName __attribute__((swift_name("adSourceInstanceName")));
@property (readonly) NSString *adSourceName __attribute__((swift_name("adSourceName")));
@property (readonly) NSString *adUnitId __attribute__((swift_name("adUnitId")));
@property (readonly) NSString *currencyCode __attribute__((swift_name("currencyCode")));
@property (readonly) NSString *eventName __attribute__((swift_name("eventName")));
@property (readonly) NSString *format __attribute__((swift_name("format")));
@property (readonly) NSString *instanceId __attribute__((swift_name("instanceId")));
@property (readonly) NSString *mediationAdapter __attribute__((swift_name("mediationAdapter")));
@property (readonly) int32_t precision __attribute__((swift_name("precision")));
@property (readonly) NSString *responseId __attribute__((swift_name("responseId")));
@property (readonly) int64_t valueMicros __attribute__((swift_name("valueMicros")));
@end

__attribute__((swift_name("FullscreenNativeAdCallback")))
@protocol HDCAFullscreenNativeAdCallback
@required
- (void)onClickedAd_:(HDCAFullscreenNativeAdInfo *)ad __attribute__((swift_name("onClicked(ad_:)")));
- (void)onClosedAd_:(HDCAFullscreenNativeAdInfo *)ad __attribute__((swift_name("onClosed(ad_:)")));
- (void)onDisplayableAlias:(NSString *)alias __attribute__((swift_name("onDisplayable(alias:)")));
- (void)onDisplayedAd_:(HDCAFullscreenNativeAdInfo *)ad __attribute__((swift_name("onDisplayed(ad_:)")));
- (void)onFailedToLoadError_:(HDCAFullscreenNativeAdError *)error __attribute__((swift_name("onFailedToLoad(error_:)")));
- (void)onLoadedAd_:(HDCAFullscreenNativeAdInfo *)ad __attribute__((swift_name("onLoaded(ad_:)")));
- (void)onOpenedAd_:(HDCAFullscreenNativeAdInfo *)ad __attribute__((swift_name("onOpened(ad_:)")));
@end

__attribute__((objc_subclassing_restricted))
__attribute__((swift_name("FullscreenNativeAdConfig")))
@interface HDCAFullscreenNativeAdConfig : HDCABase
- (instancetype)initWithIds:(NSArray<NSString *> *)ids preloadBufferSize:(int32_t)preloadBufferSize enableReloadAfterShow:(BOOL)enableReloadAfterShow __attribute__((swift_name("init(ids:preloadBufferSize:enableReloadAfterShow:)"))) __attribute__((objc_designated_initializer));
- (HDCAFullscreenNativeAdConfig *)doCopyIds:(NSArray<NSString *> *)ids preloadBufferSize:(int32_t)preloadBufferSize enableReloadAfterShow:(BOOL)enableReloadAfterShow __attribute__((swift_name("doCopy(ids:preloadBufferSize:enableReloadAfterShow:)")));
- (BOOL)isEqual:(id _Nullable)other __attribute__((swift_name("isEqual(_:)")));
- (NSUInteger)hash __attribute__((swift_name("hash()")));
- (NSString *)description __attribute__((swift_name("description()")));
@property (readonly) BOOL enableReloadAfterShow __attribute__((swift_name("enableReloadAfterShow")));
@property (readonly) NSArray<NSString *> *ids __attribute__((swift_name("ids")));
@property (readonly) int32_t preloadBufferSize __attribute__((swift_name("preloadBufferSize")));
@end

__attribute__((objc_subclassing_restricted))
__attribute__((swift_name("FullscreenNativeAdError")))
@interface HDCAFullscreenNativeAdError : HDCABase
- (instancetype)initWithAlias:(NSString *)alias adUnitId:(NSString *)adUnitId code:(int32_t)code message:(NSString *)message __attribute__((swift_name("init(alias:adUnitId:code:message:)"))) __attribute__((objc_designated_initializer));
- (HDCAFullscreenNativeAdError *)doCopyAlias:(NSString *)alias adUnitId:(NSString *)adUnitId code:(int32_t)code message:(NSString *)message __attribute__((swift_name("doCopy(alias:adUnitId:code:message:)")));
- (BOOL)isEqual:(id _Nullable)other __attribute__((swift_name("isEqual(_:)")));
- (NSUInteger)hash __attribute__((swift_name("hash()")));
- (NSString *)description __attribute__((swift_name("description()")));
@property (readonly) NSString *adUnitId __attribute__((swift_name("adUnitId")));
@property (readonly) NSString *alias __attribute__((swift_name("alias")));
@property (readonly) int32_t code __attribute__((swift_name("code")));
@property (readonly) NSString *message __attribute__((swift_name("message")));
@end

__attribute__((objc_subclassing_restricted))
__attribute__((swift_name("FullscreenNativeAdInfo")))
@interface HDCAFullscreenNativeAdInfo : HDCABase
- (instancetype)initWithAlias:(NSString *)alias adUnitId:(NSString *)adUnitId layoutName:(NSString *)layoutName adSourceId:(NSString *)adSourceId adSourceName:(NSString *)adSourceName __attribute__((swift_name("init(alias:adUnitId:layoutName:adSourceId:adSourceName:)"))) __attribute__((objc_designated_initializer));
- (HDCAFullscreenNativeAdInfo *)doCopyAlias:(NSString *)alias adUnitId:(NSString *)adUnitId layoutName:(NSString *)layoutName adSourceId:(NSString *)adSourceId adSourceName:(NSString *)adSourceName __attribute__((swift_name("doCopy(alias:adUnitId:layoutName:adSourceId:adSourceName:)")));
- (BOOL)isEqual:(id _Nullable)other __attribute__((swift_name("isEqual(_:)")));
- (NSUInteger)hash __attribute__((swift_name("hash()")));
- (NSString *)description __attribute__((swift_name("description()")));

/** The ad source (mediation network) that filled the ad, when the network tells. */
@property (readonly) NSString *adSourceId __attribute__((swift_name("adSourceId")));
@property (readonly) NSString *adSourceName __attribute__((swift_name("adSourceName")));
@property (readonly) NSString *adUnitId __attribute__((swift_name("adUnitId")));
@property (readonly) NSString *alias __attribute__((swift_name("alias")));
@property (readonly) NSString *layoutName __attribute__((swift_name("layoutName")));
@end

__attribute__((swift_name("KotlinComparable")))
@protocol HDCAKotlinComparable
@required
- (int32_t)compareToOther:(id _Nullable)other __attribute__((swift_name("compareTo(other:)")));
@end

__attribute__((swift_name("KotlinEnum")))
@interface HDCAKotlinEnum<E> : HDCABase <HDCAKotlinComparable>
- (instancetype)initWithName:(NSString *)name ordinal:(int32_t)ordinal __attribute__((swift_name("init(name:ordinal:)"))) __attribute__((objc_designated_initializer));
@property (class, readonly, getter=companion) HDCAKotlinEnumCompanion *companion __attribute__((swift_name("companion")));
- (int32_t)compareToOther:(E)other __attribute__((swift_name("compareTo(other:)")));
- (BOOL)isEqual:(id _Nullable)other __attribute__((swift_name("isEqual(_:)")));
- (NSUInteger)hash __attribute__((swift_name("hash()")));
- (NSString *)description __attribute__((swift_name("description()")));
@property (readonly) NSString *name __attribute__((swift_name("name")));
@property (readonly) int32_t ordinal __attribute__((swift_name("ordinal")));
@end

__attribute__((objc_subclassing_restricted))
__attribute__((swift_name("FullscreenNativeAdMode")))
@interface HDCAFullscreenNativeAdMode : HDCAKotlinEnum<HDCAFullscreenNativeAdMode *>
+ (instancetype)alloc __attribute__((unavailable));
+ (instancetype)allocWithZone:(struct _NSZone *)zone __attribute__((unavailable));
- (instancetype)initWithName:(NSString *)name ordinal:(int32_t)ordinal __attribute__((swift_name("init(name:ordinal:)"))) __attribute__((objc_designated_initializer)) __attribute__((unavailable));
@property (class, readonly, getter=companion) HDCAFullscreenNativeAdModeCompanion *companion __attribute__((swift_name("companion")));
@property (class, readonly) HDCAFullscreenNativeAdMode *single __attribute__((swift_name("single")));
@property (class, readonly) HDCAFullscreenNativeAdMode *transparent __attribute__((swift_name("transparent")));
@property (class, readonly) HDCAFullscreenNativeAdMode *ctr __attribute__((swift_name("ctr")));
@property (class, readonly) HDCAFullscreenNativeAdMode *multiple __attribute__((swift_name("multiple")));
@property (class, readonly) HDCAFullscreenNativeAdMode *sequence __attribute__((swift_name("sequence")));
@property (class, readonly) HDCAFullscreenNativeAdMode *overlay __attribute__((swift_name("overlay")));
@property (class, readonly) HDCAFullscreenNativeAdMode *overlayTransparent __attribute__((swift_name("overlayTransparent")));
@property (class, readonly) HDCAFullscreenNativeAdMode *overlayCtr __attribute__((swift_name("overlayCtr")));
@property (class, readonly) HDCAFullscreenNativeAdMode *overlayCls __attribute__((swift_name("overlayCls")));
@property (class, readonly) HDCAFullscreenNativeAdMode *overlayNav __attribute__((swift_name("overlayNav")));
@property (class, readonly) HDCAFullscreenNativeAdMode *overlayPgrs __attribute__((swift_name("overlayPgrs")));
@property (class, readonly) HDCAFullscreenNativeAdMode *overlayPgrsCls __attribute__((swift_name("overlayPgrsCls")));
@property (class, readonly) HDCAFullscreenNativeAdMode *loop __attribute__((swift_name("loop")));
+ (HDCAKotlinArray<HDCAFullscreenNativeAdMode *> *)values __attribute__((swift_name("values()")));
@property (class, readonly) NSArray<HDCAFullscreenNativeAdMode *> *entries __attribute__((swift_name("entries")));
@property (readonly) BOOL alwaysPausesGameplay __attribute__((swift_name("alwaysPausesGameplay")));
@property (readonly) BOOL isProgressOverlay __attribute__((swift_name("isProgressOverlay")));
@property (readonly) BOOL keepsDuplicateLayouts __attribute__((swift_name("keepsDuplicateLayouts")));
@end

__attribute__((objc_subclassing_restricted))
__attribute__((swift_name("FullscreenNativeAdMode.Companion")))
@interface HDCAFullscreenNativeAdModeCompanion : HDCABase
+ (instancetype)alloc __attribute__((unavailable));
+ (instancetype)allocWithZone:(struct _NSZone *)zone __attribute__((unavailable));
+ (instancetype)companion __attribute__((swift_name("init()")));
@property (class, readonly, getter=shared) HDCAFullscreenNativeAdModeCompanion *shared __attribute__((swift_name("shared")));
- (HDCAFullscreenNativeAdMode *)fromNameValue:(NSString * _Nullable)value __attribute__((swift_name("fromName(value:)")));
@end

__attribute__((objc_subclassing_restricted))
__attribute__((swift_name("FullscreenNativeAdRequest")))
@interface HDCAFullscreenNativeAdRequest : HDCABase
- (instancetype)initWithInstanceId:(NSString *)instanceId adUnitIds:(NSArray<NSString *> *)adUnitIds __attribute__((swift_name("init(instanceId:adUnitIds:)"))) __attribute__((objc_designated_initializer));
- (HDCAFullscreenNativeAdRequest *)doCopyInstanceId:(NSString *)instanceId adUnitIds:(NSArray<NSString *> *)adUnitIds __attribute__((swift_name("doCopy(instanceId:adUnitIds:)")));
- (BOOL)isEqual:(id _Nullable)other __attribute__((swift_name("isEqual(_:)")));
- (NSUInteger)hash __attribute__((swift_name("hash()")));
- (NSString *)description __attribute__((swift_name("description()")));
@property (readonly) NSArray<NSString *> *adUnitIds __attribute__((swift_name("adUnitIds")));
@property (readonly) NSString *instanceId __attribute__((swift_name("instanceId")));
@end

__attribute__((objc_subclassing_restricted))
__attribute__((swift_name("FullscreenNativeAdShowOptions")))
@interface HDCAFullscreenNativeAdShowOptions : HDCABase
- (instancetype)initWithLayoutName:(NSString *)layoutName durationSeconds:(double)durationSeconds autoClose:(BOOL)autoClose pauseGameplay:(BOOL)pauseGameplay mode:(HDCAFullscreenNativeAdMode *)mode layoutNames:(NSArray<NSString *> *)layoutNames durationsSeconds:(NSArray<HDCADouble *> *)durationsSeconds orientation:(NSString *)orientation enableAdComeback:(BOOL)enableAdComeback showTCD:(BOOL)showTCD delaySeconds:(double)delaySeconds timeUpCSeconds:(int32_t)timeUpCSeconds clickAssets:(HDCANativeClickAssetOptions *)clickAssets assetVisibility:(HDCANativeAssetVisibilityOptions *)assetVisibility __attribute__((swift_name("init(layoutName:durationSeconds:autoClose:pauseGameplay:mode:layoutNames:durationsSeconds:orientation:enableAdComeback:showTCD:delaySeconds:timeUpCSeconds:clickAssets:assetVisibility:)"))) __attribute__((objc_designated_initializer));
@property (class, readonly, getter=companion) HDCAFullscreenNativeAdShowOptionsCompanion *companion __attribute__((swift_name("companion")));
- (HDCAFullscreenNativeAdShowOptions *)doCopyLayoutName:(NSString *)layoutName durationSeconds:(double)durationSeconds autoClose:(BOOL)autoClose pauseGameplay:(BOOL)pauseGameplay mode:(HDCAFullscreenNativeAdMode *)mode layoutNames:(NSArray<NSString *> *)layoutNames durationsSeconds:(NSArray<HDCADouble *> *)durationsSeconds orientation:(NSString *)orientation enableAdComeback:(BOOL)enableAdComeback showTCD:(BOOL)showTCD delaySeconds:(double)delaySeconds timeUpCSeconds:(int32_t)timeUpCSeconds clickAssets:(HDCANativeClickAssetOptions *)clickAssets assetVisibility:(HDCANativeAssetVisibilityOptions *)assetVisibility __attribute__((swift_name("doCopy(layoutName:durationSeconds:autoClose:pauseGameplay:mode:layoutNames:durationsSeconds:orientation:enableAdComeback:showTCD:delaySeconds:timeUpCSeconds:clickAssets:assetVisibility:)")));
- (BOOL)isEqual:(id _Nullable)other __attribute__((swift_name("isEqual(_:)")));
- (NSUInteger)hash __attribute__((swift_name("hash()")));
- (NSString *)description __attribute__((swift_name("description()")));
@property (readonly) HDCANativeAssetVisibilityOptions *assetVisibility __attribute__((swift_name("assetVisibility")));
@property (readonly) BOOL autoClose __attribute__((swift_name("autoClose")));
@property (readonly) HDCANativeClickAssetOptions *clickAssets __attribute__((swift_name("clickAssets")));
@property (readonly) double delaySeconds __attribute__((swift_name("delaySeconds")));
@property (readonly) double durationSeconds __attribute__((swift_name("durationSeconds")));
@property (readonly) NSArray<HDCADouble *> *durationsSeconds __attribute__((swift_name("durationsSeconds")));
@property (readonly) BOOL enableAdComeback __attribute__((swift_name("enableAdComeback")));
@property (readonly) NSString *layoutName __attribute__((swift_name("layoutName")));
@property (readonly) NSArray<NSString *> *layoutNames __attribute__((swift_name("layoutNames")));
@property (readonly) HDCAFullscreenNativeAdMode *mode __attribute__((swift_name("mode")));
@property (readonly) NSArray<HDCADouble *> *normalizedDurationsSeconds __attribute__((swift_name("normalizedDurationsSeconds")));
@property (readonly) NSString *normalizedLayoutName __attribute__((swift_name("normalizedLayoutName")));
@property (readonly) NSArray<NSString *> *normalizedLayoutNames __attribute__((swift_name("normalizedLayoutNames")));
@property (readonly) NSString *orientation __attribute__((swift_name("orientation")));
@property (readonly) BOOL pauseGameplay __attribute__((swift_name("pauseGameplay")));
@property (readonly) double resolvedDurationSeconds __attribute__((swift_name("resolvedDurationSeconds")));
@property (readonly) BOOL showTCD __attribute__((swift_name("showTCD")));
@property (readonly) int32_t timeUpCSeconds __attribute__((swift_name("timeUpCSeconds")));
@end

__attribute__((objc_subclassing_restricted))
__attribute__((swift_name("FullscreenNativeAdShowOptions.Companion")))
@interface HDCAFullscreenNativeAdShowOptionsCompanion : HDCABase
+ (instancetype)alloc __attribute__((unavailable));
+ (instancetype)allocWithZone:(struct _NSZone *)zone __attribute__((unavailable));
+ (instancetype)companion __attribute__((swift_name("init()")));
@property (class, readonly, getter=shared) HDCAFullscreenNativeAdShowOptionsCompanion *shared __attribute__((swift_name("shared")));
@end

__attribute__((objc_subclassing_restricted))
__attribute__((swift_name("NativeAdShowOptions")))
@interface HDCANativeAdShowOptions : HDCABase
- (instancetype)initWithInstanceId:(NSString *)instanceId layoutName:(NSString *)layoutName durationSeconds:(double)durationSeconds autoClose:(BOOL)autoClose pauseGameplay:(BOOL)pauseGameplay enableAdComeback:(BOOL)enableAdComeback showTCD:(BOOL)showTCD delaySeconds:(double)delaySeconds timeUpCSeconds:(int32_t)timeUpCSeconds mode:(HDCAFullscreenNativeAdMode *)mode layoutNames:(NSArray<NSString *> *)layoutNames durationsSeconds:(NSArray<HDCADouble *> *)durationsSeconds clickAssets:(HDCANativeClickAssetOptions *)clickAssets assetVisibility:(HDCANativeAssetVisibilityOptions *)assetVisibility orientation:(NSString *)orientation __attribute__((swift_name("init(instanceId:layoutName:durationSeconds:autoClose:pauseGameplay:enableAdComeback:showTCD:delaySeconds:timeUpCSeconds:mode:layoutNames:durationsSeconds:clickAssets:assetVisibility:orientation:)"))) __attribute__((objc_designated_initializer));
- (HDCANativeAdShowOptions *)doCopyInstanceId:(NSString *)instanceId layoutName:(NSString *)layoutName durationSeconds:(double)durationSeconds autoClose:(BOOL)autoClose pauseGameplay:(BOOL)pauseGameplay enableAdComeback:(BOOL)enableAdComeback showTCD:(BOOL)showTCD delaySeconds:(double)delaySeconds timeUpCSeconds:(int32_t)timeUpCSeconds mode:(HDCAFullscreenNativeAdMode *)mode layoutNames:(NSArray<NSString *> *)layoutNames durationsSeconds:(NSArray<HDCADouble *> *)durationsSeconds clickAssets:(HDCANativeClickAssetOptions *)clickAssets assetVisibility:(HDCANativeAssetVisibilityOptions *)assetVisibility orientation:(NSString *)orientation __attribute__((swift_name("doCopy(instanceId:layoutName:durationSeconds:autoClose:pauseGameplay:enableAdComeback:showTCD:delaySeconds:timeUpCSeconds:mode:layoutNames:durationsSeconds:clickAssets:assetVisibility:orientation:)")));
- (BOOL)isEqual:(id _Nullable)other __attribute__((swift_name("isEqual(_:)")));
- (NSUInteger)hash __attribute__((swift_name("hash()")));
- (NSString *)description __attribute__((swift_name("description()")));
@property (readonly) HDCANativeAssetVisibilityOptions *assetVisibility __attribute__((swift_name("assetVisibility")));
@property (readonly) BOOL autoClose __attribute__((swift_name("autoClose")));
@property (readonly) HDCANativeClickAssetOptions *clickAssets __attribute__((swift_name("clickAssets")));
@property (readonly) double delaySeconds __attribute__((swift_name("delaySeconds")));
@property (readonly) double durationSeconds __attribute__((swift_name("durationSeconds")));
@property (readonly) NSArray<HDCADouble *> *durationsSeconds __attribute__((swift_name("durationsSeconds")));
@property (readonly) BOOL enableAdComeback __attribute__((swift_name("enableAdComeback")));
@property (readonly) NSString *instanceId __attribute__((swift_name("instanceId")));
@property (readonly) NSString *layoutName __attribute__((swift_name("layoutName")));
@property (readonly) NSArray<NSString *> *layoutNames __attribute__((swift_name("layoutNames")));
@property (readonly) HDCAFullscreenNativeAdMode *mode __attribute__((swift_name("mode")));
@property (readonly) NSString *orientation __attribute__((swift_name("orientation")));
@property (readonly) BOOL pauseGameplay __attribute__((swift_name("pauseGameplay")));
@property (readonly) BOOL showTCD __attribute__((swift_name("showTCD")));
@property (readonly) int32_t timeUpCSeconds __attribute__((swift_name("timeUpCSeconds")));
@end

__attribute__((objc_subclassing_restricted))
__attribute__((swift_name("NativeAssetVisibilityOptions")))
@interface HDCANativeAssetVisibilityOptions : HDCABase
- (instancetype)initWithCta:(BOOL)cta headline:(BOOL)headline body:(BOOL)body description:(BOOL)description icon:(BOOL)icon advertiser:(BOOL)advertiser media:(BOOL)media mediaImage:(BOOL)mediaImage mediaVideo:(BOOL)mediaVideo price:(BOOL)price store:(BOOL)store starRating:(BOOL)starRating __attribute__((swift_name("init(cta:headline:body:description:icon:advertiser:media:mediaImage:mediaVideo:price:store:starRating:)"))) __attribute__((objc_designated_initializer));
- (HDCANativeAssetVisibilityOptions *)doCopyCta:(BOOL)cta headline:(BOOL)headline body:(BOOL)body description:(BOOL)description icon:(BOOL)icon advertiser:(BOOL)advertiser media:(BOOL)media mediaImage:(BOOL)mediaImage mediaVideo:(BOOL)mediaVideo price:(BOOL)price store:(BOOL)store starRating:(BOOL)starRating __attribute__((swift_name("doCopy(cta:headline:body:description:icon:advertiser:media:mediaImage:mediaVideo:price:store:starRating:)")));
- (BOOL)isEqual:(id _Nullable)other __attribute__((swift_name("isEqual(_:)")));
- (NSUInteger)hash __attribute__((swift_name("hash()")));
- (NSString *)description __attribute__((swift_name("description()")));
@property (readonly) BOOL advertiser __attribute__((swift_name("advertiser")));
@property (readonly) BOOL anyMediaVisible __attribute__((swift_name("anyMediaVisible")));
@property (readonly) BOOL body __attribute__((swift_name("body")));
@property (readonly) BOOL bodyOrDescriptionVisible __attribute__((swift_name("bodyOrDescriptionVisible")));
@property (readonly) BOOL cta __attribute__((swift_name("cta")));
@property (readonly) BOOL description_ __attribute__((swift_name("description_")));
@property (readonly) BOOL headline __attribute__((swift_name("headline")));
@property (readonly) BOOL icon __attribute__((swift_name("icon")));
@property (readonly) BOOL media __attribute__((swift_name("media")));
@property (readonly) BOOL mediaImage __attribute__((swift_name("mediaImage")));
@property (readonly) BOOL mediaImageVisible __attribute__((swift_name("mediaImageVisible")));
@property (readonly) BOOL mediaVideo __attribute__((swift_name("mediaVideo")));
@property (readonly) BOOL mediaVideoVisible __attribute__((swift_name("mediaVideoVisible")));
@property (readonly) BOOL price __attribute__((swift_name("price")));
@property (readonly) BOOL starRating __attribute__((swift_name("starRating")));
@property (readonly) BOOL store __attribute__((swift_name("store")));
@end

__attribute__((objc_subclassing_restricted))
__attribute__((swift_name("NativeClickAssetOptions")))
@interface HDCANativeClickAssetOptions : HDCABase
- (instancetype)initWithCta:(BOOL)cta headline:(BOOL)headline body:(BOOL)body description:(BOOL)description icon:(BOOL)icon advertiser:(BOOL)advertiser media:(BOOL)media mediaImage:(BOOL)mediaImage mediaVideo:(BOOL)mediaVideo __attribute__((swift_name("init(cta:headline:body:description:icon:advertiser:media:mediaImage:mediaVideo:)"))) __attribute__((objc_designated_initializer));
- (HDCANativeClickAssetOptions *)doCopyCta:(BOOL)cta headline:(BOOL)headline body:(BOOL)body description:(BOOL)description icon:(BOOL)icon advertiser:(BOOL)advertiser media:(BOOL)media mediaImage:(BOOL)mediaImage mediaVideo:(BOOL)mediaVideo __attribute__((swift_name("doCopy(cta:headline:body:description:icon:advertiser:media:mediaImage:mediaVideo:)")));
- (BOOL)isEqual:(id _Nullable)other __attribute__((swift_name("isEqual(_:)")));
- (NSUInteger)hash __attribute__((swift_name("hash()")));
- (NSString *)description __attribute__((swift_name("description()")));
@property (readonly) BOOL advertiser __attribute__((swift_name("advertiser")));
@property (readonly) BOOL anyMediaClickEnabled __attribute__((swift_name("anyMediaClickEnabled")));
@property (readonly) BOOL body __attribute__((swift_name("body")));
@property (readonly) BOOL bodyOrDescriptionClickEnabled __attribute__((swift_name("bodyOrDescriptionClickEnabled")));
@property (readonly) BOOL cta __attribute__((swift_name("cta")));
@property (readonly) BOOL description_ __attribute__((swift_name("description_")));
@property (readonly) BOOL headline __attribute__((swift_name("headline")));
@property (readonly) BOOL icon __attribute__((swift_name("icon")));
@property (readonly) BOOL media __attribute__((swift_name("media")));
@property (readonly) BOOL mediaImage __attribute__((swift_name("mediaImage")));
@property (readonly) BOOL mediaImageClickEnabled __attribute__((swift_name("mediaImageClickEnabled")));
@property (readonly) BOOL mediaVideo __attribute__((swift_name("mediaVideo")));
@property (readonly) BOOL mediaVideoClickEnabled __attribute__((swift_name("mediaVideoClickEnabled")));
@end

__attribute__((objc_subclassing_restricted))
__attribute__((swift_name("FullscreenNativeAdRegistry")))
@interface HDCAFullscreenNativeAdRegistry : HDCABase
+ (instancetype)alloc __attribute__((unavailable));
+ (instancetype)allocWithZone:(struct _NSZone *)zone __attribute__((unavailable));
+ (instancetype)fullscreenNativeAdRegistry __attribute__((swift_name("init()")));
@property (class, readonly, getter=shared) HDCAFullscreenNativeAdRegistry *shared __attribute__((swift_name("shared")));
- (HDCAFullscreenNativeAdConfig *)configForAlias:(NSString *)alias fallbackAdUnitId:(NSString *)fallbackAdUnitId __attribute__((swift_name("configFor(alias:fallbackAdUnitId:)")));
- (void)createAlias:(NSString *)alias config:(HDCAFullscreenNativeAdConfig *)config __attribute__((swift_name("create(alias:config:)")));
- (void)destroyAlias:(NSString *)alias __attribute__((swift_name("destroy(alias:)")));
- (BOOL)isReadyAlias:(NSString *)alias __attribute__((swift_name("isReady(alias:)")));
- (NSString * _Nullable)loadedAdUnitIdAlias:(NSString *)alias __attribute__((swift_name("loadedAdUnitId(alias:)")));
- (void)markLoadingAlias:(NSString *)alias __attribute__((swift_name("markLoading(alias:)")));

/**
 * Takes the loaded ad out of the ready state as soon as its screen is built, so a second show
 * cannot reuse it. [notifyDisplayed] still runs once the ad is actually on screen.
 */
- (void)markShowingAlias:(NSString *)alias __attribute__((swift_name("markShowing(alias:)")));
- (void)notifyClickedAlias:(NSString *)alias __attribute__((swift_name("notifyClicked(alias:)")));
- (void)notifyClosedAlias:(NSString *)alias __attribute__((swift_name("notifyClosed(alias:)")));
- (void)notifyDisplayedAlias:(NSString *)alias adUnitId:(NSString *)adUnitId layoutName:(NSString *)layoutName __attribute__((swift_name("notifyDisplayed(alias:adUnitId:layoutName:)")));
- (void)notifyFailedAlias:(NSString *)alias adUnitId:(NSString *)adUnitId message:(NSString *)message code:(int32_t)code __attribute__((swift_name("notifyFailed(alias:adUnitId:message:code:)")));
- (void)notifyLoadedAlias:(NSString *)alias adUnitId:(NSString *)adUnitId adSourceId:(NSString *)adSourceId adSourceName:(NSString *)adSourceName __attribute__((swift_name("notifyLoaded(alias:adUnitId:adSourceId:adSourceName:)")));
- (void)notifyStateAlias:(NSString *)alias state:(HDCANativeAdState *)state adUnitId:(NSString * _Nullable)adUnitId errorMessage:(NSString * _Nullable)errorMessage errorCode:(int32_t)errorCode adSourceId:(NSString *)adSourceId adSourceName:(NSString *)adSourceName __attribute__((swift_name("notifyState(alias:state:adUnitId:errorMessage:errorCode:adSourceId:adSourceName:)")));

/** Undoes [markShowing] for a screen that never appeared, so the ad that is still loaded can be shown later. */
- (void)restoreReadyAlias:(NSString *)alias __attribute__((swift_name("restoreReady(alias:)")));
- (void)setCallbackAlias:(NSString *)alias callback:(id<HDCAFullscreenNativeAdCallback> _Nullable)callback __attribute__((swift_name("setCallback(alias:callback:)")));
- (BOOL)shouldReloadAfterShowAlias:(NSString *)alias __attribute__((swift_name("shouldReloadAfterShow(alias:)")));
- (HDCANativeAdState *)stateForAlias:(NSString *)alias __attribute__((swift_name("stateFor(alias:)")));
@end

__attribute__((objc_subclassing_restricted))
__attribute__((swift_name("IosFullscreenNativeAdSdk")))
@interface HDCAIosFullscreenNativeAdSdk : HDCABase
- (instancetype)init __attribute__((swift_name("init()"))) __attribute__((objc_designated_initializer));
+ (instancetype)new __attribute__((availability(swift, unavailable, message="use object initializers instead")));
- (void)createAlias:(NSString *)alias adUnitIdsCsv:(NSString *)adUnitIdsCsv preloadBufferSize:(int32_t)preloadBufferSize enableReloadAfterShow:(BOOL)enableReloadAfterShow __attribute__((swift_name("create(alias:adUnitIdsCsv:preloadBufferSize:enableReloadAfterShow:)")));
- (void)destroyAlias:(NSString *)alias __attribute__((swift_name("destroy(alias:)")));
- (BOOL)isReadyAlias:(NSString *)alias __attribute__((swift_name("isReady(alias:)")));
- (void)loadRootViewController:(UIViewController *)rootViewController alias:(NSString *)alias __attribute__((swift_name("load(rootViewController:alias:)")));
- (void)loadWithIdsRootViewController:(UIViewController *)rootViewController alias:(NSString *)alias adUnitIdsCsv:(NSString *)adUnitIdsCsv enableReloadAfterShow:(BOOL)enableReloadAfterShow __attribute__((swift_name("loadWithIds(rootViewController:alias:adUnitIdsCsv:enableReloadAfterShow:)")));
- (void)setCallbackAlias:(NSString *)alias callback:(id<HDCAFullscreenNativeAdCallback> _Nullable)callback __attribute__((swift_name("setCallback(alias:callback:)")));
- (UIViewController *)viewControllerAlias:(NSString *)alias options:(HDCAFullscreenNativeAdShowOptions *)options __attribute__((swift_name("viewController(alias:options:)")));
- (UIViewController *)viewControllerWithLayoutNameAlias:(NSString *)alias layoutName:(NSString *)layoutName durationSeconds:(double)durationSeconds autoClose:(BOOL)autoClose __attribute__((swift_name("viewControllerWithLayoutName(alias:layoutName:durationSeconds:autoClose:)")));
@end

__attribute__((swift_name("InterstitialAdCallback")))
@protocol HDCAInterstitialAdCallback
@required
- (void)onInterstitialClickedAd:(HDCAInterstitialAdInfo *)ad __attribute__((swift_name("onInterstitialClicked(ad:)")));
- (void)onInterstitialClosedAd:(HDCAInterstitialAdInfo *)ad __attribute__((swift_name("onInterstitialClosed(ad:)")));
- (void)onInterstitialDisplayable __attribute__((swift_name("onInterstitialDisplayable()")));
- (void)onInterstitialDisplayedAd:(HDCAInterstitialAdInfo *)ad __attribute__((swift_name("onInterstitialDisplayed(ad:)")));
- (void)onInterstitialFailedToLoadAdUnit:(NSString *)adUnit errorCode:(int32_t)errorCode error:(NSString *)error __attribute__((swift_name("onInterstitialFailedToLoad(adUnit:errorCode:error:)")));

/** A show that failed. By default it goes to [onInterstitialFailedToLoad], which used to hear show failures too. */
- (void)onInterstitialFailedToShowAdUnit:(NSString *)adUnit errorCode:(int32_t)errorCode error:(NSString *)error __attribute__((swift_name("onInterstitialFailedToShow(adUnit:errorCode:error:)")));
- (void)onInterstitialImpressionAd:(HDCAInterstitialAdInfo *)ad __attribute__((swift_name("onInterstitialImpression(ad:)")));
- (void)onInterstitialLoadedAd:(HDCAInterstitialAdInfo *)ad __attribute__((swift_name("onInterstitialLoaded(ad:)")));
- (void)onInterstitialOpenedAd:(HDCAInterstitialAdInfo *)ad __attribute__((swift_name("onInterstitialOpened(ad:)")));
- (void)onInterstitialPaidImpressionAd:(HDCAInterstitialAdInfo *)ad paidInfo:(HDCAInterstitialAdPaidInfo *)paidInfo __attribute__((swift_name("onInterstitialPaidImpression(ad:paidInfo:)")));
@end

__attribute__((objc_subclassing_restricted))
__attribute__((swift_name("InterstitialAdConfig")))
@interface HDCAInterstitialAdConfig : HDCABase
- (instancetype)initWithIds:(NSArray<NSString *> *)ids autoReload:(BOOL)autoReload preloadBufferSize:(int32_t)preloadBufferSize __attribute__((swift_name("init(ids:autoReload:preloadBufferSize:)"))) __attribute__((objc_designated_initializer));
- (HDCAInterstitialAdConfig *)doCopyIds:(NSArray<NSString *> *)ids autoReload:(BOOL)autoReload preloadBufferSize:(int32_t)preloadBufferSize __attribute__((swift_name("doCopy(ids:autoReload:preloadBufferSize:)")));
- (BOOL)isEqual:(id _Nullable)other __attribute__((swift_name("isEqual(_:)")));
- (NSUInteger)hash __attribute__((swift_name("hash()")));
- (NSString *)description __attribute__((swift_name("description()")));
@property (readonly) BOOL autoReload __attribute__((swift_name("autoReload")));
@property (readonly) NSArray<NSString *> *ids __attribute__((swift_name("ids")));
@property (readonly) int32_t preloadBufferSize __attribute__((swift_name("preloadBufferSize")));
@end

__attribute__((objc_subclassing_restricted))
__attribute__((swift_name("InterstitialAdInfo")))
@interface HDCAInterstitialAdInfo : HDCABase
- (instancetype)initWithAdUnitId:(NSString *)adUnitId mediationAdapter:(NSString *)mediationAdapter responseId:(NSString *)responseId adSource:(NSString * _Nullable)adSource __attribute__((swift_name("init(adUnitId:mediationAdapter:responseId:adSource:)"))) __attribute__((objc_designated_initializer));
- (HDCAInterstitialAdInfo *)doCopyAdUnitId:(NSString *)adUnitId mediationAdapter:(NSString *)mediationAdapter responseId:(NSString *)responseId adSource:(NSString * _Nullable)adSource __attribute__((swift_name("doCopy(adUnitId:mediationAdapter:responseId:adSource:)")));
- (BOOL)isEqual:(id _Nullable)other __attribute__((swift_name("isEqual(_:)")));
- (NSUInteger)hash __attribute__((swift_name("hash()")));
- (NSString *)description __attribute__((swift_name("description()")));
@property (readonly) NSString * _Nullable adSource __attribute__((swift_name("adSource")));
@property (readonly) NSString *adUnitId __attribute__((swift_name("adUnitId")));
@property (readonly) NSString *mediationAdapter __attribute__((swift_name("mediationAdapter")));
@property (readonly) NSString *responseId __attribute__((swift_name("responseId")));
@end

__attribute__((objc_subclassing_restricted))
__attribute__((swift_name("InterstitialAdPaidInfo")))
@interface HDCAInterstitialAdPaidInfo : HDCABase
- (instancetype)initWithAdUnitId:(NSString *)adUnitId revenueMicros:(int64_t)revenueMicros currencyCode:(NSString *)currencyCode mediationAdapter:(NSString *)mediationAdapter adSource:(NSString * _Nullable)adSource responseId:(NSString *)responseId precision:(int32_t)precision __attribute__((swift_name("init(adUnitId:revenueMicros:currencyCode:mediationAdapter:adSource:responseId:precision:)"))) __attribute__((objc_designated_initializer));
- (HDCAInterstitialAdPaidInfo *)doCopyAdUnitId:(NSString *)adUnitId revenueMicros:(int64_t)revenueMicros currencyCode:(NSString *)currencyCode mediationAdapter:(NSString *)mediationAdapter adSource:(NSString * _Nullable)adSource responseId:(NSString *)responseId precision:(int32_t)precision __attribute__((swift_name("doCopy(adUnitId:revenueMicros:currencyCode:mediationAdapter:adSource:responseId:precision:)")));
- (BOOL)isEqual:(id _Nullable)other __attribute__((swift_name("isEqual(_:)")));
- (NSUInteger)hash __attribute__((swift_name("hash()")));
- (NSString *)description __attribute__((swift_name("description()")));
@property (readonly) NSString * _Nullable adSource __attribute__((swift_name("adSource")));
@property (readonly) NSString *adUnitId __attribute__((swift_name("adUnitId")));
@property (readonly) NSString *currencyCode __attribute__((swift_name("currencyCode")));
@property (readonly) NSString *mediationAdapter __attribute__((swift_name("mediationAdapter")));

/** How exact the value is, as the network says: 0 unknown, 1 estimated, 2 publisher provided, 3 precise. */
@property (readonly) int32_t precision __attribute__((swift_name("precision")));
@property (readonly) NSString *responseId __attribute__((swift_name("responseId")));
@property (readonly) int64_t revenueMicros __attribute__((swift_name("revenueMicros")));
@end

__attribute__((objc_subclassing_restricted))
__attribute__((swift_name("InterstitialShowOptions")))
@interface HDCAInterstitialShowOptions : HDCABase
- (instancetype)initWithImmersiveMode:(BOOL)immersiveMode __attribute__((swift_name("init(immersiveMode:)"))) __attribute__((objc_designated_initializer));
- (HDCAInterstitialShowOptions *)doCopyImmersiveMode:(BOOL)immersiveMode __attribute__((swift_name("doCopy(immersiveMode:)")));
- (BOOL)isEqual:(id _Nullable)other __attribute__((swift_name("isEqual(_:)")));
- (NSUInteger)hash __attribute__((swift_name("hash()")));
- (NSString *)description __attribute__((swift_name("description()")));
@property (readonly) BOOL immersiveMode __attribute__((swift_name("immersiveMode")));
@end

__attribute__((objc_subclassing_restricted))
__attribute__((swift_name("InterstitialAdRegistry")))
@interface HDCAInterstitialAdRegistry : HDCABase
+ (instancetype)alloc __attribute__((unavailable));
+ (instancetype)allocWithZone:(struct _NSZone *)zone __attribute__((unavailable));
+ (instancetype)interstitialAdRegistry __attribute__((swift_name("init()")));
@property (class, readonly, getter=shared) HDCAInterstitialAdRegistry *shared __attribute__((swift_name("shared")));
- (HDCAInterstitialAdConfig *)configForAlias:(NSString *)alias fallbackAdUnitId:(NSString *)fallbackAdUnitId __attribute__((swift_name("configFor(alias:fallbackAdUnitId:)")));
- (void)createAlias:(NSString *)alias config:(HDCAInterstitialAdConfig *)config __attribute__((swift_name("create(alias:config:)")));
- (void)destroyAlias:(NSString *)alias __attribute__((swift_name("destroy(alias:)")));
- (BOOL)isReadyAlias:(NSString *)alias __attribute__((swift_name("isReady(alias:)")));
- (NSString * _Nullable)loadedAdUnitIdAlias:(NSString *)alias __attribute__((swift_name("loadedAdUnitId(alias:)")));
- (void)notifyClickedAlias:(NSString *)alias ad:(HDCAInterstitialAdInfo *)ad __attribute__((swift_name("notifyClicked(alias:ad:)")));
- (void)notifyClosedAlias:(NSString *)alias ad:(HDCAInterstitialAdInfo *)ad __attribute__((swift_name("notifyClosed(alias:ad:)")));
- (void)notifyDisplayedAlias:(NSString *)alias ad:(HDCAInterstitialAdInfo *)ad __attribute__((swift_name("notifyDisplayed(alias:ad:)")));
- (void)notifyFailedToLoadAlias:(NSString *)alias adUnit:(NSString *)adUnit errorCode:(int32_t)errorCode error:(NSString *)error __attribute__((swift_name("notifyFailedToLoad(alias:adUnit:errorCode:error:)")));
- (void)notifyFailedToShowAlias:(NSString *)alias adUnit:(NSString *)adUnit errorCode:(int32_t)errorCode error:(NSString *)error __attribute__((swift_name("notifyFailedToShow(alias:adUnit:errorCode:error:)")));
- (void)notifyImpressionAlias:(NSString *)alias ad:(HDCAInterstitialAdInfo *)ad __attribute__((swift_name("notifyImpression(alias:ad:)")));
- (void)notifyLoadedAlias:(NSString *)alias ad:(HDCAInterstitialAdInfo *)ad __attribute__((swift_name("notifyLoaded(alias:ad:)")));
- (void)notifyLoadingAlias:(NSString *)alias __attribute__((swift_name("notifyLoading(alias:)")));
- (void)notifyOpenedAlias:(NSString *)alias ad:(HDCAInterstitialAdInfo *)ad __attribute__((swift_name("notifyOpened(alias:ad:)")));
- (void)notifyPaidImpressionAlias:(NSString *)alias ad:(HDCAInterstitialAdInfo *)ad paidInfo:(HDCAInterstitialAdPaidInfo *)paidInfo __attribute__((swift_name("notifyPaidImpression(alias:ad:paidInfo:)")));
- (void)setCallbackAlias:(NSString *)alias callback:(id<HDCAInterstitialAdCallback> _Nullable)callback __attribute__((swift_name("setCallback(alias:callback:)")));
- (HDCANativeAdState *)stateForAlias:(NSString *)alias __attribute__((swift_name("stateFor(alias:)")));
@end

__attribute__((objc_subclassing_restricted))
__attribute__((swift_name("IosInterstitialAdSdk")))
@interface HDCAIosInterstitialAdSdk : HDCABase
- (instancetype)init __attribute__((swift_name("init()"))) __attribute__((objc_designated_initializer));
+ (instancetype)new __attribute__((availability(swift, unavailable, message="use object initializers instead")));
- (void)createAlias:(NSString *)alias adUnitIdsCsv:(NSString *)adUnitIdsCsv preloadBufferSize:(int32_t)preloadBufferSize autoReload:(BOOL)autoReload __attribute__((swift_name("create(alias:adUnitIdsCsv:preloadBufferSize:autoReload:)")));
- (void)destroyAlias:(NSString *)alias __attribute__((swift_name("destroy(alias:)")));
- (BOOL)isReadyAlias:(NSString *)alias __attribute__((swift_name("isReady(alias:)")));
- (void)loadRootViewController:(UIViewController *)rootViewController alias:(NSString *)alias __attribute__((swift_name("load(rootViewController:alias:)")));
- (void)loadRootViewController:(UIViewController *)rootViewController alias:(NSString *)alias bufferSize:(int32_t)bufferSize __attribute__((swift_name("load(rootViewController:alias:bufferSize:)")));
- (void)loadWithConfigRootViewController:(UIViewController *)rootViewController alias:(NSString *)alias adUnitIdsCsv:(NSString *)adUnitIdsCsv preloadBufferSize:(int32_t)preloadBufferSize autoReload:(BOOL)autoReload onStateChanged:(void (^)(HDCANativeAdState *))onStateChanged __attribute__((swift_name("loadWithConfig(rootViewController:alias:adUnitIdsCsv:preloadBufferSize:autoReload:onStateChanged:)")));
- (void)setCallbackAlias:(NSString *)alias callback:(id<HDCAInterstitialAdCallback> _Nullable)callback __attribute__((swift_name("setCallback(alias:callback:)")));
- (BOOL)showRootViewController:(UIViewController *)rootViewController alias:(NSString *)alias __attribute__((swift_name("show(rootViewController:alias:)")));
@end


/** How much the library logs: nothing, warnings, or everything including each load and show step. */
__attribute__((objc_subclassing_restricted))
__attribute__((swift_name("AdsLogLevel")))
@interface HDCAAdsLogLevel : HDCAKotlinEnum<HDCAAdsLogLevel *>
+ (instancetype)alloc __attribute__((unavailable));

/** How much the library logs: nothing, warnings, or everything including each load and show step. */
+ (instancetype)allocWithZone:(struct _NSZone *)zone __attribute__((unavailable));
- (instancetype)initWithName:(NSString *)name ordinal:(int32_t)ordinal __attribute__((swift_name("init(name:ordinal:)"))) __attribute__((objc_designated_initializer)) __attribute__((unavailable));
@property (class, readonly) HDCAAdsLogLevel *off __attribute__((swift_name("off")));
@property (class, readonly) HDCAAdsLogLevel *warn __attribute__((swift_name("warn")));
@property (class, readonly) HDCAAdsLogLevel *debug __attribute__((swift_name("debug")));
+ (HDCAKotlinArray<HDCAAdsLogLevel *> *)values __attribute__((swift_name("values()")));
@property (class, readonly) NSArray<HDCAAdsLogLevel *> *entries __attribute__((swift_name("entries")));
@end


/**
 * How much the library logs. By default a development build (a debuggable Android app, an iOS app run from Xcode or
 * built with the debug framework) logs everything and a release build only warnings.
 */
__attribute__((objc_subclassing_restricted))
__attribute__((swift_name("AdsLogging")))
@interface HDCAAdsLogging : HDCABase
+ (instancetype)alloc __attribute__((unavailable));

/**
 * How much the library logs. By default a development build (a debuggable Android app, an iOS app run from Xcode or
 * built with the debug framework) logs everything and a release build only warnings.
 */
+ (instancetype)allocWithZone:(struct _NSZone *)zone __attribute__((unavailable));
+ (instancetype)adsLogging __attribute__((swift_name("init()")));
@property (class, readonly, getter=shared) HDCAAdsLogging *shared __attribute__((swift_name("shared")));

/** The level the library logs at now. */
- (HDCAAdsLogLevel *)currentLevel __attribute__((swift_name("currentLevel()")));

/** Picks [level]; null goes back to the default of the build. */
- (void)setLevelLevel:(HDCAAdsLogLevel * _Nullable)level __attribute__((swift_name("setLevel(level:)")));

/** For Unity: "off", "warn", "debug", or "" for the default of the build. False, and no change, for another name. */
- (BOOL)setLevelNameName:(NSString *)name __attribute__((swift_name("setLevelName(name:)")));
@end

__attribute__((swift_name("PopupNativeAdCallback")))
@protocol HDCAPopupNativeAdCallback
@required
- (void)onClickedAd__:(HDCAPopupNativeAdInfo *)ad __attribute__((swift_name("onClicked(ad__:)")));
- (void)onClosedAd__:(HDCAPopupNativeAdInfo *)ad __attribute__((swift_name("onClosed(ad__:)")));
- (void)onDisplayableAlias:(NSString *)alias __attribute__((swift_name("onDisplayable(alias:)")));
- (void)onDisplayedAd__:(HDCAPopupNativeAdInfo *)ad __attribute__((swift_name("onDisplayed(ad__:)")));
- (void)onFailedToLoadError__:(HDCAPopupNativeAdError *)error __attribute__((swift_name("onFailedToLoad(error__:)")));
- (void)onLoadedAd__:(HDCAPopupNativeAdInfo *)ad __attribute__((swift_name("onLoaded(ad__:)")));
- (void)onOpenedAd__:(HDCAPopupNativeAdInfo *)ad __attribute__((swift_name("onOpened(ad__:)")));
@end


/**
 * Impressions and paid impressions of the popup native ads of an alias, for revenue reporting. It is apart from
 * [PopupNativeAdCallback] so code that implements that callback keeps working: on iOS every method of an exported
 * callback is required.
 */
__attribute__((swift_name("PopupNativeAdRevenueCallback")))
@protocol HDCAPopupNativeAdRevenueCallback
@required
- (void)onImpressionAd_:(HDCAPopupNativeAdInfo *)ad __attribute__((swift_name("onImpression(ad_:)")));
- (void)onPaidImpressionAd:(HDCAPopupNativeAdInfo *)ad paidInfo_:(HDCANativeAdPaidInfo *)paidInfo __attribute__((swift_name("onPaidImpression(ad:paidInfo_:)")));
@end

__attribute__((objc_subclassing_restricted))
__attribute__((swift_name("PopupNativeAdConfig")))
@interface HDCAPopupNativeAdConfig : HDCABase
- (instancetype)initWithIds:(NSArray<NSString *> *)ids layoutName:(NSString *)layoutName timeShowSeconds:(int32_t)timeShowSeconds timeReloadSeconds:(int32_t)timeReloadSeconds xDp:(float)xDp yDp:(float)yDp adWidthDp:(float)adWidthDp adHeightDp:(float)adHeightDp autoClose:(BOOL)autoClose enableCtrOverlay:(BOOL)enableCtrOverlay __attribute__((swift_name("init(ids:layoutName:timeShowSeconds:timeReloadSeconds:xDp:yDp:adWidthDp:adHeightDp:autoClose:enableCtrOverlay:)"))) __attribute__((objc_designated_initializer));
- (HDCAPopupNativeAdConfig *)doCopyIds:(NSArray<NSString *> *)ids layoutName:(NSString *)layoutName timeShowSeconds:(int32_t)timeShowSeconds timeReloadSeconds:(int32_t)timeReloadSeconds xDp:(float)xDp yDp:(float)yDp adWidthDp:(float)adWidthDp adHeightDp:(float)adHeightDp autoClose:(BOOL)autoClose enableCtrOverlay:(BOOL)enableCtrOverlay __attribute__((swift_name("doCopy(ids:layoutName:timeShowSeconds:timeReloadSeconds:xDp:yDp:adWidthDp:adHeightDp:autoClose:enableCtrOverlay:)")));
- (BOOL)isEqual:(id _Nullable)other __attribute__((swift_name("isEqual(_:)")));
- (NSUInteger)hash __attribute__((swift_name("hash()")));
- (NSString *)description __attribute__((swift_name("description()")));
@property (readonly) float adHeightDp __attribute__((swift_name("adHeightDp")));
@property (readonly) float adWidthDp __attribute__((swift_name("adWidthDp")));
@property (readonly) BOOL autoClose __attribute__((swift_name("autoClose")));
@property (readonly) BOOL enableCtrOverlay __attribute__((swift_name("enableCtrOverlay")));
@property (readonly) NSArray<NSString *> *ids __attribute__((swift_name("ids")));
@property (readonly) NSString *layoutName __attribute__((swift_name("layoutName")));
@property (readonly) int32_t timeReloadSeconds __attribute__((swift_name("timeReloadSeconds")));
@property (readonly) int32_t timeShowSeconds __attribute__((swift_name("timeShowSeconds")));
@property (readonly) float xDp __attribute__((swift_name("xDp")));
@property (readonly) float yDp __attribute__((swift_name("yDp")));
@end

__attribute__((objc_subclassing_restricted))
__attribute__((swift_name("PopupNativeAdError")))
@interface HDCAPopupNativeAdError : HDCABase
- (instancetype)initWithAlias:(NSString *)alias adUnitId:(NSString *)adUnitId code:(int32_t)code message:(NSString *)message __attribute__((swift_name("init(alias:adUnitId:code:message:)"))) __attribute__((objc_designated_initializer));
- (HDCAPopupNativeAdError *)doCopyAlias:(NSString *)alias adUnitId:(NSString *)adUnitId code:(int32_t)code message:(NSString *)message __attribute__((swift_name("doCopy(alias:adUnitId:code:message:)")));
- (BOOL)isEqual:(id _Nullable)other __attribute__((swift_name("isEqual(_:)")));
- (NSUInteger)hash __attribute__((swift_name("hash()")));
- (NSString *)description __attribute__((swift_name("description()")));
@property (readonly) NSString *adUnitId __attribute__((swift_name("adUnitId")));
@property (readonly) NSString *alias __attribute__((swift_name("alias")));
@property (readonly) int32_t code __attribute__((swift_name("code")));
@property (readonly) NSString *message __attribute__((swift_name("message")));
@end

__attribute__((objc_subclassing_restricted))
__attribute__((swift_name("PopupNativeAdInfo")))
@interface HDCAPopupNativeAdInfo : HDCABase
- (instancetype)initWithAlias:(NSString *)alias instanceId:(NSString *)instanceId adUnitId:(NSString *)adUnitId layoutName:(NSString *)layoutName __attribute__((swift_name("init(alias:instanceId:adUnitId:layoutName:)"))) __attribute__((objc_designated_initializer));
- (HDCAPopupNativeAdInfo *)doCopyAlias:(NSString *)alias instanceId:(NSString *)instanceId adUnitId:(NSString *)adUnitId layoutName:(NSString *)layoutName __attribute__((swift_name("doCopy(alias:instanceId:adUnitId:layoutName:)")));
- (BOOL)isEqual:(id _Nullable)other __attribute__((swift_name("isEqual(_:)")));
- (NSUInteger)hash __attribute__((swift_name("hash()")));
- (NSString *)description __attribute__((swift_name("description()")));
@property (readonly) NSString *adUnitId __attribute__((swift_name("adUnitId")));
@property (readonly) NSString *alias __attribute__((swift_name("alias")));
@property (readonly) NSString *instanceId __attribute__((swift_name("instanceId")));
@property (readonly) NSString *layoutName __attribute__((swift_name("layoutName")));
@end

__attribute__((objc_subclassing_restricted))
__attribute__((swift_name("PopupNativeAdRequest")))
@interface HDCAPopupNativeAdRequest : HDCABase
- (instancetype)initWithInstanceId:(NSString *)instanceId adUnitIds:(NSArray<NSString *> *)adUnitIds __attribute__((swift_name("init(instanceId:adUnitIds:)"))) __attribute__((objc_designated_initializer));
- (HDCAPopupNativeAdRequest *)doCopyInstanceId:(NSString *)instanceId adUnitIds:(NSArray<NSString *> *)adUnitIds __attribute__((swift_name("doCopy(instanceId:adUnitIds:)")));
- (BOOL)isEqual:(id _Nullable)other __attribute__((swift_name("isEqual(_:)")));
- (NSUInteger)hash __attribute__((swift_name("hash()")));
- (NSString *)description __attribute__((swift_name("description()")));
@property (readonly) NSArray<NSString *> *adUnitIds __attribute__((swift_name("adUnitIds")));
@property (readonly) NSString *instanceId __attribute__((swift_name("instanceId")));
@end

__attribute__((objc_subclassing_restricted))
__attribute__((swift_name("IosPopupNativeAdSdk")))
@interface HDCAIosPopupNativeAdSdk : HDCABase
- (instancetype)init __attribute__((swift_name("init()"))) __attribute__((objc_designated_initializer));
+ (instancetype)new __attribute__((availability(swift, unavailable, message="use object initializers instead")));
- (void)closeAlias:(NSString *)alias __attribute__((swift_name("close(alias:)")));
- (void)createAlias:(NSString *)alias adUnitIdsCsv:(NSString *)adUnitIdsCsv layoutName:(NSString *)layoutName timeShowSeconds:(int32_t)timeShowSeconds timeReloadSeconds:(int32_t)timeReloadSeconds xDp:(float)xDp yDp:(float)yDp adWidthDp:(float)adWidthDp adHeightDp:(float)adHeightDp autoClose:(BOOL)autoClose enableCtrOverlay:(BOOL)enableCtrOverlay __attribute__((swift_name("create(alias:adUnitIdsCsv:layoutName:timeShowSeconds:timeReloadSeconds:xDp:yDp:adWidthDp:adHeightDp:autoClose:enableCtrOverlay:)")));
- (void)destroyAlias:(NSString *)alias __attribute__((swift_name("destroy(alias:)")));
- (void)hideAlias:(NSString *)alias __attribute__((swift_name("hide(alias:)")));
- (BOOL)isDisplayableAlias:(NSString *)alias __attribute__((swift_name("isDisplayable(alias:)")));
- (BOOL)isReadyAlias:(NSString *)alias __attribute__((swift_name("isReady(alias:)")));
- (void)loadRootViewController:(UIViewController * _Nullable)rootViewController alias:(NSString *)alias __attribute__((swift_name("load(rootViewController:alias:)")));
- (void)loadWithConfigRootViewController:(UIViewController * _Nullable)rootViewController alias:(NSString *)alias adUnitIdsCsv:(NSString *)adUnitIdsCsv layoutName:(NSString *)layoutName timeShowSeconds:(int32_t)timeShowSeconds timeReloadSeconds:(int32_t)timeReloadSeconds xDp:(float)xDp yDp:(float)yDp adWidthDp:(float)adWidthDp adHeightDp:(float)adHeightDp autoClose:(BOOL)autoClose enableCtrOverlay:(BOOL)enableCtrOverlay onStateChanged:(void (^)(HDCANativeAdState *))onStateChanged __attribute__((swift_name("loadWithConfig(rootViewController:alias:adUnitIdsCsv:layoutName:timeShowSeconds:timeReloadSeconds:xDp:yDp:adWidthDp:adHeightDp:autoClose:enableCtrOverlay:onStateChanged:)")));
- (void)setCallbackAlias:(NSString *)alias callback:(void (^)(HDCANativeAdState *))callback __attribute__((swift_name("setCallback(alias:callback:)")));

/**
 * Shows the popup on the game's screen. [rootViewController] is not used for that: the popup never goes on the
 * screen of an ad. While it cannot be on screen yet (an ad or a page covers the game, the app is not active, the
 * ad is still loading), the show waits and happens as soon as it can, until [hide], [close], [stop] or [destroy].
 */
- (void)showRootViewController:(UIViewController * _Nullable)rootViewController alias:(NSString *)alias __attribute__((swift_name("show(rootViewController:alias:)")));
- (NSString *)stateForAlias:(NSString *)alias __attribute__((swift_name("stateFor(alias:)")));
- (void)stopAlias:(NSString *)alias __attribute__((swift_name("stop(alias:)")));
- (void)updatePlacementAlias:(NSString *)alias xDp:(float)xDp yDp:(float)yDp adWidthDp:(float)adWidthDp adHeightDp:(float)adHeightDp __attribute__((swift_name("updatePlacement(alias:xDp:yDp:adWidthDp:adHeightDp:)")));
@end

__attribute__((objc_subclassing_restricted))
__attribute__((swift_name("NativeAdLoadResult")))
@interface HDCANativeAdLoadResult : HDCABase
- (instancetype)initWithState:(HDCANativeAdState *)state adUnitId:(NSString *)adUnitId code:(int32_t)code message:(NSString *)message adSourceId:(NSString *)adSourceId adSourceName:(NSString *)adSourceName __attribute__((swift_name("init(state:adUnitId:code:message:adSourceId:adSourceName:)"))) __attribute__((objc_designated_initializer));
@property (class, readonly, getter=companion) HDCANativeAdLoadResultCompanion *companion __attribute__((swift_name("companion")));
- (HDCANativeAdLoadResult *)doCopyState:(HDCANativeAdState *)state adUnitId:(NSString *)adUnitId code:(int32_t)code message:(NSString *)message adSourceId:(NSString *)adSourceId adSourceName:(NSString *)adSourceName __attribute__((swift_name("doCopy(state:adUnitId:code:message:adSourceId:adSourceName:)")));
- (BOOL)isEqual:(id _Nullable)other __attribute__((swift_name("isEqual(_:)")));
- (NSUInteger)hash __attribute__((swift_name("hash()")));
- (NSString *)description __attribute__((swift_name("description()")));

/** The ad source (mediation network) that filled a loaded ad, as the network reports it. */
@property (readonly) NSString *adSourceId __attribute__((swift_name("adSourceId")));
@property (readonly) NSString *adSourceName __attribute__((swift_name("adSourceName")));
@property (readonly) NSString *adUnitId __attribute__((swift_name("adUnitId")));
@property (readonly) int32_t code __attribute__((swift_name("code")));
@property (readonly) BOOL hasErrorCode __attribute__((swift_name("hasErrorCode")));
@property (readonly) NSString *message __attribute__((swift_name("message")));
@property (readonly) HDCANativeAdState *state __attribute__((swift_name("state")));
@end

__attribute__((objc_subclassing_restricted))
__attribute__((swift_name("NativeAdLoadResult.Companion")))
@interface HDCANativeAdLoadResultCompanion : HDCABase
+ (instancetype)alloc __attribute__((unavailable));
+ (instancetype)allocWithZone:(struct _NSZone *)zone __attribute__((unavailable));
+ (instancetype)companion __attribute__((swift_name("init()")));
@property (class, readonly, getter=shared) HDCANativeAdLoadResultCompanion *shared __attribute__((swift_name("shared")));
@property (readonly) int32_t NoErrorCode __attribute__((swift_name("NoErrorCode")));
@end

__attribute__((objc_subclassing_restricted))
__attribute__((swift_name("NativeAdState")))
@interface HDCANativeAdState : HDCAKotlinEnum<HDCANativeAdState *>
+ (instancetype)alloc __attribute__((unavailable));
+ (instancetype)allocWithZone:(struct _NSZone *)zone __attribute__((unavailable));
- (instancetype)initWithName:(NSString *)name ordinal:(int32_t)ordinal __attribute__((swift_name("init(name:ordinal:)"))) __attribute__((objc_designated_initializer)) __attribute__((unavailable));
@property (class, readonly) HDCANativeAdState *idle __attribute__((swift_name("idle")));
@property (class, readonly) HDCANativeAdState *loading __attribute__((swift_name("loading")));
@property (class, readonly) HDCANativeAdState *loaded __attribute__((swift_name("loaded")));
@property (class, readonly) HDCANativeAdState *failed __attribute__((swift_name("failed")));
@property (class, readonly) HDCANativeAdState *shown __attribute__((swift_name("shown")));
@property (class, readonly) HDCANativeAdState *onclosed __attribute__((swift_name("onclosed")));
+ (HDCAKotlinArray<HDCANativeAdState *> *)values __attribute__((swift_name("values()")));
@property (class, readonly) NSArray<HDCANativeAdState *> *entries __attribute__((swift_name("entries")));
@end

__attribute__((swift_name("Platform")))
@protocol HDCAPlatform
@required
@property (readonly) NSString *name __attribute__((swift_name("name")));
@end

__attribute__((objc_subclassing_restricted))
__attribute__((swift_name("IOSPlatform")))
@interface HDCAIOSPlatform : HDCABase <HDCAPlatform>
- (instancetype)init __attribute__((swift_name("init()"))) __attribute__((objc_designated_initializer));
+ (instancetype)new __attribute__((availability(swift, unavailable, message="use object initializers instead")));
@property (readonly) NSString *name __attribute__((swift_name("name")));
@end


/**
 * @note annotations
 *   androidx.compose.runtime.Immutable
*/
__attribute__((objc_subclassing_restricted))
__attribute__((swift_name("BannerNativeAdComposeCallbacks")))
@interface HDCABannerNativeAdComposeCallbacks : HDCABase
- (instancetype)initWithOnIconClick:(void (^)(void))onIconClick onCallToActionClick:(void (^)(void))onCallToActionClick onCollapsedCloseClick:(void (^)(void))onCollapsedCloseClick onCollapseClick:(void (^)(void))onCollapseClick __attribute__((swift_name("init(onIconClick:onCallToActionClick:onCollapsedCloseClick:onCollapseClick:)"))) __attribute__((objc_designated_initializer));
- (HDCABannerNativeAdComposeCallbacks *)doCopyOnIconClick:(void (^)(void))onIconClick onCallToActionClick:(void (^)(void))onCallToActionClick onCollapsedCloseClick:(void (^)(void))onCollapsedCloseClick onCollapseClick:(void (^)(void))onCollapseClick __attribute__((swift_name("doCopy(onIconClick:onCallToActionClick:onCollapsedCloseClick:onCollapseClick:)")));
- (BOOL)isEqual:(id _Nullable)other __attribute__((swift_name("isEqual(_:)")));
- (NSUInteger)hash __attribute__((swift_name("hash()")));
- (NSString *)description __attribute__((swift_name("description()")));
@property (readonly) void (^onCallToActionClick)(void) __attribute__((swift_name("onCallToActionClick")));
@property (readonly) void (^onCollapseClick)(void) __attribute__((swift_name("onCollapseClick")));
@property (readonly) void (^onCollapsedCloseClick)(void) __attribute__((swift_name("onCollapsedCloseClick")));
@property (readonly) void (^onIconClick)(void) __attribute__((swift_name("onIconClick")));
@end


/**
 * @note annotations
 *   androidx.compose.runtime.Immutable
*/
__attribute__((objc_subclassing_restricted))
__attribute__((swift_name("BannerNativeAdComposeState")))
@interface HDCABannerNativeAdComposeState : HDCABase
- (instancetype)initWithHeadline:(NSString *)headline body:(NSString *)body advertiser:(NSString *)advertiser callToAction:(NSString *)callToAction showMedia:(BOOL)showMedia showIcon:(BOOL)showIcon showCallToAction:(BOOL)showCallToAction showAdChoices:(BOOL)showAdChoices usesNativeAssetTouchHandling:(BOOL)usesNativeAssetTouchHandling __attribute__((swift_name("init(headline:body:advertiser:callToAction:showMedia:showIcon:showCallToAction:showAdChoices:usesNativeAssetTouchHandling:)"))) __attribute__((objc_designated_initializer));
- (HDCABannerNativeAdComposeState *)doCopyHeadline:(NSString *)headline body:(NSString *)body advertiser:(NSString *)advertiser callToAction:(NSString *)callToAction showMedia:(BOOL)showMedia showIcon:(BOOL)showIcon showCallToAction:(BOOL)showCallToAction showAdChoices:(BOOL)showAdChoices usesNativeAssetTouchHandling:(BOOL)usesNativeAssetTouchHandling __attribute__((swift_name("doCopy(headline:body:advertiser:callToAction:showMedia:showIcon:showCallToAction:showAdChoices:usesNativeAssetTouchHandling:)")));
- (BOOL)isEqual:(id _Nullable)other __attribute__((swift_name("isEqual(_:)")));
- (NSUInteger)hash __attribute__((swift_name("hash()")));
- (NSString *)description __attribute__((swift_name("description()")));
@property (readonly) NSString *advertiser __attribute__((swift_name("advertiser")));
@property (readonly) NSString *body __attribute__((swift_name("body")));
@property (readonly) NSString *callToAction __attribute__((swift_name("callToAction")));
@property (readonly) NSString *headline __attribute__((swift_name("headline")));
@property (readonly) BOOL showAdChoices __attribute__((swift_name("showAdChoices")));
@property (readonly) BOOL showCallToAction __attribute__((swift_name("showCallToAction")));
@property (readonly) BOOL showIcon __attribute__((swift_name("showIcon")));
@property (readonly) BOOL showMedia __attribute__((swift_name("showMedia")));

/** Native platform asset views must receive touches directly for SDK click tracking. */
@property (readonly) BOOL usesNativeAssetTouchHandling __attribute__((swift_name("usesNativeAssetTouchHandling")));
@end

__attribute__((objc_subclassing_restricted))
__attribute__((swift_name("BannerCollapseCountdownStyle")))
@interface HDCABannerCollapseCountdownStyle : HDCAKotlinEnum<HDCABannerCollapseCountdownStyle *>
+ (instancetype)alloc __attribute__((unavailable));
+ (instancetype)allocWithZone:(struct _NSZone *)zone __attribute__((unavailable));
- (instancetype)initWithName:(NSString *)name ordinal:(int32_t)ordinal __attribute__((swift_name("init(name:ordinal:)"))) __attribute__((objc_designated_initializer)) __attribute__((unavailable));
@property (class, readonly) HDCABannerCollapseCountdownStyle *none __attribute__((swift_name("none")));
@property (class, readonly) HDCABannerCollapseCountdownStyle *ring __attribute__((swift_name("ring")));
@property (class, readonly) HDCABannerCollapseCountdownStyle *horizontal __attribute__((swift_name("horizontal")));
@property (class, readonly) HDCABannerCollapseCountdownStyle *segments __attribute__((swift_name("segments")));
+ (HDCAKotlinArray<HDCABannerCollapseCountdownStyle *> *)values __attribute__((swift_name("values()")));
@property (class, readonly) NSArray<HDCABannerCollapseCountdownStyle *> *entries __attribute__((swift_name("entries")));
@end

__attribute__((objc_subclassing_restricted))
__attribute__((swift_name("BannerNativeAdLayoutCatalog")))
@interface HDCABannerNativeAdLayoutCatalog : HDCABase
+ (instancetype)alloc __attribute__((unavailable));
+ (instancetype)allocWithZone:(struct _NSZone *)zone __attribute__((unavailable));
+ (instancetype)bannerNativeAdLayoutCatalog __attribute__((swift_name("init()")));
@property (class, readonly, getter=shared) HDCABannerNativeAdLayoutCatalog *shared __attribute__((swift_name("shared")));
- (HDCABannerCollapseCountdownStyle *)collapseCountdownStyleLayoutName:(NSString * _Nullable)layoutName __attribute__((swift_name("collapseCountdownStyle(layoutName:)")));
- (BOOL)hasCollapseCountdownLayoutName:(NSString * _Nullable)layoutName __attribute__((swift_name("hasCollapseCountdown(layoutName:)")));
- (NSString *)normalizeLayoutName:(NSString * _Nullable)layoutName __attribute__((swift_name("normalize(layoutName:)")));
@property (readonly) NSArray<NSString *> *All __attribute__((swift_name("All")));
@property (readonly) NSString *Default __attribute__((swift_name("Default")));
@end

__attribute__((objc_subclassing_restricted))
__attribute__((swift_name("BannerNativeAdSizing")))
@interface HDCABannerNativeAdSizing : HDCABase
+ (instancetype)alloc __attribute__((unavailable));
+ (instancetype)allocWithZone:(struct _NSZone *)zone __attribute__((unavailable));
+ (instancetype)bannerNativeAdSizing __attribute__((swift_name("init()")));
@property (class, readonly, getter=shared) HDCABannerNativeAdSizing *shared __attribute__((swift_name("shared")));
@property (readonly) float ExpandedHeightRatioLandscape __attribute__((swift_name("ExpandedHeightRatioLandscape")));
@property (readonly) float ExpandedHeightRatioPortrait __attribute__((swift_name("ExpandedHeightRatioPortrait")));
@property (readonly) float HeightRatioLandscape __attribute__((swift_name("HeightRatioLandscape")));
@property (readonly) float HeightRatioPortrait __attribute__((swift_name("HeightRatioPortrait")));
@property (readonly) float MinExpandedHeightLandscapeDp __attribute__((swift_name("MinExpandedHeightLandscapeDp")));
@property (readonly) float MinExpandedHeightPortraitDp __attribute__((swift_name("MinExpandedHeightPortraitDp")));
@end

__attribute__((swift_name("BannerNativeAdLoader")))
@protocol HDCABannerNativeAdLoader
@required
- (void)destroyInstanceId:(NSString *)instanceId __attribute__((swift_name("destroy(instanceId:)")));
- (void)loadRequest:(HDCABannerNativeAdRequest *)request onStateChanged:(void (^)(HDCANativeAdState *))onStateChanged __attribute__((swift_name("load(request:onStateChanged:)")));
- (void)loadAdUnitId:(NSString *)adUnitId onStateChanged:(void (^)(HDCANativeAdState *))onStateChanged __attribute__((swift_name("load(adUnitId:onStateChanged:)")));
- (void)loadWithResultRequest:(HDCABannerNativeAdRequest *)request onResult:(void (^)(HDCANativeAdLoadResult *))onResult __attribute__((swift_name("loadWithResult(request:onResult:)")));
@end


/**
 * @note annotations
 *   androidx.compose.runtime.Immutable
*/
__attribute__((objc_subclassing_restricted))
__attribute__((swift_name("FullscreenNativeAdCallbacks")))
@interface HDCAFullscreenNativeAdCallbacks : HDCABase
- (instancetype)initWithOnMediaClick:(void (^)(void))onMediaClick onIconClick:(void (^)(void))onIconClick onInfoClick:(void (^)(void))onInfoClick onCallToActionClick:(void (^)(void))onCallToActionClick onOpenStoreClick:(void (^)(void))onOpenStoreClick onCloseClick:(void (^)(void))onCloseClick __attribute__((swift_name("init(onMediaClick:onIconClick:onInfoClick:onCallToActionClick:onOpenStoreClick:onCloseClick:)"))) __attribute__((objc_designated_initializer));
- (HDCAFullscreenNativeAdCallbacks *)doCopyOnMediaClick:(void (^)(void))onMediaClick onIconClick:(void (^)(void))onIconClick onInfoClick:(void (^)(void))onInfoClick onCallToActionClick:(void (^)(void))onCallToActionClick onOpenStoreClick:(void (^)(void))onOpenStoreClick onCloseClick:(void (^)(void))onCloseClick __attribute__((swift_name("doCopy(onMediaClick:onIconClick:onInfoClick:onCallToActionClick:onOpenStoreClick:onCloseClick:)")));
- (BOOL)isEqual:(id _Nullable)other __attribute__((swift_name("isEqual(_:)")));
- (NSUInteger)hash __attribute__((swift_name("hash()")));
- (NSString *)description __attribute__((swift_name("description()")));
@property (readonly) void (^onCallToActionClick)(void) __attribute__((swift_name("onCallToActionClick")));
@property (readonly) void (^onCloseClick)(void) __attribute__((swift_name("onCloseClick")));
@property (readonly) void (^onIconClick)(void) __attribute__((swift_name("onIconClick")));
@property (readonly) void (^onInfoClick)(void) __attribute__((swift_name("onInfoClick")));
@property (readonly) void (^onMediaClick)(void) __attribute__((swift_name("onMediaClick")));
@property (readonly) void (^onOpenStoreClick)(void) __attribute__((swift_name("onOpenStoreClick")));
@end


/**
 * @note annotations
 *   androidx.compose.runtime.Immutable
*/
__attribute__((objc_subclassing_restricted))
__attribute__((swift_name("FullscreenNativeAdState")))
@interface HDCAFullscreenNativeAdState : HDCABase
- (instancetype)initWithHeadline:(NSString *)headline body:(NSString *)body advertiser:(NSString *)advertiser callToAction:(NSString *)callToAction countdownText:(NSString *)countdownText showCountdown:(BOOL)showCountdown showCloseButton:(BOOL)showCloseButton closeButtonEnabled:(BOOL)closeButtonEnabled closeButtonAlpha:(float)closeButtonAlpha closeStyle:(HDCAFullscreenNativeAdCloseStyle *)closeStyle countdownProgress:(float)countdownProgress showProgress:(BOOL)showProgress showOpenStoreButton:(BOOL)showOpenStoreButton showHeadline:(BOOL)showHeadline showBody:(BOOL)showBody showAdvertiser:(BOOL)showAdvertiser showIcon:(BOOL)showIcon showCallToAction:(BOOL)showCallToAction showMedia:(BOOL)showMedia showAdChoices:(BOOL)showAdChoices usesNativeAssetTouchHandling:(BOOL)usesNativeAssetTouchHandling renderAdvertiserText:(BOOL)renderAdvertiserText showIconSlot:(BOOL)showIconSlot __attribute__((swift_name("init(headline:body:advertiser:callToAction:countdownText:showCountdown:showCloseButton:closeButtonEnabled:closeButtonAlpha:closeStyle:countdownProgress:showProgress:showOpenStoreButton:showHeadline:showBody:showAdvertiser:showIcon:showCallToAction:showMedia:showAdChoices:usesNativeAssetTouchHandling:renderAdvertiserText:showIconSlot:)"))) __attribute__((objc_designated_initializer));
- (HDCAFullscreenNativeAdState *)doCopyHeadline:(NSString *)headline body:(NSString *)body advertiser:(NSString *)advertiser callToAction:(NSString *)callToAction countdownText:(NSString *)countdownText showCountdown:(BOOL)showCountdown showCloseButton:(BOOL)showCloseButton closeButtonEnabled:(BOOL)closeButtonEnabled closeButtonAlpha:(float)closeButtonAlpha closeStyle:(HDCAFullscreenNativeAdCloseStyle *)closeStyle countdownProgress:(float)countdownProgress showProgress:(BOOL)showProgress showOpenStoreButton:(BOOL)showOpenStoreButton showHeadline:(BOOL)showHeadline showBody:(BOOL)showBody showAdvertiser:(BOOL)showAdvertiser showIcon:(BOOL)showIcon showCallToAction:(BOOL)showCallToAction showMedia:(BOOL)showMedia showAdChoices:(BOOL)showAdChoices usesNativeAssetTouchHandling:(BOOL)usesNativeAssetTouchHandling renderAdvertiserText:(BOOL)renderAdvertiserText showIconSlot:(BOOL)showIconSlot __attribute__((swift_name("doCopy(headline:body:advertiser:callToAction:countdownText:showCountdown:showCloseButton:closeButtonEnabled:closeButtonAlpha:closeStyle:countdownProgress:showProgress:showOpenStoreButton:showHeadline:showBody:showAdvertiser:showIcon:showCallToAction:showMedia:showAdChoices:usesNativeAssetTouchHandling:renderAdvertiserText:showIconSlot:)")));
- (BOOL)isEqual:(id _Nullable)other __attribute__((swift_name("isEqual(_:)")));
- (NSUInteger)hash __attribute__((swift_name("hash()")));
- (NSString *)description __attribute__((swift_name("description()")));
@property (readonly) NSString *advertiser __attribute__((swift_name("advertiser")));
@property (readonly) NSString *body __attribute__((swift_name("body")));
@property (readonly) NSString *callToAction __attribute__((swift_name("callToAction")));
@property (readonly) float closeButtonAlpha __attribute__((swift_name("closeButtonAlpha")));
@property (readonly) BOOL closeButtonEnabled __attribute__((swift_name("closeButtonEnabled")));
@property (readonly) HDCAFullscreenNativeAdCloseStyle *closeStyle __attribute__((swift_name("closeStyle")));
@property (readonly) float countdownProgress __attribute__((swift_name("countdownProgress")));
@property (readonly) NSString *countdownText __attribute__((swift_name("countdownText")));
@property (readonly) NSString *headline __attribute__((swift_name("headline")));

/** Android can render this text through the registered native advertiser view. */
@property (readonly) BOOL renderAdvertiserText __attribute__((swift_name("renderAdvertiserText")));
@property (readonly) BOOL showAdChoices __attribute__((swift_name("showAdChoices")));
@property (readonly) BOOL showAdvertiser __attribute__((swift_name("showAdvertiser")));
@property (readonly) BOOL showBody __attribute__((swift_name("showBody")));
@property (readonly) BOOL showCallToAction __attribute__((swift_name("showCallToAction")));
@property (readonly) BOOL showCloseButton __attribute__((swift_name("showCloseButton")));
@property (readonly) BOOL showCountdown __attribute__((swift_name("showCountdown")));
@property (readonly) BOOL showHeadline __attribute__((swift_name("showHeadline")));
@property (readonly) BOOL showIcon __attribute__((swift_name("showIcon")));

/** The icon frame stays (empty) when the ad has no icon image; only hiding the icon asset removes it. */
@property (readonly) BOOL showIconSlot __attribute__((swift_name("showIconSlot")));
@property (readonly) BOOL showMedia __attribute__((swift_name("showMedia")));
@property (readonly) BOOL showOpenStoreButton __attribute__((swift_name("showOpenStoreButton")));
@property (readonly) BOOL showProgress __attribute__((swift_name("showProgress")));

/** Native platform assets must receive the touch themselves instead of Compose consuming it. */
@property (readonly) BOOL usesNativeAssetTouchHandling __attribute__((swift_name("usesNativeAssetTouchHandling")));
@end

__attribute__((objc_subclassing_restricted))
__attribute__((swift_name("FullscreenLayoutCatalog")))
@interface HDCAFullscreenLayoutCatalog : HDCABase
+ (instancetype)alloc __attribute__((unavailable));
+ (instancetype)allocWithZone:(struct _NSZone *)zone __attribute__((unavailable));
+ (instancetype)fullscreenLayoutCatalog __attribute__((swift_name("init()")));
@property (class, readonly, getter=shared) HDCAFullscreenLayoutCatalog *shared __attribute__((swift_name("shared")));
- (BOOL)isAndroidXmlParityLayoutNameLayoutName:(NSString * _Nullable)layoutName __attribute__((swift_name("isAndroidXmlParityLayoutName(layoutName:)")));
- (BOOL)isFullscreenLayoutNameLayoutName:(NSString * _Nullable)layoutName __attribute__((swift_name("isFullscreenLayoutName(layoutName:)")));
- (NSString *)normalizeLayoutName:(NSString * _Nullable)layoutName __attribute__((swift_name("normalize(layoutName:)")));
@property (readonly) NSArray<NSString *> *AarClsLayouts __attribute__((swift_name("AarClsLayouts")));

/** Every name the AAR accepts, in its order: the layout families, then ctr, ctr_transparent, multi and sequence. */
@property (readonly) NSArray<NSString *> *AarFullscreenLayouts __attribute__((swift_name("AarFullscreenLayouts")));
@property (readonly) NSArray<NSString *> *AarLoopLayouts __attribute__((swift_name("AarLoopLayouts")));
@property (readonly) NSArray<NSString *> *AarNavLayouts __attribute__((swift_name("AarNavLayouts")));
@property (readonly) NSArray<NSString *> *AarProgressClsLayouts __attribute__((swift_name("AarProgressClsLayouts")));
@property (readonly) NSArray<NSString *> *AarProgressLayouts __attribute__((swift_name("AarProgressLayouts")));
@property (readonly) NSArray<NSString *> *AarSingleTransparentLayouts __attribute__((swift_name("AarSingleTransparentLayouts")));
@property (readonly) NSArray<NSString *> *AarSingleUniversalLayouts __attribute__((swift_name("AarSingleUniversalLayouts")));
@property (readonly) NSArray<NSString *> *AllLayouts __attribute__((swift_name("AllLayouts")));

/** The layouts Android draws from an XML design (universal, cls, nav, progress, loop). */
@property (readonly) NSArray<NSString *> *AndroidXmlParityLayouts __attribute__((swift_name("AndroidXmlParityLayouts")));
@property (readonly) NSString *LoopDefault __attribute__((swift_name("LoopDefault")));
@property (readonly) NSString *MultipleDefault __attribute__((swift_name("MultipleDefault")));
@property (readonly) NSString *Nav01 __attribute__((swift_name("Nav01")));
@property (readonly) NSString *Nav02 __attribute__((swift_name("Nav02")));
@property (readonly) NSString *Nav03 __attribute__((swift_name("Nav03")));
@property (readonly) NSString *ProgressClsDefault __attribute__((swift_name("ProgressClsDefault")));
@property (readonly) NSString *ProgressDefault __attribute__((swift_name("ProgressDefault")));
@property (readonly) NSString *ProgressOnly01 __attribute__((swift_name("ProgressOnly01")));
@property (readonly) NSString *ProgressOnly02 __attribute__((swift_name("ProgressOnly02")));
@property (readonly) NSString *ProgressOnly03 __attribute__((swift_name("ProgressOnly03")));
@property (readonly) NSString *ProgressOnly04 __attribute__((swift_name("ProgressOnly04")));
@property (readonly) NSString *SequenceDefault __attribute__((swift_name("SequenceDefault")));
@property (readonly) NSString *SingleCtr __attribute__((swift_name("SingleCtr")));
@property (readonly) NSString *SingleCtrTransparent __attribute__((swift_name("SingleCtrTransparent")));
@property (readonly) NSString *SingleDefault __attribute__((swift_name("SingleDefault")));
@property (readonly) NSArray<NSString *> *SingleLayouts __attribute__((swift_name("SingleLayouts")));
@end

__attribute__((objc_subclassing_restricted))
__attribute__((swift_name("FullscreenNativeAdLayoutCatalog")))
@interface HDCAFullscreenNativeAdLayoutCatalog : HDCABase
+ (instancetype)alloc __attribute__((unavailable));
+ (instancetype)allocWithZone:(struct _NSZone *)zone __attribute__((unavailable));
+ (instancetype)fullscreenNativeAdLayoutCatalog __attribute__((swift_name("init()")));
@property (class, readonly, getter=shared) HDCAFullscreenNativeAdLayoutCatalog *shared __attribute__((swift_name("shared")));
- (NSString *)normalizeLayoutName:(NSString *)layoutName __attribute__((swift_name("normalize(layoutName:)")));
- (BOOL)requiresDetachedLandscapeControlLayoutName:(NSString *)layoutName __attribute__((swift_name("requiresDetachedLandscapeControl(layoutName:)")));
@property (readonly) NSString *Cls01 __attribute__((swift_name("Cls01")));
@property (readonly) NSString *Cls02 __attribute__((swift_name("Cls02")));
@property (readonly) NSString *Cls03 __attribute__((swift_name("Cls03")));
@property (readonly) NSString *Cls04 __attribute__((swift_name("Cls04")));
@property (readonly) NSString *Cls05 __attribute__((swift_name("Cls05")));
@property (readonly) NSString *Cls06 __attribute__((swift_name("Cls06")));
@property (readonly) NSString *Cls07 __attribute__((swift_name("Cls07")));
@property (readonly) NSString *Cls08 __attribute__((swift_name("Cls08")));
@property (readonly) NSString *Cls09 __attribute__((swift_name("Cls09")));
@property (readonly) NSString *Cls10 __attribute__((swift_name("Cls10")));
@property (readonly) NSString *Cls11 __attribute__((swift_name("Cls11")));
@property (readonly) NSString *Loop01 __attribute__((swift_name("Loop01")));
@property (readonly) NSString *Nav01 __attribute__((swift_name("Nav01")));
@property (readonly) NSString *Nav02 __attribute__((swift_name("Nav02")));
@property (readonly) NSString *Nav03 __attribute__((swift_name("Nav03")));
@property (readonly) NSString *Progress01 __attribute__((swift_name("Progress01")));
@property (readonly) NSString *ProgressCls01 __attribute__((swift_name("ProgressCls01")));
@property (readonly) NSString *ProgressOnly01 __attribute__((swift_name("ProgressOnly01")));
@property (readonly) NSString *ProgressOnly02 __attribute__((swift_name("ProgressOnly02")));
@property (readonly) NSString *ProgressOnly03 __attribute__((swift_name("ProgressOnly03")));
@property (readonly) NSString *ProgressOnly04 __attribute__((swift_name("ProgressOnly04")));
@property (readonly) NSString *Universal01 __attribute__((swift_name("Universal01")));
@property (readonly) NSString *Universal02 __attribute__((swift_name("Universal02")));
@property (readonly) NSString *Universal03 __attribute__((swift_name("Universal03")));
@property (readonly) NSString *Universal04 __attribute__((swift_name("Universal04")));
@property (readonly) NSString *Universal05 __attribute__((swift_name("Universal05")));
@property (readonly) NSString *Universal06 __attribute__((swift_name("Universal06")));
@property (readonly) NSString *Universal07 __attribute__((swift_name("Universal07")));
@property (readonly) NSString *Universal08 __attribute__((swift_name("Universal08")));
@property (readonly) NSString *Universal09 __attribute__((swift_name("Universal09")));
@property (readonly) NSString *Universal10 __attribute__((swift_name("Universal10")));
@property (readonly) NSString *Universal11 __attribute__((swift_name("Universal11")));
@property (readonly) NSString *Universal12 __attribute__((swift_name("Universal12")));
@property (readonly) NSArray<NSString *> *cls __attribute__((swift_name("cls")));
@property (readonly) NSArray<NSString *> *firstClsLayouts __attribute__((swift_name("firstClsLayouts")));
@property (readonly) NSArray<NSString *> *loop __attribute__((swift_name("loop")));
@property (readonly) NSArray<NSString *> *nav __attribute__((swift_name("nav")));
@property (readonly) NSArray<NSString *> *progress __attribute__((swift_name("progress")));
@property (readonly) NSArray<NSString *> *progressCls __attribute__((swift_name("progressCls")));

/** The layouts with a Compose design of their own (universal, cls, nav, progress, prgs_cls, loop). */
@property (readonly) NSArray<NSString *> *supported __attribute__((swift_name("supported")));
@property (readonly) NSArray<NSString *> *universal __attribute__((swift_name("universal")));
@end

__attribute__((objc_subclassing_restricted))
__attribute__((swift_name("NativeAdLayoutNames")))
@interface HDCANativeAdLayoutNames : HDCABase
+ (instancetype)alloc __attribute__((unavailable));
+ (instancetype)allocWithZone:(struct _NSZone *)zone __attribute__((unavailable));
+ (instancetype)nativeAdLayoutNames __attribute__((swift_name("init()")));
@property (class, readonly, getter=shared) HDCANativeAdLayoutNames *shared __attribute__((swift_name("shared")));
- (NSString *)default __attribute__((swift_name("default()")));
- (NSString *)normalizeLayoutName:(NSString * _Nullable)layoutName __attribute__((swift_name("normalize(layoutName:)")));
@property (readonly) NSArray<NSString *> *All __attribute__((swift_name("All")));
@property (readonly) NSString *Default __attribute__((swift_name("Default")));
@end

__attribute__((objc_subclassing_restricted))
__attribute__((swift_name("FullscreenNativeAdCloseStyle")))
@interface HDCAFullscreenNativeAdCloseStyle : HDCAKotlinEnum<HDCAFullscreenNativeAdCloseStyle *>
+ (instancetype)alloc __attribute__((unavailable));
+ (instancetype)allocWithZone:(struct _NSZone *)zone __attribute__((unavailable));
- (instancetype)initWithName:(NSString *)name ordinal:(int32_t)ordinal __attribute__((swift_name("init(name:ordinal:)"))) __attribute__((objc_designated_initializer)) __attribute__((unavailable));
@property (class, readonly) HDCAFullscreenNativeAdCloseStyle *standard __attribute__((swift_name("standard")));
@property (class, readonly) HDCAFullscreenNativeAdCloseStyle *cls __attribute__((swift_name("cls")));
@property (class, readonly) HDCAFullscreenNativeAdCloseStyle *nav __attribute__((swift_name("nav")));
@property (class, readonly) HDCAFullscreenNativeAdCloseStyle *pgrs __attribute__((swift_name("pgrs")));
@property (class, readonly) HDCAFullscreenNativeAdCloseStyle *pgrsCls __attribute__((swift_name("pgrsCls")));
@property (class, readonly) HDCAFullscreenNativeAdCloseStyle *loop __attribute__((swift_name("loop")));
+ (HDCAKotlinArray<HDCAFullscreenNativeAdCloseStyle *> *)values __attribute__((swift_name("values()")));
@property (class, readonly) NSArray<HDCAFullscreenNativeAdCloseStyle *> *entries __attribute__((swift_name("entries")));
@end

__attribute__((objc_subclassing_restricted))
__attribute__((swift_name("FullscreenNativeAdControlPhase")))
@interface HDCAFullscreenNativeAdControlPhase : HDCAKotlinEnum<HDCAFullscreenNativeAdControlPhase *>
+ (instancetype)alloc __attribute__((unavailable));
+ (instancetype)allocWithZone:(struct _NSZone *)zone __attribute__((unavailable));
- (instancetype)initWithName:(NSString *)name ordinal:(int32_t)ordinal __attribute__((swift_name("init(name:ordinal:)"))) __attribute__((objc_designated_initializer)) __attribute__((unavailable));
@property (class, readonly) HDCAFullscreenNativeAdControlPhase *progress __attribute__((swift_name("progress")));
@property (class, readonly) HDCAFullscreenNativeAdControlPhase *closeDelay __attribute__((swift_name("closeDelay")));
@property (class, readonly) HDCAFullscreenNativeAdControlPhase *finished __attribute__((swift_name("finished")));
+ (HDCAKotlinArray<HDCAFullscreenNativeAdControlPhase *> *)values __attribute__((swift_name("values()")));
@property (class, readonly) NSArray<HDCAFullscreenNativeAdControlPhase *> *entries __attribute__((swift_name("entries")));
@end

__attribute__((objc_subclassing_restricted))
__attribute__((swift_name("FullscreenNativeAdControlState")))
@interface HDCAFullscreenNativeAdControlState : HDCABase
- (instancetype)initWithRemainingSeconds:(int32_t)remainingSeconds canClose:(BOOL)canClose showCountdown:(BOOL)showCountdown phase:(HDCAFullscreenNativeAdControlPhase *)phase closeStyle:(HDCAFullscreenNativeAdCloseStyle *)closeStyle progress:(float)progress showProgress:(BOOL)showProgress showOpenStoreButton:(BOOL)showOpenStoreButton showCloseButton:(BOOL)showCloseButton closeButtonEnabled:(BOOL)closeButtonEnabled closeButtonAlpha:(float)closeButtonAlpha __attribute__((swift_name("init(remainingSeconds:canClose:showCountdown:phase:closeStyle:progress:showProgress:showOpenStoreButton:showCloseButton:closeButtonEnabled:closeButtonAlpha:)"))) __attribute__((objc_designated_initializer));
- (HDCAFullscreenNativeAdControlState *)doCopyRemainingSeconds:(int32_t)remainingSeconds canClose:(BOOL)canClose showCountdown:(BOOL)showCountdown phase:(HDCAFullscreenNativeAdControlPhase *)phase closeStyle:(HDCAFullscreenNativeAdCloseStyle *)closeStyle progress:(float)progress showProgress:(BOOL)showProgress showOpenStoreButton:(BOOL)showOpenStoreButton showCloseButton:(BOOL)showCloseButton closeButtonEnabled:(BOOL)closeButtonEnabled closeButtonAlpha:(float)closeButtonAlpha __attribute__((swift_name("doCopy(remainingSeconds:canClose:showCountdown:phase:closeStyle:progress:showProgress:showOpenStoreButton:showCloseButton:closeButtonEnabled:closeButtonAlpha:)")));
- (BOOL)isEqual:(id _Nullable)other __attribute__((swift_name("isEqual(_:)")));
- (NSUInteger)hash __attribute__((swift_name("hash()")));
- (NSString *)description __attribute__((swift_name("description()")));
@property (readonly) BOOL canClose __attribute__((swift_name("canClose")));
@property (readonly) float closeButtonAlpha __attribute__((swift_name("closeButtonAlpha")));
@property (readonly) BOOL closeButtonEnabled __attribute__((swift_name("closeButtonEnabled")));
@property (readonly) HDCAFullscreenNativeAdCloseStyle *closeStyle __attribute__((swift_name("closeStyle")));
@property (readonly) HDCAFullscreenNativeAdControlPhase *phase __attribute__((swift_name("phase")));
@property (readonly) float progress __attribute__((swift_name("progress")));
@property (readonly) int32_t remainingSeconds __attribute__((swift_name("remainingSeconds")));
@property (readonly) BOOL showCloseButton __attribute__((swift_name("showCloseButton")));
@property (readonly) BOOL showCountdown __attribute__((swift_name("showCountdown")));
@property (readonly) BOOL showOpenStoreButton __attribute__((swift_name("showOpenStoreButton")));
@property (readonly) BOOL showProgress __attribute__((swift_name("showProgress")));
@end

__attribute__((swift_name("NativeAdLoader")))
@protocol HDCANativeAdLoader
@required
- (void)loadRequest:(HDCAFullscreenNativeAdRequest *)request onStateChanged_:(void (^)(HDCANativeAdState *))onStateChanged __attribute__((swift_name("load(request:onStateChanged_:)")));
- (void)loadAdUnitId:(NSString *)adUnitId onStateChanged:(void (^)(HDCANativeAdState *))onStateChanged __attribute__((swift_name("load(adUnitId:onStateChanged:)")));
- (void)loadWithResultRequest:(HDCAFullscreenNativeAdRequest *)request onResult_:(void (^)(HDCANativeAdLoadResult *))onResult __attribute__((swift_name("loadWithResult(request:onResult_:)")));
- (void)loadWithResultAdUnitId:(NSString *)adUnitId onResult:(void (^)(HDCANativeAdLoadResult *))onResult __attribute__((swift_name("loadWithResult(adUnitId:onResult:)")));
@end

__attribute__((swift_name("InterstitialAdLoader")))
@protocol HDCAInterstitialAdLoader
@required
- (void)configureAlias:(NSString *)alias config:(HDCAInterstitialAdConfig *)config __attribute__((swift_name("configure(alias:config:)")));
- (void)destroyAlias:(NSString *)alias __attribute__((swift_name("destroy(alias:)")));
- (void)hideAlias:(NSString *)alias __attribute__((swift_name("hide(alias:)")));
- (BOOL)isReadyAlias:(NSString *)alias __attribute__((swift_name("isReady(alias:)")));
- (void)loadAlias:(NSString *)alias onResult:(void (^)(HDCANativeAdLoadResult *))onResult __attribute__((swift_name("load(alias:onResult:)")));
- (void)loadAlias:(NSString *)alias bufferSize:(int32_t)bufferSize onResult:(void (^)(HDCANativeAdLoadResult *))onResult __attribute__((swift_name("load(alias:bufferSize:onResult:)")));
- (void)setCallbackAlias:(NSString *)alias callback:(id<HDCAInterstitialAdCallback> _Nullable)callback __attribute__((swift_name("setCallback(alias:callback:)")));
- (BOOL)showAlias:(NSString *)alias options:(HDCAInterstitialShowOptions *)options __attribute__((swift_name("show(alias:options:)")));
@end


/**
 * @note annotations
 *   androidx.compose.runtime.Immutable
*/
__attribute__((objc_subclassing_restricted))
__attribute__((swift_name("PopupNativeAdComposeCallbacks")))
@interface HDCAPopupNativeAdComposeCallbacks : HDCABase
- (instancetype)initWithOnMediaClick:(void (^)(void))onMediaClick onIconClick:(void (^)(void))onIconClick onCallToActionClick:(void (^)(void))onCallToActionClick onCloseClick:(void (^)(void))onCloseClick __attribute__((swift_name("init(onMediaClick:onIconClick:onCallToActionClick:onCloseClick:)"))) __attribute__((objc_designated_initializer));
- (HDCAPopupNativeAdComposeCallbacks *)doCopyOnMediaClick:(void (^)(void))onMediaClick onIconClick:(void (^)(void))onIconClick onCallToActionClick:(void (^)(void))onCallToActionClick onCloseClick:(void (^)(void))onCloseClick __attribute__((swift_name("doCopy(onMediaClick:onIconClick:onCallToActionClick:onCloseClick:)")));
- (BOOL)isEqual:(id _Nullable)other __attribute__((swift_name("isEqual(_:)")));
- (NSUInteger)hash __attribute__((swift_name("hash()")));
- (NSString *)description __attribute__((swift_name("description()")));
@property (readonly) void (^onCallToActionClick)(void) __attribute__((swift_name("onCallToActionClick")));
@property (readonly) void (^onCloseClick)(void) __attribute__((swift_name("onCloseClick")));
@property (readonly) void (^onIconClick)(void) __attribute__((swift_name("onIconClick")));
@property (readonly) void (^onMediaClick)(void) __attribute__((swift_name("onMediaClick")));
@end


/**
 * @note annotations
 *   androidx.compose.runtime.Immutable
*/
__attribute__((objc_subclassing_restricted))
__attribute__((swift_name("PopupNativeAdComposeState")))
@interface HDCAPopupNativeAdComposeState : HDCABase
- (instancetype)initWithHeadline:(NSString *)headline body:(NSString *)body advertiser:(NSString *)advertiser callToAction:(NSString *)callToAction __attribute__((swift_name("init(headline:body:advertiser:callToAction:)"))) __attribute__((objc_designated_initializer));
- (HDCAPopupNativeAdComposeState *)doCopyHeadline:(NSString *)headline body:(NSString *)body advertiser:(NSString *)advertiser callToAction:(NSString *)callToAction __attribute__((swift_name("doCopy(headline:body:advertiser:callToAction:)")));
- (BOOL)isEqual:(id _Nullable)other __attribute__((swift_name("isEqual(_:)")));
- (NSUInteger)hash __attribute__((swift_name("hash()")));
- (NSString *)description __attribute__((swift_name("description()")));
@property (readonly) NSString *advertiser __attribute__((swift_name("advertiser")));
@property (readonly) NSString *body __attribute__((swift_name("body")));
@property (readonly) NSString *callToAction __attribute__((swift_name("callToAction")));
@property (readonly) NSString *headline __attribute__((swift_name("headline")));
@end

__attribute__((objc_subclassing_restricted))
__attribute__((swift_name("PopupNativeAdLayoutCatalog")))
@interface HDCAPopupNativeAdLayoutCatalog : HDCABase
+ (instancetype)alloc __attribute__((unavailable));
+ (instancetype)allocWithZone:(struct _NSZone *)zone __attribute__((unavailable));
+ (instancetype)popupNativeAdLayoutCatalog __attribute__((swift_name("init()")));
@property (class, readonly, getter=shared) HDCAPopupNativeAdLayoutCatalog *shared __attribute__((swift_name("shared")));
- (float)minHeightDpLayoutName:(NSString * _Nullable)layoutName __attribute__((swift_name("minHeightDp(layoutName:)")));
- (NSString *)normalizeLayoutName:(NSString * _Nullable)layoutName __attribute__((swift_name("normalize(layoutName:)")));
- (BOOL)usesAdChoicesViewLayoutName:(NSString * _Nullable)layoutName __attribute__((swift_name("usesAdChoicesView(layoutName:)")));
- (BOOL)usesMediaViewLayoutName:(NSString * _Nullable)layoutName __attribute__((swift_name("usesMediaView(layoutName:)")));
@property (readonly) NSArray<NSString *> *All __attribute__((swift_name("All")));
@property (readonly) NSString *Default __attribute__((swift_name("Default")));
@property (readonly) float MinCompactHeightDp __attribute__((swift_name("MinCompactHeightDp")));
@property (readonly) float MinHeightDp __attribute__((swift_name("MinHeightDp")));
@property (readonly) float MinWidthDp __attribute__((swift_name("MinWidthDp")));
@property (readonly) NSString *PopupSingleManual01 __attribute__((swift_name("PopupSingleManual01")));
@property (readonly) NSString *PopupSingleManual02 __attribute__((swift_name("PopupSingleManual02")));
@property (readonly) NSString *PopupSingleManual03 __attribute__((swift_name("PopupSingleManual03")));
@property (readonly) NSString *PopupSingleManual04 __attribute__((swift_name("PopupSingleManual04")));
@property (readonly) NSString *PopupSingleManual05 __attribute__((swift_name("PopupSingleManual05")));
@property (readonly) NSString *PopupSingleManual06 __attribute__((swift_name("PopupSingleManual06")));
@property (readonly) NSString *PopupSingleManual07 __attribute__((swift_name("PopupSingleManual07")));
@property (readonly) NSString *PopupSingleManual08 __attribute__((swift_name("PopupSingleManual08")));
@property (readonly) NSString *PopupSingleManual09 __attribute__((swift_name("PopupSingleManual09")));
@property (readonly) NSString *PopupSingleManual10 __attribute__((swift_name("PopupSingleManual10")));
@property (readonly) NSString *PopupSingleManual11 __attribute__((swift_name("PopupSingleManual11")));
@property (readonly) NSString *PopupSingleManual12 __attribute__((swift_name("PopupSingleManual12")));
@property (readonly) NSString *PopupSingleManual13 __attribute__((swift_name("PopupSingleManual13")));
@property (readonly) NSString *PopupSingleManual14 __attribute__((swift_name("PopupSingleManual14")));
@property (readonly) NSString *PopupSingleManual15 __attribute__((swift_name("PopupSingleManual15")));
@end

__attribute__((swift_name("PopupNativeAdLoader")))
@protocol HDCAPopupNativeAdLoader
@required
- (void)destroyInstanceId:(NSString *)instanceId __attribute__((swift_name("destroy(instanceId:)")));
- (void)loadRequest:(HDCAPopupNativeAdRequest *)request onStateChanged__:(void (^)(HDCANativeAdState *))onStateChanged __attribute__((swift_name("load(request:onStateChanged__:)")));
- (void)loadWithResultRequest:(HDCAPopupNativeAdRequest *)request onResult__:(void (^)(HDCANativeAdLoadResult *))onResult __attribute__((swift_name("loadWithResult(request:onResult__:)")));
@end


/**
 * The single entry point for Unity: [call] runs a command and the listener receives ad events.
 *
 * A command is `"<format>.<action>"`, for example `"interstitial.load"`, with a JSON object of arguments.
 * The result is a JSON object: `{"ok":true,"value":…}`, or `{"ok":false,"error":"…"}` when it fails.
 * Commands can be called from any thread; ad work runs on the main thread, and so do events.
 */
__attribute__((objc_subclassing_restricted))
@interface HDCUnityBridge : HDCABase
+ (instancetype)alloc __attribute__((unavailable));

/**
 * The single entry point for Unity: [call] runs a command and the listener receives ad events.
 *
 * A command is `"<format>.<action>"`, for example `"interstitial.load"`, with a JSON object of arguments.
 * The result is a JSON object: `{"ok":true,"value":…}`, or `{"ok":false,"error":"…"}` when it fails.
 * Commands can be called from any thread; ad work runs on the main thread, and so do events.
 */
+ (instancetype)allocWithZone:(struct _NSZone *)zone __attribute__((unavailable));
+ (instancetype)hDCUnityBridge __attribute__((swift_name("init()")));
@property (class, readonly, getter=shared) HDCUnityBridge *shared __attribute__((swift_name("shared")));
- (NSString *)callMethod:(NSString *)method argsJson:(NSString *)argsJson __attribute__((swift_name("call(method:argsJson:)")));

/** [setListener] for Objective-C callers, which pass a block. */
- (void)setEventHandlerHandler:(void (^ _Nullable)(NSString *))handler __attribute__((swift_name("setEventHandler(handler:)")));
- (void)setListenerListener:(id<HDCUnityListener> _Nullable)listener __attribute__((swift_name("setListener(listener:)")));
@end

__attribute__((objc_subclassing_restricted))
__attribute__((swift_name("NativeAdIosBridge")))
@interface HDCANativeAdIosBridge : HDCABase
- (instancetype)init __attribute__((swift_name("init()"))) __attribute__((objc_designated_initializer));
+ (instancetype)new __attribute__((availability(swift, unavailable, message="use object initializers instead")));

/** Unity entry point: closes the fullscreen native on screen for [instanceId] as its close button does. */
- (BOOL)closeFullscreenForUnityInstanceId:(NSString *)instanceId __attribute__((swift_name("closeFullscreenForUnity(instanceId:)")));
- (BOOL)createType:(NSString *)type alias:(NSString *)alias configJson:(NSString *)configJson __attribute__((swift_name("create(type:alias:configJson:)")));
- (BOOL)createInterstitialAlias:(NSString *)alias configJson:(NSString *)configJson __attribute__((swift_name("createInterstitial(alias:configJson:)")));
- (void)createInterstitialAlias:(NSString *)alias adUnitIdsCsv:(NSString *)adUnitIdsCsv preloadBufferSize:(int32_t)preloadBufferSize autoReload:(BOOL)autoReload __attribute__((swift_name("createInterstitial(alias:adUnitIdsCsv:preloadBufferSize:autoReload:)")));
- (BOOL)destroyAlias:(NSString *)alias __attribute__((swift_name("destroy(alias:)")));
- (BOOL)destroyInterstitialAlias:(NSString *)alias __attribute__((swift_name("destroyInterstitial(alias:)")));
- (void)disableMetaTestMode __attribute__((swift_name("disableMetaTestMode()")));
- (BOOL)enableMetaTestModeTestDeviceIds:(NSArray<NSString *> *)testDeviceIds __attribute__((swift_name("enableMetaTestMode(testDeviceIds:)")));

/**
 * Unity entry point: registers this device, plus any hashes in [additionalDeviceHashesCsv]
 * (separated by `,` `;` or new lines), as Meta Audience Network test devices.
 * [testAdType] is the raw `FBAdTestAdType` value (0 = default creative).
 * Call before Meta ads are requested; ads already loaded are not test ads.
 */
- (BOOL)enableMetaTestModeAdditionalDeviceHashesCsv:(NSString *)additionalDeviceHashesCsv testAdType:(int32_t)testAdType __attribute__((swift_name("enableMetaTestMode(additionalDeviceHashesCsv:testAdType:)")));

/**
 * The Unity bridge could not put the screen built for [instanceId] on screen (UIKit refused the presentation).
 * The ad was marked as showing when that screen was built; make it showable again.
 */
- (void)fullscreenShowDidFailInstanceId:(NSString *)instanceId __attribute__((swift_name("fullscreenShowDidFail(instanceId:)")));
- (UIViewController *)fullscreenViewControllerInstanceId:(NSString *)instanceId options:(HDCAFullscreenNativeAdShowOptions *)options fallbackAdUnitId:(NSString *)fallbackAdUnitId __attribute__((swift_name("fullscreenViewController(instanceId:options:fallbackAdUnitId:)")));
- (UIViewController *)fullscreenViewControllerInstanceId:(NSString *)instanceId layoutName:(NSString *)layoutName durationSeconds:(double)durationSeconds fallbackAdUnitId:(NSString *)fallbackAdUnitId __attribute__((swift_name("fullscreenViewController(instanceId:layoutName:durationSeconds:fallbackAdUnitId:)")));
- (UIViewController *)fullscreenViewControllerForUnityInstanceId:(NSString *)instanceId layoutName:(NSString *)layoutName durationSeconds:(double)durationSeconds fallbackAdUnitId:(NSString *)fallbackAdUnitId onClosed:(void (^)(void))onClosed __attribute__((swift_name("fullscreenViewControllerForUnity(instanceId:layoutName:durationSeconds:fallbackAdUnitId:onClosed:)")));
- (UIViewController *)fullscreenViewControllerForUnityWithOptionsInstanceId:(NSString *)instanceId mode:(NSString *)mode layoutNamesCsv:(NSString *)layoutNamesCsv durationSeconds:(double)durationSeconds durationsSecondsCsv:(NSString *)durationsSecondsCsv orientation:(NSString *)orientation autoClose:(BOOL)autoClose pauseGameplay:(BOOL)pauseGameplay enableAdComeback:(BOOL)enableAdComeback showTCD:(BOOL)showTCD delaySeconds:(double)delaySeconds timeUpCSeconds:(int32_t)timeUpCSeconds fallbackAdUnitId:(NSString *)fallbackAdUnitId cta:(BOOL)cta headline:(BOOL)headline body:(BOOL)body description:(BOOL)description icon:(BOOL)icon advertiser:(BOOL)advertiser media:(BOOL)media mediaImage:(BOOL)mediaImage mediaVideo:(BOOL)mediaVideo onClosed:(void (^)(void))onClosed __attribute__((swift_name("fullscreenViewControllerForUnityWithOptions(instanceId:mode:layoutNamesCsv:durationSeconds:durationsSecondsCsv:orientation:autoClose:pauseGameplay:enableAdComeback:showTCD:delaySeconds:timeUpCSeconds:fallbackAdUnitId:cta:headline:body:description:icon:advertiser:media:mediaImage:mediaVideo:onClosed:)")));
- (UIViewController *)fullscreenViewControllerWithAutoCloseInstanceId:(NSString *)instanceId layoutName:(NSString *)layoutName durationSeconds:(double)durationSeconds fallbackAdUnitId:(NSString *)fallbackAdUnitId autoClose:(BOOL)autoClose __attribute__((swift_name("fullscreenViewControllerWithAutoClose(instanceId:layoutName:durationSeconds:fallbackAdUnitId:autoClose:)")));
- (NSString *)getCurrentMetaTestDeviceHash __attribute__((swift_name("getCurrentMetaTestDeviceHash()")));
- (BOOL)isInterstitialReadyAlias:(NSString *)alias __attribute__((swift_name("isInterstitialReady(alias:)")));
- (BOOL)isMetaTestModeEnabled __attribute__((swift_name("isMetaTestModeEnabled()")));
- (BOOL)isReadyAlias:(NSString *)alias __attribute__((swift_name("isReady(alias:)")));
- (void)loadAdUnitId:(NSString *)adUnitId rootViewController:(UIViewController *)rootViewController __attribute__((swift_name("load(adUnitId:rootViewController:)")));
- (void)loadFullscreenInstanceId:(NSString *)instanceId adUnitIdsCsv:(NSString *)adUnitIdsCsv rootViewController:(UIViewController *)rootViewController __attribute__((swift_name("loadFullscreen(instanceId:adUnitIdsCsv:rootViewController:)")));
- (void)loadFullscreenInstanceId:(NSString *)instanceId adUnitIdsCsv:(NSString *)adUnitIdsCsv rootViewController:(UIViewController *)rootViewController enableReloadAfterShow:(BOOL)enableReloadAfterShow onStateChanged:(void (^)(HDCANativeAdState *))onStateChanged __attribute__((swift_name("loadFullscreen(instanceId:adUnitIdsCsv:rootViewController:enableReloadAfterShow:onStateChanged:)")));
- (void)loadFullscreenWithReloadInstanceId:(NSString *)instanceId adUnitIdsCsv:(NSString *)adUnitIdsCsv rootViewController:(UIViewController *)rootViewController enableReloadAfterShow:(BOOL)enableReloadAfterShow onStateChanged:(void (^)(HDCANativeAdState *))onStateChanged __attribute__((swift_name("loadFullscreenWithReload(instanceId:adUnitIdsCsv:rootViewController:enableReloadAfterShow:onStateChanged:)")));
- (BOOL)loadInterstitialRootViewController:(UIViewController *)rootViewController alias:(NSString *)alias __attribute__((swift_name("loadInterstitial(rootViewController:alias:)")));
- (BOOL)loadInterstitialRootViewController:(UIViewController *)rootViewController alias:(NSString *)alias bufferSize:(int32_t)bufferSize __attribute__((swift_name("loadInterstitial(rootViewController:alias:bufferSize:)")));
- (BOOL)preloadAllRootViewController:(UIViewController *)rootViewController alias:(NSString *)alias __attribute__((swift_name("preloadAll(rootViewController:alias:)")));
- (BOOL)preloadOneRootViewController:(UIViewController *)rootViewController alias:(NSString *)alias adUnitId:(NSString *)adUnitId __attribute__((swift_name("preloadOne(rootViewController:alias:adUnitId:)")));

/**
 * Unity entry point: puts a screen from [fullscreenViewControllerForUnity] or
 * [fullscreenViewControllerForUnityWithOptions] on screen. Like the reference library's Android overlay, an ad shown while an
 * interstitial is up, or together with one, goes under the interstitial and counts down there.
 * [onFailed] runs when the screen could not be shown; the ad is showable again then.
 */
- (void)presentFullscreenForUnityInstanceId:(NSString *)instanceId controller:(UIViewController *)controller onShown:(void (^)(void))onShown onFailed:(void (^)(NSString *))onFailed __attribute__((swift_name("presentFullscreenForUnity(instanceId:controller:onShown:onFailed:)")));
- (BOOL)setInterstitialCallbackAlias:(NSString *)alias callback:(id<HDCAInterstitialAdCallback> _Nullable)callback __attribute__((swift_name("setInterstitialCallback(alias:callback:)")));
- (BOOL)setInterstitialCallbackRootViewController:(UIViewController *)rootViewController alias:(NSString *)alias callback:(id<HDCAInterstitialAdCallback> _Nullable)callback __attribute__((swift_name("setInterstitialCallback(rootViewController:alias:callback:)")));
- (BOOL)showInterstitialRootViewController:(UIViewController *)rootViewController alias:(NSString *)alias __attribute__((swift_name("showInterstitial(rootViewController:alias:)")));
- (BOOL)showInterstitialRootViewController:(UIViewController *)rootViewController alias:(NSString *)alias optionsJson:(NSString * _Nullable)optionsJson __attribute__((swift_name("showInterstitial(rootViewController:alias:optionsJson:)")));
- (UIViewController *)viewControllerAdUnitId:(NSString *)adUnitId __attribute__((swift_name("viewController(adUnitId:)")));
@end


/** Receives bridge events. Each event is a JSON object: `{"id":…,"format":…,"type":…}` plus its fields. */
@protocol HDCUnityListener
@required
- (void)onEventEventJson:(NSString *)eventJson __attribute__((swift_name("onEvent(eventJson:)")));
@end

__attribute__((objc_subclassing_restricted))
__attribute__((swift_name("AdUnitDefaults_iosKt")))
@interface HDCAAdUnitDefaults_iosKt : HDCABase
+ (NSString *)defaultNativeAdUnitId __attribute__((swift_name("defaultNativeAdUnitId()")));
+ (NSString *)defaultNativeVideoAdUnitId __attribute__((swift_name("defaultNativeVideoAdUnitId()")));
@end

__attribute__((objc_subclassing_restricted))
__attribute__((swift_name("BannerNativeAdDefaultsKt")))
@interface HDCABannerNativeAdDefaultsKt : HDCABase
@property (class, readonly) NSString *DefaultBannerNativeAdInstanceId __attribute__((swift_name("DefaultBannerNativeAdInstanceId")));
@end

__attribute__((objc_subclassing_restricted))
__attribute__((swift_name("InterstitialAdDefaultsKt")))
@interface HDCAInterstitialAdDefaultsKt : HDCABase
@property (class, readonly) NSString *DefaultInterstitialAdInstanceId __attribute__((swift_name("DefaultInterstitialAdInstanceId")));
@end

__attribute__((objc_subclassing_restricted))
__attribute__((swift_name("InterstitialAdUnitDefaults_iosKt")))
@interface HDCAInterstitialAdUnitDefaults_iosKt : HDCABase
+ (NSString *)defaultInterstitialAdUnitId __attribute__((swift_name("defaultInterstitialAdUnitId()")));
@end

__attribute__((objc_subclassing_restricted))
__attribute__((swift_name("NativeAdDefaultsKt")))
@interface HDCANativeAdDefaultsKt : HDCABase
@property (class, readonly) NSString *DefaultNativeAdInstanceId __attribute__((swift_name("DefaultNativeAdInstanceId")));
@end

__attribute__((objc_subclassing_restricted))
__attribute__((swift_name("Platform_iosKt")))
@interface HDCAPlatform_iosKt : HDCABase
+ (id<HDCAPlatform>)getPlatform __attribute__((swift_name("getPlatform()")));
@end

__attribute__((objc_subclassing_restricted))
__attribute__((swift_name("PopupNativeAdDefaultsKt")))
@interface HDCAPopupNativeAdDefaultsKt : HDCABase
@property (class, readonly) NSString *DefaultPopupNativeAdInstanceId __attribute__((swift_name("DefaultPopupNativeAdInstanceId")));
@end

__attribute__((objc_subclassing_restricted))
__attribute__((swift_name("KotlinEnumCompanion")))
@interface HDCAKotlinEnumCompanion : HDCABase
+ (instancetype)alloc __attribute__((unavailable));
+ (instancetype)allocWithZone:(struct _NSZone *)zone __attribute__((unavailable));
+ (instancetype)companion __attribute__((swift_name("init()")));
@property (class, readonly, getter=shared) HDCAKotlinEnumCompanion *shared __attribute__((swift_name("shared")));
@end

__attribute__((objc_subclassing_restricted))
__attribute__((swift_name("KotlinArray")))
@interface HDCAKotlinArray<T> : HDCABase
+ (instancetype)arrayWithSize:(int32_t)size init:(T _Nullable (^)(HDCAInt *))init __attribute__((swift_name("init(size:init:)")));
+ (instancetype)alloc __attribute__((unavailable));
+ (instancetype)allocWithZone:(struct _NSZone *)zone __attribute__((unavailable));
- (T _Nullable)getIndex:(int32_t)index __attribute__((swift_name("get(index:)")));
- (id<HDCAKotlinIterator>)iterator __attribute__((swift_name("iterator()")));
- (void)setIndex:(int32_t)index value:(T _Nullable)value __attribute__((swift_name("set(index:value:)")));
@property (readonly) int32_t size __attribute__((swift_name("size")));
@end

__attribute__((swift_name("KotlinIterator")))
@protocol HDCAKotlinIterator
@required
- (BOOL)hasNext __attribute__((swift_name("hasNext()")));
- (id _Nullable)next __attribute__((swift_name("next()")));
@end

#pragma pop_macro("_Nullable_result")
#pragma clang diagnostic pop
NS_ASSUME_NONNULL_END
