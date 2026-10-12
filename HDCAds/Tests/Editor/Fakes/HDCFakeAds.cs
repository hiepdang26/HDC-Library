using System;
using System.Collections.Generic;
using HDC.Ads.Composition;
using HDC.Ads.Domain;
using HDC.Ads.Logic;
using HDC.Ads.Ports;

namespace HDC.Ads.Tests
{
    internal sealed class HDCFakeAds
    {
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

        internal HDCFakeNetwork AdMob { get; }

        internal HDCFakeNetwork Native { get; }

        internal HDCAdsRuntime Runtime { get; }
        internal HDCAdsContext Context => Runtime.Context;
        internal HDCChannels Channels => Runtime.Channels;

        internal void Start(string adsConfig, string coreConfig)
        {
            Context.Initialize(adsConfig, coreConfig, null);
            Sdk.BecomeReady();
        }

        internal void Wait(float seconds)
        {
            Clock.Advance(seconds);
            MainThread.Tick();
        }
    }
}
