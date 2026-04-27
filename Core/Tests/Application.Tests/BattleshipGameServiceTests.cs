using NUnit.Framework;
using ARBattleship.Core.Domain;
using ARBattleship.Core.Application.Services;
using ARBattleship.Core.Application.Commands;
using ARBattleship.Core.Application.Enums;
using ARBattleship.Core.Application.Events;
using ARBattleship.Core.Application.Snapshots;
using System.Collections.Generic;
using System.Linq;

namespace ARBattleship.Core.Tests.Application
{
    [TestFixture]
    public class BattleshipGameServiceTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────────

        private static BattleshipGame MakeGame() => new BattleshipGame(10);

        private static BattleshipGameService MakeService(BattleshipGame game)
            => new BattleshipGameService(game);

        private static void PlaceShip(BattleshipGame game, PlayerId player, string type, int startX, int startY)
        {
            var size = Ship.GetSize(type);
            var positions = Enumerable.Range(0, size)
                .Select(i => new Coordinate(startX + i, startY))
                .ToList();
            var ship = Ship.CreateFromType(type, ShipId.New(), positions);
            game.PlaceShip(player, ship);
        }

        private static void PlaceFullFleet(BattleshipGame game, PlayerId player, int rowOffset = 0)
        {
            PlaceShip(game, player, "Carrier",    0, rowOffset + 0);
            PlaceShip(game, player, "Battleship", 0, rowOffset + 1);
            PlaceShip(game, player, "Cruiser",    0, rowOffset + 2);
            PlaceShip(game, player, "Submarine",  0, rowOffset + 3);
            PlaceShip(game, player, "Destroyer",  0, rowOffset + 4);
        }

        private static BattleshipGameService MakeStartedGame(out BattleshipGame game)
        {
            game = MakeGame();
            PlaceFullFleet(game, PlayerId.PlayerOne, rowOffset: 0);
            PlaceFullFleet(game, PlayerId.PlayerTwo, rowOffset: 0);
            game.StartGame();
            return MakeService(game);
        }

        private static FireShotCommand ShotAt(PlayerId player, int x, int y)
            => new FireShotCommand(player, new Coordinate(x, y));

        private static ShipPlacementCommand PlacementCommand(
            PlayerId player, string type, int x, int y, Orientation orientation = Orientation.Horizontal)
            => new ShipPlacementCommand(player, type, new Coordinate(x, y), orientation);

        // ── GetSnapshot ───────────────────────────────────────────────────────────

        [Test]
        public void GetSnapshot_NewGame_ReturnsSnapshot()
        {
            var game = MakeGame();
            var service = MakeService(game);
            var snapshot = service.GetSnapshot(PlayerId.PlayerOne);
            Assert.That(snapshot, Is.Not.Null);
        }

        [Test]
        public void GetSnapshot_PlayerOnePerspective_PlayerOneCellsVisible()
        {
            var game = MakeGame();
            PlaceShip(game, PlayerId.PlayerOne, "Destroyer", 0, 0);
            var service = MakeService(game);

            var snapshot = service.GetSnapshot(PlayerId.PlayerOne);
            var shipCell = snapshot.PlayerOne.Cells.First(c => c.X == 0 && c.Y == 0);

            Assert.That(shipCell.State, Is.EqualTo(CellViewState.Ship));
        }

        [Test]
        public void GetSnapshot_PlayerOnePerspective_PlayerTwoCellsAreUnknown()
        {
            var game = MakeGame();
            PlaceShip(game, PlayerId.PlayerTwo, "Destroyer", 0, 0);
            var service = MakeService(game);

            var snapshot = service.GetSnapshot(PlayerId.PlayerOne);
            var cell = snapshot.PlayerTwo.Cells.First(c => c.X == 0 && c.Y == 0);

            Assert.That(cell.State, Is.EqualTo(CellViewState.Unknown));
        }

        [Test]
        public void GetSnapshot_AfterMiss_CellStateIsMiss()
        {
            var service = MakeStartedGame(out var game);
            game.FireShot(PlayerId.PlayerOne, new Coordinate(9, 9));

            var snapshot = service.GetSnapshot(PlayerId.PlayerOne);
            var cell = snapshot.PlayerTwo.Cells.First(c => c.X == 9 && c.Y == 9);

            Assert.That(cell.State, Is.EqualTo(CellViewState.Miss));
        }

        [Test]
        public void GetSnapshot_AfterHit_CellStateIsHit()
        {
            var service = MakeStartedGame(out var game);
            game.FireShot(PlayerId.PlayerOne, new Coordinate(0, 0)); // Carrier at row 0

            var snapshot = service.GetSnapshot(PlayerId.PlayerOne);
            var cell = snapshot.PlayerTwo.Cells.First(c => c.X == 0 && c.Y == 0);

            Assert.That(cell.State, Is.EqualTo(CellViewState.Hit));
        }

        [Test]
        public void GetSnapshot_AfterSunk_CellStateIsSunk()
        {
            var service = MakeStartedGame(out var game);

            // Sink destroyer at row 4 (size 2: cells 0,4 and 1,4)
            game.FireShot(PlayerId.PlayerOne, new Coordinate(0, 4));
            game.FireShot(PlayerId.PlayerTwo, new Coordinate(9, 9)); // AI turn
            game.FireShot(PlayerId.PlayerOne, new Coordinate(1, 4));

            var snapshot = service.GetSnapshot(PlayerId.PlayerOne);
            var cell = snapshot.PlayerTwo.Cells.First(c => c.X == 0 && c.Y == 4);

            Assert.That(cell.State, Is.EqualTo(CellViewState.Sunk));
        }

        [Test]
        public void GetSnapshot_CurrentTurn_MatchesGame()
        {
            var service = MakeStartedGame(out var game);
            var snapshot = service.GetSnapshot(PlayerId.PlayerOne);
            Assert.That(snapshot.CurrentTurn, Is.EqualTo(PlayerId.PlayerOne));
        }

        [Test]
        public void GetSnapshot_IsGameOver_FalseAtStart()
        {
            var service = MakeStartedGame(out _);
            var snapshot = service.GetSnapshot(PlayerId.PlayerOne);
            Assert.That(snapshot.IsGameOver, Is.False);
        }

        // ── TryFireShot ───────────────────────────────────────────────────────────

        [Test]
        public void TryFireShot_DuringSetup_ReturnsGameNotStarted()
        {
            var game = MakeGame();
            var service = MakeService(game);

            var result = service.TryFireShot(ShotAt(PlayerId.PlayerOne, 0, 0));

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Error, Is.EqualTo(GameErrorCode.GameNotStarted));
        }

        [Test]
        public void TryFireShot_WrongTurn_ReturnsNotPlayersTurn()
        {
            var service = MakeStartedGame(out _);

            var result = service.TryFireShot(ShotAt(PlayerId.PlayerTwo, 0, 0));

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Error, Is.EqualTo(GameErrorCode.NotPlayersTurn));
        }

        [Test]
        public void TryFireShot_ValidMiss_ReturnsSuccess()
        {
            var service = MakeStartedGame(out _);

            var result = service.TryFireShot(ShotAt(PlayerId.PlayerOne, 9, 9));

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.EqualTo(ShotOutcome.Miss));
        }

        [Test]
        public void TryFireShot_ValidHit_ReturnsHit()
        {
            var service = MakeStartedGame(out _);

            var result = service.TryFireShot(ShotAt(PlayerId.PlayerOne, 0, 0));

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.EqualTo(ShotOutcome.Hit));
        }

        [Test]
        public void TryFireShot_SinksShip_ReturnsSunk()
        {
            var service = MakeStartedGame(out var game);

            service.TryFireShot(ShotAt(PlayerId.PlayerOne, 0, 4));
            game.FireShot(PlayerId.PlayerTwo, new Coordinate(9, 9));
            var result = service.TryFireShot(ShotAt(PlayerId.PlayerOne, 1, 4));

            Assert.That(result.Value, Is.EqualTo(ShotOutcome.Sunk));
        }

        [Test]
        public void TryFireShot_OutOfBounds_ReturnsFailure()
        {
            var service = MakeStartedGame(out _);

            var result = service.TryFireShot(ShotAt(PlayerId.PlayerOne, 10, 10));

            Assert.That(result.IsSuccess, Is.False);
        }

        [Test]
        public void TryFireShot_SameCoordinateTwice_ReturnsFailure()
        {
            var service = MakeStartedGame(out var game);

            service.TryFireShot(ShotAt(PlayerId.PlayerOne, 9, 9));
            game.FireShot(PlayerId.PlayerTwo, new Coordinate(9, 8));
            var result = service.TryFireShot(ShotAt(PlayerId.PlayerOne, 9, 9));

            Assert.That(result.IsSuccess, Is.False);
        }

        [Test]
        public void TryFireShot_AfterGameFinished_ReturnsGameAlreadyFinished()
        {
            var service = MakeStartedGame(out var game);

            // Sweep every cell — PlayerOne fires at PlayerTwo's board,
            // PlayerTwo fires harmless misses at empty cells to keep turns alternating.
            int p2MissX = 9, p2MissY = 9;
            for (int x = 0; x < 10 && !game.IsGameOver; x++)
            {
                for (int y = 0; y < 10 && !game.IsGameOver; y++)
                {
                    if (game.CurrentTurn == PlayerId.PlayerOne)
                    {
                        game.FireShot(PlayerId.PlayerOne, new Coordinate(x, y));
                    }

                    if (!game.IsGameOver && game.CurrentTurn == PlayerId.PlayerTwo)
                    {
                        // Find an unshot cell on PlayerOne's board for the AI miss
                        while (p2MissX >= 0 &&
                               game.PlayerOneBoard.GetCell(new Coordinate(p2MissX, p2MissY)).IsShot)
                        {
                            p2MissX--;
                            if (p2MissX < 0) { p2MissX = 9; p2MissY--; }
                        }
                        if (p2MissX >= 0)
                            game.FireShot(PlayerId.PlayerTwo, new Coordinate(p2MissX, p2MissY));
                    }
                }
            }

            Assert.That(game.IsGameOver, Is.True, "Game should be over before testing the guard.");
            var result = service.TryFireShot(ShotAt(PlayerId.PlayerOne, 0, 0));
            Assert.That(result.Error, Is.EqualTo(GameErrorCode.GameAlreadyFinished));
        }

        // ── TryFireShot — events ──────────────────────────────────────────────────

        [Test]
        public void TryFireShot_ValidShot_RaisesShotFiredEvent()
        {
            var service = MakeStartedGame(out _);
            service.TryFireShot(ShotAt(PlayerId.PlayerOne, 9, 9));

            var events = service.ConsumeEvents();
            Assert.That(events.OfType<ShotFiredEvent>().Any(), Is.True);
        }

        [Test]
        public void TryFireShot_ValidShot_RaisesTurnChangedEvent()
        {
            var service = MakeStartedGame(out _);
            service.TryFireShot(ShotAt(PlayerId.PlayerOne, 9, 9));

            var events = service.ConsumeEvents();
            Assert.That(events.OfType<TurnChangedEvent>().Any(), Is.True);
        }

        [Test]
        public void TryFireShot_SinksShip_RaisesAnnouncementEvent()
        {
            var service = MakeStartedGame(out var game);
            service.TryFireShot(ShotAt(PlayerId.PlayerOne, 0, 4));
            game.FireShot(PlayerId.PlayerTwo, new Coordinate(9, 9));
            service.ConsumeEvents();

            service.TryFireShot(ShotAt(PlayerId.PlayerOne, 1, 4));

            var events = service.ConsumeEvents();
            Assert.That(events.OfType<AnnouncementEvent>().Any(), Is.True);
        }

        // ── TryPlaceShip ──────────────────────────────────────────────────────────

        [Test]
        public void TryPlaceShip_ValidPlacement_ReturnsSuccess()
        {
            var game = MakeGame();
            var service = MakeService(game);

            var result = service.TryPlaceShip(PlacementCommand(PlayerId.PlayerOne, "Destroyer", 0, 0));

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.True);
        }

        [Test]
        public void TryPlaceShip_DuringInProgress_ReturnsGameAlreadyStarted()
        {
            var service = MakeStartedGame(out _);

            var result = service.TryPlaceShip(PlacementCommand(PlayerId.PlayerOne, "Destroyer", 5, 5));

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Error, Is.EqualTo(GameErrorCode.GameAlreadyStarted));
        }

        [Test]
        public void TryPlaceShip_OutOfBounds_ReturnsFailure()
        {
            var game = MakeGame();
            var service = MakeService(game);

            var result = service.TryPlaceShip(PlacementCommand(PlayerId.PlayerOne, "Carrier", 8, 0));

            Assert.That(result.IsSuccess, Is.False);
        }

        [Test]
        public void TryPlaceShip_OverlappingShip_ReturnsFailure()
        {
            var game = MakeGame();
            var service = MakeService(game);

            service.TryPlaceShip(PlacementCommand(PlayerId.PlayerOne, "Destroyer", 0, 0));
            var result = service.TryPlaceShip(PlacementCommand(PlayerId.PlayerOne, "Cruiser", 0, 0));

            Assert.That(result.IsSuccess, Is.False);
        }

        [Test]
        public void TryPlaceShip_DuplicateShipType_ReturnsFailure()
        {
            var game = MakeGame();
            var service = MakeService(game);

            service.TryPlaceShip(PlacementCommand(PlayerId.PlayerOne, "Destroyer", 0, 0));
            var result = service.TryPlaceShip(PlacementCommand(PlayerId.PlayerOne, "Destroyer", 0, 2));

            Assert.That(result.IsSuccess, Is.False);
        }

        [Test]
        public void TryPlaceShip_VerticalOrientation_ReturnsSuccess()
        {
            var game = MakeGame();
            var service = MakeService(game);

            var result = service.TryPlaceShip(
                PlacementCommand(PlayerId.PlayerOne, "Destroyer", 0, 0, Orientation.Vertical));

            Assert.That(result.IsSuccess, Is.True);
        }

        [Test]
        public void TryPlaceShip_ValidPlacement_RaisesShipPlacedEvent()
        {
            var game = MakeGame();
            var service = MakeService(game);

            service.TryPlaceShip(PlacementCommand(PlayerId.PlayerOne, "Destroyer", 0, 0));

            var events = service.ConsumeEvents();
            Assert.That(events.OfType<ShipPlacedEvent>().Any(), Is.True);
        }

        // ── ConsumeEvents ─────────────────────────────────────────────────────────

        [Test]
        public void ConsumeEvents_AfterConsume_BufferIsEmpty()
        {
            var service = MakeStartedGame(out _);
            service.TryFireShot(ShotAt(PlayerId.PlayerOne, 9, 9));
            service.ConsumeEvents();

            var second = service.ConsumeEvents();

            Assert.That(second, Is.Empty);
        }

        [Test]
        public void ConsumeEvents_NoActions_ReturnsEmptyList()
        {
            var game = MakeGame();
            var service = MakeService(game);

            var events = service.ConsumeEvents();

            Assert.That(events, Is.Empty);
        }
    }
}