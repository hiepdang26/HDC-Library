using System;

namespace HDC.Onboarding
{
    [Serializable]
    public sealed class HDCSplashConfig
    {
        public float loadingTime;
        public string nativeSplashGroup;
        public float nativeSplashShowTime;
        public float nativeSplashLoadTimeOut;
        public bool enableAdmobLayout;
        public HDCSplashLaunchChannel launchChannel = new HDCSplashLaunchChannel();
        public string interSplashBackupAdUnitId;
        public bool waitNativeSplashLoad;
        public int interSplashPreload;
        public int interSplashPreloadWaitCount;
        public float interLoadAdTimeout;
        public float delayShowInterstitials;
        public string nativeAfterInterId;
        public string nativeAfterLayoutGroup;
        public string firstOpenNextScene;
        public string nextScene;
    }
}
