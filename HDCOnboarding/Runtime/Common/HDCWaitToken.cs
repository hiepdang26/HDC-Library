namespace HDC.Onboarding
{
    internal sealed class HDCWaitToken
    {
        internal bool Cancelled { get; private set; }

        internal void Cancel() => Cancelled = true;
    }
}
