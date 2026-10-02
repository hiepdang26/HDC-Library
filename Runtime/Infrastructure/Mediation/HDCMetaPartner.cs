using HDC.Ads.Ports;

namespace HDC.Ads.Infrastructure
{
    internal sealed class HDCMetaPartner : IMediationPartner
    {
        public string Name => "Meta Audience Network";

#if UNITY_IOS
        public string AdapterClass => "GADMediationAdapterFacebook";
#else
        public string AdapterClass => "com.google.ads.mediation.facebook.FacebookMediationAdapter";
#endif

        public bool HasTestMode => true;

        public bool IsTestMode => HDCAdsSdk.IsMetaTestMode();

        public string TestDeviceId => HDCAdsSdk.GetMetaTestDeviceHash();

        internal int TestAdType { get; set; }

        public bool EnableTestMode(string[] extraDeviceIds) => HDCAdsSdk.EnableMetaTestMode(extraDeviceIds, TestAdType);

        public void DisableTestMode() => HDCAdsSdk.DisableMetaTestMode();
    }
}
