using System;
using System.Collections.Generic;
using Octoplug.Power;
using Octoplug.RoomGeneration;
using UnityEngine;

namespace Octoplug.RoomGeneration.Unity
{
    /// <summary>
    /// Applies pure room plans to one typed room prefab instance without scaling its root transform.
    /// </summary>
    public sealed class RoomGenerationRoomBinder : MonoBehaviour
    {
        private const float GeometryTolerance = 0.0001f;

        [Header("Geometry")]
        [SerializeField]
        private BoxCollider2D floorCollider;

        [SerializeField]
        private RoomWallBinding[] walls = Array.Empty<RoomWallBinding>();

        [Header("Visual state")]
        [SerializeField]
        private GameObject lockedRoot;

        [SerializeField]
        private GameObject unlockedRoot;

        [SerializeField]
        private GameObject lockIcon;

        [Tooltip("Optional authored hatching root. Its absence emits one warning because approved hatching art is not available yet.")]
        [SerializeField]
        private GameObject hatchingRoot;

        [SerializeField]
        private GameObject gameplayRoot;

        [Header("Door")]
        [SerializeField]
        private Transform doorTemplate;

        [SerializeField]
        private Transform runtimeDoorRoot;

        private readonly Dictionary<WallSide, RoomWallBinding> wallsBySide = new();
        private readonly List<Transform> runtimeDoors = new();
        private RoomBounds2D authoredLocalBounds;
        private Vector2 floorSizeDelta;
        private bool initialized;
        private bool hatchingWarningIssued;
        private RoomArea roomArea;

        public RoomId RoomId { get; private set; }
        public RoomBounds2D Bounds { get; private set; }
        public RoomBounds2D AuthoredLocalBounds
        {
            get
            {
                EnsureInitialized();
                return authoredLocalBounds;
            }
        }

        public float DoorWidth
        {
            get
            {
                EnsureInitialized();
                if (doorTemplate == null)
                {
                    throw new InvalidOperationException($"{name} has no authored door template.");
                }

                var line = doorTemplate.GetComponent<LineRenderer>();
                if (line == null || line.positionCount < 2)
                {
                    throw new InvalidOperationException($"{name}'s door template requires a LineRenderer with at least two points.");
                }

                var firstWorld = line.useWorldSpace ? line.GetPosition(0) : line.transform.TransformPoint(line.GetPosition(0));
                var lastWorld = line.useWorldSpace
                    ? line.GetPosition(line.positionCount - 1)
                    : line.transform.TransformPoint(line.GetPosition(line.positionCount - 1));
                var firstRoomLocal = transform.InverseTransformPoint(firstWorld);
                var lastRoomLocal = transform.InverseTransformPoint(lastWorld);
                return Vector3.Distance(firstRoomLocal, lastRoomLocal);
            }
        }

        public float WallThickness
        {
            get
            {
                EnsureInitialized();
                var thickness = 0f;
                foreach (var wall in wallsBySide.Values)
                {
                    thickness = Mathf.Max(thickness, wall.GetAuthoredThickness());
                }

                return thickness;
            }
        }

        public float SafetyMargin => WallThickness * 0.5f;

        private void Awake()
        {
            EnsureInitialized();
        }

        private void OnValidate()
        {
            initialized = false;
            if (!TryValidate(out var error))
            {
                Debug.LogError($"{nameof(RoomGenerationRoomBinder)} on '{name}' is invalid: {error}", this);
            }
        }

        public bool TryValidate(out string error)
        {
            if (floorCollider == null)
            {
                error = "Floor collider is not assigned.";
                return false;
            }

            if (GetComponent<RoomArea>() == null)
            {
                error = $"A {nameof(RoomArea)} component is required on the room root.";
                return false;
            }

            var euler = transform.eulerAngles;
            if (Mathf.Abs(Mathf.DeltaAngle(euler.x, 0f)) > GeometryTolerance
                || Mathf.Abs(Mathf.DeltaAngle(euler.y, 0f)) > GeometryTolerance
                || Mathf.Abs(Mathf.DeltaAngle(euler.z, 0f)) > GeometryTolerance)
            {
                error = "Room root rotation must be exactly zero; generated rooms are axis-aligned and doors own the only runtime Z rotation.";
                return false;
            }

            if (Mathf.Abs(transform.lossyScale.x - 1f) > GeometryTolerance
                || Mathf.Abs(transform.lossyScale.y - 1f) > GeometryTolerance)
            {
                error = "Room root world scale must be one; geometry is resized through wall endpoints and colliders.";
                return false;
            }

            if (lockedRoot == null || unlockedRoot == null || lockIcon == null)
            {
                error = "Locked, unlocked, and lock-icon roots are required.";
                return false;
            }

            if (doorTemplate == null || runtimeDoorRoot == null)
            {
                error = "Door template and runtime door root are required.";
                return false;
            }

            if (walls == null || walls.Length != 4)
            {
                error = "Exactly four typed wall bindings are required.";
                return false;
            }

            var seenSides = new HashSet<WallSide>();
            for (var i = 0; i < walls.Length; i++)
            {
                var wall = walls[i];
                if (wall == null)
                {
                    error = $"Wall binding at index {i} is null.";
                    return false;
                }

                if (!wall.TryValidate(out error) || !seenSides.Add(wall.Side))
                {
                    error ??= $"Wall side {wall.Side} is assigned more than once.";
                    return false;
                }
            }

            foreach (WallSide side in Enum.GetValues(typeof(WallSide)))
            {
                if (!seenSides.Contains(side))
                {
                    error = $"Missing wall binding for {side}.";
                    return false;
                }
            }

            var doorEuler = doorTemplate.eulerAngles;
            var doorZ = Mathf.Round(Mathf.Repeat(doorEuler.z, 360f));
            if (Mathf.Abs(Mathf.DeltaAngle(doorEuler.x, 0f)) > GeometryTolerance
                || Mathf.Abs(Mathf.DeltaAngle(doorEuler.y, 0f)) > GeometryTolerance
                || (doorZ != 0f && doorZ != 90f && doorZ != 180f))
            {
                error = "Door template must use zero X/Y rotation and an allowed Z rotation of 0, 90, or 180 degrees.";
                return false;
            }

            var doorLine = doorTemplate.GetComponent<LineRenderer>();
            if (doorLine == null || doorLine.positionCount < 2)
            {
                error = "Door template requires a LineRenderer with at least two authored points.";
                return false;
            }

            var firstDoorPoint = doorLine.GetPosition(0);
            var lastDoorPoint = doorLine.GetPosition(doorLine.positionCount - 1);
            if ((lastDoorPoint - firstDoorPoint).sqrMagnitude <= GeometryTolerance * GeometryTolerance)
            {
                error = "Door template width must be positive.";
                return false;
            }

            error = null;
            return true;
        }

        public RoomPlacement CreateSeedPlacement(string roomId)
        {
            EnsureInitialized();
            var worldMin = transform.TransformPoint(new Vector3(authoredLocalBounds.MinX, authoredLocalBounds.MinY));
            return new RoomPlacement(
                new RoomId(roomId),
                new RoomBounds2D(worldMin.x, worldMin.y, authoredLocalBounds.Width, authoredLocalBounds.Height));
        }

        public void ApplyPlacement(RoomPlacement placement, RoomVisualIntent visualIntent)
        {
            EnsureInitialized();
            if (!placement.IsValid)
            {
                throw new ArgumentException("Room placement must be valid.", nameof(placement));
            }

            if (Mathf.Abs(transform.lossyScale.x - 1f) > GeometryTolerance
                || Mathf.Abs(transform.lossyScale.y - 1f) > GeometryTolerance)
            {
                throw new InvalidOperationException($"{name} must keep unit world scale; room size is applied to wall and collider geometry only.");
            }

            RoomId = placement.Id;
            Bounds = placement.Bounds;
            transform.position = new Vector3(
                placement.Bounds.MinX - authoredLocalBounds.MinX,
                placement.Bounds.MinY - authoredLocalBounds.MinY,
                transform.position.z);
            RebuildFullWalls();
            floorCollider.offset = new Vector2(
                authoredLocalBounds.MinX + placement.Bounds.Width * 0.5f,
                authoredLocalBounds.MinY + placement.Bounds.Height * 0.5f);
            floorCollider.size = new Vector2(
                placement.Bounds.Width + floorSizeDelta.x,
                placement.Bounds.Height + floorSizeDelta.y);

            CenterLockIcon();
            ApplyVisualIntent(visualIntent);
        }

        public void RebuildFullWalls()
        {
            EnsureInitialized();
            if (!Bounds.IsValid)
            {
                return;
            }

            foreach (WallSide side in Enum.GetValues(typeof(WallSide)))
            {
                GetLocalWallEndpoints(side, out var start, out var end);
                wallsBySide[side].ApplyFullSpan(transform, start, end);
            }
        }

        public void OpenDoor(RoomWallId wallId, WallSpan doorSpan)
        {
            EnsureInitialized();
            if (wallId.RoomId != RoomId)
            {
                throw new ArgumentException($"Wall {wallId} does not belong to bound room {RoomId}.", nameof(wallId));
            }

            if (wallId.Orientation != doorSpan.Orientation)
            {
                throw new ArgumentException("Door span orientation must match the room wall.", nameof(doorSpan));
            }

            GetLocalWallEndpoints(wallId.Side, out var wallStart, out var wallEnd);
            var gapStartWorld = doorSpan.PointAt(doorSpan.Start);
            var gapEndWorld = doorSpan.PointAt(doorSpan.End);
            var gapStart = (Vector2)transform.InverseTransformPoint(new Vector3(gapStartWorld.X, gapStartWorld.Y));
            var gapEnd = (Vector2)transform.InverseTransformPoint(new Vector3(gapEndWorld.X, gapEndWorld.Y));
            wallsBySide[wallId.Side].ApplyGap(transform, wallStart, wallEnd, gapStart, gapEnd);
        }

        public void SetAuthoredDoorTemplateVisible(bool visible)
        {
            EnsureInitialized();
            doorTemplate.gameObject.SetActive(visible);
        }

        public Transform SpawnDoor(Vector2 hingePosition, float zRotationDegrees)
        {
            EnsureInitialized();
            if (zRotationDegrees != 0f && zRotationDegrees != 90f && zRotationDegrees != 180f)
            {
                throw new ArgumentOutOfRangeException(nameof(zRotationDegrees), "Door rotation must be exactly 0, 90, or 180 degrees.");
            }

            var instance = Instantiate(doorTemplate, runtimeDoorRoot);
            instance.name = $"{doorTemplate.name} (Runtime)";
            instance.gameObject.SetActive(true);
            instance.position = new Vector3(hingePosition.x, hingePosition.y, doorTemplate.position.z);
            instance.rotation = Quaternion.Euler(0f, 0f, zRotationDegrees);
            instance.localScale = doorTemplate.localScale;
            runtimeDoors.Add(instance);
            return instance;
        }

        public void ClearRuntimeDoors()
        {
            for (var i = 0; i < runtimeDoors.Count; i++)
            {
                if (runtimeDoors[i] != null)
                {
                    runtimeDoors[i].gameObject.SetActive(false);
                    if (Application.isPlaying)
                    {
                        Destroy(runtimeDoors[i].gameObject);
                    }
                    else
                    {
                        DestroyImmediate(runtimeDoors[i].gameObject);
                    }
                }
            }

            runtimeDoors.Clear();
        }

        private void CenterLockIcon()
        {
            if (lockIcon == null)
            {
                return;
            }

            // The room's visual/floor center is not assumed to be the authored local origin
            // or the transform pivot; it is derived the same way as the floor collider offset
            // above, from authored wall-derived local bounds plus the current (possibly
            // variable) placement size. Room rotation is validated to be exactly zero, so this
            // local offset maps directly onto the room's world-space visual center.
            var localPosition = lockIcon.transform.localPosition;
            lockIcon.transform.localPosition = new Vector3(
                authoredLocalBounds.MinX + Bounds.Width * 0.5f,
                authoredLocalBounds.MinY + Bounds.Height * 0.5f,
                localPosition.z);
        }

        private void ApplyVisualIntent(RoomVisualIntent intent)
        {
            var locked = intent.State == RoomLifecycleState.HintLocked;
            lockedRoot.SetActive(locked);
            unlockedRoot.SetActive(!locked);
            lockIcon.SetActive(intent.ShowLockIcon);
            roomArea.SetGameplayEnabled(intent.GameplayContentEnabled);
            if (gameplayRoot != null)
            {
                gameplayRoot.SetActive(intent.GameplayContentEnabled);
            }

            if (hatchingRoot != null)
            {
                hatchingRoot.SetActive(intent.ShowHatching);
            }
            else if (intent.ShowHatching && !hatchingWarningIssued)
            {
                hatchingWarningIssued = true;
                Debug.LogWarning($"{name}: locked-room hatching is requested but no authored hatching root is assigned.", this);
            }
        }

        private void EnsureInitialized()
        {
            if (initialized)
            {
                return;
            }

            if (!TryValidate(out var error))
            {
                throw new InvalidOperationException($"{nameof(RoomGenerationRoomBinder)} on '{name}' is invalid: {error}");
            }

            roomArea = GetComponent<RoomArea>();
            if (roomArea == null)
            {
                throw new InvalidOperationException($"{nameof(RoomGenerationRoomBinder)} on '{name}' requires a {nameof(RoomArea)} component.");
            }

            wallsBySide.Clear();
            for (var i = 0; i < walls.Length; i++)
            {
                wallsBySide.Add(walls[i].Side, walls[i]);
            }

            var minX = wallsBySide[WallSide.Left].GetAuthoredFixedCoordinate(transform);
            var maxX = wallsBySide[WallSide.Right].GetAuthoredFixedCoordinate(transform);
            var minY = wallsBySide[WallSide.Bottom].GetAuthoredFixedCoordinate(transform);
            var maxY = wallsBySide[WallSide.Top].GetAuthoredFixedCoordinate(transform);
            authoredLocalBounds = new RoomBounds2D(minX, minY, maxX - minX, maxY - minY);
            floorSizeDelta = floorCollider.size - new Vector2(authoredLocalBounds.Width, authoredLocalBounds.Height);
            foreach (var wall in wallsBySide.Values)
            {
                wall.CaptureAuthoredGeometry(transform, authoredLocalBounds);
            }

            initialized = true;
        }

        private void GetLocalWallEndpoints(WallSide side, out Vector2 start, out Vector2 end)
        {
            var minX = authoredLocalBounds.MinX;
            var minY = authoredLocalBounds.MinY;
            var maxX = minX + Bounds.Width;
            var maxY = minY + Bounds.Height;
            switch (side)
            {
                case WallSide.Left:
                    start = new Vector2(minX, minY);
                    end = new Vector2(minX, maxY);
                    return;
                case WallSide.Right:
                    start = new Vector2(maxX, minY);
                    end = new Vector2(maxX, maxY);
                    return;
                case WallSide.Bottom:
                    start = new Vector2(minX, minY);
                    end = new Vector2(maxX, minY);
                    return;
                case WallSide.Top:
                    start = new Vector2(minX, maxY);
                    end = new Vector2(maxX, maxY);
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(side), side, "Wall side must be defined.");
            }
        }
    }
}
