using System.Globalization;
using System.Text;

namespace HDC.Ads
{
    /// <summary>Builds the plain-text state reports of the channels for the debug panel.</summary>
    internal static class HDCAdsDebugText
    {
        internal static StringBuilder Title(string title) => new StringBuilder().Append("=== ").Append(title).AppendLine(" ===");

        internal static StringBuilder Section(this StringBuilder text, string name) =>
            text.AppendLine().Append("-- ").Append(name).AppendLine(" --");

        internal static StringBuilder Line(this StringBuilder text, string label, object value) =>
            text.Append(label).Append(": ").AppendLine(Format(value));

        internal static StringBuilder Lines(this StringBuilder text, string block) =>
            text.AppendLine(string.IsNullOrEmpty(block) ? "(none)" : block);

        internal static string Done(this StringBuilder text) => text.ToString().TrimEnd();

        private static string Format(object value)
        {
            switch (value)
            {
                case null:
                    return "(null)";
                case bool flag:
                    return flag ? "true" : "false";
                case float number:
                    return number.ToString("0.##", CultureInfo.InvariantCulture);
                case string text when text.Length == 0:
                    return "(empty)";
                default:
                    return value.ToString();
            }
        }
    }
}
