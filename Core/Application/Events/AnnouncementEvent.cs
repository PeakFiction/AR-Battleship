using System;

namespace ARBattleship.Core.Application.Events
{
    public class AnnouncementEvent : IGameEvent
    {
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }

        public AnnouncementEvent(string message)
        {
            Message = message;
            Timestamp = DateTime.UtcNow;
        }
    }
}