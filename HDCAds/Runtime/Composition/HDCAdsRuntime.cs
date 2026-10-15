using System;
using System.Collections.Generic;
using HDC.Ads.Diagnostics;
using HDC.Ads.Infrastructure;
using HDC.Ads.Logic;
using HDC.Ads.Ports;
using UnityEngine;

namespace HDC.Ads.Composition
{
    internal sealed class HDCAdsRuntime
    {
        internal HDCAdsRuntime(IClock clock, IKeyValueStore store, IMainThread mainThread, IAdsLog log, IAdsSdk sdk, IAdsTesting testing,
            IReadOnlyList<IAdNetwork> networks, IUnitOrderPolicy order, IReadOnlyList<IMediationPartner> partners = null,
            IEnumerable<Func<HDCAdsContext, IAdChannel>> extraChannels = null, IConsentProvider consent = null,
            ITrackingAuthorization tracking = null)
        {
            var consentGate = new HDCConsentGate(consent ?? new HDCNoConsentNeeded(), tracking ?? new HDCNoTracking(), store, clock, mainThread, log,
                () => testing.IsTestDevice);
            Context = new HDCAdsContext(clock, store, mainThread, log, sdk, networks, order, consentGate);
            Channels = new HDCChannels(Context, extraChannels);
            Testing = testing;
            Partners = partners ?? new IMediationPartner[0];
        }

        internal static HDCAdsRuntime Current { get; private set; } = CreateDefault();

        internal HDCAdsContext Context { get; }
        internal HDCChannels Channels { get; }
        internal IAdsTesting Testing { get; }

        internal IReadOnlyList<IMediationPartner> Partners { get; }

        internal static HDCAdsRuntime CreateDefault(IConsentProvider consent = null, ITrackingAuthorization tracking = null)
        {
            var meta = new HDCMetaPartner();
            return new HDCAdsRuntime(new HDCUnityClock(), new HDCPlayerPrefsStore(), new HDCUnityMainThread(), new HDCUnityAdsLog(),
                new HDCAdsSdkPort(), new HDCAdsTestingPort(meta), new IAdNetwork[] { new HDCAdMobNetwork(), new HDCNativeNetwork() },
                new HDCPriorityOrder(), new IMediationPartner[] { meta }, null, consent ?? DeviceConsent(), tracking ?? DeviceTracking());
        }

        private static IConsentProvider DeviceConsent()
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            return new HDCUmpConsent();
#else
            return new HDCNoConsentNeeded();
#endif
        }

        private static ITrackingAuthorization DeviceTracking()
        {
#if UNITY_IOS && !UNITY_EDITOR
            return new HDCAppTracking();
#else
            return new HDCNoTracking();
#endif
        }

        internal static void Use(HDCAdsRuntime runtime) => Current = runtime;

        internal static IDeviceRegionSource DeviceRegion { get; } = new HDCDeviceRegionReader();

        internal static HDCConfigSelection ConfigSelection()
        {
            var log = new HDCUnityAdsLog();
            return new HDCConfigSelection(new HDCCountryMode(DeviceRegion, log), log);
        }

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForPlayMode()
        {
            HDCMainThread.ResetStatics();
            HDCGma.ResetStatics();
            HDCAdsSdk.ResetStatics();
            HDCNativeScreens.ResetStatics();
            HDCConfigReport.Reset();
            HDCMediationReport.Reset();
            Current = CreateDefault();
        }
#endif
    }
}
