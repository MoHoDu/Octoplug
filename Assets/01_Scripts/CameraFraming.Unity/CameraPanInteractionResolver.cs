using Octoplug.Power.Input;
using UnityEngine;

namespace Octoplug.CameraFraming.Unity
{
    /// <summary>
    /// Adapts Camera Pan to the production pointer resolver. Empty-world capture is granted only
    /// after UI, Plug, Product, Socket, PowerStrip Head, and explicit blocker priorities are resolved.
    /// </summary>
    public sealed class CameraPanInteractionResolver : MonoBehaviour
    {
        [SerializeField]
        private Camera outputCamera;

        public bool TryCaptureEmptyWorld(Vector2 screenPosition, object requester)
        {
            return TryScreenToWorld(screenPosition, out var worldPosition)
                && PointerInteractionResolver.TryCaptureEmptyWorld(requester, worldPosition, screenPosition);
        }

        public void Release(object requester)
        {
            PointerInteractionResolver.Release(requester);
        }

        private bool TryScreenToWorld(Vector2 screenPosition, out Vector2 worldPosition)
        {
            worldPosition = default;
            if (outputCamera == null)
            {
                return false;
            }

            var ray = outputCamera.ScreenPointToRay(screenPosition);
            var plane = new Plane(Vector3.forward, Vector3.zero);
            if (!plane.Raycast(ray, out var distance))
            {
                return false;
            }

            worldPosition = ray.GetPoint(distance);
            return true;
        }
    }
}
