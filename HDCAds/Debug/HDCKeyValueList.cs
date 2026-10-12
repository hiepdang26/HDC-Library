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

        public void Begin()
        {
            HideTemplate(rowTemplate);
            HideTemplate(headerTemplate);
            HideTemplate(noteTemplate);
            usedRows = usedHeaders = usedNotes = 0;
        }

        public void Header(string title) =>
            HDCDebugStyle.Take(headers, headerTemplate, ref usedHeaders).GetComponent<Text>().text = (title ?? string.Empty).ToUpperInvariant();

        public void Row(string label, string value, Color valueColor)
        {
            Transform row = HDCDebugStyle.Take(rows, rowTemplate, ref usedRows).transform;
            row.Find("Label").GetComponent<Text>().text = label;
            Text valueText = row.Find("Value").GetComponent<Text>();
            valueText.text = string.IsNullOrEmpty(value) ? "-" : value;
            valueText.color = valueColor;
        }

        public void Row(string label, string value) => Row(label, value, HDCDebugStyle.TextColor);

        public void Note(string text) => HDCDebugStyle.Take(notes, noteTemplate, ref usedNotes).GetComponent<Text>().text = text;

        public void End()
        {
            HDCDebugStyle.HideRest(rows, usedRows);
            HDCDebugStyle.HideRest(headers, usedHeaders);
            HDCDebugStyle.HideRest(notes, usedNotes);
        }

        private static void HideTemplate(GameObject template)
        {
            if (template.activeSelf)
                template.SetActive(false);
        }
    }
}
