using System;
using Octoplug.RoomGeneration;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Octoplug.CameraFraming.Unity
{
    /// <summary>
    /// Owns mouse/single-touch Pan gestures and emits world deltas without knowing Cinemachine.
    /// </summary>
    /// <remarks>
    /// Root cause of "empty-world drag never pans in the Editor Game View": by default the New
    /// Input System only routes pointer BUTTON state to devices when the Game View window has OS
    /// focus (<see cref="InputSettings.editorInputBehaviorInPlayMode"/> defaults to
    /// <c>PointersAndKeyboardsRespectGameViewFocus</c>), while mouse-wheel scroll is delivered by
    /// Windows to whatever window is under the cursor regardless of focus. That is exactly why
    /// Zoom (scroll) already worked while Pan (left-button drag) looked completely dead in the
    /// Editor: the very click that starts a drag is also the click that would give the Game View
    /// focus, so its button-down transition can be dropped before any script observes it. This is
    /// an Editor Play Mode testing quirk only (it does not exist in a built Player), so it is
    /// corrected here, once, in memory only, and only inside the Editor.
    /// </remarks>
    public sealed class CameraPanInputReader : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private Camera outputCamera;

        [SerializeField]
        private CameraPanInteractionResolver interactionResolver;

        [Header("Pan Sensitivity")]
        [Tooltip("[Pan] Multiplier applied after pointer pixels are converted to orthographic world displacement. Shared by Mouse and single-finger Touch: both convert the same on-screen pixel distance through the same current Orthographic Size, so one shared value already feels identical on desktop and mobile. Tune freely in Play Mode; higher = faster Pan.")]
        [SerializeField]
        private float panSensitivity = 1f;

        public event Action<Vector2> PanDeltaRequested;

        private readonly PanGestureTracker gesture = new();
        private Vector2 previousPointerPosition;
        private int? activeTouchId;
        private bool touchSequenceActive;

        private void OnValidate()
        {
            if (outputCamera == null || interactionResolver == null)
            {
                Debug.LogError($"{nameof(CameraPanInputReader)} requires output camera and interaction resolver references.", this);
            }

            if (panSensitivity < 0f || float.IsNaN(panSensitivity) || float.IsInfinity(panSensitivity))
            {
                Debug.LogError($"{nameof(CameraPanInputReader)} Pan sensitivity must be a non-negative finite value.", this);
            }
        }

        private void Awake()
        {
#if UNITY_EDITOR
            // Editor Play Mode only, in-memory only (never touches a persisted asset, never
            // affects a build): make sure the Game View always receives pointer/keyboard input
            // during this Play session instead of requiring it to hold OS window focus first. See
            // the class remark above for why this specifically breaks empty-world Pan while Zoom
            // (scroll) still appears to work.
            InputSystem.settings.editorInputBehaviorInPlayMode =
                InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
        }

        private void OnDisable()
        {
            gesture.EndAllTouches();
            activeTouchId = null;
            touchSequenceActive = false;
        }

        private void Update()
        {
            if (ReadTouch())
            {
                return;
            }

            ReadMouse();
        }

        private bool ReadTouch()
        {
            var touchscreen = Touchscreen.current;
            if (touchscreen == null)
            {
                return false;
            }

            var pressedCount = 0;
            TouchControl firstPressed = null;
            var touches = touchscreen.touches;
            for (var i = 0; i < touches.Count; i++)
            {
                if (!touches[i].press.isPressed)
                {
                    continue;
                }

                pressedCount++;
                firstPressed ??= touches[i];
            }

            if (pressedCount == 0)
            {
                if (touchSequenceActive)
                {
                    gesture.EndAllTouches();
                    activeTouchId = null;
                    touchSequenceActive = false;
                    return true;
                }

                return false;
            }

            touchSequenceActive = true;
            if (pressedCount >= 2)
            {
                gesture.SuppressForMultiTouch();
                activeTouchId = null;
                return true;
            }

            if (gesture.Ownership == PanGestureOwnership.MultiTouchSuppressed)
            {
                return true;
            }

            var touchId = firstPressed.touchId.ReadValue();
            var position = firstPressed.position.ReadValue();
            if (!activeTouchId.HasValue)
            {
                activeTouchId = touchId;
                previousPointerPosition = position;
                gesture.TryBegin(interactionResolver == null || interactionResolver.IsReserved(position));
                return true;
            }

            if (activeTouchId.Value != touchId)
            {
                gesture.SuppressForMultiTouch();
                activeTouchId = null;
                return true;
            }

            RaisePan(position - previousPointerPosition);
            previousPointerPosition = position;
            return true;
        }

        private void ReadMouse()
        {
            var mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            var position = mouse.position.ReadValue();
            if (mouse.leftButton.wasPressedThisFrame)
            {
                previousPointerPosition = position;
                gesture.TryBegin(interactionResolver == null || interactionResolver.IsReserved(position));
            }

            if (mouse.leftButton.isPressed)
            {
                RaisePan(position - previousPointerPosition);
                previousPointerPosition = position;
            }

            if (mouse.leftButton.wasReleasedThisFrame)
            {
                gesture.EndPointer();
            }
        }

        private void RaisePan(Vector2 screenDelta)
        {
            if (gesture.Ownership != PanGestureOwnership.CameraPan
                || screenDelta == Vector2.zero
                || outputCamera == null)
            {
                return;
            }

            var worldDelta = PanDeltaMapper.Map(
                new Point2D(screenDelta.x, screenDelta.y),
                outputCamera.orthographicSize,
                outputCamera.aspect,
                outputCamera.pixelWidth,
                outputCamera.pixelHeight,
                panSensitivity);
            PanDeltaRequested?.Invoke(new Vector2(worldDelta.X, worldDelta.Y));
        }
    }
}
