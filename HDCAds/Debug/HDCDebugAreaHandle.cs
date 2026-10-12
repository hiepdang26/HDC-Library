using UnityEngine;
using UnityEngine.EventSystems;

namespace HDC.Ads.DebugUI
{
    public sealed class HDCDebugAreaHandle : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IInitializePotentialDragHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private HDCDebugAreaEditor editor;
        [Tooltip("The corner this handle drags: -1 or 1 on each axis, 1 being the right or the top.")]
        [SerializeField] private Vector2 corner = Vector2.one;

        void IPointerDownHandler.OnPointerDown(PointerEventData eventData) => editor.BeginResize(corner, eventData.position);

        void IInitializePotentialDragHandler.OnInitializePotentialDrag(PointerEventData eventData) => eventData.useDragThreshold = false;

        void IBeginDragHandler.OnBeginDrag(PointerEventData eventData) => editor.Resize(eventData.position);

        void IDragHandler.OnDrag(PointerEventData eventData) => editor.Resize(eventData.position);

        void IEndDragHandler.OnEndDrag(PointerEventData eventData) => editor.EndResize();

        void IPointerUpHandler.OnPointerUp(PointerEventData eventData) => editor.EndResize();
    }
}
