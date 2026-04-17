using System;
using System.Collections.Generic;
using System.Linq;
using Core.Application.Commands;
using Core.Application.Common;
using Core.Application.Enums;
using Core.Application.Events;
using Core.Application.Snapshots;
using Core.Domain;

namespace Core.Application.Services;

public sealed class BattleshipGameService : IBattleshipGameService
{
    private readonly Board _playerOneBoard;
    private readonly Board _playerTwoBoard;
    private readonly List<IGameEvent> _pendingEvents = new();

    private PlayerId _currentTurn;
    private bool _isGameOver;
    private PlayerId? _winner;

    public BattleshipGameService(
        Board playerOneBoard,
        Board playerTwoBoard,
        PlayerId startingTurn = PlayerId.PlayerOne)
    {
        _playerOneBoard = playerOneBoard;
        _playerTwoBoard = playerTwoBoard;
        _currentTurn = startingTurn;
        _isGameOver = false;
        _winner = null;
    }

    public GameSnapshot GetSnapshot(PlayerId viewer)
    {
        var playerOneSnapshot = BuildPlayerSnapshot(PlayerId.PlayerOne, viewer);
        var playerTwoSnapshot = BuildPlayerSnapshot(PlayerId.PlayerTwo, viewer);

        return new GameSnapshot(
            playerOneSnapshot,
            playerTwoSnapshot,
            _currentTurn,
            _isGameOver,
            _winner);
    }

    public Result<ShotOutcome, GameError> TryFireShot(FireShotCommand command)
    {
        throw new NotImplementedException();
    }

    public Result<PlacementOutcome, GameError> TryPlaceShip(ShipPlacementCommand command)
    {
        throw new NotImplementedException();
    }

    public IReadOnlyList<IGameEvent> ConsumeEvents()
    {
        var events = _pendingEvents.ToList();
        _pendingEvents.Clear();
        return events;
    }

    private void AddEvent(IGameEvent gameEvent)
    {
        _pendingEvents.Add(gameEvent);
    }

    private Board GetBoard(PlayerId playerId)
    {
        return playerId == PlayerId.PlayerOne ? _playerOneBoard : _playerTwoBoard;
    }

    private PlayerSnapshot BuildPlayerSnapshot(PlayerId boardOwner, PlayerId viewer)
    {
        var board = GetBoard(boardOwner);
        var isOwnerView = boardOwner == viewer;

        var cells = BuildCellViews(board, isOwnerView);
        var ships = BuildShipViews(board, isOwnerView);

        return new PlayerSnapshot(boardOwner, cells, ships);
    }

    private IReadOnlyList<CellView> BuildCellViews(Board board, bool isOwnerView)
    {
        var cells = new List<CellView>();

        for (int y = 0; y < board.Height; y++)
        {
            for (int x = 0; x < board.Width; x++)
            {
                var coordinate = new Coordinate(x, y);
                var cell = board.GetCell(coordinate);

                var state = ProjectCellState(cell, isOwnerView);

                cells.Add(new CellView(x, y, state));
            }
        }

        return cells;
    }

    private IReadOnlyList<ShipView> BuildShipViews(Board board, bool isOwnerView)
    {
        if (!isOwnerView)
        {
            return Array.Empty<ShipView>();
        }

        return board.GetShips()
            .Select(ship => new ShipView(
                ship.Type.ToString(),
                ship.Length,
                ship.IsSunk))
            .ToList();
    }

    private CellViewState ProjectCellState(BoardCell cell, bool isOwnerView)
    {
        if (isOwnerView)
        {
            if (!cell.HasBeenShot && !cell.HasShip)
            {
                return CellViewState.Empty;
            }

            if (!cell.HasBeenShot && cell.HasShip)
            {
                return CellViewState.Ship;
            }

            if (cell.HasBeenShot && !cell.HasShip)
            {
                return CellViewState.Miss;
            }

            if (cell.HasBeenShot && cell.HasShip && cell.IsSunk)
            {
                return CellViewState.Sunk;
            }

            if (cell.HasBeenShot && cell.HasShip)
            {
                return CellViewState.Hit;
            }
        }
        else
        {
            if (!cell.HasBeenShot)
            {
                return CellViewState.Unknown;
            }

            if (cell.HasBeenShot && !cell.HasShip)
            {
                return CellViewState.Miss;
            }

            if (cell.HasBeenShot && cell.HasShip && cell.IsSunk)
            {
                return CellViewState.Sunk;
            }

            if (cell.HasBeenShot && cell.HasShip)
            {
                return CellViewState.Hit;
            }
        }

        throw new InvalidOperationException("Unhandled fog-of-war projection state.");
    }
}