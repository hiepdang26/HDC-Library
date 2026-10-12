using HDC.Ads.Ports;

namespace HDC.Ads.Tests
{
    internal sealed class HDCFakeClock : IClock
    {
        public float RealTime { get; private set; }

        public float UnscaledTime => RealTime;

        public float DeltaTime { get; private set; }

        public int Frame { get; private set; }

        internal void Advance(float seconds)
        {
            RealTime += seconds;
            DeltaTime = seconds;
            Frame++;
        }
    }
}
