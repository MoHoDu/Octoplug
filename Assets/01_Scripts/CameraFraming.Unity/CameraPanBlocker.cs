using UnityEngine;

namespace Octoplug.CameraFraming.Unity
{
    /// <summary>Marks an explicit gameplay hit region that owns pointer input instead of Camera Pan.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class CameraPanBlocker : MonoBehaviour
    {
    }
}
