using System;
using HDC.Ads.Domain;
using HDC.Ads.Ports;
using UnityEngine;

namespace HDC.Ads.Logic
{
    internal sealed class HDCRectSource
    {
        private readonly IViewAd view;

        internal HDCRectSource(IViewAd view, IAdNetwork network)
        {
            this.view = view;
            Network = network;
            view.Event += OnAdEvent;
        }

        internal IAdNetwork Network { get; }

        internal string Id => view.Id;
        internal string Format => view.Format;
        internal string AdUnitId => view.AdUnitId;
        internal bool IsLoaded { get; private set; }
        internal Action<HDCRectSource> Failed { get; set; }
        internal Action<HDCRectSource> LoadedAd { get; set; }

        internal Vector2 SizeInPixels => view.SizeInPixels;

        internal int RefreshSeconds => view.RefreshSeconds;

        internal int FailStreak { get; set; }

        internal float ShownSeconds { get; set; }

        internal float LastSignalAt { get; set; }

        internal float RetryAt { get; set; } = -1f;

        internal int RetryCount { get; set; }

        internal bool Reloading { get; private set; }

        internal void Load() => view.Load();

        internal void Reload()
        {
            Reloading = true;
            view.Load();
        }

        internal void Show() => view.Show();

        internal void Hide() => view.Hide();

        internal bool Expand(bool enableClick) => view.Expand(enableClick);

        internal void Move(HDCAdPosition position) => view.Move(position);

        internal void Move(Vector2 screenPoint) => view.Move(screenPoint);

        private void OnAdEvent(HDCAdEvent adEvent)
        {
            if (adEvent.type == HDCAdEventType.Loaded)
            {
                IsLoaded = true;
                Reloading = false;
                FailStreak = 0;
                ShownSeconds = 0f;
                RetryAt = -1f;
                RetryCount = 0;
                LoadedAd?.Invoke(this);
            }
            else if (adEvent.type == HDCAdEventType.LoadFailed)
            {
                Reloading = false;
                Failed?.Invoke(this);
            }
        }
    }
}
