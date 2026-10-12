using UnityEngine;

namespace HDC.Onboarding
{
    public abstract class HDCIntroView : MonoBehaviour
    {
        [Tooltip("Where the popup ad of a page shown by this view goes.")]
        [SerializeField] private RectTransform adArea;

        public RectTransform AdArea => adArea;

        public abstract bool IsComplete { get; }

        public abstract bool CanRender(HDCIntroPage page);

        public abstract void Render(HDCIntroPage page);
    }
}
