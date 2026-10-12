using HDC.Ads.Domain;

namespace HDC.Ads.Ports
{
    internal interface IDeviceRegionSource
    {
        bool IsEditor { get; }

        string DeviceId { get; }

        HDCDeviceRegion Read();
    }
}
