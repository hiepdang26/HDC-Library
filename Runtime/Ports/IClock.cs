namespace HDC.Ads.Ports
{
    internal interface IClock
    {
        float RealTime { get; }

        float UnscaledTime { get; }

        float DeltaTime { get; }

        int Frame { get; }
    }
}
