using UnityEngine;
using UnityEngine.UI;

namespace HDC.Ads
{
    [DisallowMultipleComponent]
    [AddComponentMenu("HDC/Ads/HDC Break Ad")]
    public sealed class HDCBreakAdView : MonoBehaviour
    {
#pragma warning disable CS0169, CS0414
        [Tooltip("Seconds to wait for the ad after the countdown while the loading text shows. The break is skipped after that.")]
        [SerializeField] private float loadingTimeout = 10f;

        [Tooltip("Spinner speed while the ad is loading, in degrees per second.")]
        [SerializeField] private float spinSpeed = 360f;

        [SerializeField] private Image ring;
        [SerializeField] private Text seconds;
        [SerializeField] private RectTransform spinner;
#pragma warning restore CS0169, CS0414

        [Tooltip("Text shown with the countdown.")]
        [SerializeField] private string breakText = "BREAK AD...";

        [Tooltip("Text shown while the ad is still loading after the countdown.")]
        [SerializeField] private string loadingText = "LOADING AD...";

        [SerializeField] private GameObject notice;
        [SerializeField] private Text message;
        [SerializeField] private GameObject countdown;
        [SerializeField] private GameObject loading;

        private void Awake()
        {
            Font font = BuiltinFont();
            foreach (Text text in GetComponentsInChildren<Text>(true))
            {
                if (text.font == null)
                    text.font = font;
            }

            Show(false, false);
        }

#if HDC_ADS
        private static int activeViews;

        private void OnEnable()
        {
            HDCAds.BreakAd.LoadingTimeout = loadingTimeout;
            HDCAds.BreakAd.StateChanged += OnStateChanged;
            if (activeViews++ == 0)
                HDCAds.BreakAd.Start();
            OnStateChanged(HDCAds.BreakAd.State);
        }

        private void OnDisable()
        {
            HDCAds.BreakAd.StateChanged -= OnStateChanged;
            if (--activeViews == 0)
                HDCAds.BreakAd.Stop();
            Show(false, false);
        }

        private void Update()
        {
            IBreakAds breakAd = HDCAds.BreakAd;
            if (breakAd.State == HDCBreakAdState.Countdown)
                ShowCountdown(breakAd);
            else if (breakAd.State == HDCBreakAdState.Loading && spinner != null)
                spinner.Rotate(0f, 0f, -spinSpeed * Time.unscaledDeltaTime);
        }

        private void OnStateChanged(HDCBreakAdState state)
        {
            Show(state == HDCBreakAdState.Countdown, state == HDCBreakAdState.Loading);
            if (state == HDCBreakAdState.Countdown)
                ShowCountdown(HDCAds.BreakAd);
        }

        private void ShowCountdown(IBreakAds breakAd)
        {
            float total = breakAd.CountdownSeconds;
            float left = breakAd.CountdownLeft;
            if (ring != null)
                ring.fillAmount = total > 0f ? left / total : 0f;
            if (seconds != null)
                seconds.text = Mathf.CeilToInt(left).ToString();
        }
#endif

        private void Show(bool countdownOn, bool loadingOn)
        {
            if (notice != null)
                notice.SetActive(countdownOn || loadingOn);
            if (countdown != null)
                countdown.SetActive(countdownOn);
            if (loading != null)
                loading.SetActive(loadingOn);
            if (message != null)
                message.text = loadingOn ? loadingText : breakText;
        }

        private static Font BuiltinFont() =>
            Resources.GetBuiltinResource<Font>(
#if UNITY_2022_2_OR_NEWER
                "LegacyRuntime.ttf"
#else
                "Arial.ttf"
#endif
            );

#if UNITY_EDITOR && HDC_ADS
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForPlayMode() => activeViews = 0;
#endif
    }
}
