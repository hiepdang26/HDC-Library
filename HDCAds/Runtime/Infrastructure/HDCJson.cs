using System.Text;
using UnityEngine;

namespace HDC.Ads.Infrastructure
{
    internal static class HDCJson
    {
        internal static string Args(string id, object fields = null, string[] adUnitIds = null)
        {
            var json = new StringBuilder("{\"id\":");
            AppendString(json, id ?? string.Empty);
            if (adUnitIds != null)
            {
                json.Append(",\"adUnitIds\":[");
                for (int i = 0; i < adUnitIds.Length; i++)
                {
                    if (i > 0)
                        json.Append(',');
                    AppendString(json, adUnitIds[i] ?? string.Empty);
                }

                json.Append(']');
            }

            if (fields != null)
            {
                string fieldsJson = JsonUtility.ToJson(fields);
                if (fieldsJson.Length > 2)
                    json.Append(',').Append(fieldsJson, 1, fieldsJson.Length - 2);
            }

            return json.Append('}').ToString();
        }

        internal static void AppendString(StringBuilder json, string value)
        {
            json.Append('"');
            foreach (char c in value)
            {
                switch (c)
                {
                    case '"':
                        json.Append("\\\"");
                        break;
                    case '\\':
                        json.Append("\\\\");
                        break;
                    case '\n':
                        json.Append("\\n");
                        break;
                    case '\r':
                        json.Append("\\r");
                        break;
                    case '\t':
                        json.Append("\\t");
                        break;
                    default:
                        if (c < ' ')
                            json.Append("\\u").Append(((int)c).ToString("x4"));
                        else
                            json.Append(c);
                        break;
                }
            }

            json.Append('"');
        }
    }
}
