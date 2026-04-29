using System;
using ARBattleship.Core.Domain; // Ensure this is the source of truth for PlayerId

namespace ARBattleship.Core.Application.Snapshots
{
    [Serializable]
    public sealed record GameSnapshot
    {
        // Using 'init' allows for both constructor and object initializer usage
        public PlayerSnapshot PlayerOne { get; init; }
        public PlayerSnapshot PlayerTwo { get; init; }
        public PlayerId CurrentTurn { get; init; }
        public bool IsGameOver { get; init; }
        public PlayerId? Winner { get; init; }

        public GameSnapshot(
            PlayerSnapshot playerOne,
            PlayerSnapshot playerTwo,
            PlayerId currentTurn,
            bool isGameOver,
            PlayerId? winner)
        {
            PlayerOne = playerOne;
            PlayerTwo = playerTwo;
            CurrentTurn = currentTurn;
            IsGameOver = isGameOver;
            Winner = winner;
        }
    }
}