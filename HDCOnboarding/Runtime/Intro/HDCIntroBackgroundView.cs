using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HDC.Onboarding
{
    [AddComponentMenu("HDC/Onboarding/HDC Intro Background View")]
    public sealed class HDCIntroBackgroundView : HDCIntroView
    {
        [SerializeField] private Image background;
        [SerializeField] private GameObject countdownRoot;
        [SerializeField] private TMP_Text countdown;
        [SerializeField] private Button continueButton;

        private bool complete;
        private Coroutine counting;

        public override bool IsComplete => complete;

        public override bool CanRender(HDCIntroPage page) => page != null && page.IntroType == HDCIntroType.PT;

        public override void Render(HDCIntroPage page)
        {
            complete = false;
            HDCIntroImages.Apply(background, page.IntroBackgroundImage);
            if (counting != null)
                StopCoroutine(counting);
            counting = StartCoroutine(CountDown(page.IntroInterval));
        }

        private void Awake()
        {
            if (continueButton != null)
                continueButton.onClick.AddListener(() => complete = true);
        }

        private IEnumerator CountDown(float interval)
        {
            if (countdownRoot != null)
                countdownRoot.SetActive(true);
            if (continueButton != null)
                continueButton.gameObject.SetActive(false);
            int remainingSeconds = Mathf.Max(0, (int)interval);
            ShowCountdown(remainingSeconds);
            while (remainingSeconds > 0)
            {
                yield return new WaitForSeconds(1f);
                remainingSeconds--;
                ShowCountdown(remainingSeconds);
            }

            if (countdownRoot != null)
                countdownRoot.SetActive(false);
            if (continueButton != null)
                continueButton.gameObject.SetActive(true);
            counting = null;
        }

        private void ShowCountdown(int seconds)
        {
            if (countdown != null)
                countdown.text = seconds.ToString();
        }
    }
}
