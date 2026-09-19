using UnityEngine;

namespace Octoplug.Power
{
    /// <summary>
    /// Marks a Room prefab instance's valid floor area for cable/plug and
    /// power-strip placement. The Collider2D expresses the physical space;
    /// this component is the typed handle other systems (Runtime Grid,
    /// power-strip movement) read it through, per the Collider2D+Grid
    /// split confirmed for this domain.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class RoomArea : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The valid floor area. Defaults to this GameObject's own Collider2D.")]
        private Collider2D floorArea;

        public Collider2D FloorArea
        {
            get
            {
                if (floorArea == null)
                {
                    floorArea = GetComponent<Collider2D>();
                }

                return floorArea;
            }
        }
    }
}
