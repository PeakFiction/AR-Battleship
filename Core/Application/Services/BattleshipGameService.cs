// 1. Enforcing application-level preconditions (phase, turn).
// 2. Translating commands into domain calls.
// 3. Mapping domain errors to typed GameErrorCode values.
// 4. Building events and queuing them for GameManager to consume.
// 5. Projecting board state into snapshots for UI rendering.
// GetSnapshot uses BuildPlayerSnapshot which calls ProjectCellState to
// apply the viewer's perspective:  own cells show ships; opponent cells hide
// ships unless they have been hit or the whole ship is sunk.
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
    /// <summary>
    /// Sole implementation of <see cref="IBattleshipGameService"/>.
    /// Wraps a <see cref="BattleshipGame"/> domain object, adds application-level
    /// validation, and produces typed events for downstream consumers.
    /// </summary>
    public sealed class BattleshipGameService : IBattleshipGameService
    {
        private readonly BattleshipGame _game;

        /// <summary>Events queued since the last ConsumeEvents call.</summary>
        private readonly List<IGameEvent> _pendingEvents = new();

        /// <summary>
        /// Creates the service wrapping the given domain game.
        /// The game must have already been constructed but need not be started.
        /// </summary>
        public BattleshipGameService(BattleshipGame game) => _game = game;


        public GameSnapshot GetSnapshot(PlayerId viewer)
        {
            return new GameSnapshot(
                playerOne:   BuildPlayerSnapshot(PlayerId.PlayerOne, viewer),
                playerTwo:   BuildPlayerSnapshot(PlayerId.PlayerTwo, viewer),
                currentTurn: _game.CurrentTurn,
                isGameOver:  _game.IsGameOver,
                winner:      _game.Winner);
        }

        public Result<ShotOutcome, GameErrorCode> TryFireShot(FireShotCommand command)
        {
            if (_game.Phase == GamePhase.Setup)
                return Result<ShotOutcome, GameErrorCode>.Failure(GameErrorCode.GameNotStarted);

            if (_game.Phase == GamePhase.Finished)
                return Result<ShotOutcome, GameErrorCode>.Failure(GameErrorCode.GameAlreadyFinished);

            if (_game.CurrentTurn != command.PlayerId)
                return Result<ShotOutcome, GameErrorCode>.Failure(GameErrorCode.NotPlayersTurn);

            var result = _game.FireShot(command.PlayerId, command.Coordinate);

            if (!result.IsSuccess)
            {
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

            var fireResult = result.Value!;
            var outcome    = MapToOutcome(fireResult);

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
            if (_game.Phase != GamePhase.Setup)
                return Result<bool, GameErrorCode>.Failure(GameErrorCode.GameAlreadyStarted);

            var positions = CalculatePositions(
                command.StartCoordinate,
                command.Orientation,
                Ship.GetSize(command.ShipType));

            Ship shipToPlace;
            try
            {
                shipToPlace = Ship.CreateFromType(command.ShipType, ShipId.New(), positions, command.Orientation);
            }
            catch (ArgumentException)
            {
                return Result<bool, GameErrorCode>.Failure(GameErrorCode.Unknown);
            }

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

        public IReadOnlyList<IGameEvent> ConsumeEvents()
        {
            // Snapshot the list before clearing so the caller receives all events
            // even if an exception occurs between snapshot and clear.
            var events = new List<IGameEvent>(_pendingEvents);
            _pendingEvents.Clear();
            return events;
        }

        /// <summary>Adds an event to the pending buffer.</summary>
        private void AddEvent(IGameEvent gameEvent) => _pendingEvents.Add(gameEvent);

        /// <summary>
        /// Calculates all cell coordinates for a ship given its start, orientation, and size.
        /// Uses Orientation.GetOffset to determine the step direction.
        /// </summary>
        private static IEnumerable<Coordinate> CalculatePositions(
            Coordinate start,
            Orientation orientation,
            int size)
        {
            var offset    = orientation.GetOffset();
            var positions = new List<Coordinate>(size);

            for (int i = 0; i < size; i++)
                positions.Add(new Coordinate(start.X + offset.X * i, start.Y + offset.Y * i));

            return positions;
        }

        /// <summary>
        /// Builds a PlayerSnapshot for <paramref name="boardOwner"/> from
        /// <paramref name="viewer"/>'s perspective.
        /// </summary>
        private PlayerSnapshot BuildPlayerSnapshot(PlayerId boardOwner, PlayerId viewer)
        {
            var board       = (boardOwner == PlayerId.PlayerOne) ? _game.PlayerOneBoard : _game.PlayerTwoBoard;
            var isOwnerView = boardOwner == viewer;

            var cells = new List<CellView>();
            for (int y = 0; y < board.Size; y++)
            {
                for (int x = 0; x < board.Size; x++)
                {
                    var coord = new Coordinate(x, y);
                    var cell  = board.GetCell(coord);

                    // Look up the ship by ID if the cell is occupied
                    Ship? ship = cell.ShipId.HasValue
                        ? board.GetShips().FirstOrDefault(s => s.Id.Equals(cell.ShipId.Value))
                        : null;

                    cells.Add(new CellView
                    {
                        X        = x,
                        Y        = y,
                        State    = ProjectCellState(cell, ship, isOwnerView),
                        // Ship type is revealed when: owner view, or the ship is sunk, or this cell has been hit
                        ShipType = (isOwnerView || (ship?.IsSunk ?? false) || (cell.IsShot && cell.HasShip))
                            ? ship?.ShipType
                            : null
                    });
                }
            }

            // Opponent's view only includes ships that have been sunk
            var ships = isOwnerView
                ? board.GetShips().Select(s => new ShipView(s.ShipType, s.Size, s.IsSunk, s.Orientation)).ToList()
                : board.GetShips().Where(s => s.IsSunk).Select(s => new ShipView(s.ShipType, s.Size, s.IsSunk, s.Orientation)).ToList();

            return new PlayerSnapshot(boardOwner, cells, ships);
        }

        /// <summary>
        /// Determines what a cell looks like from the viewer's perspective.
        /// Owner sees ships; opponent sees only the results of shots.
        /// </summary>
        private static CellViewState ProjectCellState(Cell cell, Ship? ship, bool isOwnerView)
        {
            if (!cell.IsShot)
            {
                if (isOwnerView)
                    return cell.HasShip ? CellViewState.Ship : CellViewState.Empty;

                return CellViewState.Unknown; // Unshot opponent cell is hidden
            }

            if (!cell.HasShip) return CellViewState.Miss;

            // Cell was shot and had a ship — is the ship sunk?
            return (ship != null && ship.IsSunk) ? CellViewState.Sunk : CellViewState.Hit;
        }

        /// <summary>Maps a domain FireResult to the application ShotOutcome enum.</summary>
        private static ShotOutcome MapToOutcome(FireResult result)
        {
            if (result.IsSunk) return ShotOutcome.Sunk;
            if (result.IsHit)  return ShotOutcome.Hit;
            return ShotOutcome.Miss;
        }
    }
}
