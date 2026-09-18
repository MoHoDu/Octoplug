using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Octoplug.Power.Input
{
    /// <summary>
    /// Mouse drag input for the existing Plug collider. Only detects and
    /// reports pointer world-space position on a Z=0 plane; it makes no
    /// decision about where the plug is allowed to go — that is Cable
    /// routing's job (see <see cref="Octoplug.Power.Cable.CableRoutingController"/>).
    ///
    /// Two things ruled out the legacy OnMouseDown/Drag/Up messages and the
    /// legacy <c>UnityEngine.Input</c> class here:
    /// 1. OnMouseDown's single-hit 2D ray pick has no defined tie-break
    ///    between same-Z overlapping colliders — RoomArea's full-floor
    ///    trigger sits under every Plug and won, so it never reached the
    ///    (much smaller) Plug collider.
    /// 2. This project's Active Input Handling is "Input System Package
    ///    (New)" (`ProjectSettings/ProjectSettings.asset: activeInputHandler: 1`),
    ///    under which <c>UnityEngine.Input</c> throws at runtime.
    ///
    /// So this reads the pointer through <see cref="Mouse"/> and hit-tests
    /// this GameObject's own Collider2D directly, sidestepping both issues.
    /// Mouse-first; swapping the pointer source (e.g. to a touch position)
    /// is the only change Touch support would need — Routing/Cable code is
    /// untouched either way.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class PlugDragInput : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Defaults to this GameObject's own Collider2D. The pointer must be over this collider to start a drag.")]
        private Collider2D hitCollider;

        public event Action DragStarted;
        public event Action<Vector2> Dragged;
        public event Action DragEnded;

        /// <summary>
        /// Raised whenever any Plug (any Product) starts a drag — used by
        /// <see cref="Octoplug.Power.UI.ProductTooltipController"/> to close
        /// itself the instant a Plug is grabbed, without holding a direct
        /// reference to every Plug or Product.
        /// </summary>
        public static event Action AnyDragStarted;

        public bool IsDragging { get; private set; }

        private static readonly List<PlugDragInput> ActiveInstances = new();

        /// <summary>
        /// Whether <paramref name="worldPos"/> is over any enabled Plug's
        /// hit collider — used by
        /// <see cref="Octoplug.Power.UI.ProductClickInput"/> to give Plug
        /// dragging priority over Product-tooltip clicks when the two
        /// visually overlap, so grabbing a Plug is never misread as
        /// clicking the Product it sits on.
        /// </summary>
        public static bool IsPointerOverAnyPlug(Vector2 worldPos)
        {
            foreach (var instance in ActiveInstances)
            {
                if (instance.hitCollider != null && instance.hitCollider.OverlapPoint(worldPos))
                {
                    return true;
                }
            }

            return false;
        }

        private void Awake()
        {
            if (hitCollider == null)
            {
                hitCollider = GetComponent<Collider2D>();
            }
        }

        private void OnEnable()
        {
            ActiveInstances.Add(this);
        }

        private void OnDisable()
        {
            ActiveInstances.Remove(this);
        }

        private void Update()
        {
            var mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            if (!IsDragging)
            {
                if (mouse.leftButton.wasPressedThisFrame
                    && TryGetPointerWorldPosition(out var downPos)
                    && hitCollider != null
                    && hitCollider.OverlapPoint(downPos))
                {
                    IsDragging = true;
                    DragStarted?.Invoke();
                    AnyDragStarted?.Invoke();
                }

                return;
            }

            if (mouse.leftButton.isPressed)
            {
                if (TryGetPointerWorldPosition(out var dragPos))
                {
                    Dragged?.Invoke(dragPos);
                }
            }

            if (mouse.leftButton.wasReleasedThisFrame)
            {
                IsDragging = false;
                DragEnded?.Invoke();
            }
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
