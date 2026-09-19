using UnityEngine;

namespace Octoplug.Power.Cable
{
    /// <summary>
    /// Turns the Power Flow overlay LineRenderer on/off and scrolls its
    /// tiled texture while on. This component only displays a power
    /// state — it never decides one. The real power system (not built
    /// yet) calls <see cref="SetPowered"/>; until then, <see cref="previewPowered"/>
    /// exists purely as an Inspector/Editor preview toggle and must never
    /// become a real game rule.
    ///
    /// Shares the exact path <see cref="CablePathRenderer"/> already wrote
    /// onto this same LineRenderer — no separate path calculation here.
    /// </summary>
    public class CablePowerFlowEffect : MonoBehaviour
    {
        [SerializeField]
        private LineRenderer flowLine;

        [SerializeField]
        [Tooltip("World units per second the flow texture scrolls while shown.")]
        private float scrollSpeed = 1.5f;

        [SerializeField]
        [Tooltip("Editor/Inspector preview only — lets art/design see the flow effect before a real power system exists. Never treat this as a gameplay rule.")]
        private bool previewPowered;

        private bool isPowered;
        private float scrollOffset;
        private Material runtimeMaterialInstance;

        /// <summary>Called by the (future) power system to show or hide the flow effect. Owns no power logic itself.</summary>
        public void SetPowered(bool powered)
        {
            isPowered = powered;
        }

        private void Start()
        {
            ApplyVisibility();
        }

        private void Update()
        {
            var shouldShow = isPowered || previewPowered;
            if (flowLine == null)
            {
                return;
            }

            if (flowLine.enabled != shouldShow)
            {
                ApplyVisibility();
            }

            if (!shouldShow)
            {
                return;
            }

            // LineRenderer's Tile UV.x runs 0 (Origin end) -> max (Plug/Socket
            // end). Increasing mainTextureOffset.x samples further along
            // that UV at each fixed world point, which visually slides the
            // pattern toward decreasing UV — i.e. Plug/Socket -> Origin,
            // the direction power actually flows (from the supplying
            // Socket toward the consuming Product/PowerStrip body). This
            // never touches path geometry — Base and Flow keep sharing the
            // exact same points; only which way the texture appears to
            // travel along them changes.
            scrollOffset += scrollSpeed * Time.deltaTime;
            var material = GetRuntimeMaterial();
            if (material == null)
            {
                return;
            }

            var offset = material.mainTextureOffset;
            offset.x = scrollOffset;
            material.mainTextureOffset = offset;
        }

        private void ApplyVisibility()
        {
            if (flowLine == null)
            {
                return;
            }

            flowLine.enabled = isPowered || previewPowered;
        }

        private Material GetRuntimeMaterial()
        {
            if (runtimeMaterialInstance == null && flowLine != null)
            {
                runtimeMaterialInstance = flowLine.material; // Unity instances the material on first access at runtime.
            }

            return runtimeMaterialInstance;
        }
    }
}
