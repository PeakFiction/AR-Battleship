using System.Collections.Generic;
using Core.Application.Commands;
using Core.Application.Common;
using Core.Application.Events;
using Core.Application.Snapshots;

namespace Core.Application.Services;

public interface IBattleshipGameService
{
    GameSnapshot GetSnapshot(PlayerId viewer);
    Result<ShotOutcome, GameError> TryFireShot(FireShotCommand command);
    Result<PlacementOutcome, GameError> TryPlaceShip(ShipPlacementCommand command);
    IReadOnlyList<IGameEvent> ConsumeEvents();
}