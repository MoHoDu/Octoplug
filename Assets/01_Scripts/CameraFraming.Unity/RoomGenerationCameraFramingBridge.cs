using System.Collections.Generic;
using Octoplug.RoomGeneration;
using Octoplug.RoomGeneration.Unity;
using UnityEngine;

namespace Octoplug.CameraFraming.Unity
{
    /// <summary>
    /// The only connection between Room Generation and the camera: Room Generation announces that a
    /// hint room was created (<see cref="RoomGenerationTestController.HintRoomCreated"/>) and this
    /// bridge asks the camera to keep its dynamic max zoom in sync and to frame that hint. Neither
    /// side is otherwise aware of the other, and no global event bus is introduced.
    /// </summary>
    public sealed class RoomGenerationCameraFramingBridge : MonoBehaviour
    {
        [SerializeField]
        private RoomGenerationTestController roomGeneration;

        [SerializeField]
        private HouseCameraZoomController cameraZoom;

        [SerializeField]
        private HouseCameraPanController cameraPan;

        private void OnValidate()
        {
            if (roomGeneration == null || cameraZoom == null || cameraPan == null)
            {
                Debug.LogError($"{nameof(RoomGenerationCameraFramingBridge)} requires Room Generation controller plus camera zoom and Pan references.", this);
            }
        }

        private void OnEnable()
        {
            if (roomGeneration != null)
            {
                roomGeneration.HintRoomCreated += OnHintRoomCreated;
            }
        }

        private void OnDisable()
        {
            if (roomGeneration != null)
            {
                roomGeneration.HintRoomCreated -= OnHintRoomCreated;
            }
        }

        private void OnHintRoomCreated(RoomPlacement hint)
        {
            if (roomGeneration == null || cameraZoom == null || cameraPan == null)
            {
                return;
            }

            var unlockedBounds = CollectRoomBounds(roomGeneration.State.UnlockedLayout);

            // Zoom's dynamic maximum zoom-out is deliberately unchanged: it still reflects only the
            // already-unlocked house, per the existing Hint-framing policy (a brand-new hint widens
            // the effective zoom range itself when needed; see HouseCameraZoomController).
            var unlockedHouseBounds = DynamicZoomLimit.CombineBounds(unlockedBounds);
            cameraZoom.RecomputeDynamicMaxOrthographicSize(unlockedHouseBounds);

            // Pan boundary uses the "visible house" instead: unlocked rooms PLUS the current
            // HintLocked room, so a Hint sitting just outside the unlocked footprint is still fully
            // reachable by Pan, not just by the one-shot zoom-out framing below.
            var visibleHouseBounds = DynamicZoomLimit.CombineBounds(AppendBounds(unlockedBounds, hint.Bounds));
            cameraPan.UpdateHouseBounds(visibleHouseBounds);

            cameraZoom.RequestHintFraming(hint.Bounds);
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

        private static IReadOnlyList<RoomBounds2D> AppendBounds(IReadOnlyList<RoomBounds2D> bounds, RoomBounds2D extra)
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
