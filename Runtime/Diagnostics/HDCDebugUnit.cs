using UnityEngine;

namespace HDC.Ads.Diagnostics
{
    /// <summary>One ad unit of a group, for the debug panel: what the configs say, and what it did so far.</summary>
    internal sealed class HDCDebugUnit
    {
        internal int Index;

        /// <summary>What serves it and in which format, such as "AdMob Interstitial".</summary>
        internal string Name;

        internal string Format;
        internal string Id;
        internal string AdUnitId;

        /// <summary>The channel made the unit's group.</summary>
        internal bool Created;

        /// <summary>The group started the unit: backups start only after the units before them failed.</summary>
        internal bool Started;

        internal bool Ready;
        internal bool OnScreen;
        internal HDCAdRecord Record;

        /// <summary>A state only the native side knows, such as a popup's.</summary>
        internal string NativeState;

        internal string Status(out HDCUnitTone tone)
        {
            HDCAdState state = Record?.State ?? HDCAdState.Idle;
            if (!Created)
            {
                tone = HDCUnitTone.Idle;
                return "NOT INITIALIZED";
            }

            if (!Started && state == HDCAdState.Idle)
            {
                tone = HDCUnitTone.Idle;
                return "BACKUP · NOT STARTED";
            }

            if (OnScreen || state == HDCAdState.Showing)
            {
                tone = HDCUnitTone.Live;
                return "SHOWING";
            }

            if (Ready)
            {
                tone = HDCUnitTone.Good;
                return "READY";
            }

            switch (state)
            {
                case HDCAdState.Loading:
                    tone = HDCUnitTone.Busy;
                    return "LOADING";
                case HDCAdState.Loaded:
                    tone = HDCUnitTone.Good;
                    return "LOADED";
                case HDCAdState.LoadFailed:
                    tone = HDCUnitTone.Bad;
                    float retryIn = Record.RetryAt - Time.realtimeSinceStartup;
                    return retryIn > 0f ? $"LOAD FAILED · RETRY IN {Mathf.CeilToInt(retryIn)}s" : "LOAD FAILED";
                case HDCAdState.ShowFailed:
                    tone = HDCUnitTone.Bad;
                    return "SHOW FAILED";
                case HDCAdState.Closed:
                    tone = HDCUnitTone.Busy;
                    return "CLOSED";
                case HDCAdState.Destroyed:
                    tone = HDCUnitTone.Idle;
                    return "DESTROYED";
                default:
                    tone = HDCUnitTone.Idle;
                    return "IDLE";
            }
        }
    }
}
