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

        public CableRoutingGrid Grid => grid ??= new CableRoutingGrid(cellSize);

        private void Start()
        {
            RebuildFromScene();
        }

        /// <summary>Rebuilds the grid from every RoomArea currently loaded in the scene.</summary>
        public void RebuildFromScene()
        {
            Grid.Clear();

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
            Grid.MarkArea(bounds, GridCellState.Walkable);

            var wallHits = Physics2D.OverlapAreaAll(bounds.min, bounds.max, wallMask);
            foreach (var hit in wallHits)
            {
                Grid.MarkArea(hit.bounds, GridCellState.Blocked);
            }

            var doorHits = Physics2D.OverlapAreaAll(bounds.min, bounds.max, doorMask);
            foreach (var hit in doorHits)
            {
                Grid.MarkArea(hit.bounds, GridCellState.Door);
            }
        }
    }
}
