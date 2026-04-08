using System;

public readonly struct ShipId(Guid value) : IEquatable<ShipId>
{
    private readonly Guid _value = value;

    public static ShipId New() => new(Guid.NewGuid());

    public bool Equals(ShipId other) => _value.Equals(other._value);

    public override bool Equals(object? obj) =>
        obj is ShipId other && Equals(other);

    public override int GetHashCode() => _value.GetHashCode();

    public override string ToString() => _value.ToString();
}