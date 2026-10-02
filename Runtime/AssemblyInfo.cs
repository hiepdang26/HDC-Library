using System.Runtime.CompilerServices;

// Game code sees the API (Runtime/Api). These parts of the library see the rest too: the debug panel, Remote
// Config, the setup prefab, which starts every group of the configs, and the tests.
[assembly: InternalsVisibleTo("HDC.Ads.Debug")]
[assembly: InternalsVisibleTo("HDC.Ads.Firebase")]
[assembly: InternalsVisibleTo("HDC.Ads.Setup")]
[assembly: InternalsVisibleTo("HDC.Ads.Tests")]
