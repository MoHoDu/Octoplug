using System;
using System.Collections.Generic;
using UnityEngine;

namespace Octoplug.Power
{
    /// <summary>
    /// Inspector-authored mapping for a persistent, variable-count Socket source.
    /// Module order is capacity order; runtime changes only activate an existing
    /// contiguous prefix and never infer identity or geometry from object names.
    /// </summary>
    public class SocketModuleLayout : MonoBehaviour
    {
        [Serializable]
        private class Module
        {
            [SerializeField]
            private SocketConnector socket;

            [SerializeField]
            private GameObject whole;

            [SerializeField]
            private GameObject end;

            [SerializeField]
            [Tooltip("Authored Body geometry present whenever this module is active.")]
            private List<BoxCollider2D> bodyColliders = new();

            [SerializeField]
            [Tooltip("Authored End geometry present only when this is the final active module.")]
            private List<BoxCollider2D> terminalColliders = new();

            public SocketConnector Socket => socket;
            public GameObject Whole => whole;
            public GameObject End => end;
            public IReadOnlyList<BoxCollider2D> BodyColliders => bodyColliders;
            public IReadOnlyList<BoxCollider2D> TerminalColliders => terminalColliders;
        }

        [SerializeField]
        [Tooltip("Whole01's authored First visual. It remains active for every valid count.")]
        private GameObject first;

        [SerializeField]
        [Tooltip("Whole01 First geometry present for every valid count.")]
        private List<BoxCollider2D> firstColliders = new();

        [SerializeField]
        private List<Module> modules = new();

        public int Capacity => modules.Count;

        public bool TryValidate(int socketCount)
        {
            if (socketCount < 1 || socketCount > modules.Count
                || first == null
                || firstColliders == null
                || firstColliders.Count == 0
                || !AreCollidersValid(firstColliders))
            {
                return false;
            }

            for (var i = 0; i < modules.Count; i++)
            {
                var module = modules[i];
                if (module == null
                    || module.Socket == null
                    || module.Whole == null
                    || module.End == null
                    || module.BodyColliders == null
                    || module.BodyColliders.Count == 0
                    || module.TerminalColliders == null
                    || module.TerminalColliders.Count == 0
                    || !AreCollidersValid(module.BodyColliders)
                    || !AreCollidersValid(module.TerminalColliders))
                {
                    return false;
                }
            }

            return true;
        }

        public SocketConnector GetSocket(int index)
        {
            return index >= 0 && index < modules.Count
                ? modules[index].Socket
                : null;
        }

        public void GetColliders(int socketCount, List<BoxCollider2D> results)
        {
            results.Clear();
            var activeCount = Mathf.Clamp(socketCount, 0, modules.Count);
            if (activeCount == 0)
            {
                return;
            }

            AddColliders(firstColliders, results);
            for (var i = 0; i < activeCount; i++)
            {
                AddColliders(modules[i].BodyColliders, results);
            }

            AddColliders(modules[activeCount - 1].TerminalColliders, results);
        }

        public bool ContainsPoint(int socketCount, Vector2 worldPosition)
        {
            var activeCount = Mathf.Clamp(socketCount, 0, modules.Count);
            if (activeCount == 0)
            {
                return false;
            }

            if (ContainsPoint(firstColliders, worldPosition))
            {
                return true;
            }

            for (var i = 0; i < activeCount; i++)
            {
                if (ContainsPoint(modules[i].BodyColliders, worldPosition))
                {
                    return true;
                }
            }

            return ContainsPoint(
                modules[activeCount - 1].TerminalColliders,
                worldPosition);
        }

        public float SqrDistanceToHitCenter(int socketCount, Vector2 worldPosition)
        {
            var activeCount = Mathf.Clamp(socketCount, 0, modules.Count);
            if (activeCount == 0)
            {
                return float.PositiveInfinity;
            }

            var best = GetSqrDistanceToHitCenter(
                firstColliders,
                worldPosition,
                float.PositiveInfinity);
            for (var i = 0; i < activeCount; i++)
            {
                best = GetSqrDistanceToHitCenter(
                    modules[i].BodyColliders,
                    worldPosition,
                    best);
            }

            return GetSqrDistanceToHitCenter(
                modules[activeCount - 1].TerminalColliders,
                worldPosition,
                best);
        }

        public void Apply(int socketCount)
        {
            var activeCount = Mathf.Clamp(socketCount, 0, modules.Count);
            first.SetActive(activeCount > 0);
            for (var i = 0; i < modules.Count; i++)
            {
                var active = i < activeCount;
                modules[i].Whole.SetActive(active);
                modules[i].Socket.gameObject.SetActive(active);
                modules[i].End.SetActive(active && i == activeCount - 1);
            }
        }

        private static bool AreCollidersValid(IReadOnlyList<BoxCollider2D> colliders)
        {
            for (var i = 0; i < colliders.Count; i++)
            {
                if (colliders[i] == null)
                {
                    return false;
                }
            }

            return true;
        }

        private static void AddColliders(
            IReadOnlyList<BoxCollider2D> source,
            List<BoxCollider2D> results)
        {
            for (var i = 0; i < source.Count; i++)
            {
                if (source[i] != null)
                {
                    results.Add(source[i]);
                }
            }
        }

        private static bool ContainsPoint(
            IReadOnlyList<BoxCollider2D> colliders,
            Vector2 worldPosition)
        {
            for (var i = 0; i < colliders.Count; i++)
            {
                if (colliders[i] != null && colliders[i].OverlapPoint(worldPosition))
                {
                    return true;
                }
            }

            return false;
        }

        private static float GetSqrDistanceToHitCenter(
            IReadOnlyList<BoxCollider2D> colliders,
            Vector2 worldPosition,
            float best)
        {
            for (var i = 0; i < colliders.Count; i++)
            {
                var collider = colliders[i];
                if (collider != null)
                {
                    best = Mathf.Min(
                        best,
                        ((Vector2)collider.bounds.center - worldPosition).sqrMagnitude);
                }
            }

            return best;
        }
    }
}
