using System;
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
    /// <c>GetComponent</c>, same GameObject) and whichever owner exists —
    /// a Product's <see cref="Octoplug.Power.ApplianceSource"/> or a
    /// <see cref="Octoplug.Power.PowerStrip"/> (both found via
    /// <c>GetComponentInParent</c>; exactly one, never both, for any real
    /// Cable instance) — reflecting the real Powered result of
    /// <see cref="Octoplug.Power.Connection.PowerValidationService"/> plus
    /// live-upstream-source availability, re-evaluated whenever a
    /// connection is made/broken here or a PowerStrip upstream changes its
    /// own Powered state (see <see cref="RefreshPoweredFromUpstream"/>).
    /// </summary>
    public class CableRoutingController : MonoBehaviour
    {
        [SerializeField]
        private Octoplug.Power.CableInfo cableInfo;

        /// <summary>
        /// Read-only access to this Cable's Origin/Plug/CableLength — used
        /// by <see cref="PowerStripHeadController"/> to keep the Plug's
        /// world position independent of the owning PowerStrip's Head
        /// movement (the Plug is a descendant of the Head-draggable root
        /// purely for hierarchy/prefab reasons, not because moving the
        /// Head should drag it along).
        /// </summary>
        public Octoplug.Power.CableInfo CableInfo => cableInfo;

        [SerializeField]
        private Octoplug.Power.Input.PlugDragInput dragInput;

        [SerializeField]
        private CablePathRenderer pathRenderer;

        [SerializeField]
        [Tooltip("How many grid cells outward to search for a valid cell when the pointer or release point lands outside walkable space.")]
        private int nearestValidSearchRadius = 24;

        [SerializeField]
        [Tooltip("Multiplier applied to each Socket's authored visual/hit radius for magnetic acquisition.")]
        [Min(0f)]
        private float socketVisualRadiusMultiplier = 1f;

        [SerializeField]
        [Tooltip("Additional magnetic acquisition padding measured in Grid cell sizes. Raised from the original 0.5 placeholder so first-touch acquisition is at least as generous as socketDetachRadius's forgiving-reconnect radius (Mini Metro-style: intent should win before the pointer visually reaches the Wall).")]
        [Min(0f)]
        private float socketCellPaddingMultiplier = 1.5f;

        [SerializeField]
        [Tooltip("World-unit radius, from a connected Socket, that a grabbed Plug can wander within without actually detaching. This is the reconnect hysteresis threshold; normal acquisition uses each Socket's visual size plus Grid-cell padding.")]
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
        private List<Vector2> originalSocketPath;

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
        /// instead of the normal visual/cell-size acquisition radius —
        /// "went a little too far and came right back" should not require
        /// re-acquiring more precisely than the wiggle that was still fine
        /// a moment earlier. Any other Socket still uses its computed
        /// acquisition radius. Cleared at the start of every new
        /// drag and the moment any connection succeeds.
        /// </summary>
        private Octoplug.Power.SocketConnector recentlyDetachedSocket;

        /// <summary>
        /// True once this drag's raw pointer has moved beyond
        /// <see cref="socketDetachRadius"/> from <see cref="originalSocket"/>.
        /// Sticky for the remainder of the drag. Recorded only — the actual
        /// <see cref="PlugSocketConnection.Disconnect"/>/Powered mutation is
        /// deferred to <see cref="ResolveDragEnd"/> so no graph, Powered, or
        /// Flow state changes while the pointer is still down (transactional
        /// drag: only Pointer Up may mutate the graph).
        /// </summary>
        private bool driftedFromOriginalSocket;

        /// <summary>Stable Grid anchor cache for the ordinary (non-Socket) drag route — cleared at drag start; see <see cref="OnDragged"/>.</summary>
        private GridCoord cachedLooseOriginCell;
        private GridCoord cachedLooseTargetCell;
        private List<GridCoord> cachedLooseCellPath;
        private bool hasCachedLooseRoute;

        private SpriteRenderer[] plugSpriteRenderers;

        /// <summary>The Plug sprites' authored sortingOrder, captured once at Start — the "not connected, not dragging" resting state.</summary>
        private int[] plugRestingSortingOrders;

        private CablePowerFlowEffect powerFlowEffect;
        private Octoplug.Power.ApplianceSource applianceSource;
        private Octoplug.Power.PowerStrip powerStrip;
        private Octoplug.Power.HousePowerBudget houseBudget;

        /// <summary>
        /// Delivers a machine-readable rejection reason to future Alert UI or
        /// other observers. No player-facing wording is authored here.
        /// </summary>
        public static event Action<ConnectionFailureReason>
            AnyConnectionRejected;

        public event Action<ConnectionFailureReason> ConnectionRejected;

        public ConnectionFailureReason LastFailureReason { get; private set; }

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
        /// visual/cell-size acquisition radius, regardless of user precision.
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
            powerStrip = GetComponentInParent<Octoplug.Power.PowerStrip>();
#if UNITY_2023_1_OR_NEWER
            houseBudget = UnityEngine.Object.FindFirstObjectByType<Octoplug.Power.HousePowerBudget>();
#else
            houseBudget = UnityEngine.Object.FindObjectOfType<Octoplug.Power.HousePowerBudget>();
#endif

            plugSpriteRenderers = cableInfo.Plug.GetComponentsInChildren<SpriteRenderer>(true);
            plugRestingSortingOrders = new int[plugSpriteRenderers.Length];
            for (var i = 0; i < plugSpriteRenderers.Length; i++)
            {
                plugRestingSortingOrders[i] = plugSpriteRenderers[i].sortingOrder;
            }

            ResolveProductVisualMaxSortingOrder();
            pathRenderer?.Clear();
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
            if (!cableInfo.Plug.IsConnected)
            {
                return;
            }

            var socket = cableInfo.Plug.ConnectedSocket;
            var powered = PowerValidationService.TryValidateGraphConnection(powerStrip, socket, out var failureReason)
                && PowerValidationService.TryValidate(applianceSource, powerStrip, socket, houseBudget, out failureReason);
            if (!powered)
            {
                Debug.LogWarning($"{name}: authored connection failed validation on load — {failureReason}. Leaving physically Connected but not Powered.", this);
            }

            ApplyPowered(powered && ResolvePoweredForConnectedSocket(socket));
        }

        /// <summary>
        /// Applies the resulting Powered/Flow state to whichever owner
        /// exists (Product's <see cref="Octoplug.Power.ApplianceSource"/>,
        /// a <see cref="Octoplug.Power.PowerStrip"/>, or neither) — the
        /// single place either owner's Powered state is ever written from.
        /// When a PowerStrip's own Powered state actually changes, cascades
        /// the new value to every Product/strip currently connected to any
        /// of its Sockets.
        /// </summary>
        private void ApplyPowered(bool powered)
        {
            applianceSource?.SetPowered(powered);
            powerFlowEffect?.SetPowered(powered);

            if (powerStrip != null)
            {
                var changed = powerStrip.IsPowered != powered;
                powerStrip.SetPowered(powered);
                if (changed)
                {
                    CascadeToDownstream(powerStrip, new HashSet<Octoplug.Power.PowerStrip>());
                }
            }
        }

        /// <summary>
        /// Re-evaluates this Cable's own Powered/Flow state purely from its
        /// current upstream Socket connection — no new route/Cable-length/
        /// wattage-budget checks, since those only apply at the moment a
        /// connection is actually made. Used when a PowerStrip somewhere
        /// upstream just changed its own Powered state and this Cable's
        /// connection needs to react.
        /// </summary>
        public void RefreshPoweredFromUpstream()
        {
            if (cableInfo == null || cableInfo.Plug == null || !cableInfo.Plug.IsConnected)
            {
                return;
            }

            ApplyPowered(ResolvePoweredForConnectedSocket(cableInfo.Plug.ConnectedSocket));
        }

        /// <summary>
        /// The final Powered value for a Plug already known to be validly
        /// (or authored-) connected to <paramref name="socket"/> — separate
        /// from whether the physical connection itself was allowed
        /// (<see cref="Octoplug.Power.Connection.PowerValidationService.TryValidate"/>,
        /// wattage-only): a Plug can be physically connected to a
        /// currently-unpowered upstream Socket and simply stay not-Powered
        /// until that upstream chain goes live.
        /// </summary>
        private static bool ResolvePoweredForConnectedSocket(Octoplug.Power.SocketConnector socket)
        {
            return socket != null && Octoplug.Power.Connection.PowerValidationService.IsSocketSourceLive(socket);
        }

        /// <summary>
        /// Propagates <paramref name="strip"/>'s own Powered-state change to
        /// every Product/strip currently plugged into one of its Sockets —
        /// generic over component type (Product vs. nested strip), never
        /// branching on a specific Prefab name.
        /// </summary>
        private static void CascadeToDownstream(
            Octoplug.Power.PowerStrip strip,
            HashSet<Octoplug.Power.PowerStrip> visited)
        {
            if (strip == null || !visited.Add(strip))
            {
                return;
            }

            foreach (var socket in strip.ActiveSockets)
            {
                var plug = socket != null ? socket.ConnectedPlug : null;
                if (plug == null)
                {
                    continue;
                }

                var downstreamController = plug.GetComponentInParent<CableRoutingController>();
                if (downstreamController == null)
                {
                    continue;
                }

                var downstreamStrip = plug.GetComponentInParent<Octoplug.Power.PowerStrip>();
                downstreamController.RefreshPoweredFromUpstreamWithoutCascade();
                if (downstreamStrip != null)
                {
                    CascadeToDownstream(downstreamStrip, visited);
                }
            }
        }

        private void RefreshPoweredFromUpstreamWithoutCascade()
        {
            if (cableInfo == null || cableInfo.Plug == null || !cableInfo.Plug.IsConnected)
            {
                ApplyPoweredWithoutCascade(false);
                return;
            }

            ApplyPoweredWithoutCascade(ResolvePoweredForConnectedSocket(cableInfo.Plug.ConnectedSocket));
        }

        private void ApplyPoweredWithoutCascade(bool powered)
        {
            applianceSource?.SetPowered(powered);
            powerStrip?.SetPowered(powered);
            powerFlowEffect?.SetPowered(powered);
        }

        /// <summary>
        /// Re-renders the Base/Flow path between this Cable's current
        /// Origin and its Plug's current position — used when the owning
        /// PowerStrip's Head (and therefore this Cable's Origin) moves.
        /// Deliberately does not enforce <see cref="Octoplug.Power.CableInfo.CableLength"/>
        /// here (whether moving a Head beyond its Cable's length should
        /// disconnect/block/stretch is an open Human Decision — see
        /// `meta.md`); a no-op if no route currently exists (e.g. the Head
        /// moved to a disconnected room) so the last-rendered path is left
        /// in place rather than cleared.
        /// </summary>
        public void RecomputePathFromCurrentOrigin()
        {
            var grid = ResolveGrid();
            if (grid == null || cableInfo == null || cableInfo.Origin == null || cableInfo.Plug == null)
            {
                return;
            }

            var originPos = (Vector2)cableInfo.Origin.position;
            var originCell = grid.WorldToCell(originPos);
            if (!grid.IsWalkable(originCell))
            {
                return;
            }

            // A connected Plug sits at its Socket's exact mount point, which
            // is deliberately a non-walkable (wall) cell — the same
            // Approach-Point routing TryConnectToNearbySocket/TryRouteToSocket
            // already use for that case is required here too, or the direct
            // Origin-to-Plug-cell search below would always fail to find a
            // route to it.
            if (cableInfo.Plug.IsConnected)
            {
                if (TryRouteToSocket(originPos, originCell, cableInfo.Plug.ConnectedSocket, grid, out var connectedWorldPath))
                {
                    pathRenderer?.Render(connectedWorldPath);
                }

                return;
            }

            var plugPos = (Vector2)cableInfo.Plug.transform.position;
            var plugCell = grid.WorldToCell(plugPos);

            if (!GridPathfinder.TryFindPath(grid, originCell, plugCell, out var cellPath))
            {
                return;
            }

            var worldPath = BuildWorldPath(originPos, cellPath, plugPos, grid);
            pathRenderer?.Render(worldPath);
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
            originalSocketPath = null;
            driftedFromOriginalSocket = false;
            hasCachedLooseRoute = false;
            if (originalSocket != null)
            {
                var grid = ResolveGrid();
                var originPos = (Vector2)cableInfo.Origin.position;
                if (grid != null)
                {
                    TryRouteToSocket(
                        originPos,
                        grid.WorldToCell(originPos),
                        originalSocket,
                        grid,
                        out originalSocketPath);
                }
            }

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

        /// <summary>
        /// Corrects a loose (not authored-connected) Plug whose initial
        /// world position falls outside the walkable Room area, so
        /// gameplay never starts with a visibly detached Plug/Head/Product.
        /// Only the runtime Plug transform is moved — the authored Prefab
        /// Transform, and the owning Product/PowerStrip root, are never
        /// touched or saved. An authored-connected Plug is left entirely to
        /// <see cref="ValidateInitialConnection"/>'s own Approach-Point
        /// routing instead, since its resting position is meaningful
        /// (it sits at its Socket).
        /// </summary>
        private void ValidateInitialPlugPosition()
        {
            var grid = ResolveGrid();
            if (grid == null || cableInfo.Plug.IsConnected)
            {
                return;
            }

            var plugCell = grid.WorldToCell(lastValidPlugPosition);
            if (grid.IsWalkable(plugCell))
            {
                return;
            }

            var originPos = (Vector2)cableInfo.Origin.position;
            var originCell = grid.WorldToCell(originPos);
            if (!grid.IsWalkable(originCell))
            {
                Debug.LogWarning($"{name}: authored Plug position {lastValidPlugPosition} is not on a walkable grid cell, and Origin itself is not walkable either — cannot correct at runtime. Left unchanged.", this);
                return;
            }

            // Nearest walkable cell that is also actually path-reachable
            // from Origin within this Cable's own length — the same
            // room-aware, Door-only-crossing safety net already used for a
            // live drop, so this correction can never land the Plug in a
            // different Room or on the far side of a Wall. Falls back to
            // Origin's own position (always walkable, zero-length) if even
            // that search finds nothing.
            var correctedPos = GridPathfinder.TryFindNearestValidDropCell(
                grid,
                originCell,
                plugCell,
                cableInfo.CableLength,
                nearestValidSearchRadius,
                out var correctedCell)
                ? grid.CellToWorld(correctedCell)
                : originPos;

            var plugTransform = cableInfo.Plug.transform;
            plugTransform.position = new Vector3(correctedPos.x, correctedPos.y, plugTransform.position.z);
            lastValidPlugPosition = correctedPos;
            Debug.LogWarning($"{name}: authored Plug position was outside the walkable Room area — corrected at runtime to the nearest valid in-Room position {correctedPos}. This is a runtime-only Plug move; the authored Prefab Transform and the Product/PowerStrip root are unchanged.", this);
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

            if (originalSocket != null && !driftedFromOriginalSocket)
            {
                var originalSocketPos = (Vector2)originalSocket.ConnectorTransform.position;
                if (Vector2.Distance(pointerWorldPos, originalSocketPos) > socketDetachRadius)
                {
                    // The user has actually dragged the Plug away from its
                    // Socket (not just wiggled it in place) — record it only.
                    // The real Disconnect/Powered mutation happens exactly
                    // once, at release (see ResolveDragEnd): no graph,
                    // Powered, or Flow state may change while the pointer is
                    // still down.
                    driftedFromOriginalSocket = true;
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

            // Socket acquisition always uses the raw pointer before ordinary
            // Grid/wall clamping. Preview is visual only: connection and power
            // state remain unchanged until Pointer Up re-runs validation.
            if ((originalSocket == null || driftedFromOriginalSocket)
                && TryFindNearestSocket(
                    originPos,
                    originCell,
                    pointerWorldPos,
                    grid,
                    out var previewSocket,
                    out var previewPath,
                    out _,
                    out _))
            {
                PreviewPlugAtSocket(previewSocket, previewPath);
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

            // Stable logical anchor: the interior A* route is only
            // recomputed when the resolved origin/target cell pair actually
            // changes, not every pointer pixel. Continuous pointer movement
            // inside the same target cell reuses the cached cell path.
            List<GridCoord> cellPath;
            if (hasCachedLooseRoute
                && cachedLooseOriginCell == originCell
                && cachedLooseTargetCell == targetCell)
            {
                cellPath = cachedLooseCellPath;
            }
            else if (GridPathfinder.TryFindPath(grid, originCell, targetCell, out cellPath))
            {
                cachedLooseOriginCell = originCell;
                cachedLooseTargetCell = targetCell;
                cachedLooseCellPath = cellPath;
                hasCachedLooseRoute = true;
            }
            else
            {
                hasCachedLooseRoute = false;
                return; // No valid route (e.g. different room with no door); hold last valid position.
            }

            // alwaysDropFinalCellCenter=true: the target cell's own center
            // point is never part of the rendered/measured polyline here —
            // the terminal connector always runs straight from the last
            // *stable* (cell-boundary) point to the smoothly pointer-following
            // Plug visual. Without this, the target cell's center point
            // would toggle in/out of the path (and the final jog's elbow
            // side with it) every time the raw pointer crossed the
            // corresponding sub-cell distance threshold, purely from pixel
            // movement inside one already-stable cell.
            var worldPath = BuildWorldPath(originPos, cellPath, targetContinuous, grid, alwaysDropFinalCellCenter: true);
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

            if (originalSocket != null && !driftedFromOriginalSocket)
            {
                // Never exceeded socketDetachRadius during this drag: this was
                // never actually an unplug, so restore exactly the original
                // connection instead of running the normal drop logic at all.
                var socket = originalSocket;
                originalSocket = null;

                if (TryRouteToSocket(originPos, originCell, socket, grid, out var retainedPath))
                {
                    originalSocketPath = retainedPath;
                }

                if (originalSocketPath != null && originalSocketPath.Count >= 2)
                {
                    SnapPlugToSocket(socket, originalSocketPath);
                }
                else
                {
                    cableInfo.Plug.transform.position =
                        socket.ConnectorTransform.position;
                    RecomputePathFromCurrentOrigin();
                }

                originalSocketPath = null;
                return;
            }

            if (originalSocket != null && driftedFromOriginalSocket)
            {
                // The drag left the original Socket's radius at some point,
                // but per the transactional-drag contract nothing was
                // mutated yet — perform the single deferred Disconnect/
                // Powered-false now, exactly once, before running ordinary
                // release resolution below (which may still reconnect to
                // this same Socket via the recentlyDetachedSocket hysteresis,
                // connect elsewhere, or fall back to a loose drop).
                PlugSocketConnection.Disconnect(cableInfo.Plug);
                ApplyPowered(false);
                recentlyDetachedSocket = originalSocket;
                originalSocket = null;
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
        /// position; see <see cref="lastPointerWorldPos"/>), within that
        /// Socket's visual-size plus Grid-cell acquisition radius. A
        /// wall-mounted Socket's exact position is where the Plug can never
        /// actually sit while dragging, so acquisition is based on the raw
        /// pointer rather than the clamped Plug transform.
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

            if (!TryFindNearestSocket(
                    originPos,
                    originCell,
                    pointerPosition,
                    grid,
                    out var socket,
                    out var worldPath,
                    out var rejectedSocket,
                    out var rejectedReason))
            {
                if (recentlyDetachedSocket == null
                    || recentlyDetachedSocket.IsConnected
                    || !recentlyDetachedSocket.IsPointerOnApproachSide(
                        pointerPosition)
                    || Vector2.Distance(
                        pointerPosition,
                        recentlyDetachedSocket.ConnectorTransform.position)
                        > socketDetachRadius
                    || !PowerValidationService.TryValidateGraphConnection(
                        powerStrip,
                        recentlyDetachedSocket,
                        out _)
                    || !TryRouteToSocket(
                        originPos,
                        originCell,
                        recentlyDetachedSocket,
                        grid,
                        out worldPath)
                    || GridPathfinder.PathLength(worldPath)
                        > cableInfo.CableLength + 0.001f
                    || !PowerValidationService.TryValidate(
                        applianceSource,
                        powerStrip,
                        recentlyDetachedSocket,
                        houseBudget,
                        out _))
                {
                    if (rejectedSocket != null)
                    {
                        RejectConnection(
                            rejectedReason,
                            rejectedSocket,
                            originPos,
                            originCell,
                            grid);
                    }

                    return false;
                }

                socket = recentlyDetachedSocket;
            }

            LastFailureReason = ConnectionFailureReason.None;
            if (!PowerValidationService.TryValidateGraphConnection(powerStrip, socket, out var graphFailure))
            {
                RejectConnection(graphFailure, socket, originPos, originCell, grid);
                return false;
            }

            // Power Validation runs before Connect: a failed check must
            // leave no Plug/Socket reference behind at all, not connect
            // and immediately disconnect.
            if (!PowerValidationService.TryValidate(applianceSource, powerStrip, socket, houseBudget, out var failureReason))
            {
                RejectConnection(failureReason, socket, originPos, originCell, grid);
                return false;
            }

            if (!PlugSocketConnection.Connect(cableInfo.Plug, socket))
            {
                return false;
            }

            recentlyDetachedSocket = null;
            SnapPlugToSocket(socket, worldPath);
            ApplyPowered(ResolvePoweredForConnectedSocket(socket));
            return true;
        }

        private void RejectConnection(
            ConnectionFailureReason reason,
            Octoplug.Power.SocketConnector socket,
            Vector2 originPos,
            GridCoord originCell,
            CableRoutingGrid grid)
        {
            LastFailureReason = reason;
            Debug.LogWarning($"{name}: connection to {socket.name} rejected — {reason}.", this);
            ConnectionRejected?.Invoke(reason);
            AnyConnectionRejected?.Invoke(reason);
            ApplyPowerRejectFallbackPosition(socket, originPos, originCell, grid);
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
        /// position: resolves its room-interior approach cell and paths to
        /// that cell, then extends the route with a final orthogonal jog to the
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
            if (!TryGetSocketApproachCell(socket, grid, out var approachCell))
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
        private bool TryGetSocketApproachCell(
            Octoplug.Power.SocketConnector socket,
            CableRoutingGrid grid,
            out GridCoord approachCell)
        {
            var socketPos = (Vector2)socket.ConnectorTransform.position;
            var socketCell = grid.WorldToCell(socketPos);
            approachCell = socketCell;
            if (grid.IsWalkable(socketCell))
            {
                return true;
            }

            var found = false;
            var bestSqrDistance = float.PositiveInfinity;
            for (var x = -socketApproachSearchRadius;
                 x <= socketApproachSearchRadius;
                 x++)
            {
                for (var y = -socketApproachSearchRadius;
                     y <= socketApproachSearchRadius;
                     y++)
                {
                    var candidate = new GridCoord(
                        socketCell.X + x,
                        socketCell.Y + y);
                    if (!grid.IsWalkable(candidate))
                    {
                        continue;
                    }

                    var candidatePos = grid.CellToWorld(candidate);
                    if (socket.IsTerminalEndpoint
                        && Vector2.Dot(
                            candidatePos - socketPos,
                            socket.ApproachDirection) <= 0f)
                    {
                        continue;
                    }

                    var sqrDistance =
                        (candidatePos - socketPos).sqrMagnitude;
                    if (sqrDistance < bestSqrDistance)
                    {
                        bestSqrDistance = sqrDistance;
                        approachCell = candidate;
                        found = true;
                    }
                }
            }

            return found;
        }

        /// <summary>
        /// Visually previews a valid Socket candidate without changing the
        /// graph, Powered state, or the last committed loose-Plug position.
        /// </summary>
        private void PreviewPlugAtSocket(
            Octoplug.Power.SocketConnector socket,
            List<Vector2> worldPath)
        {
            var socketPos = (Vector2)socket.ConnectorTransform.position;
            var plugTransform = cableInfo.Plug.transform;
            plugTransform.position = new Vector3(
                socketPos.x,
                socketPos.y,
                plugTransform.position.z);
            pathRenderer?.Render(worldPath);
        }

        /// <summary>Snaps the Plug to <paramref name="socket"/>'s exact position and renders the given route.</summary>
        private void SnapPlugToSocket(Octoplug.Power.SocketConnector socket, List<Vector2> worldPath)
        {
            PreviewPlugAtSocket(socket, worldPath);
            lastValidPlugPosition =
                (Vector2)socket.ConnectorTransform.position;
        }

        /// <summary>
        /// Finds the nearest enabled, free Socket that is fully valid for the
        /// proposed connection. Every in-range candidate is considered, so a
        /// graph- or power-invalid nearer Socket cannot hide a farther valid
        /// Socket. If none is valid, the nearest typed graph/power rejection is
        /// returned separately for the Alert path; silent route/side/length
        /// failures are excluded.
        /// </summary>
        private bool TryFindNearestSocket(
            Vector2 originPos,
            GridCoord originCell,
            Vector2 fromPosition,
            CableRoutingGrid grid,
            out Octoplug.Power.SocketConnector socket,
            out List<Vector2> worldPath,
            out Octoplug.Power.SocketConnector rejectedSocket,
            out ConnectionFailureReason rejectedReason)
        {
            socket = null;
            worldPath = null;
            rejectedSocket = null;
            rejectedReason = ConnectionFailureReason.None;
            var closestSqrDistance = float.PositiveInfinity;
            var closestRejectedSqrDistance = float.PositiveInfinity;

#if UNITY_2023_1_OR_NEWER
            var candidates = UnityEngine.Object.FindObjectsByType<Octoplug.Power.SocketConnector>(FindObjectsSortMode.None);
#else
            var candidates = UnityEngine.Object.FindObjectsOfType<Octoplug.Power.SocketConnector>();
#endif
            foreach (var candidate in candidates)
            {
                if (!candidate.isActiveAndEnabled
                    || !candidate.IsActiveSocket
                    || candidate.IsConnected
                    || !candidate.IsPointerOnApproachSide(fromPosition))
                {
                    continue;
                }

                var candidatePos =
                    (Vector2)candidate.ConnectorTransform.position;
                var sqrDistance =
                    (candidatePos - fromPosition).sqrMagnitude;
                // Floor first-touch acquisition at socketDetachRadius so
                // initial acquisition is never stricter than the forgiving
                // reconnect radius (Mini Metro-style: Socket intent should
                // win generously before the pointer visually reaches a Wall).
                var radius = Mathf.Max(
                    socketDetachRadius,
                    candidate.GetAcquisitionRadius(
                        socketVisualRadiusMultiplier,
                        grid.CellSize,
                        socketCellPaddingMultiplier));
                if (sqrDistance > radius * radius
                    || !TryRouteToSocket(
                        originPos,
                        originCell,
                        candidate,
                        grid,
                        out var candidatePath)
                    || GridPathfinder.PathLength(candidatePath)
                        > cableInfo.CableLength + 0.001f)
                {
                    continue;
                }

                var valid = PowerValidationService.TryValidateGraphConnection(
                    powerStrip,
                    candidate,
                    out var candidateFailure);
                if (valid)
                {
                    valid = PowerValidationService.TryValidate(
                        applianceSource,
                        powerStrip,
                        candidate,
                        houseBudget,
                        out candidateFailure);
                }

                if (!valid)
                {
                    if (sqrDistance < closestRejectedSqrDistance
                        || Mathf.Approximately(
                            sqrDistance,
                            closestRejectedSqrDistance)
                        && (rejectedSocket == null
                            || candidate.GetInstanceID()
                            < rejectedSocket.GetInstanceID()))
                    {
                        closestRejectedSqrDistance = sqrDistance;
                        rejectedSocket = candidate;
                        rejectedReason = candidateFailure;
                    }

                    continue;
                }

                if (sqrDistance < closestSqrDistance
                    || Mathf.Approximately(
                        sqrDistance,
                        closestSqrDistance)
                    && (socket == null
                        || candidate.GetInstanceID()
                        < socket.GetInstanceID()))
                {
                    closestSqrDistance = sqrDistance;
                    socket = candidate;
                    worldPath = candidatePath;
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
        private List<Vector2> BuildWorldPath(
            Vector2 originPos,
            IReadOnlyList<GridCoord> cellPath,
            Vector2 targetContinuous,
            CableRoutingGrid grid,
            bool alwaysDropFinalCellCenter = false)
        {
            var worldPath = new List<Vector2> { originPos };

            // The sub-cell gap between Origin's exact position and the
            // first cell center is the only place an artificial orthogonal
            // bend belongs here — every cell-to-cell step in the middle
            // (including the last one) is a real step the route actually
            // took, diagonal or not, and must be appended as-is so a
            // diagonal step renders as one clean segment instead of being
            // rewritten into an L-shaped jog. A cell center closer than the
            // visible-segment threshold is absorbed into the first real Grid
            // segment; retaining it creates a sub-width join whose generated
            // LineRenderer geometry visibly spikes.
            var firstCellIndex = 0;
            var firstCellPosition = grid.CellToWorld(cellPath[0]);
            if (cellPath.Count > 1
                && Vector2.Distance(originPos, firstCellPosition)
                    < minRenderSegmentLength)
            {
                firstCellIndex = 1;
            }
            else
            {
                AppendOrthogonalJog(worldPath, firstCellPosition);
                firstCellIndex = 1;
            }

            for (var i = firstCellIndex; i < cellPath.Count; i++)
            {
                worldPath.Add(grid.CellToWorld(cellPath[i]));
            }

            // The sub-cell gap between the last cell center and the exact
            // target position (e.g. a Socket's precise mount point) is the
            // other real sub-cell gap that needs reconciling. Absorb a final
            // sub-width cell-center segment into the preceding real segment;
            // otherwise its direction flips abruptly when the pointer crosses
            // that center even though the A* route itself is stable.
            if (worldPath.Count > 2
                && (alwaysDropFinalCellCenter
                    || Vector2.Distance(worldPath[^1], targetContinuous)
                        < minRenderSegmentLength))
            {
                worldPath.RemoveAt(worldPath.Count - 1);
            }

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

            if (dx > minRenderSegmentLength
                && dy > minRenderSegmentLength)
            {
                var incoming = path.Count > 1
                    ? from - path[^2]
                    : Vector2.zero;
                var continueHorizontal =
                    Mathf.Abs(incoming.x)
                    >= Mathf.Abs(incoming.y);
                path.Add(
                    continueHorizontal
                        ? new Vector2(to.x, from.y)
                        : new Vector2(from.x, to.y));
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
