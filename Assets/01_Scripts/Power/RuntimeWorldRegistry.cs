using System;
using System.Collections.Generic;
using Octoplug.RoomGeneration;

namespace Octoplug.Power
{
    /// <summary>
    /// Live index of gameplay world objects. Scene-authored and runtime-created
    /// objects use the same component lifecycle, so consumers never depend on a
    /// reward-phase or game-start snapshot.
    /// </summary>
    public static class RuntimeWorldRegistry
    {
        private static readonly HashSet<ApplianceSource> Products = new();
        private static readonly HashSet<PowerStrip> PowerStrips = new();
        private static readonly HashSet<WallOutlet> WallOutlets = new();
        private static readonly Dictionary<UnityEngine.Component, RoomId> RoomOwners = new();

        public static event Action<UnityEngine.Component, RoomId> ObjectFinalized;

        public static IEnumerable<ApplianceSource> GetProducts()
        {
            Products.RemoveWhere(value => value == null);
            foreach (var product in Products)
            {
                if (product.isActiveAndEnabled)
                {
                    yield return product;
                }
            }
        }

        public static IEnumerable<PowerStrip> GetPowerStrips()
        {
            PowerStrips.RemoveWhere(value => value == null);
            foreach (var powerStrip in PowerStrips)
            {
                if (powerStrip.isActiveAndEnabled)
                {
                    yield return powerStrip;
                }
            }
        }

        public static IEnumerable<WallOutlet> GetWallOutlets()
        {
            WallOutlets.RemoveWhere(value => value == null);
            foreach (var wallOutlet in WallOutlets)
            {
                if (wallOutlet.isActiveAndEnabled)
                {
                    yield return wallOutlet;
                }
            }
        }

        public static bool TryGetRoomOwner(
            UnityEngine.Component value,
            out RoomId roomId)
        {
            return value != null && RoomOwners.TryGetValue(value, out roomId);
        }

        public static void FinalizeRuntimeObject(
            UnityEngine.Component value,
            RoomId roomId)
        {
            if (value == null || !value.gameObject.activeInHierarchy)
            {
                return;
            }

            RoomOwners[value] = roomId;
            switch (value)
            {
                case ApplianceSource product:
                    Products.Add(product);
                    break;
                case PowerStrip powerStrip:
                    PowerStrips.Add(powerStrip);
                    break;
                case WallOutlet wallOutlet:
                    WallOutlets.Add(wallOutlet);
                    break;
            }

            ObjectFinalized?.Invoke(value, roomId);
        }

        internal static void Register(ApplianceSource value)
        {
            if (!RuntimeSpawnGate.IsStaging)
            {
                Products.Add(value);
            }
        }

        internal static void Register(PowerStrip value)
        {
            if (!RuntimeSpawnGate.IsStaging)
            {
                PowerStrips.Add(value);
            }
        }

        internal static void Register(WallOutlet value)
        {
            if (!RuntimeSpawnGate.IsStaging)
            {
                WallOutlets.Add(value);
            }
        }

        internal static void Unregister(ApplianceSource value)
        {
            Products.Remove(value);
            RoomOwners.Remove(value);
        }

        internal static void Unregister(PowerStrip value)
        {
            PowerStrips.Remove(value);
            RoomOwners.Remove(value);
        }

        internal static void Unregister(WallOutlet value)
        {
            WallOutlets.Remove(value);
            RoomOwners.Remove(value);
        }
    }
}
