using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace HDC.Ads
{
    /// <summary>How the debug panel colors a value.</summary>
    internal enum HDCDebugTone
    {
        Normal,
        Muted,
        Good,
        Warn,
        Bad,
    }

    /// <summary>
    /// The state of a channel or group for the debug panel: named sections of labeled values, each with the tone
    /// the panel colors it with.
    /// </summary>
    internal sealed class HDCDebugInfo
    {
        internal sealed class Row
        {
            internal Row(string label, string value, HDCDebugTone tone)
            {
                Label = label;
                Value = value;
                Tone = tone;
            }

            internal string Label { get; }
            internal string Value { get; }
            internal HDCDebugTone Tone { get; }
        }

        internal sealed class Part
        {
            internal Part(string name) => Name = name;

            internal string Name { get; }
            internal List<Row> Rows { get; } = new List<Row>();
        }

        internal List<Part> Parts { get; } = new List<Part>();

        /// <summary>Starts a section; the rows that follow go in it.</summary>
        internal HDCDebugInfo Section(string name)
        {
            Parts.Add(new Part(name));
            return this;
        }

        /// <summary>A value as it is; true and false read Yes and No, and No is dimmed.</summary>
        internal HDCDebugInfo Line(string label, object value) =>
            Add(label, Format(value), value is bool flag && !flag ? HDCDebugTone.Muted : HDCDebugTone.Normal);

        /// <summary>A switch the channel needs on: green when it is, red when not.</summary>
        internal HDCDebugInfo Needed(string label, bool on) => Add(label, Format(on), on ? HDCDebugTone.Good : HDCDebugTone.Bad);

        /// <summary>A gate that stops the ads while true: red when it does, green when not.</summary>
        internal HDCDebugInfo Gate(string label, bool closed) => Add(label, Format(closed), closed ? HDCDebugTone.Bad : HDCDebugTone.Good);

        internal HDCDebugInfo Add(string label, string value, HDCDebugTone tone)
        {
            if (Parts.Count == 0)
                Section(string.Empty);
            Parts[Parts.Count - 1].Rows.Add(new Row(label, string.IsNullOrEmpty(value) ? "-" : value, tone));
            return this;
        }

        /// <summary>Plain text, for copying.</summary>
        internal string ToText()
        {
            var text = new StringBuilder();
            foreach (Part part in Parts)
            {
                if (!string.IsNullOrEmpty(part.Name))
                    text.Append('[').Append(part.Name.ToUpperInvariant()).AppendLine("]");
                foreach (Row row in part.Rows)
                    text.Append(row.Label).Append(": ").AppendLine(row.Value);
                text.AppendLine();
            }

            return text.ToString().TrimEnd();
        }

        internal static string Format(object value)
        {
            switch (value)
            {
                case null:
                    return "-";
                case bool flag:
                    return flag ? "Yes" : "No";
                case float number:
                    return number.ToString("0.##", CultureInfo.InvariantCulture);
                case double number:
                    return number.ToString("0.##", CultureInfo.InvariantCulture);
                case string text when text.Length == 0:
                    return "-";
                default:
                    return value.ToString();
            }
        }
    }
}
