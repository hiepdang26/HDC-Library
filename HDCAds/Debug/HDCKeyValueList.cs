using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HDC.Ads.DebugUI
{
    public sealed class HDCKeyValueList : MonoBehaviour
    {
        [Tooltip("A row with a \"Label\" and a \"Value\" text.")]
        [SerializeField] private GameObject rowTemplate;
        [SerializeField] private GameObject headerTemplate;
        [SerializeField] private GameObject noteTemplate;

        private readonly List<GameObject> rows = new List<GameObject>();
        private readonly List<GameObject> headers = new List<GameObject>();
        private readonly List<GameObject> notes = new List<GameObject>();
        private int usedRows;
        private int usedHeaders;
        private int usedNotes;
        private int cursor;

        public void Begin()
        {
            HideTemplate(rowTemplate);
            HideTemplate(headerTemplate);
            HideTemplate(noteTemplate);
            usedRows = usedHeaders = usedNotes = 0;
            cursor = Mathf.Max(rowTemplate.transform.GetSiblingIndex(),
                Mathf.Max(headerTemplate.transform.GetSiblingIndex(), noteTemplate.transform.GetSiblingIndex())) + 1;
        }

        public void Header(string title) =>
            InOrder(HDCDebugStyle.Next(headers, headerTemplate, ref usedHeaders)).GetComponent<Text>().text = (title ?? string.Empty).ToUpperInvariant();

        public void Row(string label, string value, Color valueColor)
        {
            Transform row = InOrder(HDCDebugStyle.Next(rows, rowTemplate, ref usedRows)).transform;
            row.Find("Label").GetComponent<Text>().text = label;
            Text valueText = row.Find("Value").GetComponent<Text>();
            valueText.text = string.IsNullOrEmpty(value) ? "-" : value;
            valueText.color = valueColor;
        }

        public void Row(string label, string value) => Row(label, value, HDCDebugStyle.TextColor);

        public void Note(string text) => InOrder(HDCDebugStyle.Next(notes, noteTemplate, ref usedNotes)).GetComponent<Text>().text = text;

        public void End()
        {
            HDCDebugStyle.HideRest(rows, usedRows);
            HDCDebugStyle.HideRest(headers, usedHeaders);
            HDCDebugStyle.HideRest(notes, usedNotes);
        }

        private GameObject InOrder(GameObject item)
        {
            HDCDebugStyle.Place(item.transform, cursor++);
            return item;
        }

        private static void HideTemplate(GameObject template)
        {
            if (template.activeSelf)
                template.SetActive(false);
        }
    }
}
