using Octoplug.Power;
using Octoplug.Power.Connection;
using Octoplug.Power.Grid;
using Octoplug.RoomGeneration;
using UnityEngine;

namespace Octoplug.RoomGeneration.Unity
{
    /// <summary>
    /// Production transaction for runtime equipment. A clone remains absent
    /// from gameplay discovery until its transform, authored socket layout,
    /// occupancy, active hierarchy, and physics state are all ready.
    /// </summary>
    public static class RuntimeEquipmentFactory
    {
        public static GameObject Stage(
            GameObject prefab,
            Vector2 position,
            Quaternion rotation,
            Transform parent)
        {
            if (prefab == null)
            {
                return null;
            }

            using (RuntimeSpawnGate.Begin())
            {
                var instance = Object.Instantiate(
                    prefab,
                    position,
                    rotation,
                    parent);
                instance.SetActive(false);
                instance.transform.SetPositionAndRotation(position, rotation);
                instance.GetComponent<PlacementFootprint>()?.ReleaseReservation();
                return instance;
            }
        }

        public static bool TryFinalizePowerStrip(
            GameObject staged,
            RoomId roomId,
            CableRoutingGrid grid,
            int socketCount,
            out PowerStrip powerStrip,
            out string failure)
        {
            powerStrip = staged != null ? staged.GetComponent<PowerStrip>() : null;
            var footprint = staged != null
                ? staged.GetComponent<PlacementFootprint>()
                : null;
            if (powerStrip == null || footprint == null)
            {
                failure = "production Multitap is missing required components";
                Abort(staged);
                powerStrip = null;
                return false;
            }

            if (!powerStrip.TryInitializeActiveSocketCount(
                    socketCount,
                    out var socketFailure))
            {
                failure = socketFailure.ToString();
                Abort(staged);
                powerStrip = null;
                return false;
            }

            return ActivateAndRegister(staged, powerStrip, roomId, out failure);
        }

        public static bool TryFinalizeWallOutlet(
            GameObject staged,
            RoomId roomId,
            int socketCount,
            out WallOutlet wallOutlet,
            out string failure)
        {
            wallOutlet = staged != null ? staged.GetComponent<WallOutlet>() : null;
            if (wallOutlet == null)
            {
                failure = "production Wall Outlet has no WallOutlet component";
                Abort(staged);
                return false;
            }

            if (!wallOutlet.TryInitializeActiveSocketCount(
                    socketCount,
                    out var socketFailure))
            {
                failure = socketFailure.ToString();
                Abort(staged);
                wallOutlet = null;
                return false;
            }

            return ActivateAndRegister(staged, wallOutlet, roomId, out failure);
        }

        public static bool TryFinalizeProduct(
            GameObject staged,
            RoomId roomId,
            CableRoutingGrid grid,
            out ApplianceSource product,
            out string failure)
        {
            product = staged != null ? staged.GetComponent<ApplianceSource>() : null;
            var footprint = staged != null
                ? staged.GetComponent<PlacementFootprint>()
                : null;
            if (product == null || footprint == null)
            {
                failure = "production Product is missing required components";
                Abort(staged);
                product = null;
                return false;
            }

            if (!footprint.TryReserveAt(grid, staged.transform.position, socketCount: 0))
            {
                failure = "placement reservation changed before acceptance";
                Abort(staged);
                product = null;
                return false;
            }

            return ActivateAndRegister(staged, product, roomId, out failure);
        }

        public static void Abort(GameObject staged)
        {
            if (staged == null)
            {
                return;
            }

            staged.GetComponent<PlacementFootprint>()?.ReleaseReservation();
            staged.SetActive(false);
            if (Application.isPlaying)
            {
                Object.Destroy(staged);
            }
            else
            {
                Object.DestroyImmediate(staged);
            }
        }

        private static bool ActivateAndRegister<T>(
            GameObject staged,
            T component,
            RoomId roomId,
            out string failure)
            where T : Component
        {
            using (RuntimeSpawnGate.Begin())
            {
                staged.SetActive(true);
            }

            Physics2D.SyncTransforms();
            if (!component.gameObject.activeInHierarchy)
            {
                failure = $"{typeof(T).Name} did not become gameplay-active";
                Abort(staged);
                return false;
            }

            RuntimeWorldRegistry.FinalizeRuntimeObject(component, roomId);
            PlugSocketConnection.NotifyTopologyChanged();
            failure = null;
            return true;
        }
    }
}
