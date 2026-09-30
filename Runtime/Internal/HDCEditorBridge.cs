#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace HDC.Ads.Internal
{
    /// <summary>
    /// Simulates the native SDK in the Editor so game flows run without a device. Interstitials load
    /// after half a second; a show sends Shown, Impression and Paid, then Closed a second later.
    /// </summary>
    internal sealed class HDCEditorBridge : IHDCNativeBridge
    {
        private const float LoadSeconds = 0.5f;
        private const float ShowSeconds = 1f;

        private readonly Dictionary<string, SimulatedInterstitial> interstitials =
            new Dictionary<string, SimulatedInterstitial>();

        private Action<string> handler;

        public void SetEventHandler(Action<string> onEvent) => handler = onEvent;

        public string Call(string method, string argsJson)
        {
            switch (method)
            {
                case "init":
                    Send(new HDCAdEvent { format = HDCAdFormat.Sdk, type = HDCAdEventType.Initialized });
                    return Result(true);
                case "interstitial.load":
                    return LoadInterstitial(JsonUtility.FromJson<LoadArgs>(argsJson));
                case "interstitial.show":
                    return Result(ShowInterstitial(IdOf(argsJson)));
                case "interstitial.isReady":
                    return Result(interstitials.TryGetValue(IdOf(argsJson), out SimulatedInterstitial ad) && ad.Ready);
                case "interstitial.destroy":
                    interstitials.Remove(IdOf(argsJson));
                    return Result(true);
                default:
                    return Failure("Unknown method: " + method);
            }
        }

        private string LoadInterstitial(LoadArgs args)
        {
            if (string.IsNullOrWhiteSpace(args.id))
                return Failure("id is required");
            if (args.adUnitIds == null || args.adUnitIds.Length == 0)
                return Failure("adUnitIds is empty");

            var ad = new SimulatedInterstitial { AdUnitId = args.adUnitIds[0], AutoReload = args.autoReload };
            interstitials[args.id] = ad;
            SimulateLoad(args.id, ad);
            return Result(true);
        }

        private void SimulateLoad(string id, SimulatedInterstitial ad)
        {
            HDCMainThread.PostDelayed(LoadSeconds, () =>
            {
                if (!interstitials.TryGetValue(id, out SimulatedInterstitial current) || current != ad)
                    return;
                ad.Ready = true;
                Send(Interstitial(id, ad, HDCAdEventType.Loaded));
            });
        }

        private bool ShowInterstitial(string id)
        {
            if (!interstitials.TryGetValue(id, out SimulatedInterstitial ad) || !ad.Ready)
            {
                HDCAdEvent failed = Interstitial(id, ad, HDCAdEventType.ShowFailed);
                failed.message = "Interstitial not ready. Load first";
                Send(failed);
                return false;
            }

            ad.Ready = false;
            Send(Interstitial(id, ad, HDCAdEventType.Shown));
            Send(Interstitial(id, ad, HDCAdEventType.Impression));
            HDCAdEvent paid = Interstitial(id, ad, HDCAdEventType.Paid);
            paid.currency = "USD";
            Send(paid);
            HDCMainThread.PostDelayed(ShowSeconds, () =>
            {
                Send(Interstitial(id, ad, HDCAdEventType.Closed));
                if (ad.AutoReload && interstitials.TryGetValue(id, out SimulatedInterstitial current) && current == ad)
                    SimulateLoad(id, ad);
            });
            return true;
        }

        private static HDCAdEvent Interstitial(string id, SimulatedInterstitial ad, string type) =>
            new HDCAdEvent
            {
                id = id,
                format = HDCAdFormat.Interstitial,
                type = type,
                adUnitId = ad?.AdUnitId ?? string.Empty,
                adSource = "Editor simulation",
            };

        private void Send(HDCAdEvent adEvent)
        {
            string json = JsonUtility.ToJson(adEvent);
            HDCMainThread.Post(() => handler?.Invoke(json));
        }

        private static string IdOf(string argsJson) => JsonUtility.FromJson<LoadArgs>(argsJson).id;

        private static string Result(bool value) => value ? "{\"ok\":true,\"value\":true}" : "{\"ok\":true,\"value\":false}";

        private static string Failure(string error) =>
            JsonUtility.ToJson(new FailureResult { ok = false, error = error });

        private sealed class SimulatedInterstitial
        {
            public string AdUnitId;
            public bool AutoReload;
            public bool Ready;
        }

#pragma warning disable 0649 // Assigned by JsonUtility.
        [Serializable]
        private sealed class LoadArgs
        {
            public string id;
            public string[] adUnitIds;
            public bool autoReload;
        }
#pragma warning restore 0649

        [Serializable]
        private sealed class FailureResult
        {
            public bool ok;
            public string error;
        }
    }
}
#endif
