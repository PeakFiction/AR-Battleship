// Shared network phase enum for code that needs to describe the multiplayer
// lifecycle without depending on UI-specific state.

namespace ARBattleship.Networking.Battleship
{
    /// <summary>
    /// Represents the coarse game phase used by multiplayer networking code.
    /// </summary>
    public enum NetworkGamePhase
    {
        WaitingForPlayer = 0,
        PlacingShips = 1,
        Battle = 2,
        GameOver = 3
    }
}
