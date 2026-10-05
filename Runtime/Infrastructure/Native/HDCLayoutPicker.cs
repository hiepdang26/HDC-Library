using System;
using System.Collections.Generic;
using HDC.Ads.Domain;

namespace HDC.Ads.Infrastructure
{
    internal sealed class HDCLayoutPicker
    {
        private const int DefaultLayouts = -1;

        private readonly HDCAdCoreConfig.LayoutGroup group;
        private readonly HDCAdCoreConfig config;
        private readonly Dictionary<int, List<HDCAdCoreConfig.Layout>> bags = new Dictionary<int, List<HDCAdCoreConfig.Layout>>();

        internal HDCLayoutPicker(HDCAdCoreConfig config, string groupName)
        {
            this.config = config;
            group = config?.LayoutGroupNamed(groupName);
        }

        internal HDCFullscreenOptions Next(string adSourceId = "")
        {
            if (group == null)
                return new HDCFullscreenOptions();

            int source = SourceGroupOf(adSourceId);
            List<HDCAdCoreConfig.Layout> layouts = Usable(source);
            if (layouts.Count == 0)
            {
                source = FirstSourceGroupWithLayouts();
                layouts = Usable(source);
            }

            if (layouts.Count == 0)
                return new HDCFullscreenOptions();

            if (!bags.TryGetValue(source, out List<HDCAdCoreConfig.Layout> bag) || bag.Count == 0)
                bags[source] = bag = layouts;
            int index = UnityEngine.Random.Range(0, bag.Count);
            HDCAdCoreConfig.Layout layout = bag[index];
            bag.RemoveAt(index);
            return Options(layout, config.AssetConfigNamed(layout.assetConfigName));
        }

        private int SourceGroupOf(string adSourceId)
        {
            if (string.IsNullOrEmpty(adSourceId))
                return DefaultLayouts;
            HDCAdCoreConfig.AdSourceGroup[] groups = group.adSourceGroups ?? new HDCAdCoreConfig.AdSourceGroup[0];
            for (int i = 0; i < groups.Length; i++)
            {
                if (Usable(i).Count > 0 && Array.IndexOf(groups[i].adSourceIds ?? new string[0], adSourceId) >= 0)
                    return i;
            }

            return DefaultLayouts;
        }

        private int FirstSourceGroupWithLayouts()
        {
            int count = group.adSourceGroups?.Length ?? 0;
            for (int i = 0; i < count; i++)
            {
                if (Usable(i).Count > 0)
                    return i;
            }

            return DefaultLayouts;
        }

        private List<HDCAdCoreConfig.Layout> Usable(int source)
        {
            HDCAdCoreConfig.Layout[] layouts = source == DefaultLayouts ? group.layouts : group.adSourceGroups?[source]?.layouts;
            return new List<HDCAdCoreConfig.Layout>(Array.FindAll(layouts ?? new HDCAdCoreConfig.Layout[0], l => l != null && !string.IsNullOrEmpty(l.layout)));
        }

        private static HDCFullscreenOptions Options(HDCAdCoreConfig.Layout layout, HDCAdCoreConfig.AssetConfig assets)
        {
            var options = new HDCFullscreenOptions
            {
                layoutNames = new[] { HDCAdLayouts.Fullscreen(layout.layout) },
                duration = layout.layoutTime,
                delay = layout.delay,
                timeUpC = layout.timeUpC,
                pauseGameplay = layout.pauseGameplay,
                showTCD = layout.showTCD,
                enableAdComeback = !layout.disableAdComeback,
            };
            if (assets != null)
            {
                options.assetVisibility = new HDCNativeAssets
                {
                    cta = assets.show_cta,
                    headline = assets.show_headline,
                    body = assets.show_body,
                    description = assets.show_description,
                    icon = assets.show_icon,
                    advertiser = assets.show_advertiser,
                    media = assets.show_media,
                    mediaImage = assets.show_media && assets.show_media_image,
                    mediaVideo = assets.show_media && assets.show_media_video,
                    price = assets.show_price,
                    store = assets.show_store,
                    starRating = assets.show_star_rating,
                };
            }

            return options;
        }
    }
}
