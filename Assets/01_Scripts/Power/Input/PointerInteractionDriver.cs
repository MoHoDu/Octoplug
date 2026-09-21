using UnityEngine;
using UnityEngine.InputSystem;

namespace Octoplug.Power.Input
{
    /// <summary>
    /// Central pointer lifecycle driver. Resolves ownership once on pointer-down
    /// and clears every capture after pointer-up consumers have run, including
    /// non-draggable UI, Product, Socket, and empty-space captures.
    /// </summary>
    public sealed class PointerInteractionDriver : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateRuntimeInstance()
        {
            var instance = new GameObject(nameof(PointerInteractionDriver));
            DontDestroyOnLoad(instance);
            instance.AddComponent<PointerInteractionDriver>();
        }

        private void Update()
        {
            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
            {
                return;
            }

            if (TryGetPointerWorldPosition(out var worldPos))
            {
                PointerInteractionResolver.BeginPointerDown(worldPos);
            }
        }

        private void LateUpdate()
        {
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasReleasedThisFrame)
            {
                PointerInteractionResolver.EndPointerGesture();
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
