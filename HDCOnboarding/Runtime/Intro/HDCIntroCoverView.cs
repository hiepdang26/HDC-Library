using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HDC.Onboarding
{
    [AddComponentMenu("HDC/Onboarding/HDC Intro Cover View")]
    public sealed class HDCIntroCoverView : HDCIntroView
    {
        [Tooltip("AL pages put the text on the left and the ad on the right, AR pages the other way round.")]
        [SerializeField] private HDCIntroType introType = HDCIntroType.AL;

        [SerializeField] private Image background;
        [SerializeField] private Image cover;
        [SerializeField] private TMP_Text description;
        [SerializeField] private TMP_Text countdown;
        [SerializeField] private Button continueButton;

        private bool complete;
        private Coroutine counting;

        public override bool IsComplete => complete;

        public override bool CanRender(HDCIntroPage page) => page != null && page.IntroType == introType;

        public override void Render(HDCIntroPage page)
        {
            complete = false;
            HDCIntroImages.Apply(background, page.IntroBackgroundImage);
            HDCIntroImages.Apply(cover, page.IntroCoverImage);
            if (description != null && !string.IsNullOrEmpty(page.IntroDescription))
                description.text = page.IntroDescription;
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
            int remainingSeconds = Mathf.Max(0, (int)interval);
            ShowCountdown(remainingSeconds, true);
            if (continueButton != null)
                continueButton.gameObject.SetActive(false);
            while (remainingSeconds > 0)
            {
                yield return new WaitForSeconds(1f);
                remainingSeconds--;
                ShowCountdown(remainingSeconds, true);
            }

            ShowCountdown(0, false);
            if (continueButton != null)
                continueButton.gameObject.SetActive(true);
            counting = null;
        }

        private void ShowCountdown(int seconds, bool visible)
        {
            if (countdown == null)
                return;
            countdown.text = $"Next in {seconds}s.";
            countdown.gameObject.SetActive(visible);
        }
    }
}
