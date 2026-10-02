#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text;
using HDC.Ads.Domain;
using UnityEngine;

namespace HDC.Ads.Infrastructure
{
    internal sealed class HDCEditorBridge : IHDCNativeBridge
    {
        private const float LoadSeconds = 0.5f;
        private const float ShowSeconds = 1f;
        private const float PopupShowSeconds = 3f;
        private const string SimulatedSource = "Editor simulation";
        private const string FailingUnitMarker = "fail";

        private const int NoFillCode = 3;

        private readonly Dictionary<string, SimulatedAd> ads = new Dictionary<string, SimulatedAd>();
        private Action<string> handler;
        private bool metaTestMode;

        public void SetEventHandler(Action<string> onEvent) => handler = onEvent;

        public string Call(string method, string argsJson)
        {
            int dot = method.IndexOf('.');
            string format = dot > 0 ? method.Substring(0, dot) : method;
            string action = dot > 0 ? method.Substring(dot + 1) : string.Empty;
            switch (format)
            {
                case "init":
                    Send(new HDCAdEvent { format = HDCAdFormat.Sdk, type = HDCAdEventType.Initialized });
                    return Result(true);
                case "meta":
                    return Meta(action);
                case HDCAdFormat.Interstitial:
                case HDCAdFormat.Fullscreen:
                case HDCAdFormat.Popup:
                case HDCAdFormat.Banner:
                    return Ad(format, action, JsonUtility.FromJson<Args>(string.IsNullOrEmpty(argsJson) ? "{}" : argsJson));
                default:
                    return Failure("Unknown method: " + method);
            }
        }

        private string Ad(string format, string action, Args args)
        {
            if (string.IsNullOrWhiteSpace(args.id))
                return Failure("id is required");

            string key = format + "/" + args.id;
            ads.TryGetValue(key, out SimulatedAd ad);
            switch (action)
            {
                case "load":
                    if (args.adUnitIds == null || args.adUnitIds.Length == 0)
                        return Failure("adUnitIds is empty");
                    ad = new SimulatedAd(format, args.id, args.adUnitIds[0], ReloadsAfterShow(format, args));
                    ads[key] = ad;
                    SimulateLoad(key, ad);
                    return Result(true);
                case "show":
                    return Result(Show(key, format, args.id, ad));
                case "isReady":
                case "isDisplayable":
                    return Result(ad != null && (ad.Ready || ad.Hidden));
                case "state":
                    return StringResult(State(ad));
                case "expand":
                    return Result(ad != null && ad.Showing);
                case "hide":
                    if (format == HDCAdFormat.Popup)
                        Hide(ad);
                    else
                        Close(key, ad);
                    return Result(true);
                case "close":
                case "stop":
                    Close(key, ad);
                    return Result(true);
                case "destroy":
                    Close(key, ad);
                    ads.Remove(key);
                    return Result(true);
                default:
                    return Result(true);
            }
        }

        private static bool ReloadsAfterShow(string format, Args args)
        {
            switch (format)
            {
                case HDCAdFormat.Interstitial:
                    return args.autoReload;
                case HDCAdFormat.Fullscreen:
                    return args.reloadAfterShow;
                case HDCAdFormat.Popup:
                    return false;
                default:
                    return true;
            }
        }

        private static string State(SimulatedAd ad)
        {
            if (ad == null)
                return "NotLoaded";
            if (ad.Showing)
                return "Showing";
            if (ad.Hidden)
                return "Hidden";
            if (ad.Ready)
                return "Displayable";
            return ad.Closed ? "Closed" : "Loading";
        }

        private void SimulateLoad(string key, SimulatedAd ad)
        {
            HDCMainThread.PostDelayed(LoadSeconds, () =>
            {
                if (!IsCurrent(key, ad))
                    return;
                if (ad.AdUnitId.IndexOf(FailingUnitMarker, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    HDCAdEvent failed = Event(ad, HDCAdEventType.LoadFailed);
                    failed.code = NoFillCode;
                    failed.message = "No fill (Editor simulation: the ad unit id contains \"fail\")";
                    Send(failed);
                    return;
                }

                ad.Ready = true;
                ad.Closed = false;
                Send(Event(ad, HDCAdEventType.Loaded));
                if (ad.ShowWhenLoaded)
                    Show(key, ad.Format, ad.Id, ad);
            });
        }

        private bool Show(string key, string format, string id, SimulatedAd ad)
        {
            if (ad != null && ad.Format == HDCAdFormat.Banner && !ad.Ready && !ad.Showing)
            {
                ad.ShowWhenLoaded = true;
                return true;
            }

            if (ad != null)
                ad.ShowWhenLoaded = false;
            if (ad != null && ad.Hidden)
            {
                ad.Hidden = false;
                ad.Showing = true;
                Send(Event(ad, HDCAdEventType.Shown));
                ClosePopupLater(key, ad);
                return true;
            }

            if (ad == null || !ad.Ready || ad.Showing)
            {
                Send(new HDCAdEvent
                {
                    id = id,
                    format = format,
                    type = HDCAdEventType.ShowFailed,
                    adUnitId = ad?.AdUnitId ?? string.Empty,
                    message = "Not ready. Load first",
                });
                return false;
            }

            ad.Ready = false;
            ad.Showing = true;
            Send(Event(ad, HDCAdEventType.Shown));
            Send(Event(ad, HDCAdEventType.Impression));
            HDCAdEvent paid = Event(ad, HDCAdEventType.Paid);
            paid.currency = "USD";
            Send(paid);

            if (ad.Format == HDCAdFormat.Popup)
            {
                ClosePopupLater(key, ad);
            }
            else if (ad.Format != HDCAdFormat.Banner)
            {
                HDCMainThread.PostDelayed(ShowSeconds, () =>
                {
                    if (IsCurrent(key, ad))
                        Close(key, ad);
                });
            }

            return true;
        }

        private void ClosePopupLater(string key, SimulatedAd ad)
        {
            int turn = ++ad.ShowTurn;
            HDCMainThread.PostDelayed(PopupShowSeconds, () =>
            {
                if (IsCurrent(key, ad) && ad.ShowTurn == turn && ad.Showing)
                    Close(key, ad);
            });
        }

        private void Hide(SimulatedAd ad)
        {
            if (ad == null)
                return;

            ad.ShowWhenLoaded = false;
            if (!ad.Showing)
                return;

            ad.Showing = false;
            ad.Hidden = true;
            Send(Event(ad, HDCAdEventType.Closed));
        }

        private void Close(string key, SimulatedAd ad)
        {
            if (ad == null)
                return;

            ad.ShowWhenLoaded = false;
            bool wasShowing = ad.Showing;
            if (!wasShowing && !ad.Hidden)
                return;

            ad.Showing = false;
            ad.Hidden = false;
            if (wasShowing)
                Send(Event(ad, HDCAdEventType.Closed));
            if (ad.Format == HDCAdFormat.Banner)
                ad.Ready = true;
            else if (ad.ReloadsAfterShow)
                SimulateLoad(key, ad);
            else
                ad.Closed = true;
        }

        private string Meta(string action)
        {
            switch (action)
            {
                case "enableTestMode":
                    metaTestMode = true;
                    return Result(true);
                case "disableTestMode":
                    metaTestMode = false;
                    return Result(true);
                case "isTestMode":
                    return Result(metaTestMode);
                case "deviceHash":
                    return StringResult("EDITOR");
                default:
                    return Failure("Unknown method: meta." + action);
            }
        }

        private bool IsCurrent(string key, SimulatedAd ad) =>
            ads.TryGetValue(key, out SimulatedAd current) && current == ad;

        private static HDCAdEvent Event(SimulatedAd ad, string type) =>
            new HDCAdEvent
            {
                id = ad.Id,
                format = ad.Format,
                type = type,
                adUnitId = ad.AdUnitId,
                adSource = SimulatedSource,
            };

        private void Send(HDCAdEvent adEvent)
        {
            string json = JsonUtility.ToJson(adEvent);
            HDCMainThread.Post(() => handler?.Invoke(json));
        }

        private static string Result(bool value) => value ? "{\"ok\":true,\"value\":true}" : "{\"ok\":true,\"value\":false}";

        private static string StringResult(string value)
        {
            var json = new StringBuilder("{\"ok\":true,\"value\":");
            HDCJson.AppendString(json, value);
            return json.Append('}').ToString();
        }

        private static string Failure(string error)
        {
            var json = new StringBuilder("{\"ok\":false,\"error\":");
            HDCJson.AppendString(json, error);
            return json.Append('}').ToString();
        }

        private sealed class SimulatedAd
        {
            public readonly string Format;
            public readonly string Id;
            public readonly string AdUnitId;
            public readonly bool ReloadsAfterShow;
            public bool Ready;
            public bool Showing;
            public bool Hidden;
            public bool Closed;
            public bool ShowWhenLoaded;
            public int ShowTurn;

            public SimulatedAd(string format, string id, string adUnitId, bool reloadsAfterShow)
            {
                Format = format;
                Id = id;
                AdUnitId = adUnitId;
                ReloadsAfterShow = reloadsAfterShow;
            }
        }

#pragma warning disable 0649
        [Serializable]
        private sealed class Args
        {
            public string id;
            public string[] adUnitIds;
            public bool autoReload;
            public bool reloadAfterShow;
        }
#pragma warning restore 0649
    }
}
#endif
