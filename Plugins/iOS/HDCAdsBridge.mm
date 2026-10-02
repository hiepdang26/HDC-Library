#import <Foundation/Foundation.h>
#import <HDCAds/HDCAds.h>
#include <stdlib.h>
#include <string.h>

typedef void (*HDCAdsEventCallback)(const char *eventJson);

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
