using System;

namespace Octoplug.RoomGeneration
{
    /// <summary>Positive room dimensions measured in exact integer lattice units.</summary>
    public readonly struct IntegerRoomSize : IEquatable<IntegerRoomSize>
    {
        public IntegerRoomSize(int width, int height)
        {
            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width), "Room width must be positive.");
            }

            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height), "Room height must be positive.");
            }

            Width = width;
            Height = height;
        }

        public int Width { get; }
        public int Height { get; }

        public bool Equals(IntegerRoomSize other) => Width == other.Width && Height == other.Height;
        public override bool Equals(object obj) => obj is IntegerRoomSize other && Equals(other);
        public override int GetHashCode() => (Width * 397) ^ Height;
    }
}
