using NUnit.Framework;
using ARBattleship.Core.Domain;
using System.Collections.Generic;

namespace ARBattleship.Core.Tests.Domain
{
    [TestFixture]
    public class BattleshipGameTests
    {
        private static void PlaceOneShip(BattleshipGame game, PlayerId player, int startX = 0, int startY = 0)
        {
            var positions = new List<Coordinate>
            {
                new Coordinate(startX, startY),
                new Coordinate(startX + 1, startY)
            };
            var ship = Ship.CreateDestroyer(ShipId.New(), positions);
            game.PlaceShip(player, ship);
        }

        private static void PlaceFullFleet(BattleshipGame game, PlayerId player, int rowOffset = 0)
        {
            var fleet = new (System.Func<ShipId, IEnumerable<Coordinate>, Ship> Factory, int Size)[]
            {
                (Ship.CreateCarrier,    5),
                (Ship.CreateBattleship, 4),
                (Ship.CreateCruiser,    3),
                (Ship.CreateSubmarine,  3),
                (Ship.CreateDestroyer,  2),
            };

            for (int i = 0; i < fleet.Length; i++)
            {
                var (factory, size) = fleet[i];
                var positions = new List<Coordinate>();
                for (int x = 0; x < size; x++)
                    positions.Add(new Coordinate(x, rowOffset + i));
                game.PlaceShip(player, factory(ShipId.New(), positions));
            }
        }

        private static void SinkAllPlayerTwoShips(BattleshipGame game)
        {
            for (int x = 0; x < game.PlayerTwoBoard.Size; x++)
            {
                for (int y = 0; y < game.PlayerTwoBoard.Size; y++)
                {
                    if (game.Phase == GamePhase.Finished) return;
                    if (game.CurrentTurn != PlayerId.PlayerOne) continue;

                    game.FireShot(PlayerId.PlayerOne, new Coordinate(x, y));

                    if (game.Phase != GamePhase.Finished && game.CurrentTurn == PlayerId.PlayerTwo)
                        game.FireShot(PlayerId.PlayerTwo, new Coordinate(x, y));
                }
            }
        }

        [Test]
        public void Constructor_NewGame_PhaseIsSetup()
        {
            var game = new BattleshipGame();
            Assert.That(game.Phase, Is.EqualTo(GamePhase.Setup));
        }

        [Test]
        public void Constructor_NewGame_CurrentTurnIsPlayerOne()
        {
            var game = new BattleshipGame();
            Assert.That(game.CurrentTurn, Is.EqualTo(PlayerId.PlayerOne));
        }

        [Test]
        public void Constructor_NewGame_IsGameOverIsFalse()
        {
            var game = new BattleshipGame();
            Assert.That(game.IsGameOver, Is.False);
        }

        [Test]
        public void Constructor_NewGame_WinnerIsNull()
        {
            var game = new BattleshipGame();
            Assert.That(game.Winner, Is.Null);
        }

        [Test]
        public void Constructor_NewGame_BoardsAreInitialised()
        {
            var game = new BattleshipGame();
            Assert.That(game.PlayerOneBoard, Is.Not.Null);
            Assert.That(game.PlayerTwoBoard, Is.Not.Null);
        }

        [Test]
        public void Constructor_CustomSize_BoardsUseCustomSize()
        {
            var game = new BattleshipGame(8);
            Assert.That(game.PlayerOneBoard.Size, Is.EqualTo(8));
            Assert.That(game.PlayerTwoBoard.Size, Is.EqualTo(8));
        }

        [Test]
        public void StartGame_BothPlayersHaveShips_ReturnsSuccess()
        {
            var game = new BattleshipGame();
            PlaceOneShip(game, PlayerId.PlayerOne);
            PlaceOneShip(game, PlayerId.PlayerTwo);

            var result = game.StartGame();

            Assert.That(result.IsSuccess, Is.True);
        }

        [Test]
        public void StartGame_BothPlayersHaveShips_PhaseBecomesInProgress()
        {
            var game = new BattleshipGame();
            PlaceOneShip(game, PlayerId.PlayerOne);
            PlaceOneShip(game, PlayerId.PlayerTwo);

            game.StartGame();

            Assert.That(game.Phase, Is.EqualTo(GamePhase.InProgress));
        }

        [Test]
        public void StartGame_PlayerOneHasNoShips_ReturnsFailure()
        {
            var game = new BattleshipGame();
            PlaceOneShip(game, PlayerId.PlayerTwo);

            var result = game.StartGame();

            Assert.That(result.IsSuccess, Is.False);
        }

        [Test]
        public void StartGame_PlayerTwoHasNoShips_ReturnsFailure()
        {
            var game = new BattleshipGame();
            PlaceOneShip(game, PlayerId.PlayerOne);

            var result = game.StartGame();

            Assert.That(result.IsSuccess, Is.False);
        }

        [Test]
        public void StartGame_NeitherPlayerHasShips_ReturnsFailure()
        {
            var game = new BattleshipGame();

            var result = game.StartGame();

            Assert.That(result.IsSuccess, Is.False);
        }

        [Test]
        public void StartGame_CalledDuringInProgress_ReturnsFailure()
        {
            var game = new BattleshipGame();
            PlaceOneShip(game, PlayerId.PlayerOne);
            PlaceOneShip(game, PlayerId.PlayerTwo);
            game.StartGame();

            var result = game.StartGame();

            Assert.That(result.IsSuccess, Is.False);
        }

        [Test]
        public void StartGame_CalledAfterGameFinished_ReturnsFailure()
        {
            var game = new BattleshipGame();
            PlaceFullFleet(game, PlayerId.PlayerOne, rowOffset: 0);
            PlaceFullFleet(game, PlayerId.PlayerTwo, rowOffset: 0);
            game.StartGame();
            SinkAllPlayerTwoShips(game);

            var result = game.StartGame();

            Assert.That(result.IsSuccess, Is.False);
        }

        [Test]
        public void PlaceShip_DuringSetup_ReturnsSuccess()
        {
            var game = new BattleshipGame();

            var result = PlaceShipWithResult(game, PlayerId.PlayerOne, 0, 0);

            Assert.That(result.IsSuccess, Is.True);
        }

        [Test]
        public void PlaceShip_DuringInProgress_ReturnsFailure()
        {
            var game = new BattleshipGame();
            PlaceOneShip(game, PlayerId.PlayerOne);
            PlaceOneShip(game, PlayerId.PlayerTwo);
            game.StartGame();

            var result = PlaceShipWithResult(game, PlayerId.PlayerOne, 5, 5);

            Assert.That(result.IsSuccess, Is.False);
        }

        [Test]
        public void PlaceShip_DuringFinished_ReturnsFailure()
        {
            var game = new BattleshipGame();
            PlaceFullFleet(game, PlayerId.PlayerOne, rowOffset: 0);
            PlaceFullFleet(game, PlayerId.PlayerTwo, rowOffset: 0);
            game.StartGame();
            SinkAllPlayerTwoShips(game);

            var result = PlaceShipWithResult(game, PlayerId.PlayerOne, 5, 5);

            Assert.That(result.IsSuccess, Is.False);
        }

        [Test]
        public void PlaceShip_BothPlayersDuringSetup_BothSucceed()
        {
            var game = new BattleshipGame();

            var p1Result = PlaceShipWithResult(game, PlayerId.PlayerOne, 0, 0);
            var p2Result = PlaceShipWithResult(game, PlayerId.PlayerTwo, 0, 0);

            Assert.That(p1Result.IsSuccess, Is.True);
            Assert.That(p2Result.IsSuccess, Is.True);
        }

        [Test]
        public void PlaceShip_OverlappingPosition_ReturnsFailure()
        {
            var game = new BattleshipGame();
            PlaceShipWithResult(game, PlayerId.PlayerOne, 0, 0);

            var positions = new List<Coordinate>
            {
                new Coordinate(1, 0),
                new Coordinate(1, 1),
                new Coordinate(1, 2)
            };
            var ship = Ship.CreateCruiser(ShipId.New(), positions);
            var result = game.PlaceShip(PlayerId.PlayerOne, ship);

            Assert.That(result.IsSuccess, Is.False);
        }

        [Test]
        public void FireShot_DuringSetup_ReturnsFailure()
        {
            var game = new BattleshipGame();

            var result = game.FireShot(PlayerId.PlayerOne, new Coordinate(0, 0));

            Assert.That(result.IsSuccess, Is.False);
        }

        [Test]
        public void FireShot_WrongTurn_ReturnsFailure()
        {
            var game = new BattleshipGame();
            PlaceOneShip(game, PlayerId.PlayerOne);
            PlaceOneShip(game, PlayerId.PlayerTwo);
            game.StartGame();

            var result = game.FireShot(PlayerId.PlayerTwo, new Coordinate(0, 0));

            Assert.That(result.IsSuccess, Is.False);
        }

        [Test]
        public void FireShot_ValidMiss_ReturnsSuccess()
        {
            var game = new BattleshipGame();
            PlaceOneShip(game, PlayerId.PlayerOne, 0, 0);
            PlaceOneShip(game, PlayerId.PlayerTwo, 0, 0);
            game.StartGame();

            var result = game.FireShot(PlayerId.PlayerOne, new Coordinate(9, 9));

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value!.Outcome, Is.EqualTo(ShotResult.Miss));
        }

        [Test]
        public void FireShot_ValidHit_ReturnsHit()
        {
            var game = new BattleshipGame();
            PlaceOneShip(game, PlayerId.PlayerOne, 0, 0);
            PlaceOneShip(game, PlayerId.PlayerTwo, 0, 0);
            game.StartGame();

            var result = game.FireShot(PlayerId.PlayerOne, new Coordinate(0, 0));

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value!.IsHit, Is.True);
        }

        [Test]
        public void FireShot_ValidShot_SwitchesTurn()
        {
            var game = new BattleshipGame();
            PlaceOneShip(game, PlayerId.PlayerOne);
            PlaceOneShip(game, PlayerId.PlayerTwo);
            game.StartGame();

            game.FireShot(PlayerId.PlayerOne, new Coordinate(9, 9));

            Assert.That(game.CurrentTurn, Is.EqualTo(PlayerId.PlayerTwo));
        }

        [Test]
        public void FireShot_SameCoordinateTwice_SecondShotReturnsFailure()
        {
            var game = new BattleshipGame();
            PlaceOneShip(game, PlayerId.PlayerOne);
            PlaceOneShip(game, PlayerId.PlayerTwo);
            game.StartGame();

            game.FireShot(PlayerId.PlayerOne, new Coordinate(9, 9));
            game.FireShot(PlayerId.PlayerTwo, new Coordinate(9, 9));

            var result = game.FireShot(PlayerId.PlayerOne, new Coordinate(9, 9));

            Assert.That(result.IsSuccess, Is.False);
        }

        [Test]
        public void FireShot_OutOfBounds_ReturnsFailure()
        {
            var game = new BattleshipGame();
            PlaceOneShip(game, PlayerId.PlayerOne);
            PlaceOneShip(game, PlayerId.PlayerTwo);
            game.StartGame();

            var result = game.FireShot(PlayerId.PlayerOne, new Coordinate(10, 10));

            Assert.That(result.IsSuccess, Is.False);
        }

        [Test]
        public void FireShot_AfterGameFinished_ReturnsFailure()
        {
            var game = new BattleshipGame();
            PlaceFullFleet(game, PlayerId.PlayerOne, rowOffset: 0);
            PlaceFullFleet(game, PlayerId.PlayerTwo, rowOffset: 0);
            game.StartGame();
            SinkAllPlayerTwoShips(game);

            var result = game.FireShot(PlayerId.PlayerOne, new Coordinate(0, 0));

            Assert.That(result.IsSuccess, Is.False);
        }

        [Test]
        public void FireShot_LastShipSunk_PhaseBecomesFinished()
        {
            var game = new BattleshipGame();
            PlaceFullFleet(game, PlayerId.PlayerOne, rowOffset: 0);
            PlaceFullFleet(game, PlayerId.PlayerTwo, rowOffset: 0);
            game.StartGame();

            SinkAllPlayerTwoShips(game);

            Assert.That(game.Phase, Is.EqualTo(GamePhase.Finished));
        }

        [Test]
        public void FireShot_LastShipSunk_WinnerIsCorrectPlayer()
        {
            var game = new BattleshipGame();
            PlaceFullFleet(game, PlayerId.PlayerOne, rowOffset: 0);
            PlaceFullFleet(game, PlayerId.PlayerTwo, rowOffset: 0);
            game.StartGame();

            SinkAllPlayerTwoShips(game);

            Assert.That(game.Winner, Is.EqualTo(PlayerId.PlayerOne));
        }

        [Test]
        public void FireShot_LastShipSunk_IsGameOverIsTrue()
        {
            var game = new BattleshipGame();
            PlaceFullFleet(game, PlayerId.PlayerOne, rowOffset: 0);
            PlaceFullFleet(game, PlayerId.PlayerTwo, rowOffset: 0);
            game.StartGame();

            SinkAllPlayerTwoShips(game);

            Assert.That(game.IsGameOver, Is.True);
        }

        [Test]
        public void IsGameOver_DuringSetup_IsFalse()
        {
            var game = new BattleshipGame();
            Assert.That(game.IsGameOver, Is.False);
        }

        [Test]
        public void IsGameOver_DuringInProgress_IsFalse()
        {
            var game = new BattleshipGame();
            PlaceOneShip(game, PlayerId.PlayerOne);
            PlaceOneShip(game, PlayerId.PlayerTwo);
            game.StartGame();

            Assert.That(game.IsGameOver, Is.False);
        }

        private static Result<bool> PlaceShipWithResult(BattleshipGame game, PlayerId player, int startX, int startY)
        {
            var positions = new List<Coordinate>
            {
                new Coordinate(startX, startY),
                new Coordinate(startX + 1, startY)
            };
            var ship = Ship.CreateDestroyer(ShipId.New(), positions);
            return game.PlaceShip(player, ship);
        }
    }
}