using System;
using UnityEngine;
using UnityEngine.UI;

namespace HDC.Ads.DebugUI
{
    /// <summary>A modal for one long text, such as the break ad state, refreshed while it stays open.</summary>
    public sealed class HDCAdsDebugViewer : MonoBehaviour
    {
        private const float CopiedSeconds = 1.5f;

        [SerializeField] private GameObject modalRoot;
        [SerializeField] private Text titleText;
        [SerializeField] private Text bodyText;
        [SerializeField] private Button copyButton;
        [SerializeField] private Button closeButton;
        [Tooltip("Seconds between refreshes of the text.")]
        [SerializeField] private float refreshSeconds = 1f;

        private Func<string> body;
        private float nextRefresh;
        private float copiedUntil;

        public bool IsOpen => modalRoot.activeSelf;

        private void Awake()
        {
            copyButton.onClick.AddListener(Copy);
            closeButton.onClick.AddListener(Close);
            modalRoot.SetActive(false);
        }

        /// <summary>Shows <paramref name="bodyBuilder"/>'s text under <paramref name="title"/>, refreshed every second.</summary>
        public void Open(string title, Func<string> bodyBuilder)
        {
            titleText.text = string.IsNullOrEmpty(title) ? "Viewer" : title;
            body = bodyBuilder;
            modalRoot.SetActive(true);
            Render();
        }

        public void Close()
        {
            body = null;
            modalRoot.SetActive(false);
        }

        private void Update()
        {
            if (!IsOpen)
                return;
            if (copiedUntil > 0f && Time.unscaledTime >= copiedUntil)
            {
                copiedUntil = 0f;
                SetLabel(copyButton, "Copy");
            }

            if (Time.unscaledTime >= nextRefresh)
                Render();
        }

        private void Render()
        {
            nextRefresh = Time.unscaledTime + Mathf.Max(0.2f, refreshSeconds);
            string text;
            try
            {
                text = body?.Invoke();
            }
            catch (Exception exception)
            {
                text = "[debug read failed] " + exception.Message;
            }

            bodyText.text = string.IsNullOrEmpty(text) ? "(empty)" : text;
        }

        private void Copy()
        {
            GUIUtility.systemCopyBuffer = bodyText.text;
            copiedUntil = Time.unscaledTime + CopiedSeconds;
            SetLabel(copyButton, "Copied");
        }

        private static void SetLabel(Button button, string label) => button.GetComponentInChildren<Text>(true).text = label;
    }
}
