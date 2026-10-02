using System;
using System.Collections.Generic;
using HDC.Ads.Composition;
using HDC.Ads.Domain;
using HDC.Ads.Logic;
using HDC.Ads.Ports;

namespace HDC.Ads.Tests
{
    /// <summary>
    /// A runtime on fakes: a clock, a store, a main thread and an SDK the test drives, and a fake network under
    /// each unit key of the configs. Nothing in it touches Unity's time, PlayerPrefs or the native library.
    /// </summary>
    internal sealed class HDCFakeAds
    {
        /// <param name="extraChannels">Channels beyond the seven, such as <see cref="HDCFakeChannel"/>.</param>
        internal HDCFakeAds(HDCFakeStore store = null, IEnumerable<Func<HDCAdsContext, IAdChannel>> extraChannels = null)
        {
            Store = store ?? new HDCFakeStore();
            AdMob = new HDCFakeNetwork(HDCAdUnitKeys.AdMob, Sdk);
            Native = new HDCFakeNetwork(HDCAdUnitKeys.Native, Sdk);
            Runtime = new HDCAdsRuntime(Clock, Store, MainThread, Log, Sdk, new HDCFakeTesting(),
                new IAdNetwork[] { AdMob, Native }, new HDCPriorityOrder(), null, extraChannels);
        }

        internal HDCFakeClock Clock { get; } = new HDCFakeClock();
        internal HDCFakeStore Store { get; }
        internal HDCFakeMainThread MainThread { get; } = new HDCFakeMainThread();
        internal HDCFakeLog Log { get; } = new HDCFakeLog();
        internal HDCFakeSdk Sdk { get; } = new HDCFakeSdk();

        /// <summary>The network under "admobUnit".</summary>
        internal HDCFakeNetwork AdMob { get; }

        /// <summary>The network under "androidUnit".</summary>
        internal HDCFakeNetwork Native { get; }

        internal HDCAdsRuntime Runtime { get; }
        internal HDCAdsContext Context => Runtime.Context;
        internal HDCChannels Channels => Runtime.Channels;

        /// <summary>Applies the configs and lets the SDK become ready, which starts the channels.</summary>
        internal void Start(string adsConfig, string coreConfig)
        {
            Context.Initialize(adsConfig, coreConfig, null);
            Sdk.BecomeReady();
        }

        /// <summary>Moves time on, then runs a frame.</summary>
        internal void Wait(float seconds)
        {
            Clock.Advance(seconds);
            MainThread.Tick();
        }
    }
}
