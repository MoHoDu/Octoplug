using System.Collections.Generic;
using UnityEngine;
using Octoplug.Power.Grid;
using Octoplug.Power.Routing;

namespace Octoplug.Power.Cable
{
    /// <summary>
    /// Orchestrates dragging the existing Plug: reads pointer events from
    /// <see cref="Octoplug.Power.Input.PlugDragInput"/>, asks
    /// <see cref="GridPathfinder"/> for the shortest orthogonal route
    /// through the current <see cref="CableRoutingGridService"/> grid, and
    /// hands the resulting waypoints to <see cref="CablePathRenderer"/>.
    ///
    /// This class does not do pathfinding math or LineRenderer math itself
    /// — it only coordinates. It does not touch Socket connection state or
    /// power validation (out of scope for this stage).
    /// </summary>
    public class CableRoutingController : MonoBehaviour
    {
        [SerializeField]
        private Octoplug.Power.CableInfo cableInfo;

        [SerializeField]
        private Octoplug.Power.Input.PlugDragInput dragInput;

        [SerializeField]
        private CablePathRenderer pathRenderer;

        [SerializeField]
        [Tooltip("How many grid cells outward to search for a valid cell when the pointer or release point lands outside walkable space.")]
        private int nearestValidSearchRadius = 24;

        private Vector2 lastValidPlugPosition;

        private void Start()
        {
            if (cableInfo == null || cableInfo.Plug == null || cableInfo.Origin == null)
            {
                Debug.LogWarning($"{name}: CableRoutingController is missing an Origin/Plug/CableInfo reference; drag routing is disabled.", this);
                enabled = false;
                return;
            }

            lastValidPlugPosition = cableInfo.Plug.transform.position;
            ValidateInitialPlugPosition();
            RenderInitialPath();

            if (dragInput != null)
            {
                dragInput.Dragged += OnDragged;
                dragInput.DragEnded += OnDragEnded;
            }
        }

        private void OnDestroy()
        {
            if (dragInput != null)
            {
                dragInput.Dragged -= OnDragged;
                dragInput.DragEnded -= OnDragEnded;
            }
        }

        private void ValidateInitialPlugPosition()
        {
            var grid = ResolveGrid();
            if (grid == null)
            {
                return;
            }

            var plugCell = grid.WorldToCell(lastValidPlugPosition);
            if (!grid.IsWalkable(plugCell))
            {
                Debug.LogWarning($"{name}: authored Plug position {lastValidPlugPosition} is not on a walkable grid cell. Left unchanged.", this);
            }
        }

        /// <summary>
        /// Draws the Cable from Origin to the current (authored) Plug
        /// position immediately on Start, before any drag happens. Never
        /// moves the Plug — an invalid authored position is reported as a
        /// warning and left as-is, per the human-placement policy.
        /// </summary>
        private void RenderInitialPath()
        {
            var grid = ResolveGrid();
            if (grid == null)
            {
                return;
            }

            var originPos = (Vector2)cableInfo.Origin.position;
            var plugPos = (Vector2)cableInfo.Plug.transform.position;
            var originCell = grid.WorldToCell(originPos);
            var plugCell = grid.WorldToCell(plugPos);

            if (!grid.IsWalkable(originCell) || !grid.IsWalkable(plugCell))
            {
                Debug.LogWarning($"{name}: cannot draw the initial Cable path — Origin or the authored Plug position is not on a walkable grid cell.", this);
                return;
            }

            if (!GridPathfinder.TryFindPath(grid, originCell, plugCell, out var cellPath))
            {
                Debug.LogWarning($"{name}: cannot draw the initial Cable path — no valid route between Origin and the authored Plug position.", this);
                return;
            }

            var worldPath = BuildWorldPath(originPos, cellPath, plugPos, grid);
            pathRenderer?.Render(worldPath);
        }

        private void OnDragged(Vector2 pointerWorldPos)
        {
            var grid = ResolveGrid();
            if (grid == null)
            {
                return;
            }

            var plugTransform = cableInfo.Plug.transform;
            var originPos = (Vector2)cableInfo.Origin.position;
            var originCell = grid.WorldToCell(originPos);
            if (!grid.IsWalkable(originCell))
            {
                return;
            }

            var pointerCell = grid.WorldToCell(pointerWorldPos);
            GridCoord targetCell;
            Vector2 targetContinuous;

            if (grid.IsWalkable(pointerCell))
            {
                targetCell = pointerCell;
                targetContinuous = pointerWorldPos;
            }
            else if (GridPathfinder.TryFindNearestWalkable(grid, pointerCell, nearestValidSearchRadius, out targetCell))
            {
                targetContinuous = grid.CellToWorld(targetCell);
            }
            else
            {
                return; // Nothing reachable near the pointer this frame; hold last valid position.
            }

            if (!GridPathfinder.TryFindPath(grid, originCell, targetCell, out var cellPath))
            {
                return; // No valid route (e.g. different room with no door); hold last valid position.
            }

            var worldPath = BuildWorldPath(originPos, cellPath, targetContinuous, grid);
            var totalLength = GridPathfinder.PathLength(worldPath);

            List<Vector2> finalPath;
            Vector2 finalPosition;
            if (totalLength <= cableInfo.CableLength)
            {
                finalPath = worldPath;
                finalPosition = targetContinuous;
            }
            else
            {
                finalPath = GridPathfinder.TruncateToLength(worldPath, cableInfo.CableLength);
                finalPosition = finalPath[^1];
            }

            plugTransform.position = new Vector3(finalPosition.x, finalPosition.y, plugTransform.position.z);
            pathRenderer?.Render(finalPath);
            lastValidPlugPosition = finalPosition;
        }

        private void OnDragEnded()
        {
            var grid = ResolveGrid();
            if (grid == null)
            {
                return;
            }

            var plugTransform = cableInfo.Plug.transform;
            var originPos = (Vector2)cableInfo.Origin.position;
            var originCell = grid.WorldToCell(originPos);
            var currentPos = (Vector2)plugTransform.position;
            var currentCell = grid.WorldToCell(currentPos);

            if (grid.IsWalkable(currentCell)
                && GridPathfinder.TryFindPath(grid, originCell, currentCell, out var currentPath)
                && GridPathfinder.PathLength(BuildWorldPath(originPos, currentPath, currentPos, grid)) <= cableInfo.CableLength + 0.001f)
            {
                lastValidPlugPosition = currentPos;
                return;
            }

            if (GridPathfinder.TryFindNearestValidDropCell(grid, originCell, currentCell, cableInfo.CableLength, nearestValidSearchRadius, out var snappedCell))
            {
                var snappedPos = grid.CellToWorld(snappedCell);
                if (GridPathfinder.TryFindPath(grid, originCell, snappedCell, out var snappedPath))
                {
                    var snappedWorldPath = BuildWorldPath(originPos, snappedPath, snappedPos, grid);
                    plugTransform.position = new Vector3(snappedPos.x, snappedPos.y, plugTransform.position.z);
                    pathRenderer?.Render(snappedWorldPath);
                    lastValidPlugPosition = snappedPos;
                    return;
                }
            }

            // No valid nearby position found: fall back to the last known valid position.
            plugTransform.position = new Vector3(lastValidPlugPosition.x, lastValidPlugPosition.y, plugTransform.position.z);
            if (GridPathfinder.TryFindPath(grid, originCell, grid.WorldToCell(lastValidPlugPosition), out var fallbackPath))
            {
                pathRenderer?.Render(BuildWorldPath(originPos, fallbackPath, lastValidPlugPosition, grid));
            }
        }

        /// <summary>
        /// Builds the full world-space waypoint list: the exact Origin
        /// point, each interior grid-cell center on the route, and the
        /// exact target point — with a single axis-aligned bend inserted
        /// at each end where the exact continuous position does not
        /// already line up with its nearest grid cell center, so the
        /// rendered cable is never diagonal even over that sub-cell gap.
        /// </summary>
        private static List<Vector2> BuildWorldPath(Vector2 originPos, IReadOnlyList<GridCoord> cellPath, Vector2 targetContinuous, CableRoutingGrid grid)
        {
            var worldPath = new List<Vector2> { originPos };

            AppendOrthogonalJog(worldPath, grid.CellToWorld(cellPath[0]));

            for (var i = 1; i < cellPath.Count - 1; i++)
            {
                worldPath.Add(grid.CellToWorld(cellPath[i]));
            }

            if (cellPath.Count > 1)
            {
                AppendOrthogonalJog(worldPath, grid.CellToWorld(cellPath[^1]));
            }

            AppendOrthogonalJog(worldPath, targetContinuous);

            return worldPath;
        }

        /// <summary>
        /// Appends <paramref name="to"/> to the path, first inserting one
        /// axis-aligned bend point if hopping directly from the path's
        /// current last point to <paramref name="to"/> would be diagonal.
        /// </summary>
        private static void AppendOrthogonalJog(List<Vector2> path, Vector2 to)
        {
            var from = path[^1];
            var dx = Mathf.Abs(from.x - to.x);
            var dy = Mathf.Abs(from.y - to.y);

            if (dx > 0.0001f && dy > 0.0001f)
            {
                path.Add(new Vector2(from.x, to.y));
            }

            if (Vector2.Distance(path[^1], to) > 0.0001f)
            {
                path.Add(to);
            }
        }

        private CableRoutingGrid ResolveGrid()
        {
            var service = CableRoutingGridService.Instance;
            if (service == null)
            {
                Debug.LogWarning($"{name}: no CableRoutingGridService found in the scene.", this);
                return null;
            }

            return service.Grid;
        }
    }
}
