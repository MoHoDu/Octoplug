using UnityEngine;
using UnityEngine.InputSystem;

namespace Octoplug.Power.Input
{
    /// <summary>
    /// Mouse drag input for a PowerStrip Head. The active modules' explicit,
    /// Inspector-sized child BoxCollider2D geometry is the hit-test authority;
    /// Renderer bounds are never used. Eligibility is checked once on pointer
    /// down and capture remains held until pointer up.
    /// </summary>
    public class HeadDragInput : MonoBehaviour
    {
        [SerializeField]
        private SocketModuleLayout socketLayout;

        public event System.Action<Vector2> DragStarted;
        public event System.Action<Vector2> Dragged;
        public event System.Action DragEnded;

        public bool IsDragging { get; private set; }

        private PowerStrip owner;

        internal bool CanStartDragAt(Vector2 worldPos)
        {
            return ContainsPoint(worldPos) && IsOwnerDisconnected();
        }

        internal bool ContainsPoint(Vector2 worldPos)
        {
            return isActiveAndEnabled
                && owner != null
                && socketLayout != null
                && socketLayout.ContainsPoint(owner.ActiveSocketCount, worldPos);
        }

        internal float SqrDistanceToHitCenter(Vector2 worldPos)
        {
            return owner != null && socketLayout != null
                ? socketLayout.SqrDistanceToHitCenter(owner.ActiveSocketCount, worldPos)
                : float.PositiveInfinity;
        }

        private void Awake()
        {
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

            if (mouse.leftButton.isPressed && TryGetPointerWorldPosition(out var dragPos))
            {
                Dragged?.Invoke(dragPos);
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

            foreach (var socket in owner.ActiveSockets)
            {
                if (socket.IsConnected)
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
