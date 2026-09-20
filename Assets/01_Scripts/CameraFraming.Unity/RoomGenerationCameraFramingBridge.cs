using System;
using System.Collections.Generic;
using Octoplug.RoomGeneration;
using Octoplug.RoomGeneration.Unity;
using UnityEngine;

namespace Octoplug.CameraFraming.Unity
{
    /// <summary>
    /// The sole production Room Generation to Camera connection. Automatic Hint framing follows
    /// Room Generation events, while future GameFlow can explicitly request and await Room Reveal.
    /// </summary>
    public sealed class RoomGenerationCameraFramingBridge : MonoBehaviour
    {
        [SerializeField]
        private ProductionRoomGenerationController roomGeneration;

        [SerializeField]
        private HouseCameraZoomController cameraZoom;

        [SerializeField]
        private HouseCameraPanController cameraPan;

        public event Action CameraRevealCompleted;

        private void Awake()
        {
            if (roomGeneration == null || cameraZoom == null || cameraPan == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(RoomGenerationCameraFramingBridge)} requires Room Generation controller plus camera zoom and Pan references.");
            }
        }

        private void OnEnable()
        {
            if (roomGeneration != null)
            {
                roomGeneration.NextHintCreated += OnNextHintCreated;
            }

            if (cameraZoom != null)
            {
                cameraZoom.CameraRevealCompleted += OnCameraRevealCompleted;
            }
        }

        private void OnDisable()
        {
            if (roomGeneration != null)
            {
                roomGeneration.NextHintCreated -= OnNextHintCreated;
            }

            if (cameraZoom != null)
            {
                cameraZoom.CameraRevealCompleted -= OnCameraRevealCompleted;
            }
        }

        /// <summary>
        /// Requests a zoom-only reveal for a newly unlocked Room. Future GameFlow owns when this is
        /// called and may continue only after <see cref="CameraRevealCompleted"/>.
        /// </summary>
        public void RequestRoomReveal(RoomPlacement room)
        {
            if (cameraZoom == null)
            {
                return;
            }

            cameraZoom.RequestRoomReveal(room.Bounds);
        }

        private void OnNextHintCreated(RoomPlacement hint)
        {
            if (roomGeneration == null || cameraZoom == null || cameraPan == null)
            {
                return;
            }

            var unlockedBounds = CollectRoomBounds(roomGeneration.State.UnlockedLayout);

            // Zoom's dynamic maximum remains based on already-unlocked rooms. A new Hint may widen
            // the effective range itself when full-bounds framing requires more zoom-out.
            var unlockedHouseBounds = DynamicZoomLimit.CombineBounds(unlockedBounds);
            cameraZoom.RecomputeDynamicMaxOrthographicSize(unlockedHouseBounds);

            // Pan uses the visible House: unlocked Rooms plus the current HintLocked Room.
            var visibleHouseBounds = DynamicZoomLimit.CombineBounds(
                AppendBounds(unlockedBounds, hint.Bounds));
            cameraPan.UpdateHouseBounds(visibleHouseBounds);

            cameraZoom.RequestHintFraming(hint.Bounds);
        }

        private void OnCameraRevealCompleted()
        {
            CameraRevealCompleted?.Invoke();
        }

        private static IReadOnlyList<RoomBounds2D> CollectRoomBounds(RoomLayout layout)
        {
            var rooms = layout.Rooms;
            var bounds = new RoomBounds2D[rooms.Count];
            for (var i = 0; i < rooms.Count; i++)
            {
                bounds[i] = rooms[i].Bounds;
            }

            return bounds;
        }

        private static IReadOnlyList<RoomBounds2D> AppendBounds(
            IReadOnlyList<RoomBounds2D> bounds,
            RoomBounds2D extra)
        {
            var combined = new RoomBounds2D[bounds.Count + 1];
            for (var i = 0; i < bounds.Count; i++)
            {
                combined[i] = bounds[i];
            }

            combined[bounds.Count] = extra;
            return combined;
        }
    }
}
