using System;
using Octoplug.RoomGeneration;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Serialization;

namespace Octoplug.CameraFraming.Unity
{
    /// <summary>최초 fit과 수동 Zoom 보정, 위치를 보존하는 Hint/Reveal을 pre-Brain tick에서 처리한다.</summary>
    [DefaultExecutionOrder(0)]
    public sealed class HouseCameraZoomController : MonoBehaviour
    {
        [Header("Cinemachine")]
        [SerializeField]
        private CinemachineCamera cinemachineCamera;

        [SerializeField]
        private Camera outputCamera;

        [Tooltip("가용 화면과 Pan을 공유하는 명시 참조. 미지정된 기존 사용처는 전체 viewport로 동작합니다.")]
        [SerializeField]
        private HouseCameraPanController cameraPan;

        [Header("Input")]
        [Tooltip("Reads raw mouse-wheel / touchpad-scroll / pinch input; kept separate so this class only owns zoom-range and Cinemachine control.")]
        [SerializeField]
        private CameraZoomInputReader zoomInput;

        [Header("[Zoom] Limits")]
        [Tooltip("[Zoom] The camera never zooms in past this orthographic size. Never bypassed by Zoom sensitivity or Hint framing.")]
        [SerializeField]
        private float minOrthographicSize = 5f;

        [Tooltip("최초 화면 fit과 수동 Zoom 범위 계산에 사용하는 집 경계 여백(world units). 자동 Hint/Reveal에는 적용하지 않습니다.")]
        [FormerlySerializedAs("houseFramingMargin")]
        [SerializeField]
        private float houseFramingMargin = 2f;

        [Header("[Zoom] Feel")]
        [Tooltip("[Zoom] Approximate seconds for zoom and manual boundary correction to settle. Zero applies the target immediately.")]
        [FormerlySerializedAs("zoomSmoothTime")]
        [SerializeField]
        private float zoomSmoothing = 0.15f;

        private float targetOrthographicSize;
        private float zoomVelocity;
        private ZoomRange zoomRange;
        private bool initialized;
        private bool revealCompletionPending;
        private RoomBounds2D revealBounds;
        private bool hintFramingPending;
        private RoomBounds2D pendingHintBounds;
        private bool initialFramingPending;
        private bool initialFramingCompleted;
        private RoomBounds2D initialVisibleBounds;
        private bool hasHouseBounds;
        private RoomBounds2D houseBounds;
        private bool manualCorrectionPending;
        private Vector2 correctionVelocity;

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
            Tick(Time.unscaledDeltaTime);
        }

        public void TickForVerification(float unscaledDeltaTime)
        {
            Tick(unscaledDeltaTime);
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

            if (!IsPositiveFinite(minOrthographicSize))
            {
                throw new InvalidOperationException($"{nameof(HouseCameraZoomController)} minimum orthographic size must be positive.");
            }

            targetOrthographicSize = Mathf.Max(cinemachineCamera.Lens.OrthographicSize, minOrthographicSize);
            zoomRange = new ZoomRange(minOrthographicSize, targetOrthographicSize);
            cinemachineCamera.Lens.OrthographicSize = targetOrthographicSize;
            initialized = true;
            cameraPan?.SetFrameDriver(this);
        }

        private void OnEnable()
        {
            if (zoomInput != null)
            {
                zoomInput.ZoomDeltaRequested += ApplyZoomDelta;
            }
            cameraPan?.SetFrameDriver(this);
        }

        private void OnDisable()
        {
            if (zoomInput != null)
            {
                zoomInput.ZoomDeltaRequested -= ApplyZoomDelta;
            }
            cameraPan?.SetFrameDriver(null);
            cameraPan?.DiscardQueuedPixelPan();
        }

        private void LateUpdate()
        {
            Tick(Time.unscaledDeltaTime);
        }

        private void Tick(float deltaTime)
        {
            if (!initialized)
            {
                return;
            }

            // 시작 생성의 연속 이벤트를 모두 모은 뒤 실제 Canvas layout으로 최초 화면을 정한다.
            if (initialFramingPending)
            {
                Canvas.ForceUpdateCanvases();
            }

            if (!TryGetUsableViewport(out var viewport)
                || (initialFramingPending && !TryApplyInitialFraming(viewport)))
            {
                cameraPan?.DiscardQueuedPixelPan();
                return;
            }

            RefreshDynamicRange(viewport);
            var framingReady = true;
            if (hintFramingPending)
            {
                hintFramingPending = !TryApplyFixedCenterFraming(pendingHintBounds, viewport);
                framingReady = !hintFramingPending;
            }
            if (revealCompletionPending)
            {
                framingReady &= TryApplyFixedCenterFraming(revealBounds, viewport);
            }
            if (!framingReady)
            {
                // 화면은 유효하므로 사용자가 중심을 옮겨 불가능한 fixed fit을 복구할 수 있다.
                // 자동 Lens/위치 이동과 완료만 보류하고 다음 tick에 다시 판정한다.
                cameraPan?.ApplyQueuedPixelPan();
                return;
            }

            SmoothLens(deltaTime);
            CorrectManualPosition(viewport, deltaTime);
            cameraPan?.ApplyQueuedPixelPan();
            CompleteRevealIfSettled();
        }

        private void SmoothLens(float deltaTime)
        {
            var current = cinemachineCamera.Lens.OrthographicSize;
            if (Mathf.Approximately(current, targetOrthographicSize))
            {
                return;
            }

            var next = zoomSmoothing <= 0f
                ? targetOrthographicSize
                : Mathf.SmoothDamp(current, targetOrthographicSize, ref zoomVelocity,
                    zoomSmoothing, Mathf.Infinity, Mathf.Max(0f, deltaTime));
            if (Mathf.Abs(next - targetOrthographicSize) <= 0.001f)
            {
                next = targetOrthographicSize;
                zoomVelocity = 0f;
            }
            SetLens(next);
        }

        private void CorrectManualPosition(UsableViewport viewport, float deltaTime)
        {
            if (!manualCorrectionPending || cameraPan == null
                || !cameraPan.TryGetCorrectionTarget(viewport, out var target))
            {
                return;
            }

            var position = cinemachineCamera.transform.position;
            var current = new Vector2(position.x, position.y);
            // 매 tick의 최근접 안전 위치까지만 보정한다. Lens보다 늦게 남은 오차도 마무리한다.
            var next = zoomSmoothing <= 0f
                ? target
                : Vector2.SmoothDamp(current, target, ref correctionVelocity,
                    zoomSmoothing, Mathf.Infinity, Mathf.Max(0f, deltaTime));
            next.x = Mathf.Clamp(next.x, Mathf.Min(current.x, target.x), Mathf.Max(current.x, target.x));
            next.y = Mathf.Clamp(next.y, Mathf.Min(current.y, target.y), Mathf.Max(current.y, target.y));
            if ((next - target).sqrMagnitude <= 0.000001f)
            {
                next = target;
                correctionVelocity = Vector2.zero;
                if (Mathf.Approximately(cinemachineCamera.Lens.OrthographicSize, targetOrthographicSize))
                {
                    manualCorrectionPending = false;
                }
            }
            SetPosition(new Vector3(next.x, next.y, position.z));
        }

        public void ApplyZoomDelta(float sizeDelta)
        {
            if (!initialized || sizeDelta == 0f || float.IsNaN(sizeDelta) || float.IsInfinity(sizeDelta)
                || initialFramingPending || !TryGetUsableViewport(out var viewport))
            {
                return;
            }

            RefreshDynamicRange(viewport);
            var nextTarget = zoomRange.Clamp(targetOrthographicSize + sizeDelta);
            if (Mathf.Approximately(nextTarget, targetOrthographicSize))
            {
                return;
            }

            targetOrthographicSize = nextTarget;
            // Reveal/대기 중 Hint의 위치 보존 계약은 수동 입력과 겹쳐도 우선한다.
            manualCorrectionPending = !revealCompletionPending && !hintFramingPending;
            CameraMotionStarted?.Invoke();
        }

        public void RecomputeDynamicMaxOrthographicSize(RoomBounds2D bounds)
        {
            if (!bounds.IsValid)
            {
                throw new ArgumentException("House bounds must be valid.", nameof(bounds));
            }

            houseBounds = bounds;
            hasHouseBounds = true;
            if (initialized && TryGetUsableViewport(out var viewport))
            {
                RefreshDynamicRange(viewport);
            }
        }

        private void RefreshDynamicRange(UsableViewport viewport)
        {
            if (hasHouseBounds && viewport.TryComputeInitialFit(houseBounds, outputCamera.aspect,
                houseFramingMargin, minOrthographicSize, out var houseMax, out _))
            {
                zoomRange = new ZoomRange(minOrthographicSize, Mathf.Max(houseMax, targetOrthographicSize));
            }
        }

        // 최초 tick 전에는 마지막 visible bounds로 덮어쓰며, 완료 후 호출은 수락하지 않는다.
        public bool QueueInitialFraming(RoomBounds2D visibleBounds)
        {
            if (initialFramingCompleted)
            {
                return false;
            }
            if (!visibleBounds.IsValid)
            {
                throw new ArgumentException("Visible bounds must be valid.", nameof(visibleBounds));
            }

            initialVisibleBounds = visibleBounds;
            initialFramingPending = true;
            hintFramingPending = false;
            CancelManualCorrection();
            return true;
        }

        private bool TryApplyInitialFraming(UsableViewport viewport)
        {
            if (!viewport.TryComputeInitialFit(initialVisibleBounds, outputCamera.aspect,
                houseFramingMargin, minOrthographicSize, out var size, out var center))
            {
                return false;
            }

            var previous = cinemachineCamera.transform.position;
            var position = new Vector3(center.X, center.Y, previous.z);
            var changed = previous != position || !Mathf.Approximately(cinemachineCamera.Lens.OrthographicSize, size);
            targetOrthographicSize = size;
            zoomVelocity = 0f;
            zoomRange = new ZoomRange(minOrthographicSize, size);
            CancelManualCorrection();
            SetLens(size);
            SetPosition(position);
            initialFramingPending = false;
            initialFramingCompleted = true;
            if (changed)
            {
                CameraMotionStarted?.Invoke();
            }
            return true;
        }

        public void RequestHintFraming(RoomBounds2D hintBounds)
        {
            if (!initialized)
            {
                return;
            }

            CancelManualCorrection();
            // 수동 zoom-in의 잔여 속도가 자동 zoom-out 첫 tick을 거꾸로 움직이지 않게 한다.
            zoomVelocity = 0f;
            pendingHintBounds = hintBounds;
            hintFramingPending = true;
            if (!initialFramingPending && TryGetUsableViewport(out var viewport))
            {
                hintFramingPending = !TryApplyFixedCenterFraming(hintBounds, viewport);
            }
        }

        public void RequestRoomReveal(RoomBounds2D roomBounds)
        {
            if (!initialized)
            {
                return;
            }

            CancelManualCorrection();
            zoomVelocity = 0f;
            revealBounds = roomBounds;
            revealCompletionPending = true;
            CompleteRevealIfSettled();
        }

        private bool TryApplyFixedCenterFraming(RoomBounds2D bounds, UsableViewport viewport)
        {
            var position = cinemachineCamera.transform.position;
            // Brain 반영 전의 output Camera 대신 authoritative 가상 카메라 중심/Lens를 사용한다.
            var baseline = Mathf.Max(targetOrthographicSize, cinemachineCamera.Lens.OrthographicSize);
            // 자동 Hint/Reveal은 추가 여백 없이 방 전체가 보이는 최소 크기까지만 넓힌다.
            if (!viewport.TryComputeFixedCenterFit(bounds, new Point2D(position.x, position.y),
                outputCamera.aspect, 0f, baseline, out var size))
            {
                return false;
            }

            if (!Mathf.Approximately(targetOrthographicSize, size))
            {
                targetOrthographicSize = size;
                CameraMotionStarted?.Invoke();
            }
            if (targetOrthographicSize > zoomRange.Max)
            {
                zoomRange = new ZoomRange(zoomRange.Min, targetOrthographicSize);
            }
            return true;
        }

        private void CompleteRevealIfSettled()
        {
            if (!revealCompletionPending || initialFramingPending || hintFramingPending
                || !TryGetUsableViewport(out var viewport)
                || !TryApplyFixedCenterFraming(revealBounds, viewport)
                || !Mathf.Approximately(cinemachineCamera.Lens.OrthographicSize, targetOrthographicSize))
            {
                return;
            }

            revealCompletionPending = false;
            CameraRevealCompleted?.Invoke();
        }

        private void CancelManualCorrection()
        {
            manualCorrectionPending = false;
            correctionVelocity = Vector2.zero;
        }

        private bool TryGetUsableViewport(out UsableViewport viewport)
        {
            if (cameraPan != null)
            {
                return cameraPan.TryGetUsableViewport(out viewport);
            }

            viewport = default;
            var pixels = outputCamera.pixelRect;
            return IsPositiveFinite(pixels.width) && IsPositiveFinite(pixels.height)
                && IsPositiveFinite(outputCamera.aspect);
        }

        private void SetLens(float size)
        {
            cinemachineCamera.Lens.OrthographicSize = size;
            if (Time.timeScale == 0f)
            {
                outputCamera.orthographicSize = size;
            }
        }

        private void SetPosition(Vector3 position)
        {
            cinemachineCamera.transform.position = position;
            if (Time.timeScale == 0f)
            {
                outputCamera.transform.position = position;
            }
        }

        private static bool IsPositiveFinite(float value) =>
            value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
