using UnityEngine;

namespace Octoplug.Power
{
    /// <summary>
    /// Marks a single socket slot on a WallOutlet or PowerStrip (the
    /// existing "Socket" child transforms). Holds only connection state;
    /// drag/drop, validation, and power checks are implemented in a later
    /// stage (P0-1C+).
    /// </summary>
    public class SocketConnector : MonoBehaviour, IPowerConnector
    {
        [SerializeField]
        private PlugConnector connectedPlug;

        [SerializeField]
        [Tooltip("Optional authored Collider used for pointer hit-testing and magnetic acquisition sizing.")]
        private Collider2D interactionCollider;

        [SerializeField]
        [Tooltip("Optional authored Renderer used when the Socket has no interaction Collider.")]
        private Renderer interactionRenderer;

        [SerializeField]
        [Tooltip("Fallback local interaction size when no Collider or Renderer is assigned.")]
        private Vector2 fallbackInteractionSize = new(0.25f, 0.25f);

        [SerializeField]
        [Tooltip("Wall-mounted terminal Sockets accept drops only from their configured room-facing side.")]
        private bool terminalEndpoint;

        [SerializeField]
        [Tooltip("Local direction pointing from a terminal Socket into its room. Rotates with the Socket transform.")]
        private Vector2 localApproachDirection = Vector2.left;

        public bool IsConnected => connectedPlug != null;

        public Transform ConnectorTransform => transform;

        public PlugConnector ConnectedPlug => connectedPlug;

        public bool IsTerminalEndpoint => terminalEndpoint;

        public Vector2 ApproachDirection
        {
            get
            {
                var direction = transform.TransformDirection(localApproachDirection);
                return ((Vector2)direction).normalized;
            }
        }

        public Bounds InteractionBounds
        {
            get
            {
                if (interactionCollider != null)
                {
                    return interactionCollider.bounds;
                }

                if (interactionRenderer != null)
                {
                    return interactionRenderer.bounds;
                }

                var size = Vector3.Scale(
                    new Vector3(fallbackInteractionSize.x, fallbackInteractionSize.y, 0f),
                    transform.lossyScale);
                return new Bounds(transform.position, size);
            }
        }

        public float GetAcquisitionRadius(
            float visualSizeMultiplier,
            float cellSize,
            float cellSizeMultiplier)
        {
            var extents = InteractionBounds.extents;
            return Mathf.Max(extents.x, extents.y)
                * Mathf.Max(0f, visualSizeMultiplier)
                + cellSize * Mathf.Max(0f, cellSizeMultiplier);
        }

        public bool IsPointerOnApproachSide(Vector2 worldPos)
        {
            if (!terminalEndpoint)
            {
                return true;
            }

            return Vector2.Dot(
                worldPos - (Vector2)ConnectorTransform.position,
                ApproachDirection) >= 0f;
        }

        /// <summary>
        /// Whether the pointer is within this Socket's interaction region.
        /// Sockets intentionally have no required Collider2D; the renderer's
        /// authored visual bounds provide the minimal invisible exclusion area
        /// that prevents a Head drag from starting through the Socket.
        /// </summary>
        public bool IsPointerInInteractionArea(Vector2 worldPos)
        {
            if (interactionCollider != null)
            {
                return interactionCollider.OverlapPoint(worldPos);
            }

            return InteractionBounds.Contains(worldPos);
        }

        /// <summary>Raw state assignment. No capacity/length/room validation.</summary>
        public void AssignPlug(PlugConnector plug)
        {
            connectedPlug = plug;
        }

        /// <summary>Raw state clear. No side effects on the plug's own state.</summary>
        public void ClearPlug()
        {
            connectedPlug = null;
        }
    }
}
