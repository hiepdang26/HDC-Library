using UnityEngine;

namespace HDC.Onboarding
{
    [DisallowMultipleComponent]
    [AddComponentMenu("HDC/Onboarding/HDC No Internet")]
    public sealed class HDCNoInternet : MonoBehaviour
    {
        [Tooltip("Shown while the device is offline. The game is paused (Time.timeScale = 0) until the device is back online.")]
        [SerializeField] private GameObject popup;

        [Tooltip("Shown in the popup until the player taps Try Reconnect.")]
        [SerializeField] private GameObject tryReconnectContent;

        [Tooltip("Shown in the popup after the player taps Try Reconnect.")]
        [SerializeField] private GameObject reconnectContent;

        public bool IsInternetAvailable { get; private set; }

        public bool IsPopupShown => popup != null && popup.activeInHierarchy;

        public void ToggleTryReconnect() => ShowContent(true);

        public void ToggleReconnect() => ShowContent(false);

        private void Awake()
        {
            if (transform.parent != null)
                transform.SetParent(null, false);
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            IsInternetAvailable = Application.internetReachability != NetworkReachability.NotReachable;
            if (popup == null)
                return;
            if (!IsInternetAvailable)
            {
                if (!popup.activeInHierarchy)
                {
                    HDCOnboardingInput.EnsureEventSystem();
                    Time.timeScale = 0f;
                    ShowContent(true);
                    popup.SetActive(true);
                }
            }
            else if (popup.activeInHierarchy)
            {
                Time.timeScale = 1f;
                popup.SetActive(false);
            }
        }

        private void ShowContent(bool tryReconnect)
        {
            if (tryReconnectContent != null)
                tryReconnectContent.SetActive(tryReconnect);
            if (reconnectContent != null)
                reconnectContent.SetActive(!tryReconnect);
        }
    }
}
