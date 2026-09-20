using System.Collections.Generic;
using Octoplug.ResidentDemand.Unity;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Octoplug.Power.UI
{
    /// <summary>
    /// Renders an authoritative Product usage snapshot with only the person
    /// icons already authored in the shared UseInfo prefab.
    /// </summary>
    public class UseInfoView : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Common parent whose direct Image children form the authored icon pool in sibling order.")]
        private Transform iconPoolParent;

        [SerializeField]
        [FormerlySerializedAs("activeIconTemplate")]
        [Tooltip("Authored reference whose color represents an active user.")]
        private Image activeColorReference;

        [SerializeField]
        [FormerlySerializedAs("waitingIconTemplate")]
        [Tooltip("Authored reference whose color represents a waiting user.")]
        private Image waitingColorReference;

        private readonly List<Image> icons = new();
        private Color _activeColor;
        private Color _waitingColor;
        private bool _overflowReported;
        private bool _initialized;

        public static UseInfoView FindFor(ApplianceSource product)
        {
            return product == null
                ? null
                : product.GetComponentInChildren<UseInfoView>(true);
        }

        public void Bind(ProductUsageSnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new System.ArgumentNullException(nameof(snapshot));
            }

            EnsureInitialized();
            var available =
                snapshot.Product.IsConnected &&
                snapshot.Product.IsPowered;
            var active = available ? snapshot.ActiveResidents.Count : 0;
            var waiting = available ? snapshot.WaitingResidents.Count : 0;
            ApplyCounts(active, waiting);
            gameObject.SetActive(available && active + waiting > 0);
        }

        public void Hide()
        {
            EnsureInitialized();
            ApplyCounts(0, 0);
            gameObject.SetActive(false);
        }

        private void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            icons.Clear();
            var poolParent = iconPoolParent;
            if (poolParent == null && activeColorReference != null)
            {
                poolParent = activeColorReference.transform.parent;
            }

            if (poolParent == null && waitingColorReference != null)
            {
                poolParent = waitingColorReference.transform.parent;
            }

            if (poolParent != null)
            {
                foreach (Transform child in poolParent)
                {
                    var image = child.GetComponent<Image>();
                    if (image != null)
                    {
                        icons.Add(image);
                    }
                }
            }

            if (activeColorReference != null)
            {
                _activeColor = activeColorReference.color;
            }

            if (waitingColorReference != null)
            {
                _waitingColor = waitingColorReference.color;
            }

            _initialized = true;
        }

        private void ApplyCounts(int active, int waiting)
        {
            active = Mathf.Max(0, active);
            waiting = Mathf.Max(0, waiting);
            var visibleCount = Mathf.Min(icons.Count, active + waiting);
            var visibleActiveCount = Mathf.Min(icons.Count, active);

            for (var index = 0; index < icons.Count; index++)
            {
                var icon = icons[index];
                icon.gameObject.SetActive(index < visibleCount);
                if (index < visibleActiveCount)
                {
                    icon.color = _activeColor;
                }
                else if (index < visibleCount)
                {
                    icon.color = _waitingColor;
                }
            }

            if (active + waiting <= icons.Count || _overflowReported)
            {
                return;
            }

            Debug.LogWarning(
                $"UseInfo authored icon pool has {icons.Count} slots, " +
                $"but the authoritative snapshot requires {active + waiting} " +
                $"({active} active, {waiting} waiting). " +
                "The display is clamped with active residents taking priority; no icons were created.",
                this);
            _overflowReported = true;
        }
    }
}
