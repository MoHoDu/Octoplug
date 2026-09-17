using UnityEngine;

namespace Octoplug.Power
{
    /// <summary>
    /// Named access to the two user Layers this domain adds to
    /// ProjectSettings ("Wall", "Door"), so code never spells out layer
    /// names as raw strings.
    /// </summary>
    public static class PowerLayers
    {
        public const string WallLayerName = "Wall";
        public const string DoorLayerName = "Door";

        public static int WallLayer => LayerMask.NameToLayer(WallLayerName);
        public static int DoorLayer => LayerMask.NameToLayer(DoorLayerName);

        public static LayerMask WallMask => 1 << WallLayer;
        public static LayerMask DoorMask => 1 << DoorLayer;
    }
}
