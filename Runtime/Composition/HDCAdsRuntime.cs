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
            IEnumerable<Func<HDCAdsContext, IAdChannel>> extraChannels = null)
        {
            Context = new HDCAdsContext(clock, store, mainThread, log, sdk, networks, order);
            Channels = new HDCChannels(Context, extraChannels);
            Testing = testing;
            Partners = partners ?? new IMediationPartner[0];
        }

        internal static HDCAdsRuntime Current { get; private set; } = CreateDefault();

        internal HDCAdsContext Context { get; }
        internal HDCChannels Channels { get; }
        internal IAdsTesting Testing { get; }

        internal IReadOnlyList<IMediationPartner> Partners { get; }

        internal static HDCAdsRuntime CreateDefault()
        {
            var meta = new HDCMetaPartner();
            return new HDCAdsRuntime(new HDCUnityClock(), new HDCPlayerPrefsStore(), new HDCUnityMainThread(), new HDCUnityAdsLog(),
                new HDCAdsSdkPort(), new HDCAdsTestingPort(meta), new IAdNetwork[] { new HDCAdMobNetwork(), new HDCNativeNetwork() },
                new HDCPriorityOrder(), new IMediationPartner[] { meta });
        }

        internal static void Use(HDCAdsRuntime runtime) => Current = runtime;

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForPlayMode()
        {
            HDCMainThread.ResetStatics();
            HDCGma.ResetStatics();
            HDCAdsSdk.ResetStatics();
            HDCConfigReport.Reset();
            HDCMediationReport.Reset();
            Current = CreateDefault();
        }
#endif
    }
}
