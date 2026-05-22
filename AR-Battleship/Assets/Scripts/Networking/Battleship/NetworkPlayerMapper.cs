// Maps Unity Netcode client IDs to Battleship domain player identities.
// This keeps networking IDs separate from domain concepts such as PlayerOne
// and PlayerTwo.

using System.Collections.Generic;
using ARBattleship.Core.Domain;

namespace ARBattleship.Multiplayer.Battleship
{
    /// <summary>
    /// Assigns the first two connected clients to PlayerOne and PlayerTwo.
    /// </summary>
    public sealed class NetworkPlayerMapper
    {
        private readonly Dictionary<ulong, PlayerId> _clientToPlayer = new();

        public int PlayerCount => _clientToPlayer.Count;

        /// <summary>
        /// Registers a Netcode client ID and returns its Battleship player ID.
        /// Existing clients keep their previous assignment.
        /// </summary>
        public bool TryRegisterPlayer(ulong clientId, out PlayerId playerId)
        {
            if (_clientToPlayer.TryGetValue(clientId, out playerId))
            {
                return true;
            }

            if (_clientToPlayer.Count == 0)
            {
                playerId = PlayerId.PlayerOne;
                _clientToPlayer[clientId] = playerId;
                return true;
            }

            if (_clientToPlayer.Count == 1)
            {
                playerId = PlayerId.PlayerTwo;
                _clientToPlayer[clientId] = playerId;
                return true;
            }

            // A standard Battleship match only supports two players.
            playerId = default!;
            return false;
        }

        /// <summary>
        /// Gets the Battleship player ID assigned to a Netcode client ID.
        /// </summary>
        public bool TryGetPlayerId(ulong clientId, out PlayerId playerId)
        {
            return _clientToPlayer.TryGetValue(clientId, out playerId);
        }

        /// <summary>
        /// Converts a domain player ID into the 1-based player number used by UI events.
        /// </summary>
        public int ToPlayerNumber(PlayerId playerId)
        {
            return playerId == PlayerId.PlayerOne ? 1 : 2;
        }
    }
}
