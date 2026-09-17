using UnityEngine;

namespace Octoplug.Power.Grid
{
    /// <summary>
    /// Scene-level access point for the cable-routing grid. Builds the
    /// grid from every <see cref="RoomArea"/> currently in the scene by
    /// overlaying Wall- and Door-layer colliders inside each room's floor
    /// bounds. Safe to call again after new rooms are generated.
    ///
    /// This is data-population only — no path search is implemented here
    /// yet (see <see cref="CableRoutingGrid"/>).
    /// </summary>
    public class CableRoutingGridService : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("World-unit size of one grid cell.")]
        private float cellSize = 0.25f;

        [SerializeField]
        [Tooltip("Layer used by Wall colliders.")]
        private LayerMask wallMask;

        [SerializeField]
        [Tooltip("Layer used by Door colliders.")]
        private LayerMask doorMask;

        private CableRoutingGrid grid;
        private bool hasBuiltOnce;

        /// <summary>
        /// Convenience scene-wide access so Cable routing does not need to
        /// search the scene for this service. Set on the first instance to
        /// wake up; there is expected to be exactly one per loaded scene.
        /// </summary>
        public static CableRoutingGridService Instance { get; private set; }

        /// <summary>
        /// The grid, guaranteed to have been built at least once before it
        /// is returned — Unity does not order different GameObjects'
        /// Start() calls, so a consumer's Start() may run before this
        /// service's own Start() does.
        /// </summary>
        public CableRoutingGrid Grid
        {
            get
            {
                EnsureBuilt();
                return grid;
            }
        }

        private void Awake()
        {
            Instance = this;
            grid = new CableRoutingGrid(cellSize);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Start()
        {
            EnsureBuilt();
        }

        private void EnsureBuilt()
        {
            if (!hasBuiltOnce)
            {
                RebuildFromScene();
            }
        }

        /// <summary>Rebuilds the grid from every RoomArea currently loaded in the scene.</summary>
        public void RebuildFromScene()
        {
            grid.Clear();
            hasBuiltOnce = true;

#if UNITY_2023_1_OR_NEWER
            var rooms = Object.FindObjectsByType<RoomArea>(FindObjectsSortMode.None);
#else
            var rooms = Object.FindObjectsOfType<RoomArea>();
#endif
            foreach (var room in rooms)
            {
                RebuildFromRoom(room);
            }
        }

        /// <summary>Registers one room's floor area, then overlays its wall and door colliders.</summary>
        public void RebuildFromRoom(RoomArea room)
        {
            if (room == null || room.FloorArea == null)
            {
                return;
            }

            var bounds = room.FloorArea.bounds;
            grid.MarkArea(bounds, GridCellState.Walkable);

            var wallHits = Physics2D.OverlapAreaAll(bounds.min, bounds.max, wallMask);
            foreach (var hit in wallHits)
            {
                grid.MarkArea(hit.bounds, GridCellState.Blocked);
            }

            var doorHits = Physics2D.OverlapAreaAll(bounds.min, bounds.max, doorMask);
            foreach (var hit in doorHits)
            {
                grid.MarkArea(hit.bounds, GridCellState.Door);
            }
        }
    }
}
