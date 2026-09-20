using UnityEngine;
using UnityEngine.UI;

namespace Octoplug.ResidentDemand.Unity
{
    public sealed class GameStatusInfoView : MonoBehaviour
    {
        private static readonly Color DangerColor = new(1f, 0f, 0.19607843f, 1f);

        [SerializeField]
        private Slider satisfactionSlider;

        [SerializeField]
        private Image satisfactionFill;

        [SerializeField]
        private Slider experienceSlider;

        private Color _authoredSatisfactionColor;
        private bool _capturedAuthoredColor;

        private void Awake()
        {
            CaptureAuthoredColor();
        }

        public void Bind(
            int globalSatisfaction,
            float satisfactionNormalized,
            float experienceNormalized)
        {
            CaptureAuthoredColor();
            satisfactionSlider.value = Mathf.Clamp01(satisfactionNormalized);
            satisfactionFill.color = globalSatisfaction < 30
                ? DangerColor
                : _authoredSatisfactionColor;
            experienceSlider.value = Mathf.Clamp01(experienceNormalized);
        }

        private void CaptureAuthoredColor()
        {
            if (_capturedAuthoredColor)
            {
                return;
            }

            _authoredSatisfactionColor = satisfactionFill.color;
            _capturedAuthoredColor = true;
        }
    }
}
