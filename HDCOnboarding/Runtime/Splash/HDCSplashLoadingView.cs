using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace HDC.Onboarding
{
    [AddComponentMenu("HDC/Onboarding/HDC Splash Loading View")]
    public sealed class HDCSplashLoadingView : MonoBehaviour
    {
        [SerializeField] private TMP_Text loadingText;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private Image fill;

        [Tooltip("Seconds between two loading texts.")]
        [SerializeField] private float textSeconds = 2f;

        [SerializeField] private string[] loadingTexts =
        {
            "Starting up...", "Loading the assets...", "Preparing the levels...", "Warming up...", "Almost ready...",
        };

        private float elapsed;
        private int nextText;

        public void UpdateProgress(float progress)
        {
            if (fill != null)
                fill.fillAmount = progress;
            if (progressText != null)
                progressText.text = $"{(int)(progress * 100)}%";
        }

        private void Awake()
        {
            UpdateProgress(0f);
            if (loadingText == null || loadingTexts == null || loadingTexts.Length == 0)
                return;
            loadingText.text = loadingTexts[0];
            nextText = 1 % loadingTexts.Length;
        }

        private void Update()
        {
            if (!SplashScreen.isFinished || loadingText == null || loadingTexts == null || loadingTexts.Length == 0)
                return;
            elapsed += Time.deltaTime;
            if (elapsed < textSeconds)
                return;
            elapsed = 0f;
            loadingText.text = loadingTexts[nextText];
            nextText = (nextText + 1) % loadingTexts.Length;
        }
    }
}
