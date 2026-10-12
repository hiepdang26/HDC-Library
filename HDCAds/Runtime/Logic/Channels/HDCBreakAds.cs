using System;
using System.Collections.Generic;
using HDC.Ads.Domain;
using HDC.Ads.Ports;
using UnityEngine;

namespace HDC.Ads.Logic
{
    internal sealed partial class HDCBreakAds : IBreakAds, IAdChannel
    {
        internal const string Position = "break_ad";
        internal const float DefaultLoadingTimeout = 10f;
        private const float MaxTickSeconds = 1f;

        private readonly HDCAdsContext context;
        private readonly HashSet<string> otherFullscreens = new HashSet<string>(StringComparer.Ordinal);
        private HDCFullscreenSource source;
        private bool running;
        private bool paused;
        private bool loading;
        private bool displayed;
        private float lastTime;
        private float elapsed;
        private float waitedForLoad;
        private float loadingTimeout = DefaultLoadingTimeout;
        private string offReason = "not started";
        private string lastResult = "";

        internal HDCBreakAds(HDCAdsContext context)
        {
            this.context = context;
            context.Sdk.AdEvent += OnAdEvent;
        }

        public bool IsRunning => running;

        public HDCBreakAdState State { get; private set; }

        public float CountdownSeconds { get; private set; }

        public float CountdownLeft { get; private set; }

        public float LoadingTimeout
        {
            get => loadingTimeout;
            set => loadingTimeout = Mathf.Max(0f, value);
        }

        public event Action<HDCBreakAdState> StateChanged;

        private HDCAdCoreConfig.BreakAds Config => context.CoreConfig.break_ads_config ?? new HDCAdCoreConfig.BreakAds();

        public void Start()
        {
            if (running)
                return;
            running = true;
            paused = false;
            lastTime = context.Clock.UnscaledTime;
            context.MainThread.Ticked += Tick;
            context.MainThread.ApplicationPaused += OnApplicationPaused;
            context.Log.Info("break ad started");
            if (State == HDCBreakAdState.Showing)
                return;
            string blocker = Blocker();
            if (blocker == null)
                BeginCycle(null);
            else
                Halt(blocker);
        }

        public void Stop()
        {
            if (!running)
                return;
            running = false;
            context.MainThread.Ticked -= Tick;
            context.MainThread.ApplicationPaused -= OnApplicationPaused;
            context.Log.Info("break ad stopped");
            if (State != HDCBreakAdState.Showing)
                Halt("not started");
        }

        public void Restart()
        {
            if (running && (State == HDCBreakAdState.Waiting || State == HDCBreakAdState.Countdown || State == HDCBreakAdState.Loading))
                BeginCycle("restarted");
        }

        string IAdChannel.Key => "BRK";

        string IAdChannel.Title => "BreakAd";

        void IAdChannel.OnSdkInitialized()
        {
        }

        void IAdChannel.InitializeAll()
        {
        }

        void IAdChannel.OnAdsRemoved()
        {
            if (State != HDCBreakAdState.Showing)
                Halt("ads removed");
        }

        internal void BreakNow()
        {
            if (running && State == HDCBreakAdState.Waiting)
                elapsed = Config.NoticeAt;
        }

        private void Tick()
        {
            float now = context.Clock.UnscaledTime;
            float delta = Mathf.Clamp(now - lastTime, 0f, MaxTickSeconds);
            lastTime = now;
            if (paused || State == HDCBreakAdState.Showing)
                return;

            string blocker = Blocker();
            if (blocker != null)
            {
                Halt(blocker);
                return;
            }

            if (State == HDCBreakAdState.Off)
                BeginCycle(null);
            if (otherFullscreens.Count > 0 || context.HasOverlayAd)
                return;

            switch (State)
            {
                case HDCBreakAdState.Waiting:
                    elapsed += delta;
                    if (elapsed >= Config.NoticeAt)
                        BeginCountdown();
                    break;
                case HDCBreakAdState.Countdown:
                    CountdownLeft = Mathf.Max(0f, CountdownLeft - delta);
                    if (CountdownLeft <= 0f)
                        EndCountdown();
                    break;
                case HDCBreakAdState.Loading:
                    WaitForLoad(delta);
                    break;
            }
        }

        private string Blocker()
        {
            if (!running)
                return "not started";
            if (!context.IsInitialized)
                return "waiting for the SDK";
            if (context.IsAdsRemoved)
                return "ads removed";
            if (!Config.IsOn)
                return "the ad core has no break_ads_config with an adUnitId and a showBreakAdTime above 0";
            if (!HasSource())
                return "no ad network serves break_ads_config.adUnitId";
            return null;
        }

        private bool HasSource()
        {
            if (source != null)
                return true;
            HDCAdPlan plan = context.Groups.BreakAdPlan();
            if (plan == null)
                return false;
            source = new HDCFullscreenSource(plan.Network.CreateFullscreen(plan), plan.Network, context.Log)
            {
                Failed = _ => loading = false,
            };
            return true;
        }

        private void BeginCountdown()
        {
            Load();
            CountdownSeconds = Config.NoticeSeconds;
            CountdownLeft = CountdownSeconds;
            Enter(HDCBreakAdState.Countdown);
            if (CountdownLeft <= 0f)
                EndCountdown();
        }

        private void EndCountdown()
        {
            if (source.IsReady)
            {
                ShowAd();
            }
            else if (loading && loadingTimeout > 0f)
            {
                waitedForLoad = 0f;
                Enter(HDCBreakAdState.Loading);
            }
            else
            {
                Skip(loading ? "the ad is still loading" : "no ad loaded");
            }
        }

        private void WaitForLoad(float delta)
        {
            if (source.IsReady)
            {
                ShowAd();
                return;
            }

            waitedForLoad += delta;
            if (!loading)
                Skip("the ad failed to load");
            else if (waitedForLoad >= loadingTimeout)
                Skip($"the ad did not load within {loadingTimeout:0.#} s");
        }

        private void Load()
        {
            if (source.IsReady || loading)
                return;
            loading = true;
            source.Load();
        }

        private void ShowAd()
        {
            loading = false;
            displayed = false;
            context.Placements.Record(source.Id, HDCAdChannel.ForceAd, Position, source.Network.RevenueNetwork);
            context.NotifyFullscreenOpening();
            Enter(HDCBreakAdState.Showing);
            if (!source.Show(OnDisplayed, OnClosed) && State == HDCBreakAdState.Showing)
                Skip("the ad did not start");
        }

        private void OnDisplayed()
        {
            displayed = true;
            context.SetOverlayAd(source.Id, true);
        }

        private void OnClosed(bool rewarded)
        {
            context.SetOverlayAd(source.Id, false);
            if (State != HDCBreakAdState.Showing)
                return;
            if (displayed)
                BeginCycle("shown and closed");
            else
                Skip("the ad failed to show");
        }

        private void Skip(string reason)
        {
            context.Log.Info("break ad skipped: " + reason);
            BeginCycle("skipped: " + reason);
        }

        private void BeginCycle(string result)
        {
            elapsed = 0f;
            lastTime = context.Clock.UnscaledTime;
            if (result != null)
                lastResult = result;
            if (running)
                Enter(HDCBreakAdState.Waiting);
            else
                Halt("not started");
        }

        private void Halt(string reason)
        {
            elapsed = 0f;
            if (State == HDCBreakAdState.Off && offReason == reason)
                return;
            offReason = reason;
            context.Log.Info("break ad off: " + reason);
            Enter(HDCBreakAdState.Off);
        }

        private void Enter(HDCBreakAdState state)
        {
            if (state != HDCBreakAdState.Countdown)
                CountdownLeft = 0f;
            if (State == state)
                return;
            State = state;
            Action<HDCBreakAdState> handlers = StateChanged;
            if (handlers == null)
                return;
            foreach (Action<HDCBreakAdState> handler in handlers.GetInvocationList())
                HDCCallbacks.Run(() => handler(state), context.Log);
        }

        private void OnApplicationPaused(bool isPaused)
        {
            paused = isPaused;
            if (!isPaused)
                lastTime = context.Clock.UnscaledTime;
        }

        private void OnAdEvent(HDCAdEvent adEvent)
        {
            if (!IsFullscreen(adEvent.format) || (source != null && adEvent.id == source.Id && adEvent.format == source.Format))
                return;

            string key = adEvent.format + "/" + adEvent.id;
            switch (adEvent.type)
            {
                case HDCAdEventType.Shown:
                    otherFullscreens.Add(key);
                    if (State == HDCBreakAdState.Waiting || State == HDCBreakAdState.Countdown || State == HDCBreakAdState.Loading)
                        BeginCycle("another full-screen ad showed");
                    break;
                case HDCAdEventType.Closed:
                case HDCAdEventType.ShowFailed:
                    otherFullscreens.Remove(key);
                    break;
            }
        }

        private static bool IsFullscreen(string format) =>
            format == HDCAdFormat.Interstitial || format == HDCAdFormat.Fullscreen || format == HDCAdFormat.Rewarded || format == HDCAdFormat.AppOpen;
    }
}
