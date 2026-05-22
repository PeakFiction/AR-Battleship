// Human-readable announcement suitable for display in the battle log.
// Currently raised when a ship is confirmed sunk.
using System;

namespace ARBattleship.Core.Application.Events
{
    /// <summary>
    /// A human-readable message for the battle log UI.
    /// Raised by BattleshipGameService when a noteworthy game event occurs
    /// (e.g. "The Carrier has been sunk!").
    /// </summary>
    public class AnnouncementEvent : IGameEvent
    {
        /// <summary>The text to display in the battle log.</summary>
        public string Message { get; set; }

        /// <summary>UTC timestamp of the event, for log ordering.</summary>
        public DateTime Timestamp { get; set; }

        /// <summary>Creates an announcement with the given message and the current UTC time.</summary>
        public AnnouncementEvent(string message)
        {
            Message   = message;
            Timestamp = DateTime.UtcNow;
        }
    }
}
