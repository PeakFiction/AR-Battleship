using System;
using System.Collections.Generic;
using System.Linq;

namespace ARBattleship.Core.Domain
{
    /// <summary>
    /// Medium difficulty enemy strategy using a hunt/target approach:
    ///
    ///   HUNT mode:  Fires at unshot cells on a checkerboard pattern (parity optimisation).
    ///               Since the smallest ship is size 2, a checkerboard guarantees every ship
    ///               will be hit eventually without wasting shots on every cell.
    ///
    ///   TARGET mode: Once a hit is scored, works through adjacent (N/S/E/W) cells to finish
    ///               the ship. Tracks the axis of confirmed hits to avoid backtracking once a
    ///               direction is established.
    ///               Returns to HUNT mode after a ship is confirmed sunk.
    /// </summary>
    public class HuntTargetStrategy : IEnemyStrategy
    {
        private enum Mode { Hunt, Target }

        private Mode _mode = Mode.Hunt;

        // The first cell we hit that started the current targeting run
        private Coordinate? _initialHit;

        // All hits on the current ship during this targeting run
        private readonly List<Coordinate> _currentHits = new();

        // Queued candidates to try during TARGET mode
        private readonly Queue<Coordinate> _targetQueue = new();

        // All coordinates we have already fired at (hits and misses)
        private readonly HashSet<Coordinate> _shotsFired = new();

        private readonly Random _random;

        public HuntTargetStrategy(Random? random = null)
        {
            _random = random ?? new Random();
        }

        public Coordinate SelectMove(Board opponentBoard)
        {
            // Sync _shotsFired with the actual board state. This ensures correctness
            // even if SelectMove is called after external state changes (e.g. save/load).
            SyncShotsFired(opponentBoard);

            return _mode == Mode.Target
                ? SelectTargetMove(opponentBoard)
                : SelectHuntMove(opponentBoard);
        }

        /// <summary>
        /// Called by the application layer after a shot resolves so the strategy can
        /// update its internal state based on the outcome.
        /// </summary>
        public void NotifyResult(FireResult result)
        {
            _shotsFired.Add(result.Coordinate);

            if (result.Outcome == ShotResult.Miss)
            {
                // If we were targeting and missed, the queue will try the next candidate
                return;
            }

            if (result.Outcome == ShotResult.Hit)
            {
                if (_mode == Mode.Hunt)
                {
                    // Transition to target mode
                    _mode = Mode.Target;
                    _initialHit = result.Coordinate;
                    _currentHits.Clear();
                    EnqueueAdjacentCandidates(result.Coordinate);
                }

                _currentHits.Add(result.Coordinate);

                // If we have 2+ hits we can now lock onto an axis and prune the queue
                if (_currentHits.Count == 2)
                {
                    LockAxis();
                }
            }

            if (result.Outcome == ShotResult.Sunk)
            {
                // Ship is finished — reset targeting state and return to hunt
                _mode = Mode.Hunt;
                _initialHit = null;
                _currentHits.Clear();
                _targetQueue.Clear();
            }
        }

        // ── Private helpers ────────────────────────────────────────────────────────

        private Coordinate SelectHuntMove(Board opponentBoard)
        {
            // Build a list of unshot cells that match our parity (checkerboard pattern)
            var candidates = GetUnshotCells(opponentBoard)
                .Where(c => (c.X + c.Y) % 2 == 0)
                .ToList();

            // Fallback: if parity cells are exhausted, try all unshot cells
            if (candidates.Count == 0)
                candidates = GetUnshotCells(opponentBoard).ToList();

            return candidates[_random.Next(candidates.Count)];
        }

        private Coordinate SelectTargetMove(Board opponentBoard)
        {
            // Drain stale entries from the queue (already shot or out of bounds)
            while (_targetQueue.Count > 0)
            {
                var candidate = _targetQueue.Peek();

                if (_shotsFired.Contains(candidate))
                {
                    _targetQueue.Dequeue();
                    continue;
                }

                // Valid unshot candidate
                _targetQueue.Dequeue();
                return candidate;
            }

            // Target queue exhausted (can happen after misses on both ends of an axis).
            // This means we hit a ship but lost track — fall back to hunt.
            _mode = Mode.Hunt;
            _initialHit = null;
            _currentHits.Clear();
            return SelectHuntMove(opponentBoard);
        }

        /// <summary>
        /// Once two hits are confirmed on the same ship, determine whether the ship is
        /// horizontal or vertical and discard queue entries that are off-axis.
        /// Also enqueues the far end of the axis beyond the second hit.
        /// </summary>
        private void LockAxis()
        {
            if (_currentHits.Count < 2) return;

            var first = _currentHits[0];
            var second = _currentHits[1];

            bool isHorizontal = first.Y == second.Y;

            // Remove off-axis candidates from the queue
            var filtered = _targetQueue
                .Where(c => isHorizontal ? c.Y == first.Y : c.X == first.X)
                .ToList();

            _targetQueue.Clear();

            foreach (var c in filtered)
                _targetQueue.Enqueue(c);

            // Also enqueue the far end beyond the latest hit
            var minHit = isHorizontal
                ? _currentHits.OrderBy(c => c.X).First()
                : _currentHits.OrderBy(c => c.Y).First();

            var maxHit = isHorizontal
                ? _currentHits.OrderBy(c => c.X).Last()
                : _currentHits.OrderBy(c => c.Y).Last();

            if (isHorizontal)
            {
                _targetQueue.Enqueue(new Coordinate(minHit.X - 1, minHit.Y));
                _targetQueue.Enqueue(new Coordinate(maxHit.X + 1, maxHit.Y));
            }
            else
            {
                _targetQueue.Enqueue(new Coordinate(minHit.X, minHit.Y - 1));
                _targetQueue.Enqueue(new Coordinate(maxHit.X, maxHit.Y + 1));
            }
        }

        private void EnqueueAdjacentCandidates(Coordinate coord)
        {
            _targetQueue.Enqueue(new Coordinate(coord.X - 1, coord.Y));
            _targetQueue.Enqueue(new Coordinate(coord.X + 1, coord.Y));
            _targetQueue.Enqueue(new Coordinate(coord.X, coord.Y - 1));
            _targetQueue.Enqueue(new Coordinate(coord.X, coord.Y + 1));
        }

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

        private void SyncShotsFired(Board board)
        {
            for (int x = 0; x < board.Size; x++)
                for (int y = 0; y < board.Size; y++)
                {
                    var coord = new Coordinate(x, y);
                    var cell = board.GetCell(coord);
                    if (cell.IsShot)
                        _shotsFired.Add(coord);
                }
        }
    }
}