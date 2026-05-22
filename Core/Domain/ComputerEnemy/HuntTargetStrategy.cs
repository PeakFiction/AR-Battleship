// HUNT MODE:
// Fires at unshot cells on a checkerboard parity pattern ((X+Y) % 2 == 0).
// Falls back to any unshot cell when all parity cells are exhausted.
//
// TARGET MODE (activated after a hit):
// Enqueues neighbouring tiles to fire at
// After a second hit, LockAxis() removes tiles along the wrong axis
// Resets to HUNT mode when a Sunk result is received.
using System;
using System.Collections.Generic;
using System.Linq;

namespace ARBattleship.Core.Domain
{
    public class HuntTargetStrategy : IEnemyStrategy
    {
        // ─────────────────────────────────────────────────────────────────────
        // Private state
        // ─────────────────────────────────────────────────────────────────────

        private enum Mode { Hunt, Target }

        /// <summary>Current operating mode: Hunt (random parity) or Target (directed).</summary>
        private Mode _mode = Mode.Hunt;

        /// <summary>The first cell struck during the current targeting run.</summary>
        private Coordinate? _initialHit;

        /// <summary>All confirmed hit coordinates on the current unfinished ship.</summary>
        private readonly List<Coordinate> _currentHits = new();

        /// <summary>Ordered queue of candidate cells to fire at during Target mode.</summary>
        private readonly Queue<Coordinate> _targetQueue = new();

        /// <summary>Every coordinate fired at (hits and misses) across the full game.</summary>
        private readonly HashSet<Coordinate> _shotsFired = new();

        private readonly Random _random;
        private readonly int _boardSize;

        public HuntTargetStrategy(int boardSize = 10, Random? random = null)
        {
            _boardSize = boardSize;
            _random    = random ?? new Random();
        }

        public Coordinate SelectMove(Board opponentBoard)
        {
            // Sync internal state with board reality before choosing
            SyncShotsFired(opponentBoard);

            return _mode == Mode.Target
                ? SelectTargetMove(opponentBoard)
                : SelectHuntMove(opponentBoard);
        }

        /// <summary>
        /// Called by EnemyTurnService after each shot resolves.
        /// Updates the internal Hunt/Target state based on the outcome.
        /// </summary>
        public void NotifyResult(FireResult result)
        {
            _shotsFired.Add(result.Coordinate);

            if (result.Outcome == ShotResult.Miss)
                return; // Queue retains remaining candidates

            if (result.Outcome == ShotResult.Hit)
            {
                if (_mode == Mode.Hunt)
                {
                    // First hit — enter Target mode and queue adjacent cells
                    _mode       = Mode.Target;
                    _initialHit = result.Coordinate;
                    _currentHits.Clear();
                    EnqueueAdjacentCandidates(result.Coordinate);
                }

                _currentHits.Add(result.Coordinate);

                // Two confirmed hits → lock onto the ship's axis to prune off-axis candidates
                if (_currentHits.Count >= 2)
                    LockAxis();
            }

            if (result.Outcome == ShotResult.Sunk)
            {
                // Ship finished — reset all targeting state and return to Hunt
                _mode = Mode.Hunt;
                _initialHit = null;
                _currentHits.Clear();
                _targetQueue.Clear();
            }
        }

        /// <summary>
        /// Hunt mode: randomly selects an unshot cell from the checkerboard
        /// parity pattern.  Falls back to any unshot cell when all parity
        /// cells have been exhausted.
        /// </summary>
        private Coordinate SelectHuntMove(Board opponentBoard)
        {
            var candidates = GetUnshotCells(opponentBoard)
                .Where(c => (c.X + c.Y) % 2 == 0) // Checkerboard parity
                .ToList();

            if (candidates.Count == 0) // Fallback: all parity cells are spent
                candidates = GetUnshotCells(opponentBoard).ToList();

            if (candidates.Count == 0)
                throw new InvalidOperationException("No valid cells remaining to fire at.");

            return candidates[_random.Next(candidates.Count)];
        }

        /// <summary>
        /// Target mode: drains the target queue, skipping stale entries that
        /// are already shot or out of bounds.  Falls back to Hunt if the queue
        /// empties without finding a valid cell (edge case: all neighbours shot).
        /// </summary>
        private Coordinate SelectTargetMove(Board opponentBoard)
        {
            while (_targetQueue.Count > 0)
            {
                var candidate = _targetQueue.Peek();

                if (!IsInBounds(candidate) || _shotsFired.Contains(candidate))
                {
                    _targetQueue.Dequeue(); // Stale — skip
                    continue;
                }

                _targetQueue.Dequeue();
                return candidate;
            }

            // Target queue exhausted — lost track of the ship; fall back to Hunt
            _mode       = Mode.Hunt;
            _initialHit = null;
            _currentHits.Clear();
            return SelectHuntMove(opponentBoard);
        }

        /// <summary>
        /// After two confirmed hits, determines the ship's axis (horizontal or
        /// vertical) and removes off-axis candidates from the queue.  Also adds
        /// the far ends of the axis (beyond the outermost known hits) to avoid
        /// missing ship cells that extend past the current hit range.
        /// </summary>
        private void LockAxis()
        {
            if (_currentHits.Count < 2) return;

            var first        = _currentHits[0];
            var second       = _currentHits[1];
            bool isHorizontal = first.Y == second.Y;

            // Remove off-axis entries from queue
            var filtered = _targetQueue
                .Where(c => isHorizontal ? c.Y == first.Y : c.X == first.X)
                .ToList();

            _targetQueue.Clear();
            foreach (var c in filtered) _targetQueue.Enqueue(c);

            // Add the far ends of the hit axis
            var minHit = isHorizontal
                ? _currentHits.OrderBy(c => c.X).First()
                : _currentHits.OrderBy(c => c.Y).First();

            var maxHit = isHorizontal
                ? _currentHits.OrderBy(c => c.X).Last()
                : _currentHits.OrderBy(c => c.Y).Last();

            if (isHorizontal)
            {
                var left  = new Coordinate(minHit.X - 1, minHit.Y);
                var right = new Coordinate(maxHit.X + 1, maxHit.Y);
                if (IsInBounds(left))  _targetQueue.Enqueue(left);
                if (IsInBounds(right)) _targetQueue.Enqueue(right);
            }
            else
            {
                var above = new Coordinate(minHit.X, minHit.Y - 1);
                var below = new Coordinate(maxHit.X, maxHit.Y + 1);
                if (IsInBounds(above)) _targetQueue.Enqueue(above);
                if (IsInBounds(below)) _targetQueue.Enqueue(below);
            }
        }

        /// <summary>
        /// Enqueues the four neighbouring tiles
        /// that are within board bounds.
        /// </summary>
        private void EnqueueAdjacentCandidates(Coordinate coord)
        {
            var candidates = new[]
            {
                new Coordinate(coord.X - 1, coord.Y), // W
                new Coordinate(coord.X + 1, coord.Y), // E
                new Coordinate(coord.X, coord.Y - 1), // N
                new Coordinate(coord.X, coord.Y + 1), // S
            };

            foreach (var c in candidates.Where(IsInBounds))
                _targetQueue.Enqueue(c);
        }

        /// <summary>Yields every coordinate not yet present in _shotsFired.</summary>
        private IEnumerable<Coordinate> GetUnshotCells(Board board)
        {
            for (int x = 0; x < board.Size; x++)
                for (int y = 0; y < board.Size; y++)
                {
                    var coord = new Coordinate(x, y);
                    if (!_shotsFired.Contains(coord))
                        yield return coord;
                }
        }

        /// <summary>
        /// Synchronises _shotsFired with the actual board state.
        /// Necessary after game load or any external board modification.
        /// </summary>
        private void SyncShotsFired(Board board)
        {
            for (int x = 0; x < board.Size; x++)
                for (int y = 0; y < board.Size; y++)
                {
                    var coord = new Coordinate(x, y);
                    if (board.GetCell(coord).IsShot)
                        _shotsFired.Add(coord);
                }
        }

        /// <summary>True when the coordinate falls within the board boundary.</summary>
        private bool IsInBounds(Coordinate c) =>
            c.X >= 0 && c.X < _boardSize && c.Y >= 0 && c.Y < _boardSize;
    }
}
