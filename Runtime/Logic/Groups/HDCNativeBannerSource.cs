using System.Collections.Generic;
using System.Linq;
using HDC.Ads.Domain;
using HDC.Ads.Infrastructure;

namespace HDC.Ads.Logic
{
    /// <summary>The native banner, which can expand.</summary>
    internal sealed class HDCNativeBannerSource : HDCRectSource
    {
        private readonly string[] adUnitIds;
        private readonly HDCBannerOptions options;

        internal HDCNativeBannerSource(string id, HDCAdCoreConfig.NativeUnit unit) : base(id, HDCAdFormat.Banner)
        {
            var ids = new List<string>();
            foreach (string adUnitId in new[] { unit.id }.Concat(unit.ids ?? new string[0]))
            {
                if (!string.IsNullOrWhiteSpace(adUnitId) && !ids.Contains(adUnitId.Trim()))
                    ids.Add(adUnitId.Trim());
            }

            adUnitIds = ids.ToArray();
            options = new HDCBannerOptions { layoutNames = unit.layouts ?? new string[0], timeReload = unit.reloadTime };
        }

        internal override string AdUnitId => string.Join(", ", adUnitIds);

        internal override void Load() => HDCAdsSdk.LoadBanner(Id, adUnitIds, options);

        internal override void Show() => HDCAdsSdk.ShowBanner(Id);

        internal override void Hide() => HDCAdsSdk.HideBanner(Id);

        internal override bool Expand(bool enableClick) => HDCAdsSdk.ExpandBanner(Id, enableClick);
    }
}
