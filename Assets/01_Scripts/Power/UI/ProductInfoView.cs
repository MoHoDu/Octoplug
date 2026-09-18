using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Octoplug.Power.UI
{
    /// <summary>
    /// Drives the existing always-on `ProductInfo` prefab instance nested
    /// under each Product: the Power row shows consumption as a *count* of
    /// active lightning icons (no number), and the Tag row shows one icon
    /// per active <see cref="UsageType"/> flag. This class only toggles
    /// active state, picks a sprite, and reads data — it never sets a
    /// RectTransform, Scale, spacing, or color; the current Prefab's visual
    /// design (as saved) is the source of truth.
    /// </summary>
    public class ProductInfoView : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Existing 'Power' row's icon pool — toggled active/inactive by count, never resized beyond however many the Prefab currently has.")]
        private Transform powerIconRow;

        [SerializeField]
        [Tooltip("Existing 'Tag' row's single Image — the template cloned for additional UsageType icons on a multi-tag product. Its authored color/sprite/size are left untouched; only clones' sprite is set to the matching UsageType icon.")]
        private Image tagIconTemplate;

        [SerializeField]
        private UsageTypeIconLibrary iconLibrary;

        [SerializeField]
        [Tooltip("Overall display alpha for this always-on info (power + usage tag), via a CanvasGroup on this GameObject. Never affects the Product's own sprite.")]
        private float displayAlpha = 0.8f;

        private ApplianceSource product;
        private Image[] powerIcons;
        private CanvasGroup canvasGroup;
        private readonly List<Image> tagIcons = new();

        private void Start()
        {
            product = GetComponentInParent<ApplianceSource>();
            if (powerIconRow != null)
            {
                powerIcons = powerIconRow.GetComponentsInChildren<Image>(true);
            }

            if (tagIconTemplate != null)
            {
                tagIcons.Add(tagIconTemplate);
            }

            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            canvasGroup.alpha = displayAlpha;

            Refresh();
        }

        /// <summary>Re-reads the sibling ApplianceSource and redraws both rows. Products are static for this stage; exposed for future callers.</summary>
        public void Refresh()
        {
            if (product == null)
            {
                return;
            }

            RefreshPowerRow();
            RefreshTagRow();
        }

        private void RefreshPowerRow()
        {
            if (powerIcons == null || powerIcons.Length == 0)
            {
                return;
            }

            var rawCount = Mathf.RoundToInt(product.PowerConsumptionWatts);
            if (rawCount > powerIcons.Length)
            {
                Debug.LogWarning($"{name}: {product.name}'s PowerConsumptionWatts ({rawCount}) exceeds the ProductInfo Power icon pool ({powerIcons.Length}) — display is clamped and under-represents actual consumption. Needs either more icons in the Prefab or a lower value; see Human Setup Required.", this);
            }

            var activeCount = Mathf.Clamp(rawCount, 0, powerIcons.Length);
            for (var i = 0; i < powerIcons.Length; i++)
            {
                powerIcons[i].gameObject.SetActive(i < activeCount);
            }
        }

        private void RefreshTagRow()
        {
            if (tagIconTemplate == null || iconLibrary == null)
            {
                return;
            }

            var flags = new List<UsageType>(UsageTypeUtility.EnumerateFlags(product.UsageTypes));
            EnsurePoolSize(flags.Count);

            for (var i = 0; i < tagIcons.Count; i++)
            {
                if (i < flags.Count)
                {
                    tagIcons[i].sprite = iconLibrary.GetIcon(flags[i]);
                    tagIcons[i].gameObject.SetActive(true);
                }
                else
                {
                    tagIcons[i].gameObject.SetActive(false);
                }
            }
        }

        private void EnsurePoolSize(int required)
        {
            while (tagIcons.Count < required)
            {
                var clone = Instantiate(tagIconTemplate, tagIconTemplate.transform.parent);
                tagIcons.Add(clone);
            }
        }
    }
}
