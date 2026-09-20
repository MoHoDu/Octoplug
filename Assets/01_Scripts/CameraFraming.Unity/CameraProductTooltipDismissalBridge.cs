using Octoplug.Power.UI;
using UnityEngine;

namespace Octoplug.CameraFraming.Unity
{
    /// <summary>
    /// Dismisses the screen-positioned Product tooltip when the production camera view starts
    /// changing. Camera controllers remain the authority for whether a requested view change
    /// actually moves or zooms the camera.
    /// </summary>
    public sealed class CameraProductTooltipDismissalBridge : MonoBehaviour
    {
        [SerializeField]
        private HouseCameraZoomController cameraZoom;

        [SerializeField]
        private HouseCameraPanController cameraPan;

        [SerializeField]
        private ProductTooltipController productTooltip;

        private void OnEnable()
        {
            Subscribe();
        }

#if UNITY_EDITOR
        public void InitializeForVerification()
        {
            Subscribe();
        }
#endif

        private void Subscribe()
        {
            if (cameraZoom != null)
            {
                cameraZoom.CameraMotionStarted += HideTooltip;
            }

            if (cameraPan != null)
            {
                cameraPan.CameraMotionStarted += HideTooltip;
            }
        }

        private void OnDisable()
        {
            if (cameraZoom != null)
            {
                cameraZoom.CameraMotionStarted -= HideTooltip;
            }

            if (cameraPan != null)
            {
                cameraPan.CameraMotionStarted -= HideTooltip;
            }
        }

        private void HideTooltip()
        {
            productTooltip?.Hide();
        }
    }
}
