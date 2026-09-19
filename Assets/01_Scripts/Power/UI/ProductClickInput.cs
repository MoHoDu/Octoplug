using System.Collections.Generic;
using UnityEngine;

namespace Octoplug.Power.UI
{
    /// <summary>
    /// Passive per-Product click target: just a Collider2D + the Product's
    /// ApplianceSource, self-registered in a static list. All actual mouse
    /// polling for Product clicks lives in the single
    /// <see cref="ProductTooltipController"/> (mirroring how
    /// <see cref="Octoplug.Power.Input.PlugDragInput"/> owns Plug-drag
    /// polling) so there is exactly one input owner deciding what a click
    /// hit, instead of every Product racing its own <c>Mouse.current</c>
    /// read against Plug dragging.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class ProductClickInput : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Defaults to this GameObject's own Collider2D.")]
        private Collider2D hitCollider;

        [SerializeField]
        private ApplianceSource product;

        private static readonly List<ProductClickInput> ActiveInstances = new();

        private void Awake()
        {
            if (hitCollider == null)
            {
                hitCollider = GetComponent<Collider2D>();
            }

            if (product == null)
            {
                product = GetComponentInParent<ApplianceSource>();
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

        /// <summary>The deterministic nearest Product click target under <paramref name="worldPos"/>, or null if none.</summary>
        public static ApplianceSource TryGetProductAt(Vector2 worldPos)
        {
            ProductClickInput best = null;
            var bestDistance = float.PositiveInfinity;
            var bestInstanceId = int.MaxValue;
            foreach (var instance in ActiveInstances)
            {
                if (instance == null
                    || !instance.isActiveAndEnabled
                    || instance.product == null
                    || instance.hitCollider == null
                    || !instance.hitCollider.OverlapPoint(worldPos))
                {
                    continue;
                }

                var distance = ((Vector2)instance.hitCollider.bounds.center - worldPos).sqrMagnitude;
                var instanceId = instance.GetInstanceID();
                if (distance < bestDistance
                    || (Mathf.Approximately(distance, bestDistance) && instanceId < bestInstanceId))
                {
                    best = instance;
                    bestDistance = distance;
                    bestInstanceId = instanceId;
                }
            }

            return best != null ? best.product : null;
        }
    }
}
