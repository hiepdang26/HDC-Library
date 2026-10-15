#import <Foundation/Foundation.h>
#import <UIKit/UIKit.h>
#import <AppTrackingTransparency/AppTrackingTransparency.h>
#import <HDCAds/HDCAds.h>
#include <stdlib.h>
#include <string.h>

typedef void (*HDCAdsEventCallback)(const char *eventJson);
typedef void (*HDCAdsTrackingCallback)(int status);

static HDCAdsEventCallback HDCAdsEventCallbackPointer = NULL;

static NSString *HDCAdsString(const char *value) {
    return value == NULL ? @"" : [NSString stringWithUTF8String:value];
}

static void HDCAdsDeliverEvent(NSString *eventJson) {
    HDCAdsEventCallback callback = HDCAdsEventCallbackPointer;
    if (callback != NULL) {
        callback(eventJson.UTF8String);
    }
}

API_AVAILABLE(ios(14.0))
static void HDCAdsAskTracking(HDCAdsTrackingCallback callback) {
    if (UIApplication.sharedApplication.applicationState != UIApplicationStateActive) {
        __block id observer = nil;
        observer = [NSNotificationCenter.defaultCenter addObserverForName:UIApplicationDidBecomeActiveNotification
                                                                   object:nil
                                                                    queue:NSOperationQueue.mainQueue
                                                               usingBlock:^(NSNotification *notification) {
            [NSNotificationCenter.defaultCenter removeObserver:observer];
            HDCAdsAskTracking(callback);
        }];
        return;
    }

    [ATTrackingManager requestTrackingAuthorizationWithCompletionHandler:^(ATTrackingManagerAuthorizationStatus status) {
        dispatch_async(dispatch_get_main_queue(), ^{
            if (callback != NULL) {
                callback((int)status);
            }
        });
    }];
}

extern "C" {
    const char *HDCAds_Call(const char *method, const char *argsJson) {
        NSString *nativeMethod = HDCAdsString(method);
        NSString *nativeArgs = HDCAdsString(argsJson);
        __block NSString *result = nil;
        void (^call)(void) = ^{
            result = [HDCUnityBridge.shared callMethod:nativeMethod argsJson:nativeArgs];
        };
        if ([NSThread isMainThread]) {
            call();
        } else {
            dispatch_sync(dispatch_get_main_queue(), call);
        }
        const char *utf8 = (result ?: @"{\"ok\":false,\"error\":\"No result\"}").UTF8String;
        return strdup(utf8 != NULL ? utf8 : "");
    }

    const char *HDCAds_LocaleInfo(void) {
        NSLocale *locale = NSLocale.currentLocale;
        NSString *language = [locale objectForKey:NSLocaleLanguageCode] ?: @"";
        NSString *preferred = NSLocale.preferredLanguages.firstObject ?: @"";
        NSString *region = [locale objectForKey:NSLocaleCountryCode] ?: @"";
        NSString *zone = NSTimeZone.localTimeZone.name ?: @"";
        NSString *info = [NSString stringWithFormat:@"%@|%@|%@|%@", language, preferred, region, zone];
        const char *utf8 = info.UTF8String;
        return strdup(utf8 != NULL ? utf8 : "");
    }

    const char *HDCAds_ViewMetrics(void) {
        id<UIApplicationDelegate> delegate = UIApplication.sharedApplication.delegate;
        UIWindow *window = [delegate respondsToSelector:@selector(window)] ? delegate.window : nil;
        UIView *view = window.rootViewController.view;
        if (view == nil) {
            return strdup("");
        }
        CGRect bounds = view.bounds;
        CGRect safe = view.safeAreaLayoutGuide.layoutFrame;
        if (CGSizeEqualToSize(safe.size, CGSizeZero)) {
            safe = bounds;
        }
        NSString *metrics = [NSString stringWithFormat:@"%.3f|%.3f|%.3f|%.3f",
                             bounds.size.width, bounds.size.height, safe.origin.x, safe.origin.y];
        const char *utf8 = metrics.UTF8String;
        return strdup(utf8 != NULL ? utf8 : "");
    }

    void HDCAds_ClearUserDefaults(void) {
        NSMutableSet<NSString *> *domains = [NSMutableSet set];
        NSString *app = NSBundle.mainBundle.bundleIdentifier;
        if (app.length > 0) {
            [domains addObject:app];
        }
        NSString *folder = [NSHomeDirectory() stringByAppendingPathComponent:@"Library/Preferences"];
        for (NSString *file in [NSFileManager.defaultManager contentsOfDirectoryAtPath:folder error:NULL]) {
            NSString *domain = file.stringByDeletingPathExtension;
            if ([file.pathExtension isEqualToString:@"plist"] && ![domain hasPrefix:@"com.apple."]) {
                [domains addObject:domain];
            }
        }
        for (NSString *domain in domains) {
            [NSUserDefaults.standardUserDefaults removePersistentDomainForName:domain];
        }
        [NSUserDefaults.standardUserDefaults synchronize];
    }

    int HDCAds_TrackingStatus(void) {
        if (@available(iOS 14.0, *)) {
            return (int)ATTrackingManager.trackingAuthorizationStatus;
        }
        return -1;
    }

    void HDCAds_RequestTracking(HDCAdsTrackingCallback callback) {
        if (@available(iOS 14.0, *)) {
            dispatch_async(dispatch_get_main_queue(), ^{
                HDCAdsAskTracking(callback);
            });
            return;
        }
        if (callback != NULL) {
            callback(-1);
        }
    }

    void HDCAds_SetEventCallback(HDCAdsEventCallback callback) {
        HDCAdsEventCallbackPointer = callback;
        if (callback == NULL) {
            [HDCUnityBridge.shared setEventHandlerHandler:nil];
            return;
        }
        [HDCUnityBridge.shared setEventHandlerHandler:^(NSString *eventJson) {
            if ([NSThread isMainThread]) {
                HDCAdsDeliverEvent(eventJson);
            } else {
                dispatch_async(dispatch_get_main_queue(), ^{
                    HDCAdsDeliverEvent(eventJson);
                });
            }
        }];
    }
}
