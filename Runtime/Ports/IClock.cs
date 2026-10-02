namespace HDC.Ads.Ports
{
    /// <summary>The time the channels read: capping, the launch clock and the break ad timer.</summary>
    internal interface IClock
    {
        /// <summary>Seconds since startup, counting while the game is paused.</summary>
        float RealTime { get; }

        /// <summary>Game seconds since startup, without the time scale.</summary>
        float UnscaledTime { get; }

        /// <summary>Seconds the last frame took, with the time scale.</summary>
        float DeltaTime { get; }

        /// <summary>The number of the current frame.</summary>
        int Frame { get; }
    }
}
