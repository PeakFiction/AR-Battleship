using System;

namespace ARBattleship.Core.Domain
{
    public record PlayerId
    {
        public string Value { get; init; }

        // Private constructor ensures we control how IDs are made
        private PlayerId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("PlayerId cannot be empty");
            Value = value;
        }

        // Static factory methods for the two players
        public static PlayerId PlayerOne => new("Player1");
        public static PlayerId PlayerTwo => new("Player2");

        // Helper for quick logic checks
        public bool IsOpponentOf(PlayerId other) => Value != other.Value;

        // Override ToString for easier debugging in the Unity Console
        public override string ToString() => Value;
    }
}