using Octoplug.Power.Input;
using UnityEngine;

namespace Octoplug.CameraFraming.Unity
{
    /// <summary>Registers an explicit gameplay hit region with the authoritative pointer resolver.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class CameraPanBlocker : MonoBehaviour
    {
        private Collider2D hitCollider;

        private void Awake()
        {
            hitCollider = GetComponent<Collider2D>();
        }

        private void OnEnable()
        {
            if (hitCollider == null)
            {
                hitCollider = GetComponent<Collider2D>();
            }

            PointerInteractionResolver.RegisterBlocker(hitCollider);
        }

        private void OnDisable()
        {
            PointerInteractionResolver.UnregisterBlocker(hitCollider);
        }
    }
}
