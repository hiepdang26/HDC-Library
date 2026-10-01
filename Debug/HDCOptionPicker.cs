using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HDC.Ads.DebugUI
{
    /// <summary>A modal list for picking one option, such as a group or a position from the ads configs.</summary>
    public sealed class HDCOptionPicker : MonoBehaviour
    {
        private static readonly Color OptionColor = new Color(0.2f, 0.255f, 0.333f, 1f);
        private static readonly Color SelectedOptionColor = new Color(0.31f, 0.275f, 0.898f, 1f);

        [SerializeField] private GameObject modalRoot;
        [SerializeField] private Text titleText;
        [SerializeField] private Text subtitleText;
        [SerializeField] private RectTransform optionsRoot;
        [SerializeField] private Button optionTemplate;
        [SerializeField] private Button closeButton;

        private readonly List<Button> options = new List<Button>();

        private void Awake()
        {
            optionTemplate.gameObject.SetActive(false);
            closeButton.onClick.AddListener(Close);
            Close();
        }

        /// <summary>Shows <paramref name="values"/>; picking one closes the list and hands it to <paramref name="onPicked"/>.</summary>
        public void Open(string title, string subtitle, IEnumerable<string> values, string selected, Action<string> onPicked)
        {
            titleText.text = title;
            subtitleText.text = subtitle;
            ClearOptions();
            foreach (string value in values)
            {
                Button option = Instantiate(optionTemplate, optionsRoot);
                option.gameObject.SetActive(true);
                option.GetComponentInChildren<Text>(true).text = value;
                option.image.color = value == selected ? SelectedOptionColor : OptionColor;
                string picked = value;
                option.onClick.AddListener(() =>
                {
                    Close();
                    onPicked(picked);
                });
                options.Add(option);
            }

            modalRoot.SetActive(true);
        }

        public void Close()
        {
            modalRoot.SetActive(false);
            ClearOptions();
        }

        private void ClearOptions()
        {
            foreach (Button option in options)
                Destroy(option.gameObject);
            options.Clear();
        }
    }
}
