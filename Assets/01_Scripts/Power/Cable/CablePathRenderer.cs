using System.Collections.Generic;
using UnityEngine;

namespace Octoplug.Power.Cable
{
    /// <summary>
    /// Draws a world-space waypoint path on the existing Cable LineRenderer
    /// (Base) and, if assigned, a second LineRenderer (Flow, the future
    /// powered-flow overlay) using the exact same simplified point list —
    /// the path is computed once here and never duplicated or recomputed.
    ///
    /// Only touches point positions and (once) each renderer's
    /// corner-vertex smoothing count — never coordinate-space mode,
    /// material, width, sorting, or other authored LineRenderer design
    /// values. Corner-vertex smoothing (numCornerVertices) was silently
    /// dropped from this class during TASK-007's Stabilization pass — with
    /// it gone, every 8-direction bend became a hard, uncapped miter (the
    /// primary source of the "spike"/"width pop" defects), so it is
    /// restored here rather than being a new behavior.
    ///
    /// Collinear points are merged so a straight run stays one segment
    /// instead of one LineRenderer point per grid cell. This never touches
    /// the two genuine sub-cell endpoint corrections (Origin's/the
    /// Socket's exact continuous position vs. the nearest grid cell
    /// center) — those are handled once, surgically, in
    /// <see cref="Octoplug.Power.Cable.CableRoutingController.AppendOrthogonalJog"/>,
    /// so every interior point here is already a real, 8-direction-aligned
    /// A* grid cell and is never merged away.
    /// </summary>
    public class CablePathRenderer : MonoBehaviour
    {
        [SerializeField]
        private LineRenderer line;

        [SerializeField]
        [Tooltip("Optional Power Flow overlay LineRenderer. Shares this exact routed path; rendering it on/off is CablePowerFlowEffect's job, not this component's.")]
        private LineRenderer flowLine;

        [SerializeField]
        [Tooltip("Rounds the 90-degree corners' line geometry only (LineRenderer.numCornerVertices). Does not change the underlying route. Restores TASK-006's join-rounding after it was accidentally dropped during Stabilization.")]
        [Range(0, 8)]
        private int cornerVertices = 4;

        private bool baseCornerVerticesApplied;
        private bool flowCornerVerticesApplied;

        /// <summary>Renders a simplified version of the given world-space waypoints on Base (and Flow, if assigned).</summary>
        public void Render(IReadOnlyList<Vector2> worldWaypoints)
        {
            if (worldWaypoints == null || worldWaypoints.Count == 0)
            {
                return;
            }

            var simplified = Simplify(worldWaypoints);
            if (simplified.Count < 2)
            {
                Clear();
                return;
            }

            ApplyTo(line, simplified, ref baseCornerVerticesApplied);
            ApplyTo(flowLine, simplified, ref flowCornerVerticesApplied);
        }

        /// <summary>
        /// Removes every rendered point from Base and Flow together. Used
        /// before startup routing so authored fallback points can never render
        /// when the live Grid rejects the initial route.
        /// </summary>
        public void Clear()
        {
            Clear(line);
            Clear(flowLine);
        }

        private void ApplyTo(
            LineRenderer target,
            List<Vector2> simplifiedPoints,
            ref bool cornerVerticesApplied)
        {
            if (target == null)
            {
                return;
            }

            if (!cornerVerticesApplied)
            {
                target.numCornerVertices = cornerVertices;
                cornerVerticesApplied = true;
            }

            target.positionCount = simplifiedPoints.Count;
            for (var i = 0; i < simplifiedPoints.Count; i++)
            {
                var worldPoint = simplifiedPoints[i];
                var point = new Vector3(worldPoint.x, worldPoint.y, 0f);
                target.SetPosition(
                    i,
                    target.useWorldSpace
                        ? point
                        : target.transform.InverseTransformPoint(point));
            }
        }

        private static void Clear(LineRenderer target)
        {
            if (target != null)
            {
                target.positionCount = 0;
            }
        }

        /// <summary>Removes interior points that do not change direction from the polyline (including near-duplicate points, whose in/out segment is ~zero length).</summary>
        private static List<Vector2> Simplify(
            IReadOnlyList<Vector2> points)
        {
            var distinct = new List<Vector2>(points.Count);
            foreach (var point in points)
            {
                if (distinct.Count == 0
                    || (point - distinct[^1]).sqrMagnitude >= 1e-8f)
                {
                    distinct.Add(point);
                }
            }

            if (distinct.Count <= 2)
            {
                return distinct;
            }

            var result = new List<Vector2> { distinct[0] };
            for (var i = 1; i < distinct.Count - 1; i++)
            {
                var incoming = distinct[i] - result[^1];
                var outgoing = distinct[i + 1] - distinct[i];
                var sameDirection = Vector2.Dot(
                    incoming.normalized,
                    outgoing.normalized) > 0.999f;

                if (!sameDirection)
                {
                    result.Add(distinct[i]);
                }
            }

            result.Add(distinct[^1]);
            return result;
        }
    }
}
