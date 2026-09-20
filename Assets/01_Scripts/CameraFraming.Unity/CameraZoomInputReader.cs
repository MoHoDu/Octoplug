using System;
using Octoplug.CameraFraming;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace Octoplug.CameraFraming.Unity
{
    /// <summary>
    /// Reads raw zoom input (mouse wheel / touchpad scroll / two-finger pinch) and converts it to an
    /// orthographic-size delta with the pure <see cref="ZoomInputMapper"/>. It does not know about
    /// Cinemachine or any camera; it only raises <see cref="ZoomDeltaRequested"/>.
    /// </summary>
    public sealed class CameraZoomInputReader : MonoBehaviour
    {
        [Header("[Zoom] Sensitivity (per input device)")]
        [Tooltip("[Zoom] Orthographic-size change per unit of Desktop Mouse Wheel / Touchpad scroll delta. The New Input System reports a physical mouse-wheel notch and a Windows touchpad two-finger scroll gesture through the SAME Mouse.scroll control, so this one field intentionally covers BOTH — they cannot be told apart on this platform, so they are not force-split into separate fields. Tune freely in Play Mode; higher = faster Zoom.")]
        [FormerlySerializedAs("scrollSensitivity")]
        [SerializeField]
        private float desktopScrollZoomSensitivity = 0.2f;

        [Tooltip("[Zoom] Orthographic-size change per pixel of two-finger mobile Pinch distance change. Independent of Desktop Scroll Sensitivity above. Tune freely in Play Mode; higher = faster Zoom.")]
        [FormerlySerializedAs("pinchSensitivity")]
        [SerializeField]
        private float pinchZoomSensitivity = 0.2f;

        public event Action<float> ZoomDeltaRequested;

        private float? previousPinchDistance;

        private void Update()
        {
            if (Octoplug.GameFlow.GameplayInputLock.IsLocked)
            {
                previousPinchDistance = null;
                return;
            }

            ReadScroll();
            ReadPinch();
        }

        private void ReadScroll()
        {
            var mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            // The New Input System reports both mouse-wheel notches and touchpad two-finger
            // scroll gestures through the same Mouse.scroll control on PC.
            var scrollY = mouse.scroll.ReadValue().y;
            if (Mathf.Approximately(scrollY, 0f))
            {
                return;
            }

            RaiseZoomDelta(ZoomInputMapper.MapScrollDelta(scrollY, desktopScrollZoomSensitivity));
        }

        private void ReadPinch()
        {
            var touchscreen = Touchscreen.current;
            if (touchscreen == null)
            {
                previousPinchDistance = null;
                return;
            }

            Vector2? first = null;
            Vector2? second = null;
            var touches = touchscreen.touches;
            for (var i = 0; i < touches.Count && second == null; i++)
            {
                var touch = touches[i];
                if (!touch.press.isPressed)
                {
                    continue;
                }

                if (first == null)
                {
                    first = touch.position.ReadValue();
                }
                else
                {
                    second = touch.position.ReadValue();
                }
            }

            if (first == null || second == null)
            {
                previousPinchDistance = null;
                return;
            }

            var currentDistance = Vector2.Distance(first.Value, second.Value);
            if (previousPinchDistance.HasValue)
            {
                RaiseZoomDelta(ZoomInputMapper.MapPinchDelta(previousPinchDistance.Value, currentDistance, pinchZoomSensitivity));
            }

            previousPinchDistance = currentDistance;
        }

        private void RaiseZoomDelta(float delta)
        {
            if (!Mathf.Approximately(delta, 0f))
            {
                ZoomDeltaRequested?.Invoke(delta);
            }
        }
    }
}
