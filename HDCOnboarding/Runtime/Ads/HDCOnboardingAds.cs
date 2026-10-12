using System.Collections;
using HDC.Ads;
using HDC.Ads.Composition;
using HDC.Ads.Diagnostics;
using HDC.Ads.Domain;
using HDC.Ads.Logic;
using UnityEngine;

namespace HDC.Onboarding
{
    internal static class HDCOnboardingAds
    {
        internal const string SplashInterstitialId = "splash_it";
        internal const string SplashLaunchNativeId = "splash_launch_native";
        internal const string SplashLaunchAppOpenId = "splash_launch_open";
        internal const string SplashNativeAfterId = "splash_naf";
        internal const string SplashPosition = "splash";
        internal const float PopupRetrySeconds = 1f;

        internal static HDCAdsContext Context => HDCAdsRuntime.Current.Context;

        internal static bool SdkReady => HDCAds.IsInitialized;

        internal static bool FirebaseChecked => HDCConfigReport.Started ? HDCConfigReport.FirebaseKnown : ConfigApplied;

        internal static bool RemoteConfigDone => HDCConfigReport.Started ? HDCConfigReport.Finished : ConfigApplied;

        private static bool ConfigApplied => HDCConfigReport.AppliedAds != null;

        internal static void RecordSplash(string instanceId) =>
            Context.Placements.Record(instanceId, HDCAdChannel.AppLaunch, SplashPosition, HDCAdRevenue.AdMob);

        internal static bool IsPopupReady(string position) =>
            !string.IsNullOrEmpty(position) && HDCAds.Channels.Popup.IsReadyAt(position);

        internal static bool ShowPopup(string position, RectTransform area)
        {
            if (string.IsNullOrEmpty(position))
                return false;
            if (area != null)
            {
                Canvas canvas = area.GetComponentInParent<Canvas>();
                Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
                HDCAds.Popup.Move(position, area, camera);
            }

            return HDCAds.Popup.Show(position);
        }

        internal static void HidePopup(string position)
        {
            if (string.IsNullOrEmpty(position))
                return;
            HDCPopupEvents.Hidden(position);
            HDCAds.Popup.Hide(position);
        }

        internal static string PopupInstance(string position)
        {
            string group = HDCAds.CoreConfig.PopupGroupAt(position);
            return HDCAdNames.NativePopup(string.IsNullOrEmpty(group) ? position : group);
        }

        internal static IEnumerator ShowPopupUntilShown(string position, RectTransform area, HDCWaitToken token, bool waitForOverlay = true)
        {
            HDCPopupEvents.Counts counts = HDCPopupEvents.Of(PopupInstance(position));
            int shown = counts.Shown;
            int failed = counts.Failed;
            int closed = counts.Closed;
            int hides = HDCPopupEvents.Hides(position);
            if (waitForOverlay && Context.HasOverlayAd)
                HDCOnboardingLog.Log($"Popup {position} waits for the full-screen ad on screen to close");
            while (waitForOverlay && Context.HasOverlayAd)
            {
                if ((token != null && token.Cancelled) || HDCPopupEvents.Hides(position) != hides)
                    yield break;
                yield return null;
            }

            if (!ShowPopup(position, area))
                yield break;

            bool skipped = false;
            float sinceShow = 0f;
            while (counts.Shown == shown && counts.Failed == failed)
            {
                yield return null;
                if ((token != null && token.Cancelled) || HDCPopupEvents.Hides(position) != hides)
                    yield break;
                if (!skipped)
                {
                    if (counts.Closed == closed)
                        continue;
                    skipped = true;
                    HDCOnboardingLog.Log($"Popup {position} closed before it showed: showing it again until it does");
                }

                sinceShow += Time.unscaledDeltaTime;
                if (sinceShow < PopupRetrySeconds || (waitForOverlay && Context.HasOverlayAd))
                    continue;
                sinceShow = 0f;
                if (!HDCAds.Popup.CanShow(position))
                    yield break;
                ShowPopup(position, area);
            }
        }

        internal static void InitializePopup(string groupName)
        {
            if (!string.IsNullOrEmpty(groupName))
                HDCAds.Popup.Initialize(groupName);
        }

        internal static void InitializeForceAd(string groupName)
        {
            if (!string.IsNullOrEmpty(groupName))
                HDCAds.ForceAd.Initialize(groupName);
        }

        internal static bool CanShowForceAd(string position) => !string.IsNullOrEmpty(position) && HDCAds.ForceAd.CanShow(position);

        internal static IEnumerator WaitForceAd(string position, float timeOut)
        {
            float waited = 0f;
            while (!CanShowForceAd(position) && waited < timeOut)
            {
                yield return null;
                waited += Time.deltaTime;
            }
        }

        internal static IEnumerator ShowForceAd(string position)
        {
            bool done = false;
            HDCAds.ForceAd.Show(position, () => done = true);
            while (!done)
                yield return null;
        }
    }
}
