using UnityEngine.UI;

namespace HDC.Ads.DebugUI
{
    public sealed class HDCDebugText : Text
    {
        private string widthText;
        private float widthScale;
        private float width;
        private string heightText;
        private float heightScale;
        private float heightWrap;
        private float height;

        public override string text
        {
            get => base.text;
            set
            {
                if (string.IsNullOrEmpty(value) || m_Text == value || !IsActive() || !SizeKnown)
                {
                    base.text = value;
                    return;
                }

                float oldWidth = width;
                float oldHeight = height;
                m_Text = value;
                SetVerticesDirty();
                if (preferredWidth != oldWidth || preferredHeight != oldHeight)
                    SetLayoutDirty();
            }
        }

        public override float preferredWidth
        {
            get
            {
                float scale = pixelsPerUnit;
                if (!ReferenceEquals(widthText, text) || widthScale != scale)
                {
                    width = base.preferredWidth;
                    widthText = text;
                    widthScale = scale;
                }

                return width;
            }
        }

        public override float preferredHeight
        {
            get
            {
                float scale = pixelsPerUnit;
                float wrap = GetPixelAdjustedRect().width;
                if (!ReferenceEquals(heightText, text) || heightScale != scale || heightWrap != wrap)
                {
                    height = base.preferredHeight;
                    heightText = text;
                    heightScale = scale;
                    heightWrap = wrap;
                }

                return height;
            }
        }

        public override void SetLayoutDirty()
        {
            widthText = null;
            heightText = null;
            base.SetLayoutDirty();
        }

        private bool SizeKnown =>
            ReferenceEquals(widthText, m_Text) && ReferenceEquals(heightText, m_Text)
            && widthScale == pixelsPerUnit && heightScale == widthScale && heightWrap == GetPixelAdjustedRect().width;
    }
}
