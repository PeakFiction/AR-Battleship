using System;

namespace ARBattleship.Core.Domain
{
    /// <summary>
    /// Immutable globally-unique identifier for a Ship instance.
    /// </summary>
    public readonly struct ShipId : IEquatable<ShipId>
    {
        private readonly Guid _value;

        /// <summary>Creates a ShipId wrapping the given Guid.</summary>
        public ShipId(Guid value) => _value = value;

        /// <summary>Creates a new ShipId with a globally unique Guid.</summary>
        public static ShipId New() => new(Guid.NewGuid());

        /// <summary>True when both ShipIds wrap the same Guid.</summary>
        public bool Equals(ShipId other) => _value.Equals(other._value);

        public override bool Equals(object? obj) => obj is ShipId other && Equals(other);

        /// <summary>Delegates to Guid.GetHashCode, safe as Dictionary key.</summary>
        public override int GetHashCode() => _value.GetHashCode();

        /// <summary>Returns the Guid as a lowercase string.</summary>
        public override string ToString() => _value.ToString();
    }
}
