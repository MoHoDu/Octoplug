using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Octoplug.Power.UI
{
    /// <summary>
    /// Drives the existing `UI_ProductTooltip` prefab instance: binds one
    /// Product's 5 detail fields on click and positions the panel relative
    /// to the clicked Product's on-screen sprite bounds. Owns all
    /// Product-click mouse polling itself (mirroring
    /// <see cref="Octoplug.Power.Input.PlugDragInput"/>'s single-owner
    /// pattern) while the shared pointer resolver decides whether the click
    /// belongs to UI, a Plug, Product, Socket, eligible Head, or empty space.
    /// A captured Product shows/replaces the tooltip and an empty-space click
    /// closes it. Also closes the instant any Plug starts a drag anywhere (see
    /// <see cref="Octoplug.Power.Input.PlugDragInput.AnyDragStarted"/>).
    /// </summary>
    public class ProductTooltipController : MonoBehaviour
    {
        [SerializeField]
        private RectTransform panel;

        [SerializeField]
        private Image serviceIcon;

        [SerializeField]
        [Tooltip("The Power section's existing lightning-icon pool — toggled by count (min(watts, pool size)), not shown alongside a number.")]
        private Transform powerIconRow;

        [SerializeField]
        [Tooltip("Unused for Power (icon-count style, no number) — hidden once at Awake. Kept only because the prefab still has the node.")]
        private TextMeshProUGUI powerLabel;

        private Image[] powerIcons;
        private ApplianceSource displayedProduct;
        private CableInfo displayedCable;

        [SerializeField]
        private TextMeshProUGUI cableLabel;

        [SerializeField]
        private TextMeshProUGUI capacityLabel;

        [SerializeField]
        private TextMeshProUGUI durationLabel;

        [SerializeField]
        private UsageTypeIconLibrary iconLibrary;

        [SerializeField]
        [Tooltip("Screen-pixel gap kept between the panel and the Product's visual bounds, and between the panel and the screen edge.")]
        private float margin = 16f;

        private void Awake()
        {
            if (panel != null)
            {
                panel.gameObject.SetActive(false);
            }

            if (powerIconRow != null)
            {
                powerIcons = powerIconRow.GetComponentsInChildren<Image>(true);
            }

            if (powerLabel != null)
            {
                powerLabel.gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            Octoplug.Power.Input.PlugDragInput.AnyDragStarted += Hide;
        }

        private void OnDisable()
        {
            Octoplug.Power.Input.PlugDragInput.AnyDragStarted -= Hide;
            SetDisplayedProduct(null);
        }

        private void Update()
        {
            if (Octoplug.GameFlow.GameplayInputLock.IsLocked)
            {
                if (panel != null && panel.gameObject.activeSelf)
                {
                    Hide();
                }
                return;
            }

            var mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            if (mouse.leftButton.wasPressedThisFrame)
            {
                var screenPosition = mouse.position.ReadValue();
                var pointerInsidePanel = panel != null
                    && panel.gameObject.activeSelf
                    && RectTransformUtility.RectangleContainsScreenPoint(
                        panel,
                        screenPosition,
                        ResolveUiCamera(panel));
                if (panel != null
                    && panel.gameObject.activeSelf
                    && !pointerInsidePanel)
                {
                    Hide();
                }

                if (!pointerInsidePanel
                    && TryGetPointerWorldPosition(out var downPos))
                {
                    Octoplug.Power.Input.PointerInteractionResolver.BeginPointerDown(downPos);
                }
            }

            if (!mouse.leftButton.wasReleasedThisFrame)
            {
                return;
            }

            var product = Octoplug.Power.Input.PointerInteractionResolver.CapturedProduct;
            if (product != null)
            {
                Show(product);
            }
            else if (Octoplug.Power.Input.PointerInteractionResolver.IsEmptyCapture)
            {
                Hide();
            }
        }

        public void Show(ApplianceSource product)
        {
            if (product == null || panel == null)
            {
                return;
            }

            SetDisplayedProduct(product);
            BindContent(product);
            PositionPanel(product);
            panel.gameObject.SetActive(true);
        }

        public void Hide()
        {
            SetDisplayedProduct(null);
            if (panel != null)
            {
                panel.gameObject.SetActive(false);
            }
        }

        private void SetDisplayedProduct(ApplianceSource product)
        {
            if (displayedCable != null)
            {
                displayedCable.CableLengthChanged -= OnCableLengthChanged;
            }

            displayedProduct = product;
            displayedCable = displayedProduct != null
                ? displayedProduct.Cable
                : null;

            if (displayedCable != null)
            {
                displayedCable.CableLengthChanged += OnCableLengthChanged;
            }
        }

        private void OnCableLengthChanged(
            CableInfo changedCable,
            float oldLength,
            float newLength)
        {
            if (changedCable == displayedCable
                && panel != null
                && panel.gameObject.activeSelf
                && cableLabel != null)
            {
                cableLabel.text = $"{newLength:0.#}m";
            }
        }

        private void BindContent(ApplianceSource product)
        {
            if (serviceIcon != null && iconLibrary != null)
            {
                var firstFlag = UsageTypeUtility.EnumerateFlags(product.UsageTypes).FirstOrDefault();
                serviceIcon.sprite = iconLibrary.GetIcon(firstFlag);
            }

            if (powerIcons != null && powerIcons.Length > 0)
            {
                var rawCount = Mathf.RoundToInt(product.PowerConsumptionWatts);
                if (rawCount > powerIcons.Length)
                {
                    Debug.LogWarning($"{name}: {product.name}'s PowerConsumptionWatts ({rawCount}) exceeds the Tooltip Power icon pool ({powerIcons.Length}) — display is clamped and under-represents actual consumption. Needs either more icons in the Prefab or a lower value; see Human Setup Required.", this);
                }

                var activeCount = Mathf.Clamp(rawCount, 0, powerIcons.Length);
                for (var i = 0; i < powerIcons.Length; i++)
                {
                    powerIcons[i].gameObject.SetActive(i < activeCount);
                }
            }

            if (cableLabel != null)
            {
                var cableLength = product.Cable != null ? product.Cable.CableLength : 0f;
                cableLabel.text = $"{cableLength:0.#}m";
            }

            if (capacityLabel != null)
            {
                capacityLabel.text = product.Capacity.ToString();
            }

            if (durationLabel != null)
            {
                durationLabel.text = $"{product.SatisfactionDurationSeconds:0.#}s";
            }
        }

        /// <summary>
        /// Below the Product's sprite if it is in the screen's top half,
        /// above it if in the bottom half, always kept clear of the
        /// Product's actual on-screen visual bounds (not just its pivot
        /// point) and clamped fully on-screen. If clamping would force an
        /// overlap with the Product, the opposite side is tried instead.
        /// No manual per-Product anchor is required.
        /// </summary>
        private void PositionPanel(ApplianceSource product)
        {
            var camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            var bounds = GetScreenBounds(product, camera);
            var preferBelow = bounds.center.y > Screen.height / 2f;

            var position = ComputePosition(bounds, preferBelow);
            if (Overlaps(position, bounds))
            {
                var alternative = ComputePosition(bounds, !preferBelow);
                if (!Overlaps(alternative, bounds))
                {
                    position = alternative;
                }
            }

            panel.position = new Vector3(position.x, position.y, 0f);
        }

        /// <summary>The Product's SpriteRenderer bounds projected to screen space, or a zero-size Rect at its transform position if no sprite is found.</summary>
        private static Rect GetScreenBounds(ApplianceSource product, Camera camera)
        {
            var renderer = product.GetComponentInChildren<SpriteRenderer>();
            if (renderer == null)
            {
                var p = (Vector2)camera.WorldToScreenPoint(product.transform.position);
                return new Rect(p.x, p.y, 0f, 0f);
            }

            var b = renderer.bounds;
            var min = (Vector2)camera.WorldToScreenPoint(new Vector3(b.min.x, b.min.y, 0f));
            var max = (Vector2)camera.WorldToScreenPoint(new Vector3(b.max.x, b.max.y, 0f));
            return new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
        }

        /// <summary>Target screen position for the panel's pivot, given the Product's screen-space bounds and which side to place it on, clamped fully on-screen.</summary>
        private Vector2 ComputePosition(Rect productBounds, bool below)
        {
            var size = new Vector2(panel.rect.width * panel.lossyScale.x, panel.rect.height * panel.lossyScale.y);
            var pivot = panel.pivot;

            float targetY;
            if (below)
            {
                var topEdgeY = productBounds.yMin - margin;
                targetY = topEdgeY - (1f - pivot.y) * size.y;
            }
            else
            {
                var bottomEdgeY = productBounds.yMax + margin;
                targetY = bottomEdgeY + pivot.y * size.y;
            }

            var targetX = productBounds.center.x;

            var minX = pivot.x * size.x + margin;
            var maxX = Screen.width - (1f - pivot.x) * size.x - margin;
            var minY = pivot.y * size.y + margin;
            var maxY = Screen.height - (1f - pivot.y) * size.y - margin;

            targetX = Mathf.Clamp(targetX, Mathf.Min(minX, maxX), Mathf.Max(minX, maxX));
            targetY = Mathf.Clamp(targetY, Mathf.Min(minY, maxY), Mathf.Max(minY, maxY));

            return new Vector2(targetX, targetY);
        }

        /// <summary>Whether the panel, placed with its pivot at <paramref name="position"/>, would overlap <paramref name="productBounds"/>.</summary>
        private bool Overlaps(Vector2 position, Rect productBounds)
        {
            var size = new Vector2(panel.rect.width * panel.lossyScale.x, panel.rect.height * panel.lossyScale.y);
            var pivot = panel.pivot;

            var left = position.x - pivot.x * size.x;
            var right = left + size.x;
            var bottom = position.y - pivot.y * size.y;
            var top = bottom + size.y;

            return right > productBounds.xMin && left < productBounds.xMax
                                               && top > productBounds.yMin && bottom < productBounds.yMax;
        }

        private static Camera ResolveUiCamera(RectTransform target)
        {
            var canvas = target != null ? target.GetComponentInParent<Canvas>() : null;
            if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return null;
            }

            return canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
        }

        private static bool TryGetPointerWorldPosition(out Vector2 worldPos)
        {
            worldPos = default;
            var camera = Camera.main;
            var mouse = Mouse.current;
            if (camera == null || mouse == null)
            {
                return false;
            }

            var screenPos = mouse.position.ReadValue();
            var ray = camera.ScreenPointToRay(new Vector3(screenPos.x, screenPos.y, 0f));
            var plane = new Plane(Vector3.forward, Vector3.zero);
            if (!plane.Raycast(ray, out var distance))
            {
                return false;
            }

            worldPos = ray.GetPoint(distance);
            return true;
        }
    }
}
