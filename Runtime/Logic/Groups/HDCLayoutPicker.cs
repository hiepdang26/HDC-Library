using System;
using System.Collections.Generic;
using HDC.Ads.Domain;

namespace HDC.Ads.Logic
{
    /// <summary>
    /// Picks the layout of each native full-screen show: layouts come out of a shuffled bag, so every layout
    /// of the group shows once before any repeats.
    /// </summary>
    internal sealed class HDCLayoutPicker
    {
        private readonly HDCAdCoreConfig.LayoutGroup group;
        private readonly HDCAdCoreConfig config;
        private readonly List<HDCAdCoreConfig.Layout> bag = new List<HDCAdCoreConfig.Layout>();

        internal HDCLayoutPicker(HDCAdCoreConfig config, string groupName)
        {
            this.config = config;
            group = config?.LayoutGroupNamed(groupName);
        }

        internal HDCFullscreenOptions Next()
        {
            HDCAdCoreConfig.Layout[] layouts = group?.layouts;
            if (layouts == null || layouts.Length == 0)
                return new HDCFullscreenOptions();

            if (bag.Count == 0)
                bag.AddRange(Array.FindAll(layouts, l => l != null && !string.IsNullOrEmpty(l.layout)));
            if (bag.Count == 0)
                return new HDCFullscreenOptions();

            int index = UnityEngine.Random.Range(0, bag.Count);
            HDCAdCoreConfig.Layout layout = bag[index];
            bag.RemoveAt(index);
            return Options(layout, config.AssetConfigNamed(layout.assetConfigName));
        }

        private static HDCFullscreenOptions Options(HDCAdCoreConfig.Layout layout, HDCAdCoreConfig.AssetConfig assets)
        {
            var options = new HDCFullscreenOptions
            {
                layoutNames = new[] { layout.layout },
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
