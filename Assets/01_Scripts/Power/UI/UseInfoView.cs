using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Octoplug.Power.UI
{
    /// <summary>
    /// Drives the existing `UseInfo` prefab instance nested under each
    /// Product: active/waiting resident counts as person icons, hidden
    /// entirely at zero. No real Resident system exists yet — the
    /// Inspector-only preview fields below are a stand-in, mirroring
    /// <see cref="Octoplug.Power.Cable.CablePowerFlowEffect.previewPowered"/>'s
    /// precedent, and must never be read as a game rule.
    /// </summary>
    public class UseInfoView : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Existing active-user icon template ('Using_Person').")]
        private Image activeIconTemplate;

        [SerializeField]
        [Tooltip("Existing waiting-user icon template ('Icon').")]
        private Image waitingIconTemplate;

        [SerializeField]
        [Tooltip("Preview-only test values — NOT a real Resident system. Do not treat as a game rule.")]
        private int previewActiveUsers;

        [SerializeField]
        [Tooltip("Preview-only test values — NOT a real Resident system. Do not treat as a game rule.")]
        private int previewWaitingUsers;

        private readonly List<Image> activeIcons = new();
        private readonly List<Image> waitingIcons = new();

        private void Start()
        {
            // The authored prefab has two same-named siblings per role
            // ('Using_Person' x2, 'Icon' x2) — a Find/single-reference would
            // only ever see one of each and leave its sibling as an
            // untracked orphan, so the initial pool is built from every
            // same-named sibling under the template's own parent instead.
            CollectSameNameSiblings(activeIconTemplate, activeIcons);
            CollectSameNameSiblings(waitingIconTemplate, waitingIcons);

            SetCounts(previewActiveUsers, previewWaitingUsers);
        }

        private static void CollectSameNameSiblings(Image template, List<Image> pool)
        {
            if (template == null)
            {
                return;
            }

            foreach (Transform child in template.transform.parent)
            {
                if (child.name != template.name)
                {
                    continue;
                }

                var image = child.GetComponent<Image>();
                if (image != null)
                {
                    pool.Add(image);
                }
            }
        }

        /// <summary>Redraws for the given counts; hides the whole view when both are zero. Called by a future Resident system — safe to call any time.</summary>
        public void SetCounts(int active, int waiting)
        {
            active = Mathf.Max(0, active);
            waiting = Mathf.Max(0, waiting);

            gameObject.SetActive(active + waiting > 0);
            if (active + waiting == 0)
            {
                return;
            }

            ApplyCount(activeIcons, activeIconTemplate, active);
            ApplyCount(waitingIcons, waitingIconTemplate, waiting);
        }

        private void ApplyCount(List<Image> pool, Image template, int count)
        {
            if (template == null)
            {
                return;
            }

            while (pool.Count < count)
            {
                pool.Add(Instantiate(template, template.transform.parent));
            }

            for (var i = 0; i < pool.Count; i++)
            {
                pool[i].gameObject.SetActive(i < count);
            }
        }
    }
}
