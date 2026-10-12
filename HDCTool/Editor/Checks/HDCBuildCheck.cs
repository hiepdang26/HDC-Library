using System;

namespace HDC.Tool.Editor
{
    internal enum HDCCheckLevel
    {
        Ok,
        Info,
        Warning,
        Error,
    }

    internal sealed class HDCBuildCheck
    {
        internal HDCBuildCheck(string name, HDCCheckLevel level, string message = "", string fixLabel = null, Action fix = null)
        {
            Name = name;
            Level = level;
            Message = message ?? "";
            FixLabel = fixLabel;
            Fix = fix;
        }

        internal string Name { get; }

        internal HDCCheckLevel Level { get; }

        internal string Message { get; }

        internal string FixLabel { get; }

        internal Action Fix { get; }

        internal static HDCBuildCheck Ok(string name) => new HDCBuildCheck(name, HDCCheckLevel.Ok);
    }
}
