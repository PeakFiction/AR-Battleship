using System;
using System.Collections.Generic;
using System.Linq;

public class Ship
{
    private readonly HashSet<Coordinate> _positions;
    private readonly HashSet<Coordinate> _hits;

    public ShipId Id { get; }

    public Ship(ShipId id, IEnumerable<Coordinate> positions)
    {
        if (positions == null)
            throw new ArgumentNullException(nameof(positions));

        var posSet = new HashSet<Coordinate>(positions);

        if (posSet.Count == 0)
            throw new ArgumentException("Ship must have at least one position");

        Id = id;
        _positions = posSet;
        _hits = new HashSet<Coordinate>();
    }

    public IReadOnlyCollection<Coordinate> Positions => _positions.ToList().AsReadOnly();

    public IReadOnlyCollection<Coordinate> Hits => _hits.ToList().AsReadOnly();

    public bool IsSunk => _hits.SetEquals(_positions);

    public bool RegisterHit(Coordinate coordinate)
    {
        if (!_positions.Contains(coordinate))
            return false;

        if (_hits.Contains(coordinate))
            return false;

        _hits.Add(coordinate);
        return true;
    }
}