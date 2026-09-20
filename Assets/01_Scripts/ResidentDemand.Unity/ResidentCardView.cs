using Octoplug.Power.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Octoplug.ResidentDemand.Unity
{
    public sealed class ResidentCardView : MonoBehaviour
    {
        private static readonly Color WaitingColor = new(1f, 0f, 0.19607843f, 1f);

        [SerializeField]
        private TMP_Text residentNumber;

        [SerializeField]
        [FormerlySerializedAs("needIcon")]
        private Image icon01;

        [SerializeField]
        private Image icon02;

        [SerializeField]
        private GameObject none;

        [SerializeField]
        private Sprite completedCheckSprite;

        [SerializeField]
        private GameObject statusInfo;

        [SerializeField]
        private Slider progressSlider;

        [SerializeField]
        private Image progressFill;

        private Color _authoredUsingColor;
        private bool _initialized;

        public void Bind(
            ResidentDemandState resident,
            UsageTypeIconLibrary iconLibrary)
        {
            if (resident == null)
            {
                throw new System.ArgumentNullException(nameof(resident));
            }

            if (iconLibrary == null)
            {
                throw new System.ArgumentNullException(nameof(iconLibrary));
            }

            EnsureInitialized();
            if (residentNumber != null)
            {
                residentNumber.text = resident.ResidentNumber.DisplayValue;
            }

            var hasActiveDemand =
                resident.Status == ResidentDemandStatus.Using ||
                resident.Status == ResidentDemandStatus.Waiting;
            SetVisible(none, !hasActiveDemand);
            SetVisible(statusInfo, hasActiveDemand);
            BindNeedIcon(icon01, resident, iconLibrary, 0, hasActiveDemand);
            BindNeedIcon(icon02, resident, iconLibrary, 1, hasActiveDemand);
            if (!hasActiveDemand)
            {
                return;
            }

            if (progressSlider != null)
            {
                progressSlider.value = resident.Status == ResidentDemandStatus.Using
                    ? resident.SatisfactionProgress
                    : resident.PatienceProgress;
            }

            if (progressFill != null)
            {
                progressFill.color = resident.Status == ResidentDemandStatus.Using
                    ? _authoredUsingColor
                    : WaitingColor;
            }
        }

        private void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            if (progressFill != null)
            {
                _authoredUsingColor = progressFill.color;
            }

            _initialized = true;
        }

        private void BindNeedIcon(
            Image target,
            ResidentDemandState resident,
            UsageTypeIconLibrary iconLibrary,
            int needIndex,
            bool hasActiveDemand)
        {
            var visible = hasActiveDemand && needIndex < resident.NeedCount;
            SetVisible(target, visible);
            if (!visible || target == null)
            {
                return;
            }

            if (resident.IsNeedCompleted(needIndex))
            {
                target.sprite = completedCheckSprite;
                return;
            }

            var usageType = UsageTypeMapper.MapSingle(resident.GetNeed(needIndex));
            target.sprite = iconLibrary.GetIcon(usageType);
        }

        private static void SetVisible(Behaviour target, bool visible)
        {
            if (target != null)
            {
                target.gameObject.SetActive(visible);
            }
        }

        private static void SetVisible(GameObject target, bool visible)
        {
            if (target != null)
            {
                target.SetActive(visible);
            }
        }
    }
}
