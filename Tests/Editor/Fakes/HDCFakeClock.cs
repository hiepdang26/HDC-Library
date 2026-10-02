using HDC.Ads.Ports;

namespace HDC.Ads.Tests
{
    /// <summary>A clock the test moves on by hand; real and game time move together.</summary>
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
