using UnityEngine;
using UnityEngine.InputSystem;

namespace Octoplug.Power.Input
{
    /// <summary>
    /// Mouse drag input for a PowerStrip's own "Head" collider — deliberately
    /// the same detection idiom as <see cref="PlugDragInput"/> (reads the
    /// pointer through <see cref="Mouse"/>, hit-tests this GameObject's own
    /// Collider2D directly): the explicit, Inspector-sized Collider2D is the
    /// single source of truth for "what did the pointer hit," exactly like
    /// every other interaction target in this project (Plug, Product). No
    /// runtime bounds/padding computation from a Renderer — Size/Offset are
    /// tuned by hand in the Prefab, the same way a Plug or Product's hit
    /// collider already is.
    ///
    /// Eligibility ("can this actually be grabbed?") is checked only once,
    /// at Pointer Down (<see cref="CanStartDragAt"/>) — never re-evaluated
    /// for the rest of the drag. Once <see cref="PointerInteractionResolver"/>
    /// captures this input it is held unconditionally until Pointer Up;
    /// nothing here can spontaneously release it mid-drag.
    ///
    /// Kept as a separate, distinct capability from Plug-drag: only a
    /// PowerStrip's Head is draggable this way — Wall Outlet and Product
    /// never get this component — so a Head grab is never misreported as a
    /// Plug grab to <see cref="Octoplug.Power.UI.ProductTooltipController"/>
    /// or <see cref="Octoplug.Power.UI.ProductClickInput"/>. Reports pointer
    /// world-space position only; placement legality is
    /// <see cref="Octoplug.Power.Cable.PowerStripHeadController"/>'s job.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class HeadDragInput : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The Head's own click/drag hit area. Defaults to this GameObject's own Collider2D. Size/Offset are hand-tuned per Prefab — never computed from a Renderer at runtime.")]
        private Collider2D hitCollider;

        public event System.Action<Vector2> DragStarted;
        public event System.Action<Vector2> Dragged;
        public event System.Action DragEnded;

        public bool IsDragging { get; private set; }

        private PowerStrip owner;

        /// <summary>
        /// Pointer-Down-only eligibility check: this Head is a valid capture
        /// candidate iff the pointer is over its hit collider AND the owning
        /// strip is fully disconnected right now. Never called again for the
        /// remainder of an active drag.
        /// </summary>
        internal bool CanStartDragAt(Vector2 worldPos)
        {
            return ContainsPoint(worldPos) && IsOwnerDisconnected();
        }

        internal bool ContainsPoint(Vector2 worldPos)
        {
            return isActiveAndEnabled && hitCollider != null && hitCollider.OverlapPoint(worldPos);
        }

        internal float SqrDistanceToHitCenter(Vector2 worldPos)
        {
            return hitCollider != null
                ? ((Vector2)hitCollider.bounds.center - worldPos).sqrMagnitude
                : float.PositiveInfinity;
        }

        private void Awake()
        {
            if (hitCollider == null)
            {
                hitCollider = GetComponent<Collider2D>();
            }

            owner = GetComponentInParent<PowerStrip>();
        }

        private void OnEnable()
        {
            PointerInteractionResolver.Register(this);
        }

        private void OnDisable()
        {
            PointerInteractionResolver.Unregister(this);
            IsDragging = false;
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
                    && PointerInteractionResolver.TryCapture(this, downPos))
                {
                    IsDragging = true;
                    DragStarted?.Invoke(downPos);
                }

                return;
            }

            // Once captured, held unconditionally until Pointer Up — no
            // per-frame eligibility/ownership re-check here. A previous
            // version re-ran IsOwnerDisconnected() every dragged frame and
            // silently released capture the instant it returned false; that
            // is exactly the class of "capture drops mid-drag" bug this
            // design rules out by construction (eligibility is a Pointer
            // Down-only decision — see CanStartDragAt/OnDragStarted).
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
                PointerInteractionResolver.Release(this);
                DragEnded?.Invoke();
            }
        }

        private bool IsOwnerDisconnected()
        {
            if (owner == null)
            {
                return false;
            }

            var plug = owner.Cable != null ? owner.Cable.Plug : null;
            if (plug == null || plug.IsConnected)
            {
                return false;
            }

            foreach (var socket in owner.Sockets)
            {
                if (socket == null || socket.IsConnected)
                {
                    return false;
                }
            }

            return true;
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
