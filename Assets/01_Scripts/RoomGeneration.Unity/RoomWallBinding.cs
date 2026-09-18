using System;
using System.Collections.Generic;
using Octoplug.RoomGeneration;
using UnityEngine;

namespace Octoplug.RoomGeneration.Unity
{
    /// <summary>
    /// Typed references for one authored room side in both visual states.
    /// Only endpoint and collider geometry are changed; renderer styling stays authored.
    /// </summary>
    [Serializable]
    public sealed class RoomWallBinding
    {
        [SerializeField]
        private WallSide side;

        [Header("Locked / dashed")]
        [SerializeField]
        private LineRenderer lockedLine;

        [SerializeField]
        private BoxCollider2D lockedCollider;

        [Header("Unlocked / solid")]
        [SerializeField]
        private LineRenderer unlockedLine;

        [SerializeField]
        private BoxCollider2D unlockedCollider;

        private readonly List<GameObject> runtimeSegments = new();
        private VariantGeometry lockedGeometry;
        private VariantGeometry unlockedGeometry;
        private bool geometryCaptured;

        public WallSide Side => side;

        public bool TryValidate(out string error)
        {
            if (!Enum.IsDefined(typeof(WallSide), side))
            {
                error = $"Wall side value '{side}' is not defined.";
                return false;
            }

            if (lockedLine == null || lockedCollider == null || unlockedLine == null || unlockedCollider == null)
            {
                error = $"{side} wall requires locked and unlocked LineRenderer/BoxCollider2D references.";
                return false;
            }

            if (lockedLine.positionCount != 2 || unlockedLine.positionCount != 2)
            {
                error = $"{side} wall LineRenderers must each contain exactly two authored endpoints.";
                return false;
            }

            if (!IsAxisAligned(lockedLine) || !IsAxisAligned(unlockedLine))
            {
                error = $"{side} wall LineRenderers must be axis-aligned.";
                return false;
            }

            if (GetAuthoredThickness() <= 0f)
            {
                error = $"{side} wall collider thickness must be positive.";
                return false;
            }

            error = null;
            return true;
        }

        public float GetAuthoredFixedCoordinate(Transform roomTransform)
        {
            var first = GetRoomLocalPosition(lockedLine, 0, roomTransform);
            var second = GetRoomLocalPosition(lockedLine, lockedLine.positionCount - 1, roomTransform);
            return IsVertical ? (first.x + second.x) * 0.5f : (first.y + second.y) * 0.5f;
        }

        public float GetAuthoredThickness()
        {
            var lockedThickness = GetThickness(lockedCollider);
            var unlockedThickness = GetThickness(unlockedCollider);
            return Mathf.Max(lockedThickness, unlockedThickness);
        }

        public void CaptureAuthoredGeometry(Transform roomTransform, RoomBounds2D authoredBounds)
        {
            if (geometryCaptured)
            {
                return;
            }

            GetNominalInterval(authoredBounds, out var nominalStart, out var nominalEnd);
            lockedGeometry = CaptureVariant(lockedLine, lockedCollider, roomTransform, nominalStart, nominalEnd);
            unlockedGeometry = CaptureVariant(unlockedLine, unlockedCollider, roomTransform, nominalStart, nominalEnd);
            geometryCaptured = true;
        }

        public void ApplyFullSpan(Transform roomTransform, Vector2 startRoomLocal, Vector2 endRoomLocal)
        {
            RequireCaptured();
            ClearRuntimeSegments();
            ApplyFullVariant(lockedGeometry, roomTransform, startRoomLocal, endRoomLocal);
            ApplyFullVariant(unlockedGeometry, roomTransform, startRoomLocal, endRoomLocal);
        }

        public void ApplyGap(
            Transform roomTransform,
            Vector2 wallStartRoomLocal,
            Vector2 wallEndRoomLocal,
            Vector2 gapStartRoomLocal,
            Vector2 gapEndRoomLocal)
        {
            RequireCaptured();
            ClearRuntimeSegments();
            ApplyGapToVariant(lockedGeometry, roomTransform, wallStartRoomLocal, wallEndRoomLocal, gapStartRoomLocal, gapEndRoomLocal);
            ApplyGapToVariant(unlockedGeometry, roomTransform, wallStartRoomLocal, wallEndRoomLocal, gapStartRoomLocal, gapEndRoomLocal);
        }

        private void ApplyFullVariant(VariantGeometry geometry, Transform roomTransform, Vector2 wallStart, Vector2 wallEnd)
        {
            var start = Along(wallStart);
            var end = Along(wallEnd);
            ConfigureSegment(
                geometry.Line,
                geometry.Collider,
                roomTransform,
                Fixed(wallStart),
                start + geometry.LineStartOffset,
                end + geometry.LineEndOffset,
                start + geometry.ColliderStartOffset,
                end + geometry.ColliderEndOffset,
                geometry.LineReversed);
        }

        private void ApplyGapToVariant(
            VariantGeometry geometry,
            Transform roomTransform,
            Vector2 wallStart,
            Vector2 wallEnd,
            Vector2 gapStart,
            Vector2 gapEnd)
        {
            var wallStartCoordinate = Along(wallStart);
            var wallEndCoordinate = Along(wallEnd);
            var gapStartCoordinate = Mathf.Min(Along(gapStart), Along(gapEnd));
            var gapEndCoordinate = Mathf.Max(Along(gapStart), Along(gapEnd));
            var fixedCoordinate = Fixed(wallStart);

            // Clone while the source still carries every authored renderer/collider property.
            ApplyFullVariant(geometry, roomTransform, wallStart, wallEnd);
            var cloneObject = UnityEngine.Object.Instantiate(geometry.Line.gameObject, geometry.Line.transform.parent);
            cloneObject.name = $"{geometry.Line.gameObject.name} (Runtime Segment)";
            runtimeSegments.Add(cloneObject);

            ConfigureSegment(
                geometry.Line,
                geometry.Collider,
                roomTransform,
                fixedCoordinate,
                wallStartCoordinate + geometry.LineStartOffset,
                gapStartCoordinate,
                wallStartCoordinate + geometry.ColliderStartOffset,
                gapStartCoordinate,
                geometry.LineReversed);

            ConfigureSegment(
                cloneObject.GetComponent<LineRenderer>(),
                cloneObject.GetComponent<BoxCollider2D>(),
                roomTransform,
                fixedCoordinate,
                gapEndCoordinate,
                wallEndCoordinate + geometry.LineEndOffset,
                gapEndCoordinate,
                wallEndCoordinate + geometry.ColliderEndOffset,
                geometry.LineReversed);
        }

        private void ClearRuntimeSegments()
        {
            for (var i = 0; i < runtimeSegments.Count; i++)
            {
                var segment = runtimeSegments[i];
                if (segment == null)
                {
                    continue;
                }

                segment.SetActive(false);
                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(segment);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(segment);
                }
            }

            runtimeSegments.Clear();
        }

        private void ConfigureSegment(
            LineRenderer line,
            BoxCollider2D collider,
            Transform roomTransform,
            float fixedCoordinate,
            float lineStart,
            float lineEnd,
            float colliderStart,
            float colliderEnd,
            bool reverseLine)
        {
            var visible = lineEnd - lineStart > 0.0001f && colliderEnd - colliderStart > 0.0001f;
            line.enabled = visible;
            collider.enabled = visible;
            if (!visible)
            {
                return;
            }

            var startRoomLocal = Point(fixedCoordinate, lineStart);
            var endRoomLocal = Point(fixedCoordinate, lineEnd);
            var first = reverseLine ? endRoomLocal : startRoomLocal;
            var last = reverseLine ? startRoomLocal : endRoomLocal;
            SetLinePoint(line, 0, roomTransform, first);
            SetLinePoint(line, line.positionCount - 1, roomTransform, last);

            var colliderStartLocal = (Vector2)collider.transform.InverseTransformPoint(roomTransform.TransformPoint(Point(fixedCoordinate, colliderStart)));
            var colliderEndLocal = (Vector2)collider.transform.InverseTransformPoint(roomTransform.TransformPoint(Point(fixedCoordinate, colliderEnd)));
            var offset = (colliderStartLocal + colliderEndLocal) * 0.5f;
            var size = collider.size;
            if (IsVertical)
            {
                size.y = Mathf.Abs(colliderEndLocal.y - colliderStartLocal.y);
            }
            else
            {
                size.x = Mathf.Abs(colliderEndLocal.x - colliderStartLocal.x);
            }

            collider.offset = offset;
            collider.size = size;
        }

        private VariantGeometry CaptureVariant(
            LineRenderer line,
            BoxCollider2D collider,
            Transform roomTransform,
            float nominalStart,
            float nominalEnd)
        {
            var first = GetRoomLocalPosition(line, 0, roomTransform);
            var last = GetRoomLocalPosition(line, line.positionCount - 1, roomTransform);
            var firstCoordinate = Along(first);
            var lastCoordinate = Along(last);
            var lineStart = Mathf.Min(firstCoordinate, lastCoordinate);
            var lineEnd = Mathf.Max(firstCoordinate, lastCoordinate);

            var colliderCenter = IsVertical ? collider.offset.y : collider.offset.x;
            var colliderLength = IsVertical ? collider.size.y : collider.size.x;
            var colliderStartPoint = IsVertical
                ? new Vector2(collider.offset.x, colliderCenter - colliderLength * 0.5f)
                : new Vector2(colliderCenter - colliderLength * 0.5f, collider.offset.y);
            var colliderEndPoint = IsVertical
                ? new Vector2(collider.offset.x, colliderCenter + colliderLength * 0.5f)
                : new Vector2(colliderCenter + colliderLength * 0.5f, collider.offset.y);
            var colliderStartRoom = (Vector2)roomTransform.InverseTransformPoint(collider.transform.TransformPoint(colliderStartPoint));
            var colliderEndRoom = (Vector2)roomTransform.InverseTransformPoint(collider.transform.TransformPoint(colliderEndPoint));

            return new VariantGeometry(
                line,
                collider,
                lineStart - nominalStart,
                lineEnd - nominalEnd,
                Mathf.Min(Along(colliderStartRoom), Along(colliderEndRoom)) - nominalStart,
                Mathf.Max(Along(colliderStartRoom), Along(colliderEndRoom)) - nominalEnd,
                firstCoordinate > lastCoordinate);
        }

        private static void SetLinePoint(LineRenderer line, int index, Transform roomTransform, Vector2 roomLocal)
        {
            var world = roomTransform.TransformPoint(roomLocal);
            line.SetPosition(index, line.useWorldSpace ? world : line.transform.InverseTransformPoint(world));
        }

        private static Vector2 GetRoomLocalPosition(LineRenderer line, int index, Transform roomTransform)
        {
            var position = line.GetPosition(index);
            var world = line.useWorldSpace ? position : line.transform.TransformPoint(position);
            return roomTransform.InverseTransformPoint(world);
        }

        private void GetNominalInterval(RoomBounds2D bounds, out float start, out float end)
        {
            if (IsVertical)
            {
                start = bounds.MinY;
                end = bounds.MaxY;
            }
            else
            {
                start = bounds.MinX;
                end = bounds.MaxX;
            }
        }

        private bool IsVertical => side is WallSide.Left or WallSide.Right;
        private float Along(Vector2 point) => IsVertical ? point.y : point.x;
        private float Fixed(Vector2 point) => IsVertical ? point.x : point.y;
        private Vector2 Point(float fixedCoordinate, float alongCoordinate)
            => IsVertical ? new Vector2(fixedCoordinate, alongCoordinate) : new Vector2(alongCoordinate, fixedCoordinate);

        private float GetThickness(BoxCollider2D collider) => IsVertical ? collider.size.x : collider.size.y;

        private static bool IsAxisAligned(LineRenderer line)
        {
            var first = line.GetPosition(0);
            var last = line.GetPosition(line.positionCount - 1);
            return Mathf.Abs(first.x - last.x) <= 0.0001f || Mathf.Abs(first.y - last.y) <= 0.0001f;
        }

        private void RequireCaptured()
        {
            if (!geometryCaptured)
            {
                throw new InvalidOperationException($"Authored geometry for {side} wall has not been captured.");
            }
        }

        private sealed class VariantGeometry
        {
            public VariantGeometry(
                LineRenderer line,
                BoxCollider2D collider,
                float lineStartOffset,
                float lineEndOffset,
                float colliderStartOffset,
                float colliderEndOffset,
                bool lineReversed)
            {
                Line = line;
                Collider = collider;
                LineStartOffset = lineStartOffset;
                LineEndOffset = lineEndOffset;
                ColliderStartOffset = colliderStartOffset;
                ColliderEndOffset = colliderEndOffset;
                LineReversed = lineReversed;
            }

            public LineRenderer Line { get; }
            public BoxCollider2D Collider { get; }
            public float LineStartOffset { get; }
            public float LineEndOffset { get; }
            public float ColliderStartOffset { get; }
            public float ColliderEndOffset { get; }
            public bool LineReversed { get; }
        }
    }
}
