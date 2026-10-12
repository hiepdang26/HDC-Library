using System;

namespace HDC.Ads.Logic
{
    internal sealed partial class HDCAppLaunchAds : IAppLaunchAds, IAdChannel
    {
        internal const string LaunchAdSource = "onboarding_config.splash_config.launchChannel";

        private readonly HDCAdsContext context;
        private string completion = "";

        internal HDCAppLaunchAds(HDCAdsContext context)
        {
            this.context = context;
        }

        public event Action BeforeShow;

        public event Action Completed;

        public bool IgnoreAds { get; set; }

        public bool IsCompleted { get; private set; }

        public bool IsBeforeShowRaised { get; private set; }

        public bool CanShow => false;

        public void Initialize()
        {
            if (context.IsInitialized)
                Complete("no launch ad in the core; the splash shows it from " + LaunchAdSource);
        }

        string IAdChannel.Key => "AL";

        string IAdChannel.Title => "AppLaunch";

        void IAdChannel.OnSdkInitialized() => Complete("no launch ad in the core; the splash shows it from " + LaunchAdSource);

        void IAdChannel.InitializeAll() => Initialize();

        void IAdChannel.OnAdsRemoved()
        {
        }

        internal void CompleteWithoutShowing(string reason) => Complete(reason);

        private void Complete(string reason)
        {
            if (IsCompleted)
                return;

            context.Log.Info("app launch complete: " + reason);
            completion = reason;
            if (!IsBeforeShowRaised)
            {
                IsBeforeShowRaised = true;
                HDCCallbacks.Run(BeforeShow, context.Log);
            }

            IsCompleted = true;
            HDCCallbacks.Run(Completed, context.Log);
        }
    }
}
