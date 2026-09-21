using System;
using System.Collections.Generic;
using Octoplug.Power;
using Octoplug.Power.Grid;
using Octoplug.RoomGeneration;
using Octoplug.RoomGeneration.Unity;
using UnityEngine;

namespace Octoplug.Reward.Unity
{
    /// <summary>
    /// Shares reward placement preflight and application so a placement reward
    /// cannot be offered through rules different from the rules that execute it.
    /// </summary>
    public sealed class RewardPlacementService
    {
        private static readonly int[] AllSocketCounts = { 1, 2, 3, 4, 5 };

        private readonly ProductionRoomGenerationController roomGeneration;
        private readonly CableRoutingGridService gridService;
        private readonly PowerStrip powerStripPrefab;
        private readonly Transform powerStripParent;

        public RewardPlacementService(
            ProductionRoomGenerationController roomGeneration,
            CableRoutingGridService gridService,
            PowerStrip powerStripPrefab,
            Transform powerStripParent)
        {
            this.roomGeneration = roomGeneration;
            this.gridService = gridService;
            this.powerStripPrefab = powerStripPrefab;
            this.powerStripParent = powerStripParent;
        }

        public IReadOnlyList<RoomPlacementRewardTarget> GetEligibleRooms()
        {
            var result = new List<RoomPlacementRewardTarget>();
            if (!CanUsePowerStripPlacement())
            {
                return result;
            }

            var probe = UnityEngine.Object.Instantiate(powerStripPrefab.gameObject);
            probe.SetActive(false);
            try
            {
                var footprint = probe.GetComponent<PlacementFootprint>();
                foreach (var room in roomGeneration.State.UnlockedLayout.Rooms)
                {
                    if (!TryGetGameplayBinder(room, out var binder)
                        || !RoomObjectPlacementPlanner.CanPlaceAllSocketCounts(
                            room,
                            gridService.Grid,
                            footprint,
                            AllSocketCounts))
                    {
                        continue;
                    }

                    result.Add(new RoomPlacementRewardTarget(room, binder));
                }
            }
            finally
            {
                DestroyRuntimeObject(probe);
            }

            return result;
        }

        public bool TryResolvePowerStripPlacement(
            Vector2 clickedWorldPosition,
            out RoomPlacementRewardTarget target)
        {
            target = null;
            if (!CanUsePowerStripPlacement())
            {
                return false;
            }

            var probe = UnityEngine.Object.Instantiate(powerStripPrefab.gameObject);
            probe.SetActive(false);
            try
            {
                var footprint = probe.GetComponent<PlacementFootprint>();
                foreach (var room in roomGeneration.State.UnlockedLayout.Rooms)
                {
                    if (!Contains(room.Bounds, clickedWorldPosition)
                        || !TryGetGameplayBinder(room, out var binder)
                        || !CanPlaceAnySocketCountNear(
                            room,
                            footprint,
                            clickedWorldPosition,
                            gridService.Grid.CellSize * 8f))
                    {
                        continue;
                    }

                    target = new RoomPlacementRewardTarget(
                        room,
                        binder,
                        clickedWorldPosition);
                    return true;
                }
            }
            finally
            {
                DestroyRuntimeObject(probe);
            }

            return false;
        }

        public bool TryPlacePowerStrip(
            RoomPlacementRewardTarget target,
            int socketCount,
            out PowerStrip instance,
            out string failure)
        {
            instance = null;
            if (!CanUsePowerStripPlacement()
                || target == null
                || !TryGetGameplayBinder(target.Room, out var binder)
                || binder != target.Binder)
            {
                failure = "room target is no longer eligible";
                return false;
            }

            var gameObject = RuntimeEquipmentFactory.Stage(
                powerStripPrefab.gameObject,
                Vector2.zero,
                Quaternion.identity,
                powerStripParent);
            instance = gameObject != null
                ? gameObject.GetComponent<PowerStrip>()
                : null;
            var footprint = gameObject != null
                ? gameObject.GetComponent<PlacementFootprint>()
                : null;
            var foundPosition = target.DesiredPosition.HasValue
                ? RoomObjectPlacementPlanner.TryFindNearestPosition(
                    target.Room,
                    gridService.Grid,
                    footprint,
                    socketCount,
                    target.DesiredPosition.Value,
                    gridService.Grid.CellSize * 8f,
                    out var position,
                    out failure)
                : RoomObjectPlacementPlanner.TryFindPosition(
                    target.Room,
                    gridService.Grid,
                    footprint,
                    socketCount,
                    out position,
                    out failure);
            if (!foundPosition)
            {
                RuntimeEquipmentFactory.Abort(gameObject);
                instance = null;
                return false;
            }

            gameObject.transform.position = position;
            if (!RuntimeEquipmentFactory.TryFinalizePowerStrip(
                    gameObject,
                    target.Room.Id,
                    gridService.Grid,
                    socketCount,
                    out instance,
                    out failure))
            {
                return false;
            }

            gridService.RebuildFromScene();
            return true;
        }

        private bool CanPlaceAnySocketCountNear(
            RoomPlacement room,
            PlacementFootprint footprint,
            Vector2 desiredPosition,
            float maximumCorrectionDistance)
        {
            for (var i = 0; i < AllSocketCounts.Length; i++)
            {
                if (RoomObjectPlacementPlanner.TryFindNearestPosition(
                        room,
                        gridService.Grid,
                        footprint,
                        AllSocketCounts[i],
                        desiredPosition,
                        maximumCorrectionDistance,
                        out _,
                        out _))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool Contains(RoomBounds2D bounds, Vector2 point)
        {
            return point.x >= bounds.MinX && point.x <= bounds.MaxX
                && point.y >= bounds.MinY && point.y <= bounds.MaxY;
        }

        private bool CanUsePowerStripPlacement()
        {
            return roomGeneration != null
                && roomGeneration.IsInitialized
                && gridService != null
                && powerStripPrefab != null
                && powerStripParent != null;
        }

        private bool TryGetGameplayBinder(
            RoomPlacement room,
            out RoomGenerationRoomBinder binder)
        {
            if (!roomGeneration.TryGetRoomBinder(room.Id, out binder))
            {
                return false;
            }

            var area = binder.GetComponent<RoomArea>();
            return area != null && area.IsGameplayEnabled && area.FloorArea.enabled;
        }

        private static void DestroyRuntimeObject(UnityEngine.Object value)
        {
            if (value == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(value);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(value);
            }
        }
    }
}
