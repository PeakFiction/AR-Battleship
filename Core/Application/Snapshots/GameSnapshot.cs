// =============================================================================
// GameSnapshot.cs  |  ARBattleship.Core.Application.Snapshots
// =============================================================================
// Top-level read-only view of the entire game state for one viewer.
// Returned by BattleshipGameService.GetSnapshot and used by CombatUI to
// initialise the minimap and refresh board displays.
// =============================================================================
using System;
using ARBattleship.Core.Domain;

namespace ARBattleship.Core.Application.Snapshots
{
    /// <summary>
    /// Immutable snapshot of the full game state, projected for a specific viewer.
    /// Contains both player boards, the current turn, and the game-over state.
    /// </summary>
    [Serializable]
    public sealed record GameSnapshot
    {
        /// <summary>PlayerOne's board as seen by the viewer.</summary>
        public PlayerSnapshot PlayerOne { get; init; }

        /// <summary>PlayerTwo's board as seen by the viewer.</summary>
        public PlayerSnapshot PlayerTwo { get; init; }

        /// <summary>The player whose turn it currently is.</summary>
        public PlayerId CurrentTurn { get; init; }

        /// <summary>True when the game has ended.</summary>
        public bool IsGameOver { get; init; }

        /// <summary>The winning player, or null if the game is still in progress.</summary>
        public PlayerId? Winner { get; init; }

        /// <summary>Creates a complete game snapshot.</summary>
        public GameSnapshot(
            PlayerSnapshot playerOne,
            PlayerSnapshot playerTwo,
            PlayerId currentTurn,
            bool isGameOver,
            PlayerId? winner)
        {
            PlayerOne   = playerOne;
            PlayerTwo   = playerTwo;
            CurrentTurn = currentTurn;
            IsGameOver  = isGameOver;
            Winner      = winner;
        }
    }
}
