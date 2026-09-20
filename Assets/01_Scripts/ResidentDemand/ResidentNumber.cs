using System;
using System.Globalization;

namespace Octoplug.ResidentDemand
{
    public readonly struct ResidentNumber : IEquatable<ResidentNumber>
    {
        public ResidentNumber(int value)
        {
            if (value < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            Value = value;
        }

        public int Value { get; }
        public string DisplayValue => Value.ToString("D2", CultureInfo.InvariantCulture);

        public bool Equals(ResidentNumber other) => Value == other.Value;
        public override bool Equals(object obj) => obj is ResidentNumber other && Equals(other);
        public override int GetHashCode() => Value;
        public override string ToString() => DisplayValue;
    }
}
