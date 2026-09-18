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
    /// corner-vertex smoothing count — never material, width, or other
    /// authored LineRenderer design values.
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
        [Tooltip("Rounds the 90-degree corners' line geometry only (LineRenderer.numCornerVertices). Does not change the underlying route.")]
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

            ApplyTo(line, simplified, ref baseCornerVerticesApplied);
            ApplyTo(flowLine, simplified, ref flowCornerVerticesApplied);
        }

        private void ApplyTo(LineRenderer target, List<Vector2> simplifiedPoints, ref bool cornerVerticesApplied)
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

            var targetTransform = target.transform;
            target.positionCount = simplifiedPoints.Count;
            for (var i = 0; i < simplifiedPoints.Count; i++)
            {
                target.SetPosition(i, targetTransform.InverseTransformPoint(simplifiedPoints[i]));
            }
        }

        /// <summary>Removes interior points that do not change direction from the polyline (including near-duplicate points, whose in/out segment is ~zero length).</summary>
        private static List<Vector2> Simplify(IReadOnlyList<Vector2> points)
        {
            if (points.Count <= 2)
            {
                return new List<Vector2>(points);
            }

            var result = new List<Vector2> { points[0] };

            for (var i = 1; i < points.Count - 1; i++)
            {
                var previous = points[i - 1];
                var current = points[i];
                var next = points[i + 1];

                var incoming = current - previous;
                var outgoing = next - current;

                if (incoming.sqrMagnitude < 1e-8f || outgoing.sqrMagnitude < 1e-8f)
                {
                    continue;
                }

                var sameDirection = Vector2.Dot(incoming.normalized, outgoing.normalized) > 0.999f;
                if (!sameDirection)
                {
                    result.Add(current);
                }
            }

            result.Add(points[^1]);
            return result;
        }
    }
}
