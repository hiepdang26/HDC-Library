using UnityEngine;
using UnityEngine.UI;

namespace HDC.Onboarding
{
    internal static class HDCIntroImages
    {
        internal const string Folder = "Intro/";

        internal static void Apply(Image image, string spriteName)
        {
            if (image == null || string.IsNullOrEmpty(spriteName))
                return;
            Sprite sprite = Resources.Load<Sprite>(Folder + spriteName);
            if (sprite == null)
            {
                HDCOnboardingLog.Warn($"No sprite at Resources/{Folder}{spriteName}");
                return;
            }

            image.sprite = sprite;
            image.color = Color.white;
        }
    }
}
