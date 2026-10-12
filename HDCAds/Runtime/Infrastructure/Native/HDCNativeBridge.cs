namespace HDC.Ads.Infrastructure
{
    internal static class HDCNativeBridge
    {
        internal static IHDCNativeBridge Create()
        {
#if UNITY_EDITOR
            return new HDCEditorBridge();
#elif UNITY_IOS
            return new HDCIosBridge();
#elif UNITY_ANDROID
            return new HDCAndroidBridge();
#else
            return new HDCUnsupportedBridge();
#endif
        }
    }
}
