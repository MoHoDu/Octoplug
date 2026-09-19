using System;
using UnityEngine;

namespace Octoplug.Power
{
    /// <summary>
    /// Attaches to the existing Cable.prefab root. Stores the cable's
    /// maximum length and references its existing child parts by explicit
    /// SerializeField (never by name lookup), so the CableOrigin naming
    /// decision (kept as-is) never leaks into code.
    ///
    /// Per-product/per-strip cable length differences are expressed as a
    /// normal prefab-instance override of <see cref="cableLength"/> on the
    /// nested Cable instance; this component does not hardcode any value.
    /// </summary>
    public class CableInfo : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Maximum orthogonal routing distance for this cable, in world units. Demo placeholder — tune per product/strip in the Inspector.")]
        private float cableLength = 3f;

        [SerializeField]
        [Tooltip("Existing 'CableOrigin' child: the fixed attach point on the owning appliance/strip body.")]
        private Transform origin;

        [SerializeField]
        [Tooltip("Existing 'Plug' child: the draggable endpoint.")]
        private PlugConnector plug;

        [SerializeField]
        [Tooltip("Existing 'Line' child's LineRenderer, reused as-is for the routed path drawn in a later stage.")]
        private LineRenderer line;

        public event Action<CableInfo, float, float> CableLengthChanged;

        public float CableLength => cableLength;
        public Transform Origin => origin;
        public PlugConnector Plug => plug;
        public LineRenderer Line => line;

        public bool TryUpgradeCableLength(
            out CableLengthUpgradeFailure failure)
        {
            failure = CableLengthUpgradeFailure.None;
            if (!float.IsFinite(cableLength) || cableLength < 0f)
            {
                failure = CableLengthUpgradeFailure.InvalidState;
                return false;
            }

            var upgradedLength = cableLength + 1f;
            if (!float.IsFinite(upgradedLength))
            {
                failure = CableLengthUpgradeFailure.InvalidState;
                return false;
            }

            var oldLength = cableLength;
            cableLength = upgradedLength;
            CableLengthChanged?.Invoke(this, oldLength, upgradedLength);
            return true;
        }
    }
}
