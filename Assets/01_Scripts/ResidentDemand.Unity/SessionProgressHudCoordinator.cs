using UnityEngine;

namespace Octoplug.ResidentDemand.Unity
{
    public sealed class SessionProgressHudCoordinator : MonoBehaviour
    {
        [SerializeField]
        private SessionProgressController controller;

        [SerializeField]
        private GameStatusInfoView view;

        private bool _subscribed;

        private void OnEnable()
        {
            Subscribe();
            Refresh();
        }

        private void Start()
        {
            Refresh();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void Subscribe()
        {
            if (_subscribed || controller == null)
            {
                return;
            }

            controller.ProgressChanged += Refresh;
            _subscribed = true;
        }

        public void Refresh()
        {
            if (controller == null || view == null || !controller.IsInitialized)
            {
                return;
            }

            view.Bind(
                controller.GlobalSatisfaction,
                controller.SatisfactionNormalized,
                controller.ExperienceNormalized);
        }

        private void Unsubscribe()
        {
            if (!_subscribed || controller == null)
            {
                return;
            }

            controller.ProgressChanged -= Refresh;
            _subscribed = false;
        }
    }
}
