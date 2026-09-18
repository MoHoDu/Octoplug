using System.Collections.Generic;
using Octoplug.Power;
using Octoplug.Power.Input;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Octoplug.CameraFraming.Unity
{
    /// <summary>
    /// Resolves whether a Pointer Down belongs to UI or an explicit gameplay interaction. Broad Room
    /// colliders are deliberately ignored, so an arbitrary physics hit does not make the world non-empty.
    /// </summary>
    public sealed class CameraPanInteractionResolver : MonoBehaviour
    {
        [SerializeField]
        private Camera outputCamera;

        private readonly List<RaycastResult> uiResults = new();

        public bool IsReserved(Vector2 screenPosition)
        {
            if (IsOverUi(screenPosition) || !TryScreenToWorld(screenPosition, out var worldPosition))
            {
                return true;
            }

            var hits = Physics2D.OverlapPointAll(worldPosition);
            for (var i = 0; i < hits.Length; i++)
            {
                if (HasExplicitInteraction(hits[i]))
                {
                    return true;
                }
            }

            return IsInsidePowerStripVisual(worldPosition);
        }

        private bool IsOverUi(Vector2 screenPosition)
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                return false;
            }

            uiResults.Clear();
            eventSystem.RaycastAll(
                new PointerEventData(eventSystem)
                {
                    position = screenPosition
                },
                uiResults);
            return uiResults.Count > 0;
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

        private static bool HasExplicitInteraction(Collider2D hit)
        {
            return hit.GetComponentInParent<PlugDragInput>() != null
                || hit.GetComponentInParent<PlugConnector>() != null
                || hit.GetComponentInParent<SocketConnector>() != null
                || hit.GetComponentInParent<PowerStrip>() != null
                || hit.GetComponentInParent<ApplianceSource>() != null
                || hit.GetComponentInParent<CameraPanBlocker>() != null;
        }

        private static bool IsInsidePowerStripVisual(Vector2 worldPosition)
        {
            var powerStrips = FindObjectsByType<PowerStrip>(FindObjectsSortMode.None);
            for (var stripIndex = 0; stripIndex < powerStrips.Length; stripIndex++)
            {
                var renderers = powerStrips[stripIndex].GetComponentsInChildren<Renderer>();
                for (var rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
                {
                    var renderer = renderers[rendererIndex];
                    if (!renderer.enabled || renderer.GetComponentInParent<CableInfo>() != null)
                    {
                        continue;
                    }

                    var bounds = renderer.bounds;
                    if (worldPosition.x >= bounds.min.x && worldPosition.x <= bounds.max.x
                        && worldPosition.y >= bounds.min.y && worldPosition.y <= bounds.max.y)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
