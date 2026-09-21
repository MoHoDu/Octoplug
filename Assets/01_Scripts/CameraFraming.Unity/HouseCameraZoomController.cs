using System;
using Octoplug.CameraFraming;
using Octoplug.RoomGeneration;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Serialization;

namespace Octoplug.CameraFraming.Unity
{
    /// <summary>
    /// Owns the Cinemachine 2D orthographic zoom: clamps and smooths manual zoom input, keeps a
    /// dynamic maximum zoom-out derived from the current house bounds, and performs one-shot
    /// zoom-only hint-room framing without moving the camera or locking further manual zoom.
    /// </summary>
    /// <remarks>
    /// The framing UX is designed and tuned against <see cref="CameraDesignReference"/>
    /// (1920x1080, 16:9). Every calculation here reads <c>outputCamera.aspect</c> at the moment
    /// it runs, never a hardcoded pixel resolution, so a device running at a different aspect
    /// still gets bounds that fully fit the screen instead of clipping.
    /// </remarks>
    public sealed class HouseCameraZoomController : MonoBehaviour
    {
        [Header("Cinemachine")]
        [SerializeField]
        private CinemachineCamera cinemachineCamera;

        [SerializeField]
        private Camera outputCamera;

        [Header("Input")]
        [Tooltip("Reads raw mouse-wheel / touchpad-scroll / pinch input; kept separate so this class only owns zoom-range and Cinemachine control.")]
        [SerializeField]
        private CameraZoomInputReader zoomInput;

        [Header("[Zoom] Limits")]
        [Tooltip("[Zoom] The camera never zooms in past this orthographic size. Never bypassed by Zoom sensitivity or Hint framing.")]
        [SerializeField]
        private float minOrthographicSize = 5f;

        [Tooltip("[Zoom] / [Hint Framing] World-unit padding kept around the house bounds (dynamic max zoom-out) AND around a framed Hint Room's full bounds, so content is not flush against the screen edge. Shared between both uses intentionally: both describe the same 'breathing room around visible content' concept.")]
        [FormerlySerializedAs("houseFramingMargin")]
        [SerializeField]
        private float houseFramingMargin = 2f;

        [Header("[Zoom] Feel")]
        [Tooltip("[Zoom] Approximate seconds for the zoom to settle on its target size after Wheel/Touchpad/Pinch input or Hint framing. Zero applies the target immediately.")]
        [FormerlySerializedAs("zoomSmoothTime")]
        [SerializeField]
        private float zoomSmoothing = 0.15f;

        private float targetOrthographicSize;
        private float zoomVelocity;
        private ZoomRange zoomRange;
        private bool initialized;
        private bool revealCompletionPending;

        public event Action CameraMotionStarted;
        public event Action CameraRevealCompleted;

        private void Awake()
        {
            Initialize();
        }

#if UNITY_EDITOR
        public void InitializeForVerification()
        {
            Initialize();
        }

        public void TickForVerification()
        {
            Update();
        }
#endif

        private void Initialize()
        {
            if (initialized)
            {
                return;
            }

            if (cinemachineCamera == null || outputCamera == null)
            {
                throw new InvalidOperationException($"{nameof(HouseCameraZoomController)} requires a Cinemachine camera and an output camera reference.");
            }

            if (minOrthographicSize <= 0f || float.IsNaN(minOrthographicSize) || float.IsInfinity(minOrthographicSize))
            {
                throw new InvalidOperationException($"{nameof(HouseCameraZoomController)} minimum orthographic size must be positive.");
            }

            targetOrthographicSize = Mathf.Max(cinemachineCamera.Lens.OrthographicSize, minOrthographicSize);
            zoomRange = new ZoomRange(minOrthographicSize, targetOrthographicSize);
            cinemachineCamera.Lens.OrthographicSize = targetOrthographicSize;
            initialized = true;
        }

        private void OnEnable()
        {
            if (zoomInput != null)
            {
                zoomInput.ZoomDeltaRequested += ApplyZoomDelta;
            }
        }

        private void OnDisable()
        {
            if (zoomInput != null)
            {
                zoomInput.ZoomDeltaRequested -= ApplyZoomDelta;
            }
        }

        private void Update()
        {
            if (!initialized)
            {
                return;
            }

            var current = cinemachineCamera.Lens.OrthographicSize;
            if (!Mathf.Approximately(current, targetOrthographicSize))
            {
                var next = zoomSmoothing <= 0f
                    ? targetOrthographicSize
                    : Mathf.SmoothDamp(current, targetOrthographicSize, ref zoomVelocity, zoomSmoothing, Mathf.Infinity, Time.unscaledDeltaTime);
                if (Mathf.Abs(next - targetOrthographicSize) <= 0.001f)
                {
                    next = targetOrthographicSize;
                }

                cinemachineCamera.Lens.OrthographicSize = next;
                if (Time.timeScale == 0f && outputCamera != null)
                {
                    outputCamera.orthographicSize = next;
                }
            }

            CompleteRevealIfSettled();
        }

        /// <summary>Applies a manual zoom-input delta (from <see cref="CameraZoomInputReader"/>), clamped to the current zoom range.</summary>
        public void ApplyZoomDelta(float sizeDelta)
        {
            if (!initialized || sizeDelta == 0f)
            {
                return;
            }

            var nextTarget = zoomRange.Clamp(targetOrthographicSize + sizeDelta);
            if (Mathf.Approximately(nextTarget, targetOrthographicSize))
            {
                return;
            }

            targetOrthographicSize = nextTarget;
            CameraMotionStarted?.Invoke();
        }

        /// <summary>Recomputes the dynamic maximum zoom-out from the current full house bounds.</summary>
        public void RecomputeDynamicMaxOrthographicSize(RoomBounds2D houseBounds)
        {
            if (!initialized)
            {
                return;
            }

            var houseMax = DynamicZoomLimit.ComputeMaxOrthographicSize(houseBounds, outputCamera.aspect, houseFramingMargin, minOrthographicSize);

            // A previous hint may have widened the effective range beyond the unlocked-House max.
            // Recomputing after that hint is promoted must not pull the current target inward before
            // the following hint is framed: automatic framing is zoom-out-only. Manual zoom-in can
            // still lower the target, after which a later recompute may naturally narrow the range.
            var effectiveMax = Math.Max(houseMax, targetOrthographicSize);
            zoomRange = new ZoomRange(minOrthographicSize, effectiveMax);
        }

        /// <summary>
        /// One-shot: zooms out just enough (if at all) so the hint room's FULL bounds — not just
        /// its center — fit within the current camera position. Never zooms in and never moves
        /// the camera.
        /// </summary>
        public void RequestHintFraming(RoomBounds2D hintBounds)
        {
            RequestZoomOutFraming(hintBounds);
        }

        /// <summary>
        /// Requests an externally orchestrated room reveal and reports completion after the
        /// zoom-only framing target is reached. The request never moves the camera or zooms in.
        /// </summary>
        public void RequestRoomReveal(RoomBounds2D roomBounds)
        {
            if (!initialized)
            {
                return;
            }

            revealCompletionPending = true;
            RequestZoomOutFraming(roomBounds);
            CompleteRevealIfSettled();
        }

        private void RequestZoomOutFraming(RoomBounds2D bounds)
        {
            if (!initialized)
            {
                return;
            }

            var cameraPosition = outputCamera.transform.position;
            var request = new HintFramingRequest(
                new Point2D(cameraPosition.x, cameraPosition.y),
                targetOrthographicSize,
                outputCamera.aspect,
                bounds,
                houseFramingMargin);

            var result = HintFramingCalculator.Compute(request);
            if (!result.Changed)
            {
                return;
            }

            targetOrthographicSize = result.TargetOrthographicSize;
            CameraMotionStarted?.Invoke();

            // The dynamic max (see RecomputeDynamicMaxOrthographicSize) is derived only from
            // already-unlocked rooms, so it cannot yet know about newly visible bounds outside the
            // current house footprint. Framing must never be cut short by that stale ceiling.
            if (targetOrthographicSize > zoomRange.Max)
            {
                zoomRange = new ZoomRange(zoomRange.Min, targetOrthographicSize);
            }
        }

        private void CompleteRevealIfSettled()
        {
            if (!revealCompletionPending
                || !Mathf.Approximately(
                    cinemachineCamera.Lens.OrthographicSize,
                    targetOrthographicSize))
            {
                return;
            }

            revealCompletionPending = false;
            CameraRevealCompleted?.Invoke();
        }
    }
}
