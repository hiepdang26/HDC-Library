using System.Collections.Generic;
using HDC.Ads.Diagnostics;
using HDC.Ads.Infrastructure;
using HDC.Ads.Logic;
using HDC.Ads.Ports;
using UnityEngine;

namespace HDC.Ads.Composition
{
    /// <summary>
    /// Puts the library together: the infrastructure behind each port, the context the channels share and the
    /// channels. The one class that knows both the logic and the infrastructure under it; tests build runtimes
    /// with ports of their own.
    /// </summary>
    internal sealed class HDCAdsRuntime
    {
        internal HDCAdsRuntime(IClock clock, IKeyValueStore store, IMainThread mainThread, IAdsLog log, IAdsSdk sdk, IAdsTesting testing,
            IReadOnlyList<IAdNetwork> networks, IUnitOrderPolicy order, IReadOnlyList<IMediationPartner> partners = null)
        {
            Context = new HDCAdsContext(clock, store, mainThread, log, sdk, networks, order);
            Channels = new HDCChannels(Context);
            Testing = testing;
            Partners = partners ?? new IMediationPartner[0];
        }

        /// <summary>The runtime <see cref="HDCAds"/> runs on.</summary>
        internal static HDCAdsRuntime Current { get; private set; } = CreateDefault();

        internal HDCAdsContext Context { get; }
        internal HDCChannels Channels { get; }
        internal IAdsTesting Testing { get; }

        /// <summary>The mediation partners with setup of their own.</summary>
        internal IReadOnlyList<IMediationPartner> Partners { get; }

        /// <summary>
        /// A runtime on Unity, with two ad networks, the Google Mobile Ads plugin and the native library, and the
        /// mediation partners with setup of their own. A new network is one more entry here, and its unit key in
        /// the order policy; a new partner is one more entry in the partners.
        /// </summary>
        internal static HDCAdsRuntime CreateDefault()
        {
            var meta = new HDCMetaPartner();
            return new HDCAdsRuntime(new HDCUnityClock(), new HDCPlayerPrefsStore(), new HDCUnityMainThread(), new HDCUnityAdsLog(),
                new HDCAdsSdkPort(), new HDCAdsTestingPort(meta), new IAdNetwork[] { new HDCAdMobNetwork(), new HDCNativeNetwork() },
                new HDCPriorityOrder(), new IMediationPartner[] { meta });
        }

        /// <summary>Runs <see cref="HDCAds"/> on another runtime, such as one with fake ports in a test.</summary>
        internal static void Use(HDCAdsRuntime runtime) => Current = runtime;

#if UNITY_EDITOR
        /// <summary>
        /// Puts every static of the library back to its start state when Play Mode starts. Without a domain
        /// reload (Enter Play Mode Options, the default of new Unity 6.6 projects), statics keep the last
        /// session's ads, callbacks and subscribers. Players always start fresh, so this is Editor only.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForPlayMode()
        {
            // The last runtime's channels subscribed to these statics' events: clear them, then start over.
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
