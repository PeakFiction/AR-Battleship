using System;
using System.Collections.Generic;
using System.Linq;
using ARBattleship.Core.Application.Commands;
using ARBattleship.Core.Application.Common;
using ARBattleship.Core.Application.Events;
using ARBattleship.Core.Application.Snapshots;
using ARBattleship.Core.Application.Enums;
using ARBattleship.Core.Domain;

namespace ARBattleship.Core.Application.Services
{
    public sealed class BattleshipGameService : IBattleshipGameService
    {
        private readonly BattleshipGame _game;
        private readonly List<IGameEvent> _pendingEvents = new();

        public BattleshipGameService(BattleshipGame game)
        {
            _game = game;
        }

        public GameSnapshot GetSnapshot(PlayerId viewer)
        {
            return new GameSnapshot(
                playerOne: BuildPlayerSnapshot(PlayerId.PlayerOne, viewer),
                playerTwo: BuildPlayerSnapshot(PlayerId.PlayerTwo, viewer),
                currentTurn: _game.CurrentTurn,
                isGameOver: _game.IsGameOver,
                winner: _game.Winner);
        }

        public Result<ShotOutcome, GameErrorCode> TryFireShot(FireShotCommand command)
        {
            // 1. Phase guard
            if (_game.Phase == GamePhase.Setup)
                return Result<ShotOutcome, GameErrorCode>.Failure(GameErrorCode.GameNotStarted);

            if (_game.Phase == GamePhase.Finished)
                return Result<ShotOutcome, GameErrorCode>.Failure(GameErrorCode.GameAlreadyFinished);

            // 2. Turn guard
            if (_game.CurrentTurn != command.PlayerId)
                return Result<ShotOutcome, GameErrorCode>.Failure(GameErrorCode.NotPlayersTurn);

            // 3. Delegate to domain
            var result = _game.FireShot(command.PlayerId, command.Coordinate);

            if (!result.IsSuccess)
            {
                // Map domain error message to a granular error code
                var errorCode = result.Error switch
                {
                    var e when e != null && e.Contains("out of bounds", StringComparison.OrdinalIgnoreCase)
                        => GameErrorCode.InvalidCoordinate,
                    var e when e != null && e.Contains("already shot", StringComparison.OrdinalIgnoreCase)
                        => GameErrorCode.CellAlreadyTargeted,
                    _ => GameErrorCode.Unknown
                };
                return Result<ShotOutcome, GameErrorCode>.Failure(errorCode);
            }

            // 4. Map outcome and raise events
            var fireResult = result.Value!;
            var outcome = MapToOutcome(fireResult);

            if (fireResult.IsSunk)
                AddEvent(new AnnouncementEvent($"The {fireResult.ShipType ?? "Ship"} has been sunk!"));

            AddEvent(new ShotFiredEvent(
                command.PlayerId,
                command.Coordinate.X,
                command.Coordinate.Y,
                outcome,
                fireResult.HitSegmentIndex,
                fireResult.ShipOrientation,
                fireResult.ShipType
            ));
            
            AddEvent(new TurnChangedEvent(_game.CurrentTurn));

            if (_game.IsGameOver)
                AddEvent(new GameEndedEvent(_game.Winner!));

            return Result<ShotOutcome, GameErrorCode>.Success(outcome);
        }

        public Result<bool, GameErrorCode> TryPlaceShip(ShipPlacementCommand command)
        {
            // 1. Phase guard
            if (_game.Phase != GamePhase.Setup)
                return Result<bool, GameErrorCode>.Failure(GameErrorCode.GameAlreadyStarted);

            // 2. Build positions using domain knowledge (Ship.GetSize)
            var positions = CalculatePositions(
                command.StartCoordinate,
                command.Orientation,
                Ship.GetSize(command.ShipType));

            // 3. Create ship using domain factory
            Ship shipToPlace;
            try
            {
                shipToPlace = Ship.CreateFromType(command.ShipType, ShipId.New(), positions, command.Orientation);
            }
            catch (ArgumentException)
            {
                return Result<bool, GameErrorCode>.Failure(GameErrorCode.Unknown);
            }

            // 4. Delegate placement to domain
            var result = _game.PlaceShip(command.PlayerId, shipToPlace);

            if (!result.IsSuccess)
            {
                var errorCode = result.Error switch
                {
                    var e when e != null && e.Contains("out of bounds", StringComparison.OrdinalIgnoreCase)
                        => GameErrorCode.ShipOutOfBounds,
                    var e when e != null && e.Contains("overlaps", StringComparison.OrdinalIgnoreCase)
                        => GameErrorCode.ShipOverlap,
                    _ => GameErrorCode.InvalidShipPlacement
                };
                return Result<bool, GameErrorCode>.Failure(errorCode);
            }

            AddEvent(new ShipPlacedEvent(command.PlayerId, shipToPlace.ShipType));
            return Result<bool, GameErrorCode>.Success(true);
        }

        /// <summary>
        /// Calculates ship positions from a start coordinate, orientation, and size.
        /// Size is provided by the domain (Ship.GetSize) rather than duplicated here.
        /// </summary>
        private static IEnumerable<Coordinate> CalculatePositions(
            Coordinate start,
            Orientation orientation,
            int size)
        {
            var offset = orientation.GetOffset();
            var positions = new List<Coordinate>(size);

            for (int i = 0; i < size; i++)
                positions.Add(new Coordinate(start.X + offset.X * i, start.Y + offset.Y * i));

            return positions;
        }

        /// <summary>
        /// Drains the pending event buffer and returns a snapshot of the events.
        /// Safe against exceptions between snapshot and clear.
        /// </summary>
        public IReadOnlyList<IGameEvent> ConsumeEvents()
        {
            var events = new List<IGameEvent>(_pendingEvents);
            _pendingEvents.Clear();
            return events;
        }

        private void AddEvent(IGameEvent gameEvent) => _pendingEvents.Add(gameEvent);

        private PlayerSnapshot BuildPlayerSnapshot(PlayerId boardOwner, PlayerId viewer)
        {
            var board = (boardOwner == PlayerId.PlayerOne) ? _game.PlayerOneBoard : _game.PlayerTwoBoard;
            var isOwnerView = boardOwner == viewer;

            var cells = new List<CellView>();
            for (int y = 0; y < board.Size; y++)
            {
                for (int x = 0; x < board.Size; x++)
                {
                    var coord = new Coordinate(x, y);
                    var cell = board.GetCell(coord);

                    Ship? ship = cell.ShipId.HasValue
                        ? board.GetShips().FirstOrDefault(s => s.Id.Equals(cell.ShipId.Value))
                        : null;

                    cells.Add(new CellView
                    {
                        X = x,
                        Y = y,
                        State = ProjectCellState(cell, ship, isOwnerView),
                        ShipType = (isOwnerView || (ship?.IsSunk ?? false) || (cell.IsShot && cell.HasShip))
                            ? ship?.ShipType
                            : null
                    });
                }
            }

            var ships = isOwnerView
                ? board.GetShips().Select(s => new ShipView(s.ShipType, s.Size, s.IsSunk, s.Orientation)).ToList()
                : board.GetShips().Where(s => s.IsSunk).Select(s => new ShipView(s.ShipType, s.Size, s.IsSunk, s.Orientation)).ToList();

            return new PlayerSnapshot(boardOwner, cells, ships);
        }

        private static CellViewState ProjectCellState(Cell cell, Ship? ship, bool isOwnerView)
        {
            if (!cell.IsShot)
            {
                if (isOwnerView)
                    return cell.HasShip ? CellViewState.Ship : CellViewState.Empty;

                return CellViewState.Unknown;
            }

            if (!cell.HasShip)
                return CellViewState.Miss;

            return (ship != null && ship.IsSunk) ? CellViewState.Sunk : CellViewState.Hit;
        }

        private static ShotOutcome MapToOutcome(FireResult result)
        {
            if (result.IsSunk) return ShotOutcome.Sunk;
            if (result.IsHit) return ShotOutcome.Hit;
            return ShotOutcome.Miss;
        }
    }
}