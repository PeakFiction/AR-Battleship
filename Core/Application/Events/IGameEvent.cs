// Marker interface for all domain events produced by BattleshipGameService.
// Consumers call ConsumeEvents() and pattern-match on the concrete type.

namespace ARBattleship.Core.Application.Events
{
    /// <summary>
    /// Marker interface for all game events produced by the application layer.
    /// Switch on the concrete event type using a C# pattern-matching switch.
    /// </summary>
    public interface IGameEvent { }
}
