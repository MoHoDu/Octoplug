using System;

namespace Octoplug.RoomGeneration
{
    /// <summary>Stable caller-owned identifier for a room.</summary>
    public readonly struct RoomId : IEquatable<RoomId>, IComparable<RoomId>
    {
        public RoomId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Room id must not be empty.", nameof(value));
            }

            Value = value;
        }

        public string Value { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public int CompareTo(RoomId other) => StringComparer.Ordinal.Compare(Value, other.Value);
        public bool Equals(RoomId other) => StringComparer.Ordinal.Equals(Value, other.Value);
        public override bool Equals(object obj) => obj is RoomId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);
        public override string ToString() => Value ?? string.Empty;

        public static bool operator ==(RoomId left, RoomId right) => left.Equals(right);
        public static bool operator !=(RoomId left, RoomId right) => !left.Equals(right);
    }
}
