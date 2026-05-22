using System;

namespace ARBattleship.Core.Domain
{
    /// <summary>
    /// Strongly-typed identifier for one of the two players.
    /// </summary>
    public record PlayerId
    {
        /// <summary>
        /// Serialisable string value.  "Player1" or "Player2".
        /// Used as a network payload and in log messages.
        /// </summary>
        public string Value { get; init; }

        /// <summary>
        /// Private constructor enforces that only the two canonical IDs exist.
        /// </summary>
        private PlayerId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("PlayerId cannot be empty");
            Value = value;
        }

        /// <summary>The local / human player who fires first (serialised as "Player1").</summary>
        public static PlayerId PlayerOne => new("Player1");

        /// <summary>The opponent, AI in singleplayer, remote client in multiplayer (serialised as "Player2").</summary>
        public static PlayerId PlayerTwo => new("Player2");

        /// <summary>Returns true when this player is not the same as other.</summary>
        public bool IsOpponentOf(PlayerId other) => Value != other.Value;

        /// <summary>Returns the underlying string value for logging and serialisation.</summary>
        public override string ToString() => Value;
    }
}
