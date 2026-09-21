using System;
using Octoplug.RoomGeneration;
using Unity.Cinemachine;
using UnityEngine;

namespace Octoplug.CameraFraming.Unity
{
    /// <summary>
    /// Applies bounded Pan to the Cinemachine camera while leaving zoom ownership separate.
    /// </summary>
    /// <remarks>
    /// The Pan boundary is never a cached/fixed world-space range: <see cref="ApplyPanDelta"/>
    /// recomputes it from the CURRENT <c>cinemachineCamera.Lens.OrthographicSize</c> and
    /// <c>outputCamera.aspect</c> on every single Pan request, so a Mouse Wheel / Touchpad / Pinch
    /// Zoom or a one-shot Hint auto-framing widens or narrows the reachable range immediately, with
    /// no Room Generation event required to "refresh" it. Only <see cref="houseBounds"/> itself
    /// (the actual generated House footprint) is cached, and only Room Generation state changes
    /// (<see cref="UpdateHouseBounds"/>) update that.
    /// </remarks>
    public sealed class HouseCameraPanController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private CinemachineCamera cinemachineCamera;

        [SerializeField]
        private Camera outputCamera;

        [SerializeField]
        private CameraPanInputReader panInput;

        [Header("[Pan] Boundary")]
        [Tooltip("[Pan] House Bounds 외부에 추가로 허용되는 Camera 이동 여백 (world units). The 'House Bounds' fed in here is the current unlocked House plus the current Hint Room, so Pan can always reach a Hint sitting just outside the unlocked footprint. Recomputed against the CURRENT Orthographic Size and aspect every time a Pan is applied, so Zoom In/Out immediately widens/narrows the reachable range with no Room event required.")]
        [SerializeField]
        private float panBoundaryMargin = 2f;

        private RoomBounds2D houseBounds;
        private bool hasHouseBounds;

        public event Action CameraMotionStarted;

        private void OnValidate()
        {
            if (panBoundaryMargin < 0f || float.IsNaN(panBoundaryMargin) || float.IsInfinity(panBoundaryMargin))
            {
                Debug.LogError($"{nameof(HouseCameraPanController)} boundary margin must be a non-negative finite value.", this);
            }
        }

        private void Awake()
        {
            if (cinemachineCamera == null || outputCamera == null)
            {
                throw new InvalidOperationException($"{nameof(HouseCameraPanController)} requires Cinemachine camera and output camera references.");
            }
        }

        private void OnEnable()
        {
            if (panInput != null)
            {
                panInput.PanDeltaRequested += ApplyPanDelta;
            }
        }

        private void OnDisable()
        {
            if (panInput != null)
            {
                panInput.PanDeltaRequested -= ApplyPanDelta;
            }
        }

        public void UpdateHouseBounds(RoomBounds2D bounds)
        {
            if (!bounds.IsValid)
            {
                throw new ArgumentException("House bounds must be valid.", nameof(bounds));
            }

            houseBounds = bounds;
            hasHouseBounds = true;
        }

        public void ApplyPanDelta(Vector2 worldDelta)
        {
            if (!hasHouseBounds || worldDelta == Vector2.zero)
            {
                return;
            }

            var current = cinemachineCamera.transform.position;
            var range = PanBoundaryCalculator.Compute(
                houseBounds,
                cinemachineCamera.Lens.OrthographicSize,
                outputCamera.aspect,
                panBoundaryMargin);
            var clamped = range.Clamp(
                new Point2D(current.x, current.y),
                new Point2D(current.x + worldDelta.x, current.y + worldDelta.y));
            var next = new Vector3(clamped.X, clamped.Y, current.z);
            if (next == current)
            {
                return;
            }

            cinemachineCamera.transform.position = next;
            if (Time.timeScale == 0f && outputCamera != null)
            {
                outputCamera.transform.position = next;
            }
            CameraMotionStarted?.Invoke();
        }
    }
}
