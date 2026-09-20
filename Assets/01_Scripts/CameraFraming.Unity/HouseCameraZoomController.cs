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

        private void OnValidate()
        {
            if (cinemachineCamera == null || outputCamera == null || zoomInput == null)
            {
                Debug.LogError($"{nameof(HouseCameraZoomController)} requires Cinemachine camera, output camera, and zoom input references.", this);
            }
        }

        private void Awake()
        {
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
            if (Mathf.Approximately(current, targetOrthographicSize))
            {
                return;
            }

            cinemachineCamera.Lens.OrthographicSize = zoomSmoothing <= 0f
                ? targetOrthographicSize
                : Mathf.SmoothDamp(current, targetOrthographicSize, ref zoomVelocity, zoomSmoothing);
        }

        /// <summary>Applies a manual zoom-input delta (from <see cref="CameraZoomInputReader"/>), clamped to the current zoom range.</summary>
        public void ApplyZoomDelta(float sizeDelta)
        {
            if (!initialized || sizeDelta == 0f)
            {
                return;
            }

            targetOrthographicSize = zoomRange.Clamp(targetOrthographicSize + sizeDelta);
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
            if (!initialized)
            {
                return;
            }

            var cameraPosition = outputCamera.transform.position;
            var request = new HintFramingRequest(
                new Point2D(cameraPosition.x, cameraPosition.y),
                targetOrthographicSize,
                outputCamera.aspect,
                hintBounds,
                houseFramingMargin);

            var result = HintFramingCalculator.Compute(request);
            if (!result.Changed)
            {
                return;
            }

            targetOrthographicSize = result.TargetOrthographicSize;

            // The dynamic max (see RecomputeDynamicMaxOrthographicSize) is derived only from
            // already-unlocked rooms, so it cannot yet know about a brand-new hint room that sits
            // just outside the current house footprint. Showing the hint's full bounds must never
            // be cut short by that stale, house-only ceiling, so the effective range is widened to
            // match here instead of clamping the framing result down to it. This does not change
            // how the dynamic max itself is computed from house bounds.
            if (targetOrthographicSize > zoomRange.Max)
            {
                zoomRange = new ZoomRange(zoomRange.Min, targetOrthographicSize);
            }
        }
    }
}
