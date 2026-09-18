using System;

namespace Octoplug.RoomGeneration
{
    /// <summary>Engine-independent 24-bit RGB color.</summary>
    public readonly struct Rgb24 : IEquatable<Rgb24>
    {
        public Rgb24(byte red, byte green, byte blue)
        {
            Red = red;
            Green = green;
            Blue = blue;
        }

        public byte Red { get; }
        public byte Green { get; }
        public byte Blue { get; }
        public string Hex => $"#{Red:X2}{Green:X2}{Blue:X2}";

        public bool Equals(Rgb24 other) => Red == other.Red && Green == other.Green && Blue == other.Blue;
        public override bool Equals(object obj) => obj is Rgb24 other && Equals(other);
        public override int GetHashCode() => (Red << 16) | (Green << 8) | Blue;
    }
}
