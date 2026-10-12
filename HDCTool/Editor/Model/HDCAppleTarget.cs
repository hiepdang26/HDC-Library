namespace HDC.Tool.Editor
{
    internal enum HDCAppleTarget
    {
        Phone,
        Mac,
        Simulator,
    }

    internal static class HDCAppleTargets
    {
        internal static string Title(this HDCAppleTarget target) =>
            target == HDCAppleTarget.Mac ? "My Mac" : target == HDCAppleTarget.Simulator ? "Simulator" : "iPhone";

        internal static string Sdk(this HDCAppleTarget target) => target == HDCAppleTarget.Simulator ? "Simulator SDK" : "Device SDK";
    }
}
