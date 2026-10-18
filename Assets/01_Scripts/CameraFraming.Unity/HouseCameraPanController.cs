using System;
using Octoplug.RoomGeneration;
using Unity.Cinemachine;
using UnityEngine;

namespace Octoplug.CameraFraming.Unity
{
    /// <summary>현재 가용 화면과 Lens를 기준으로 Cinemachine 카메라의 Pan을 제한한다.</summary>
    [DefaultExecutionOrder(0)]
    public sealed class HouseCameraPanController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private CinemachineCamera cinemachineCamera;

        [SerializeField]
        private Camera outputCamera;

        [SerializeField]
        private CameraPanInputReader panInput;

        [Tooltip("화면 상단을 가리는 HUD 전체 영역. 미지정하면 상단을 제외하지 않습니다.")]
        [SerializeField]
        private RectTransform topHudArea;

        [Tooltip("화면 하단을 가리는 주민 UI 전체 영역. 미지정하면 하단을 제외하지 않습니다.")]
        [SerializeField]
        private RectTransform bottomResidentsArea;

        [Header("[Pan] Boundary")]
        [Tooltip("현재 보이는 집과 Hint bounds 바깥에 허용하는 이동 여백(world units).")]
        [SerializeField]
        private float panBoundaryMargin = 2f;

        private RoomBounds2D houseBounds;
        private bool hasHouseBounds;
        private readonly Vector3[] hudCorners = new Vector3[4];
        private Vector2 queuedPixelDelta;
        private HouseCameraZoomController frameDriver;

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
                panInput.PixelPanDeltaRequested += QueuePixelPanDelta;
            }
        }

        private void OnDisable()
        {
            if (panInput != null)
            {
                panInput.PixelPanDeltaRequested -= QueuePixelPanDelta;
            }
            queuedPixelDelta = Vector2.zero;
        }

        private void LateUpdate()
        {
            // Zoom 참조가 없는 기존 사용처도 Pan 단독으로 동작한다.
            if (frameDriver == null || !frameDriver.isActiveAndEnabled)
            {
                ApplyQueuedPixelPan();
            }
        }

        internal void SetFrameDriver(HouseCameraZoomController driver)
        {
            frameDriver = driver;
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

        public void QueuePixelPanDelta(Vector2 pixelDelta, float sensitivity)
        {
            if (!IsFinite(pixelDelta.x) || !IsFinite(pixelDelta.y)
                || !IsFinite(sensitivity) || sensitivity < 0f)
            {
                return;
            }

            queuedPixelDelta += pixelDelta * sensitivity;
        }

        public void ApplyQueuedPixelPan()
        {
            var pixels = queuedPixelDelta;
            queuedPixelDelta = Vector2.zero;
            if (pixels == Vector2.zero || !TryGetUsableViewport(out var viewport))
            {
                return;
            }

            var pixelRect = outputCamera.pixelRect;
            var delta = PanDeltaMapper.Map(new Point2D(pixels.x, pixels.y),
                cinemachineCamera.Lens.OrthographicSize, outputCamera.aspect,
                pixelRect.width, pixelRect.height, 1f);
            ApplyPanDelta(new Vector2(delta.X, delta.Y), viewport);
        }

        internal void DiscardQueuedPixelPan()
        {
            queuedPixelDelta = Vector2.zero;
        }

        // 외부 world-delta 호출은 기존처럼 즉시 반영하며 production 입력만 지연 변환한다.
        public void ApplyPanDelta(Vector2 worldDelta)
        {
            if (TryGetUsableViewport(out var viewport))
            {
                ApplyPanDelta(worldDelta, viewport);
            }
        }

        private void ApplyPanDelta(Vector2 worldDelta, UsableViewport viewport)
        {
            if (worldDelta == Vector2.zero || !IsFinite(worldDelta.x) || !IsFinite(worldDelta.y)
                || !TryGetRange(viewport, out var range))
            {
                return;
            }

            var current = cinemachineCamera.transform.position;
            var clamped = range.ClampDelta(new Point2D(current.x, current.y),
                new Point2D(worldDelta.x, worldDelta.y));
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

        internal bool TryGetCorrectionTarget(UsableViewport viewport, out Vector2 target)
        {
            target = default;
            if (!TryGetRange(viewport, out var range))
            {
                return false;
            }

            var current = cinemachineCamera.transform.position;
            var closest = range.ClosestSafeCoordinate(new Point2D(current.x, current.y));
            target = new Vector2(closest.X, closest.Y);
            return true;
        }

        private bool TryGetRange(UsableViewport viewport, out PanCenterRange range)
        {
            range = default;
            if (!hasHouseBounds || !viewport.IsValid || cinemachineCamera == null
                || outputCamera == null || !IsPositiveFinite(outputCamera.aspect)
                || !IsPositiveFinite(cinemachineCamera.Lens.OrthographicSize))
            {
                return false;
            }

            range = PanBoundaryCalculator.Compute(houseBounds,
                cinemachineCamera.Lens.OrthographicSize, outputCamera.aspect,
                panBoundaryMargin, viewport.TopInsetFraction, viewport.BottomInsetFraction);
            return true;
        }

        public bool TryGetUsableViewport(out UsableViewport usableViewport)
        {
            usableViewport = default;
            if (outputCamera == null)
            {
                return false;
            }

            var viewport = outputCamera.pixelRect;
            if (!IsPositiveFinite(viewport.width) || !IsPositiveFinite(viewport.height)
                || !IsPositiveFinite(outputCamera.aspect))
            {
                return false;
            }

            usableViewport = new UsableViewport(
                GetInsetFraction(topHudArea, viewport, true),
                GetInsetFraction(bottomResidentsArea, viewport, false));
            return usableViewport.IsValid;
        }

        private float GetInsetFraction(RectTransform area, Rect viewport, bool fromTop)
        {
            if (area == null || !area.gameObject.activeInHierarchy)
            {
                return 0f;
            }

            var canvas = area.GetComponentInParent<Canvas>();
            if (canvas == null || !canvas.isActiveAndEnabled || !canvas.rootCanvas.isActiveAndEnabled)
            {
                return 0f;
            }

            var rootCanvas = canvas.rootCanvas;
            var uiCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : rootCanvas.worldCamera;
            if (rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay && uiCamera == null)
            {
                return 0f;
            }

            area.GetWorldCorners(hudCorners);
            var min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            var max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            for (var i = 0; i < hudCorners.Length; i++)
            {
                var point = RectTransformUtility.WorldToScreenPoint(uiCamera, hudCorners[i]);
                if (!IsFinite(point.x) || !IsFinite(point.y))
                {
                    return float.NaN;
                }
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }

            // 지정한 가장자리에 닿은 UI와 실제 출력 viewport의 교집합만 제외한다.
            if (max.x <= viewport.xMin || min.x >= viewport.xMax)
            {
                return 0f;
            }

            if (fromTop)
            {
                return min.y >= viewport.yMax || max.y < viewport.yMax
                    ? 0f
                    : Mathf.Clamp01((viewport.yMax - Mathf.Max(viewport.yMin, min.y)) / viewport.height);
            }

            return max.y <= viewport.yMin || min.y > viewport.yMin
                ? 0f
                : Mathf.Clamp01((Mathf.Min(viewport.yMax, max.y) - viewport.yMin) / viewport.height);
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static bool IsPositiveFinite(float value) => IsFinite(value) && value > 0f;
    }
}
