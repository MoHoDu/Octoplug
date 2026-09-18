using System.Collections.Generic;
using UnityEngine;
using Octoplug.Power.Connection;
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
    /// — it only coordinates. Physical Socket connect/disconnect (this
    /// stage, P0-1D) is attempted here because it shares the same
    /// route/length checks as a plain drop; pairing state itself is
    /// delegated to <see cref="PlugSocketConnection"/>.
    ///
    /// Also drives the sibling <see cref="CablePowerFlowEffect"/> (found via
    /// <c>GetComponent</c>, same GameObject) and the parent Product's
    /// <see cref="Octoplug.Power.ApplianceSource"/> (found via
    /// <c>GetComponentInParent</c>) — both only ever reflect the real
    /// Powered result of <see cref="Octoplug.Power.Connection.PowerValidationService"/>,
    /// evaluated once here at the moment a connection is made or broken.
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

        [SerializeField]
        [Tooltip("World-unit radius, from the pointer's last position, used to find the Socket it is being dropped onto. Demo placeholder — authored Sockets are roughly 0.53 units apart, so this should stay below half that.")]
        private float socketAcquisitionRadius = 0.25f;

        [SerializeField]
        [Tooltip("World-unit radius, from a connected Socket, that a grabbed Plug can wander within without actually detaching. Kept above socketAcquisitionRadius (hysteresis) so connect/disconnect does not flicker right at the boundary. Demo placeholder.")]
        private float socketDetachRadius = 0.4f;

        [SerializeField]
        [Tooltip("How many grid cells outward, from a Socket's own (typically wall-Blocked) cell, to look for its room-interior Approach Point — deliberately small (wall-thickness scale) so this never tunnels through to a different room on the far side of the wall. Distinct from nearestValidSearchRadius, which is for general pointer-position recovery.")]
        private int socketApproachSearchRadius = 3;

        [SerializeField]
        [Tooltip("World-unit threshold below which a sub-cell endpoint correction (Origin's or a Socket's exact continuous position vs. the nearest grid cell center) is drawn as a single direct segment instead of an axis-aligned bend — avoids a short visible 'hook' right at a Wall Outlet's approach. Only ever applies to those two genuine endpoints, never to an interior A* grid segment. Demo placeholder sized against the current 0.25 grid cell; keep it well below one cell.")]
        private float minRenderSegmentLength = 0.15f;

        [SerializeField]
        [Tooltip("Minimum world-unit distance from a Socket that a Plug is pushed to when a connection attempt fails Power Validation specifically — avoids the Plug landing visually on top of/overlapping the Outlet it was just rejected from. Only applies to that one rejection path; every other invalid-drop case is unchanged. Inspector-adjustable placeholder.")]
        private float powerRejectFallbackDistance = 0.6f;

        [SerializeField]
        [Tooltip("SpriteRenderer.sortingOrder applied to the Plug's own sprites once it is connected to a Socket — above Product/Wall/Wall Outlet sprites so a plugged-in Plug stays visible, but below draggingSortingOrder.")]
        private int connectedSortingOrder = 5;

        [SerializeField]
        [Tooltip("SpriteRenderer.sortingOrder applied to the Plug's own sprites while it is actively being dragged — above connectedSortingOrder, so the Plug the user is holding is always the topmost thing on screen. Never changes the Cable Base/Flow LineRenderer order.")]
        private int draggingSortingOrder = 6;

        private Vector2 lastValidPlugPosition;

        /// <summary>
        /// The Socket the grabbed Plug was connected to when this drag
        /// began, or null if it wasn't connected (or has since exceeded
        /// <see cref="socketDetachRadius"/> and been disconnected). While
        /// non-null, the Plug/Socket connection is intentionally left
        /// intact — grabbing a plugged-in Plug is not itself a disconnect.
        /// </summary>
        private Octoplug.Power.SocketConnector originalSocket;

        /// <summary>
        /// The Socket this same drag most recently detached from (once
        /// <see cref="originalSocket"/> is cleared), or null. Reconnecting
        /// to this specific Socket accepts <see cref="socketDetachRadius"/>
        /// instead of the tighter <see cref="socketAcquisitionRadius"/> —
        /// "went a little too far and came right back" should not require
        /// re-acquiring more precisely than the wiggle that was still fine
        /// a moment earlier. Any other Socket still uses the normal,
        /// tighter acquisition radius. Cleared at the start of every new
        /// drag and the moment any connection succeeds.
        /// </summary>
        private Octoplug.Power.SocketConnector recentlyDetachedSocket;

        private SpriteRenderer[] plugSpriteRenderers;

        /// <summary>The Plug sprites' authored sortingOrder, captured once at Start — the "not connected, not dragging" resting state.</summary>
        private int[] plugRestingSortingOrders;

        private CablePowerFlowEffect powerFlowEffect;
        private Octoplug.Power.ApplianceSource applianceSource;
        private Octoplug.Power.HousePowerBudget houseBudget;

        /// <summary>
        /// Highest authored <see cref="SpriteRenderer.sortingOrder"/> found
        /// on the owning Product's own visual renderers ("Icon"/"Background"),
        /// or -1 if none were found. Read once in <see cref="Start"/> so a
        /// dragged Plug can be kept above whatever Product it started on
        /// without hardcoding a new absolute sortingOrder — the Product's
        /// own authored values (design-owned, never changed here) are the
        /// reference point instead.
        /// </summary>
        private int productVisualMaxSortingOrder = -1;

        /// <summary>
        /// The raw pointer world position from the most recent
        /// <see cref="Octoplug.Power.Input.PlugDragInput.Dragged"/> event,
        /// before <see cref="OnDragged"/> clamps the Plug itself to the
        /// nearest walkable cell. A wall-mounted Socket sits on a
        /// deliberately non-walkable cell (see the Connection / Power
        /// Domain Map), so the live-dragged Plug is always pulled a full
        /// grid-cell's distance away from it; matching Socket candidates
        /// against where the Plug ended up — instead of where the pointer
        /// actually was — made a Socket unreachable within any reasonable
        /// <see cref="socketAcquisitionRadius"/>, regardless of user
        /// precision.
        /// </summary>
        private Vector2 lastPointerWorldPos;

        private void Start()
        {
            if (cableInfo == null || cableInfo.Plug == null || cableInfo.Origin == null)
            {
                Debug.LogWarning($"{name}: CableRoutingController is missing an Origin/Plug/CableInfo reference; drag routing is disabled.", this);
                enabled = false;
                return;
            }

            lastValidPlugPosition = cableInfo.Plug.transform.position;
            powerFlowEffect = GetComponent<CablePowerFlowEffect>();
            applianceSource = GetComponentInParent<Octoplug.Power.ApplianceSource>();
#if UNITY_2023_1_OR_NEWER
            houseBudget = Object.FindFirstObjectByType<Octoplug.Power.HousePowerBudget>();
#else
            houseBudget = Object.FindObjectOfType<Octoplug.Power.HousePowerBudget>();
#endif

            plugSpriteRenderers = cableInfo.Plug.GetComponentsInChildren<SpriteRenderer>(true);
            plugRestingSortingOrders = new int[plugSpriteRenderers.Length];
            for (var i = 0; i < plugSpriteRenderers.Length; i++)
            {
                plugRestingSortingOrders[i] = plugSpriteRenderers[i].sortingOrder;
            }

            ResolveProductVisualMaxSortingOrder();
            ValidateInitialPlugPosition();
            RenderInitialPath();
            ValidateInitialConnection();

            if (dragInput != null)
            {
                dragInput.DragStarted += OnDragStarted;
                dragInput.Dragged += OnDragged;
                dragInput.DragEnded += OnDragEnded;
            }
        }

        /// <summary>
        /// Reads (never writes) the owning Product's own "Icon"/"Background"
        /// <see cref="SpriteRenderer.sortingOrder"/> values, if present, as
        /// the reference point <see cref="EffectiveDraggingSortingOrder"/>
        /// uses to guarantee a dragged Plug renders above them.
        /// </summary>
        private void ResolveProductVisualMaxSortingOrder()
        {
            if (applianceSource == null)
            {
                return;
            }

            var icon = applianceSource.transform.Find("Icon")?.GetComponent<SpriteRenderer>();
            var background = applianceSource.transform.Find("Background")?.GetComponent<SpriteRenderer>();

            if (icon != null)
            {
                productVisualMaxSortingOrder = Mathf.Max(productVisualMaxSortingOrder, icon.sortingOrder);
            }

            if (background != null)
            {
                productVisualMaxSortingOrder = Mathf.Max(productVisualMaxSortingOrder, background.sortingOrder);
            }
        }

        /// <summary>
        /// The authored <see cref="draggingSortingOrder"/>, raised only if
        /// needed to stay above the owning Product's own visual renderers
        /// (see <see cref="ResolveProductVisualMaxSortingOrder"/>) — a
        /// dragged Plug must never be hidden behind the Product it started
        /// on, but this never lowers or otherwise touches any authored
        /// value.
        /// </summary>
        private int EffectiveDraggingSortingOrder()
        {
            return productVisualMaxSortingOrder >= 0
                ? Mathf.Max(draggingSortingOrder, productVisualMaxSortingOrder + 1)
                : draggingSortingOrder;
        }

        /// <summary>
        /// Defensive only: if the Plug is already connected at scene load
        /// (none of the current scenes author this, but a future one might),
        /// evaluate Power Validation for it immediately instead of leaving
        /// it Connected-but-never-validated until the next drag.
        /// </summary>
        private void ValidateInitialConnection()
        {
            if (!cableInfo.Plug.IsConnected || applianceSource == null)
            {
                return;
            }

            var socket = cableInfo.Plug.ConnectedSocket;
            var powered = Octoplug.Power.Connection.PowerValidationService.TryValidate(applianceSource, socket, houseBudget, out var failureReason);
            if (!powered)
            {
                Debug.LogWarning($"{name}: authored connection failed Power Validation on load — {failureReason}. Leaving physically Connected but not Powered.", this);
            }

            applianceSource.SetPowered(powered);
            powerFlowEffect?.SetPowered(powered);
        }

        private void OnDestroy()
        {
            if (dragInput != null)
            {
                dragInput.DragStarted -= OnDragStarted;
                dragInput.Dragged -= OnDragged;
                dragInput.DragEnded -= OnDragEnded;
            }
        }

        /// <summary>
        /// Grabbing a connected Plug does not disconnect it — only
        /// remembers it as <see cref="originalSocket"/> so <see cref="OnDragged"/>
        /// can decide, frame by frame, whether the drag has actually moved
        /// far enough away to count as unplugging (see
        /// <see cref="socketDetachRadius"/>). Also elevates the Plug's own
        /// sprites to <see cref="draggingSortingOrder"/> — always the
        /// topmost of the three Plug states — for the duration of the
        /// drag; the final state (connected vs. resting) is only decided
        /// once the drop's outcome is known, in <see cref="ApplyFinalSorting"/>.
        /// </summary>
        private void OnDragStarted()
        {
            originalSocket = cableInfo.Plug.ConnectedSocket;
            recentlyDetachedSocket = null;

            // Sane default in case DragEnded fires before any Dragged
            // event ever updates this (e.g. a same-frame click/release).
            lastPointerWorldPos = cableInfo.Plug.transform.position;

            SetPlugSorting(EffectiveDraggingSortingOrder());
        }

        /// <summary>
        /// Applies the Plug's sortingOrder for its current, now-settled
        /// state: <see cref="connectedSortingOrder"/> if it ended the drag
        /// connected, otherwise its original authored resting order.
        /// </summary>
        private void ApplyFinalSorting()
        {
            if (cableInfo.Plug.IsConnected)
            {
                SetPlugSorting(connectedSortingOrder);
                return;
            }

            for (var i = 0; i < plugSpriteRenderers.Length; i++)
            {
                plugSpriteRenderers[i].sortingOrder = plugRestingSortingOrders[i];
            }
        }

        private void SetPlugSorting(int order)
        {
            for (var i = 0; i < plugSpriteRenderers.Length; i++)
            {
                plugSpriteRenderers[i].sortingOrder = order;
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
            lastPointerWorldPos = pointerWorldPos;

            if (originalSocket != null)
            {
                var originalSocketPos = (Vector2)originalSocket.ConnectorTransform.position;
                if (Vector2.Distance(pointerWorldPos, originalSocketPos) > socketDetachRadius)
                {
                    // The user has actually dragged the Plug away from its Socket
                    // (not just wiggled it in place): unplug it, exactly once.
                    PlugSocketConnection.Disconnect(cableInfo.Plug);
                    applianceSource?.SetPowered(false);
                    powerFlowEffect?.SetPowered(false);
                    recentlyDetachedSocket = originalSocket;
                    originalSocket = null;
                }
            }

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

        /// <summary>
        /// Resolves the drop, then applies the final sortingOrder — the
        /// dragging-topmost sortingOrder must stay until the outcome
        /// (connected/snapped-back vs. plain fallback) is fully decided,
        /// not the instant the mouse is released.
        /// </summary>
        private void OnDragEnded()
        {
            ResolveDragEnd();
            ApplyFinalSorting();
        }

        private void ResolveDragEnd()
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

            if (originalSocket != null)
            {
                // Never exceeded socketDetachRadius during this drag: this was
                // never actually an unplug, so restore exactly the original
                // connection instead of running the normal drop logic at all.
                var socket = originalSocket;
                originalSocket = null;

                if (TryRouteToSocket(originPos, originCell, socket, grid, out var retainedPath))
                {
                    SnapPlugToSocket(socket, retainedPath);
                }
                else
                {
                    var socketPos = (Vector2)socket.ConnectorTransform.position;
                    plugTransform.position = new Vector3(socketPos.x, socketPos.y, plugTransform.position.z);
                    lastValidPlugPosition = socketPos;
                }

                return;
            }

            if (TryConnectToNearbySocket(originPos, originCell, lastPointerWorldPos, grid))
            {
                return;
            }

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
        /// Attempts to plug into whichever free Socket is nearest
        /// <paramref name="pointerPosition"/> (the raw last pointer
        /// position, not the Plug's own — possibly wall-clamped — current
        /// position; see <see cref="lastPointerWorldPos"/>), within
        /// <see cref="socketAcquisitionRadius"/> of either the Socket's
        /// exact position or its room-interior Approach Point (see
        /// <see cref="TryGetSocketApproachPosition"/>) — a wall-mounted
        /// Socket's exact position is where the Plug can never actually
        /// sit while dragging, so acquisition must also accept "the user
        /// is hovering right where the Plug visually ends up next to it".
        /// Also accepts <see cref="recentlyDetachedSocket"/> (this same
        /// drag's own former Socket) within the more forgiving
        /// <see cref="socketDetachRadius"/>, so drifting a little past the
        /// detach threshold and coming straight back still reconnects.
        /// Returns false (no state or position change) whenever no Socket
        /// is in range, the nearest one is already occupied, or no route
        /// to it exists within the Cable's length — callers then fall back
        /// to the existing plain-drop handling.
        /// </summary>
        private bool TryConnectToNearbySocket(Vector2 originPos, GridCoord originCell, Vector2 pointerPosition, CableRoutingGrid grid)
        {
            if (!grid.IsWalkable(originCell))
            {
                return false;
            }

            if (!TryFindNearestSocket(pointerPosition, socketAcquisitionRadius, grid, out var socket))
            {
                if (recentlyDetachedSocket == null
                    || recentlyDetachedSocket.IsConnected
                    || Vector2.Distance(pointerPosition, recentlyDetachedSocket.ConnectorTransform.position) > socketDetachRadius)
                {
                    return false;
                }

                socket = recentlyDetachedSocket;
            }

            if (socket.IsConnected)
            {
                return false;
            }

            if (!TryRouteToSocket(originPos, originCell, socket, grid, out var worldPath))
            {
                return false;
            }

            if (GridPathfinder.PathLength(worldPath) > cableInfo.CableLength + 0.001f)
            {
                return false;
            }

            // Power Validation runs before Connect: a failed check must
            // leave no Plug/Socket reference behind at all, not connect
            // and immediately disconnect.
            if (applianceSource != null
                && !Octoplug.Power.Connection.PowerValidationService.TryValidate(applianceSource, socket, houseBudget, out var failureReason))
            {
                Debug.LogWarning($"{name}: connection to {socket.name} rejected — {failureReason}", this);
                ApplyPowerRejectFallbackPosition(socket, originPos, originCell, grid);
                return false;
            }

            if (!PlugSocketConnection.Connect(cableInfo.Plug, socket))
            {
                return false;
            }

            applianceSource?.SetPowered(true);
            powerFlowEffect?.SetPowered(true);
            recentlyDetachedSocket = null;
            SnapPlugToSocket(socket, worldPath);
            return true;
        }

        /// <summary>
        /// After a connection is rejected by Power Validation specifically,
        /// pushes the Plug to the nearest walkable/reachable cell that is
        /// still at least <see cref="powerRejectFallbackDistance"/> from the
        /// Socket, so it does not end up visually sitting on top of the
        /// Outlet it was just refused. Sets <see cref="lastValidPlugPosition"/>
        /// and moves the Plug's transform directly (before <c>ResolveDragEnd</c>
        /// re-reads its current position) so no separate "was this drop
        /// rejected for power reasons" flag needs to be threaded through.
        /// A no-op (falls through to the normal drop-fallback chain) if no
        /// such cell exists within <see cref="nearestValidSearchRadius"/>.
        /// </summary>
        private void ApplyPowerRejectFallbackPosition(Octoplug.Power.SocketConnector socket, Vector2 originPos, GridCoord originCell, CableRoutingGrid grid)
        {
            var socketPos = (Vector2)socket.ConnectorTransform.position;
            if (!TryFindPowerRejectFallbackCell(socketPos, originCell, grid, out var fallbackCell))
            {
                return;
            }

            if (!GridPathfinder.TryFindPath(grid, originCell, fallbackCell, out var fallbackPath))
            {
                return;
            }

            var fallbackPos = grid.CellToWorld(fallbackCell);
            var worldPath = BuildWorldPath(originPos, fallbackPath, fallbackPos, grid);
            if (GridPathfinder.PathLength(worldPath) > cableInfo.CableLength + 0.001f)
            {
                return;
            }

            var plugTransform = cableInfo.Plug.transform;
            plugTransform.position = new Vector3(fallbackPos.x, fallbackPos.y, plugTransform.position.z);
            pathRenderer?.Render(worldPath);
            lastValidPlugPosition = fallbackPos;
        }

        private static readonly GridCoord[] CardinalDirections =
        {
            new(1, 0), new(-1, 0), new(0, 1), new(0, -1),
            new(1, 1), new(1, -1), new(-1, 1), new(-1, -1),
        };

        /// <summary>Nearest walkable cell to <paramref name="socketPos"/>'s own cell that is at least <see cref="powerRejectFallbackDistance"/> away from it, searched outward ring-by-ring within <see cref="nearestValidSearchRadius"/>. This is a proximity search (like <see cref="GridPathfinder.TryFindNearestWalkable"/>), not a route, so corner-cutting rules do not apply to the search order itself — the subsequent <see cref="GridPathfinder.TryFindPath"/> call still enforces them for the actual route.</summary>
        private bool TryFindPowerRejectFallbackCell(Vector2 socketPos, GridCoord originCell, CableRoutingGrid grid, out GridCoord result)
        {
            result = default;
            var socketCell = grid.WorldToCell(socketPos);
            var visited = new HashSet<GridCoord> { socketCell };
            var frontier = new Queue<GridCoord>();
            frontier.Enqueue(socketCell);
            var steps = 0;
            var maxSteps = nearestValidSearchRadius * nearestValidSearchRadius * 4;

            while (frontier.Count > 0 && steps < maxSteps)
            {
                var current = frontier.Dequeue();
                steps++;

                foreach (var dir in CardinalDirections)
                {
                    var next = new GridCoord(current.X + dir.X, current.Y + dir.Y);
                    if (!visited.Add(next))
                    {
                        continue;
                    }

                    if (Mathf.Abs(next.X - socketCell.X) > nearestValidSearchRadius || Mathf.Abs(next.Y - socketCell.Y) > nearestValidSearchRadius)
                    {
                        continue;
                    }

                    if (grid.IsWalkable(next))
                    {
                        var candidatePos = grid.CellToWorld(next);
                        if (Vector2.Distance(candidatePos, socketPos) >= powerRejectFallbackDistance
                            && GridPathfinder.TryFindPath(grid, originCell, next, out _))
                        {
                            result = next;
                            return true;
                        }
                    }

                    frontier.Enqueue(next);
                }
            }

            return false;
        }

        /// <summary>
        /// Routes from Origin to <paramref name="socket"/>'s exact world
        /// position: resolves its room-interior Approach Point (see
        /// <see cref="TryGetSocketApproachPosition"/>) and paths to that
        /// cell, then extends the route with a final orthogonal jog to the
        /// Socket's exact position — the Socket is always the route's
        /// terminal point, never a cell the path continues through, so
        /// this can never open a new way to cross the wall to whatever is
        /// on its far side. Returns false if no such route exists; does
        /// not check <see cref="Octoplug.Power.CableInfo.CableLength"/> or
        /// occupancy — callers decide those.
        /// </summary>
        private bool TryRouteToSocket(Vector2 originPos, GridCoord originCell, Octoplug.Power.SocketConnector socket, CableRoutingGrid grid, out List<Vector2> worldPath)
        {
            worldPath = null;
            var socketPos = (Vector2)socket.ConnectorTransform.position;
            if (!TryGetSocketApproachCell(socketPos, grid, out var approachCell))
            {
                return false;
            }

            if (!GridPathfinder.TryFindPath(grid, originCell, approachCell, out var cellPath))
            {
                return false;
            }

            worldPath = BuildWorldPath(originPos, cellPath, socketPos, grid);
            return true;
        }

        /// <summary>
        /// The nearest walkable cell to <paramref name="socketPos"/>'s own
        /// cell, searched within <see cref="socketApproachSearchRadius"/>
        /// cells only — deliberately short (wall-thickness scale, not
        /// <see cref="nearestValidSearchRadius"/>'s room-scale radius) so a
        /// Socket mounted on a shared wall resolves to the Approach Point
        /// on its own room's interior side, never tunnels through to a
        /// walkable cell in a different room on the wall's far side.
        /// Returns the Socket's own cell unchanged (true) when it is
        /// already walkable.
        /// </summary>
        private bool TryGetSocketApproachCell(Vector2 socketPos, CableRoutingGrid grid, out GridCoord approachCell)
        {
            approachCell = grid.WorldToCell(socketPos);
            return grid.IsWalkable(approachCell)
                   || GridPathfinder.TryFindNearestWalkable(grid, approachCell, socketApproachSearchRadius, out approachCell);
        }

        /// <summary>World-space Approach Point for <paramref name="socket"/> — see <see cref="TryGetSocketApproachCell"/>.</summary>
        private bool TryGetSocketApproachPosition(Octoplug.Power.SocketConnector socket, CableRoutingGrid grid, out Vector2 approachPos)
        {
            approachPos = default;
            if (!TryGetSocketApproachCell((Vector2)socket.ConnectorTransform.position, grid, out var approachCell))
            {
                return false;
            }

            approachPos = grid.CellToWorld(approachCell);
            return true;
        }

        /// <summary>Snaps the Plug to <paramref name="socket"/>'s exact position and renders the given route.</summary>
        private void SnapPlugToSocket(Octoplug.Power.SocketConnector socket, List<Vector2> worldPath)
        {
            var socketPos = (Vector2)socket.ConnectorTransform.position;
            var plugTransform = cableInfo.Plug.transform;
            plugTransform.position = new Vector3(socketPos.x, socketPos.y, plugTransform.position.z);
            pathRenderer?.Render(worldPath);
            lastValidPlugPosition = socketPos;
        }

        /// <summary>
        /// Nearest enabled <see cref="Octoplug.Power.SocketConnector"/> to
        /// <paramref name="fromPosition"/> within <paramref name="radius"/>
        /// world units of either its exact position or its room-interior
        /// Approach Point (see <see cref="TryGetSocketApproachPosition"/>),
        /// whichever is closer — regardless of whether it is already
        /// occupied; the caller decides what to do with an occupied
        /// result. Sockets have no Collider2D by design (see the
        /// Connection / Power Domain Map), so this is a plain distance
        /// scan; it only runs once per drag release, never per frame.
        /// </summary>
        private bool TryFindNearestSocket(Vector2 fromPosition, float radius, CableRoutingGrid grid, out Octoplug.Power.SocketConnector socket)
        {
            socket = null;
            var closestSqrDistance = radius * radius;

#if UNITY_2023_1_OR_NEWER
            var candidates = Object.FindObjectsByType<Octoplug.Power.SocketConnector>(FindObjectsSortMode.None);
#else
            var candidates = Object.FindObjectsOfType<Octoplug.Power.SocketConnector>();
#endif
            foreach (var candidate in candidates)
            {
                var candidatePos = (Vector2)candidate.ConnectorTransform.position;
                var bestSqrDistance = (candidatePos - fromPosition).sqrMagnitude;

                if (TryGetSocketApproachPosition(candidate, grid, out var approachPos))
                {
                    var sqrToApproach = (approachPos - fromPosition).sqrMagnitude;
                    if (sqrToApproach < bestSqrDistance)
                    {
                        bestSqrDistance = sqrToApproach;
                    }
                }

                if (bestSqrDistance <= closestSqrDistance)
                {
                    closestSqrDistance = bestSqrDistance;
                    socket = candidate;
                }
            }

            return socket != null;
        }

        /// <summary>
        /// Builds the full world-space waypoint list: the exact Origin
        /// point, each interior grid-cell center on the route, and the
        /// exact target point — with a single axis-aligned bend inserted
        /// at each end where the exact continuous position does not
        /// already line up with its nearest grid cell center, so the
        /// rendered cable is never diagonal even over that sub-cell gap.
        /// </summary>
        private List<Vector2> BuildWorldPath(Vector2 originPos, IReadOnlyList<GridCoord> cellPath, Vector2 targetContinuous, CableRoutingGrid grid)
        {
            var worldPath = new List<Vector2> { originPos };

            // The sub-cell gap between Origin's exact position and the
            // first cell center is the only place an artificial orthogonal
            // bend belongs here — every cell-to-cell step in the middle
            // (including the last one) is a real step the route actually
            // took, diagonal or not, and must be appended as-is so a
            // diagonal step renders as one clean segment instead of being
            // rewritten into an L-shaped jog.
            AppendOrthogonalJog(worldPath, grid.CellToWorld(cellPath[0]));

            for (var i = 1; i < cellPath.Count; i++)
            {
                worldPath.Add(grid.CellToWorld(cellPath[i]));
            }

            // The sub-cell gap between the last cell center and the exact
            // target position (e.g. a Socket's precise mount point) is the
            // other real sub-cell gap that needs reconciling.
            AppendOrthogonalJog(worldPath, targetContinuous);

            return worldPath;
        }

        /// <summary>
        /// Appends <paramref name="to"/> to the path, first inserting one
        /// axis-aligned bend point if hopping directly from the path's
        /// current last point to <paramref name="to"/> would be diagonal —
        /// but only when both the horizontal and vertical gap are
        /// meaningful (beyond <see cref="minRenderSegmentLength"/>). This
        /// is the *only* place a non-8-direction angle is allowed to occur
        /// (Origin's or a Socket's exact continuous position is never
        /// grid-aligned to begin with); when one gap is small — e.g. a
        /// Wall Outlet's Approach Point sitting a fraction of a cell off
        /// from the Socket's exact mount point — inserting a visible bend
        /// for it just to reach an exactly-orthogonal angle produces a
        /// short "hook" that reads as a rendering glitch, so it is
        /// absorbed into a single direct segment instead.
        /// </summary>
        private void AppendOrthogonalJog(List<Vector2> path, Vector2 to)
        {
            var from = path[^1];
            var dx = Mathf.Abs(from.x - to.x);
            var dy = Mathf.Abs(from.y - to.y);

            if (dx > minRenderSegmentLength && dy > minRenderSegmentLength)
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
