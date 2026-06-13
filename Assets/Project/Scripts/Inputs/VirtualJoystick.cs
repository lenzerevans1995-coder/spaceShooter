using UnityEngine;
using UnityEngine.EventSystems;

namespace SpaceShooter.Inputs
{
    /// <summary>
    /// On-screen touch joystick for mobile. Drag within the background ring; outputs a
    /// normalized [-1,1] Vector2. Works with mouse in the editor too, so it can be tested
    /// without a device. Two of these (move + aim) form the twin-stick scheme.
    /// </summary>
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] RectTransform _background;
        [SerializeField] RectTransform _handle;
        [SerializeField] float _range = 90f;

        public Vector2 Value { get; private set; }
        public bool Active { get; private set; }

        public void OnPointerDown(PointerEventData e) { Active = true; OnDrag(e); }

        public void OnDrag(PointerEventData e)
        {
            if (_background == null) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _background, e.position, e.pressEventCamera, out Vector2 local);
            Vector2 clamped = Vector2.ClampMagnitude(local, _range);
            Value = clamped / _range;
            if (_handle != null) _handle.anchoredPosition = clamped;
        }

        public void OnPointerUp(PointerEventData e)
        {
            Active = false;
            Value = Vector2.zero;
            if (_handle != null) _handle.anchoredPosition = Vector2.zero;
        }

        public void Bind(RectTransform background, RectTransform handle, float range)
        {
            _background = background;
            _handle = handle;
            _range = range;
        }
    }
}
