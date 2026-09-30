using System.Collections.Generic;
using UnityEngine;

namespace HDC.Ads.Demo
{
    /// <summary>
    /// On-screen buttons to try HDC ads on a device or in the Editor. Add it to a GameObject in any scene.
    /// The default ad units are Google's test units.
    /// </summary>
    public sealed class HDCAdsDemo : MonoBehaviour
    {
        private const int MaxLogLines = 60;

        [SerializeField] private string interstitialId = "demo_interstitial";
        [SerializeField] private string[] androidInterstitialUnits = { "ca-app-pub-3940256099942544/1033173712" };
        [SerializeField] private string[] iosInterstitialUnits = { "ca-app-pub-3940256099942544/4411468910" };

        private readonly List<string> log = new List<string>();
        private Vector2 scroll;

        private string[] InterstitialUnits =>
            Application.platform == RuntimePlatform.IPhonePlayer ? iosInterstitialUnits : androidInterstitialUnits;

        private void Awake()
        {
            HDCAdsSdk.DebugLog = true;
            HDCAdsSdk.AdEvent += OnAdEvent;
        }

        private void OnDestroy()
        {
            HDCAdsSdk.AdEvent -= OnAdEvent;
        }

        private void OnAdEvent(HDCAdEvent adEvent)
        {
            Append(adEvent.ToString());
        }

        private void OnGUI()
        {
            float scale = Mathf.Max(1f, Screen.dpi / 160f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            var area = new Rect(10f, 10f, Screen.width / scale - 20f, Screen.height / scale - 20f);
            GUILayout.BeginArea(area);

            if (Button("Initialize"))
                HDCAdsSdk.Initialize(() => Append("Initialize callback"));
            if (Button("Load interstitial"))
                Append("load accepted=" + HDCAdsSdk.LoadInterstitial(interstitialId, InterstitialUnits));
            if (Button("Is interstitial ready?"))
                Append("ready=" + HDCAdsSdk.IsInterstitialReady(interstitialId));
            if (Button("Show interstitial"))
                Append("show started=" + HDCAdsSdk.ShowInterstitial(interstitialId));
            if (Button("Destroy interstitial"))
                HDCAdsSdk.DestroyInterstitial(interstitialId);

            scroll = GUILayout.BeginScrollView(scroll);
            for (int i = log.Count - 1; i >= 0; i--)
                GUILayout.Label(log[i]);
            GUILayout.EndScrollView();

            GUILayout.EndArea();
        }

        private static bool Button(string text) => GUILayout.Button(text, GUILayout.Height(40f));

        private void Append(string line)
        {
            log.Add($"{Time.realtimeSinceStartup:0.0}s {line}");
            if (log.Count > MaxLogLines)
                log.RemoveAt(0);
        }
    }
}
