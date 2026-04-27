using System;

namespace ARBattleship.Core.Domain {
    public class Cell
    {
        private readonly Coordinate _coordinate;

        public Cell(Coordinate coordinate)
        {
            _coordinate = coordinate;
        }
        private ShipId? _shipId = null;
        private bool _isShot = false;

        public Coordinate Coordinate => _coordinate;
        public bool HasShip => _shipId != null;
        public bool IsShot => _isShot;
        public ShipId? ShipId => _shipId;

        public Result<bool> PlaceShip(ShipId shipId)
        {
            if (_shipId != null)
                return Result<bool>.Failure("Cell already has a ship.");

            _shipId = shipId;
            return Result<bool>.Success(true);
        }

        public Result<ShotResult> Shoot()
        {
            if (_isShot)
                return Result<ShotResult>.Failure("Cell already shot.");

            _isShot = true;
            return Result<ShotResult>.Success(HasShip ? ShotResult.Hit : ShotResult.Miss);
        }
    }
}