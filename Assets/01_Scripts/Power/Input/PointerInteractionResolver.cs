using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Octoplug.Power.Input
{
    /// <summary>
    /// Resolves one interaction target on pointer-down and keeps that target
    /// captured until pointer-up. Candidate order is UI, Plug, Product, Socket,
    /// eligible PowerStrip Head, explicit blocker, then empty-world Pan. Candidates
    /// within one class are ordered by distance and instance ID, never callback/update order.
    /// </summary>
    public static class PointerInteractionResolver
    {
        private static readonly List<PlugDragInput> Plugs = new();
        private static readonly List<HeadDragInput> Heads = new();
        private static readonly List<Collider2D> Blockers = new();
        private static readonly List<RaycastResult> UiRaycastResults = new();
        private static object capturedTarget;
        private static int captureFrame = -1;

        public static void Register(PlugDragInput input)
        {
            if (input != null && !Plugs.Contains(input))
            {
                Plugs.Add(input);
            }
        }

        public static void Unregister(PlugDragInput input)
        {
            Plugs.Remove(input);
            if (ReferenceEquals(capturedTarget, input))
            {
                capturedTarget = null;
            }
        }

        public static void Register(HeadDragInput input)
        {
            if (input != null && !Heads.Contains(input))
            {
                Heads.Add(input);
            }
        }

        public static void Unregister(HeadDragInput input)
        {
            Heads.Remove(input);
            if (ReferenceEquals(capturedTarget, input))
            {
                capturedTarget = null;
            }
        }

        public static void RegisterBlocker(Collider2D blocker)
        {
            if (blocker != null && !Blockers.Contains(blocker))
            {
                Blockers.Add(blocker);
            }
        }

        public static void UnregisterBlocker(Collider2D blocker)
        {
            Blockers.Remove(blocker);
            if (ReferenceEquals(capturedTarget, blocker))
            {
                capturedTarget = null;
            }
        }

        /// <summary>
        /// Captures the globally highest-priority candidate under the pointer.
        /// Every caller gets the same answer regardless of its Update order.
        /// </summary>
        public static void BeginPointerDown(Vector2 worldPos)
        {
            BeginPointerDown(worldPos, Mouse.current?.position.ReadValue() ?? Vector2.zero);
        }

        private static void BeginPointerDown(Vector2 worldPos, Vector2 screenPos)
        {
            if (capturedTarget != null && capturedTarget is UnityEngine.Object unityTarget && unityTarget == null)
            {
                capturedTarget = null;
            }

            if (capturedTarget != null || captureFrame == Time.frameCount)
            {
                return;
            }

            captureFrame = Time.frameCount;
            if (IsPointerOverUi(screenPos))
            {
                capturedTarget = typeof(EventSystem);
                return;
            }

            capturedTarget = ResolvePlug(worldPos);
            capturedTarget ??= Octoplug.Power.UI.ProductClickInput.TryGetProductAt(worldPos);
            capturedTarget ??= ResolveSocket(worldPos);
            capturedTarget ??= ResolveHead(worldPos);
            capturedTarget ??= ResolveBlocker(worldPos);
            capturedTarget ??= typeof(PointerInteractionResolver);
        }

        public static bool TryCapture(object requester, Vector2 worldPos)
        {
            BeginPointerDown(worldPos);
            return ReferenceEquals(capturedTarget, requester);
        }

        public static void Release(object requester)
        {
            if (ReferenceEquals(capturedTarget, requester))
            {
                capturedTarget = null;
            }
        }

        public static void ReleasePointer()
        {
            capturedTarget = null;
        }

        public static bool IsPointerOverPlug(Vector2 worldPos)
        {
            return ResolvePlug(worldPos) != null;
        }

        public static ApplianceSource CapturedProduct => capturedTarget as ApplianceSource;

        public static bool IsEmptyCapture => ReferenceEquals(capturedTarget, typeof(PointerInteractionResolver));

        public static bool TryCaptureEmptyWorld(object requester, Vector2 worldPos, Vector2 screenPos)
        {
            BeginPointerDown(worldPos, screenPos);
            if (!IsEmptyCapture)
            {
                return false;
            }

            capturedTarget = requester;
            return true;
        }

        private static PlugDragInput ResolvePlug(Vector2 worldPos)
        {
            PlugDragInput best = null;
            var bestDistance = float.PositiveInfinity;
            var bestInstanceId = int.MaxValue;
            foreach (var plug in Plugs)
            {
                if (plug == null || !plug.ContainsPoint(worldPos))
                {
                    continue;
                }

                var distance = plug.SqrDistanceToHitCenter(worldPos);
                var instanceId = plug.GetInstanceID();
                if (IsBetterCandidate(distance, instanceId, bestDistance, bestInstanceId))
                {
                    best = plug;
                    bestDistance = distance;
                    bestInstanceId = instanceId;
                }
            }

            return best;
        }

        private static HeadDragInput ResolveHead(Vector2 worldPos)
        {
            HeadDragInput best = null;
            var bestDistance = float.PositiveInfinity;
            var bestInstanceId = int.MaxValue;
            foreach (var head in Heads)
            {
                if (head == null || !head.CanStartDragAt(worldPos))
                {
                    continue;
                }

                var distance = head.SqrDistanceToHitCenter(worldPos);
                var instanceId = head.GetInstanceID();
                if (IsBetterCandidate(distance, instanceId, bestDistance, bestInstanceId))
                {
                    best = head;
                    bestDistance = distance;
                    bestInstanceId = instanceId;
                }
            }

            return best;
        }

        private static Collider2D ResolveBlocker(Vector2 worldPos)
        {
            Collider2D best = null;
            var bestDistance = float.PositiveInfinity;
            var bestInstanceId = int.MaxValue;
            foreach (var blocker in Blockers)
            {
                if (blocker == null || !blocker.isActiveAndEnabled || !blocker.OverlapPoint(worldPos))
                {
                    continue;
                }

                var distance = ((Vector2)blocker.bounds.center - worldPos).sqrMagnitude;
                var instanceId = blocker.GetInstanceID();
                if (IsBetterCandidate(distance, instanceId, bestDistance, bestInstanceId))
                {
                    best = blocker;
                    bestDistance = distance;
                    bestInstanceId = instanceId;
                }
            }

            return best;
        }

        private static bool IsBetterCandidate(float distance, int instanceId, float bestDistance, int bestInstanceId)
        {
            return distance < bestDistance
                || (Mathf.Approximately(distance, bestDistance) && instanceId < bestInstanceId);
        }

        private static bool IsPointerOverUi(Vector2 screenPos)
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                return false;
            }

            UiRaycastResults.Clear();
            eventSystem.RaycastAll(new PointerEventData(eventSystem)
            {
                position = screenPos
            }, UiRaycastResults);

            foreach (var result in UiRaycastResults)
            {
                if (result.gameObject != null && result.gameObject.GetComponentInParent<Canvas>() != null)
                {
                    return true;
                }
            }

            return false;
        }

        private static SocketConnector ResolveSocket(Vector2 worldPos)
        {
#if UNITY_2023_1_OR_NEWER
            var sockets = UnityEngine.Object.FindObjectsByType<SocketConnector>(FindObjectsSortMode.None);
#else
            var sockets = UnityEngine.Object.FindObjectsOfType<SocketConnector>();
#endif
            SocketConnector best = null;
            var bestDistance = float.PositiveInfinity;
            var bestInstanceId = int.MaxValue;
            foreach (var socket in sockets)
            {
                if (socket == null
                    || !socket.IsActiveSocket
                    || !socket.IsPointerInInteractionArea(worldPos))
                {
                    continue;
                }

                var distance = ((Vector2)socket.transform.position - worldPos).sqrMagnitude;
                var instanceId = socket.GetInstanceID();
                if (IsBetterCandidate(distance, instanceId, bestDistance, bestInstanceId))
                {
                    best = socket;
                    bestDistance = distance;
                    bestInstanceId = instanceId;
                }
            }

            return best;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Plugs.Clear();
            Heads.Clear();
            Blockers.Clear();
            UiRaycastResults.Clear();
            capturedTarget = null;
            captureFrame = -1;
        }
    }
}
