using System;
using System.Collections.Generic;
using UnityEngine;

namespace Octoplug.Power
{
    /// <summary>
    /// Inspector-authored mapping between one persistent Socket slot and its
    /// existing Head module. Array order is capacity order; runtime code never
    /// infers order or geometry from names or Renderer bounds.
    /// </summary>
    public class PowerStripSocketLayout : MonoBehaviour
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
            [Tooltip("Authored geometry that is present whenever this module is active.")]
            private List<BoxCollider2D> moduleColliders = new();

            [SerializeField]
            [Tooltip("Authored geometry that is present only when this module is the terminal module.")]
            private List<BoxCollider2D> terminalColliders = new();

            [SerializeField]
            private bool hasAuthoredPowerInfoPosition;

            [SerializeField]
            private Vector3 powerInfoLocalPosition;

            public SocketConnector Socket => socket;
            public GameObject Whole => whole;
            public GameObject End => end;
            public IReadOnlyList<BoxCollider2D> ModuleColliders => moduleColliders;
            public IReadOnlyList<BoxCollider2D> TerminalColliders => terminalColliders;
            public bool HasAuthoredPowerInfoPosition => hasAuthoredPowerInfoPosition;
            public Vector3 PowerInfoLocalPosition => powerInfoLocalPosition;
        }

        [SerializeField]
        private Transform powerInfo;

        [SerializeField]
        private List<Module> modules = new();

        public int Capacity => modules.Count;

        public bool TryValidate(int socketCount)
        {
            if (socketCount < 1 || socketCount > modules.Count)
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
                    || module.ModuleColliders == null
                    || module.ModuleColliders.Count == 0
                    || module.TerminalColliders == null
                    || !module.HasAuthoredPowerInfoPosition)
                {
                    return false;
                }

                if (!AreCollidersValid(module.ModuleColliders)
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
            for (var i = 0; i < activeCount; i++)
            {
                AddColliders(modules[i].ModuleColliders, results);
            }

            if (activeCount > 0)
            {
                AddColliders(modules[activeCount - 1].TerminalColliders, results);
            }
        }

        public bool ContainsPoint(int socketCount, Vector2 worldPosition)
        {
            var activeCount = Mathf.Clamp(socketCount, 0, modules.Count);
            for (var i = 0; i < activeCount; i++)
            {
                if (ContainsPoint(modules[i].ModuleColliders, worldPosition))
                {
                    return true;
                }
            }

            return activeCount > 0
                && ContainsPoint(modules[activeCount - 1].TerminalColliders, worldPosition);
        }

        public float SqrDistanceToHitCenter(int socketCount, Vector2 worldPosition)
        {
            var best = float.PositiveInfinity;
            var activeCount = Mathf.Clamp(socketCount, 0, modules.Count);
            for (var i = 0; i < activeCount; i++)
            {
                best = GetSqrDistanceToHitCenter(
                    modules[i].ModuleColliders,
                    worldPosition,
                    best);
            }

            return activeCount > 0
                ? GetSqrDistanceToHitCenter(
                    modules[activeCount - 1].TerminalColliders,
                    worldPosition,
                    best)
                : best;
        }

        public void Apply(int socketCount)
        {
            for (var i = 0; i < modules.Count; i++)
            {
                var active = i < socketCount;
                modules[i].Whole.SetActive(active);
                modules[i].Socket.gameObject.SetActive(active);
                modules[i].End.SetActive(i == socketCount - 1);
            }

            var activeModule = modules[socketCount - 1];
            if (powerInfo != null && activeModule.HasAuthoredPowerInfoPosition)
            {
                powerInfo.localPosition = activeModule.PowerInfoLocalPosition;
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
