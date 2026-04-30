using System.Collections.Generic;
using ARBattleship.Core.Domain;

namespace ARBattleship.Multiplayer.Battleship
{
    public sealed class NetworkPlayerMapper
    {
        private readonly Dictionary<ulong, PlayerId> _clientToPlayer = new();

        public int PlayerCount => _clientToPlayer.Count;

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

            playerId = default!;
            return false;
        }

        public bool TryGetPlayerId(ulong clientId, out PlayerId playerId)
        {
            return _clientToPlayer.TryGetValue(clientId, out playerId);
        }

        public int ToPlayerNumber(PlayerId playerId)
        {
            return playerId == PlayerId.PlayerOne ? 1 : 2;
        }
    }
}