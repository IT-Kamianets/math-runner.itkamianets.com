using System;
using System.Collections.Generic;

namespace MathRunner.Core.Track
{
    public enum ObstacleKind
    {
        Block,    // switch lane
        Low,      // jump over
        Overhead, // slide under
    }

    public enum SpawnKind
    {
        Gate,
        Obstacle,
        Coins,
    }

    /// <summary>One thing to place on the track. Z is distance from the start in meters.</summary>
    public readonly struct SpawnItem
    {
        public SpawnKind Kind { get; }
        public float Z { get; }
        public int Lane { get; }
        public ObstacleKind Obstacle { get; }
        /// <summary>For coins: how many in the line.</summary>
        public int Count { get; }

        public SpawnItem(SpawnKind kind, float z, int lane, ObstacleKind obstacle = ObstacleKind.Block,
            int count = 0)
        {
            Kind = kind;
            Z = z;
            Lane = lane;
            Obstacle = obstacle;
            Count = count;
        }
    }

    /// <summary>
    /// Decides what appears where, ahead of the runner. Deterministic for a seed.
    /// Every gate row spans all lanes, so lane choice is the answer.
    /// </summary>
    public sealed class TrackPlanner
    {
        private readonly Random _random;
        private readonly int _laneCount;
        private readonly float _gateSpacing;
        private float _nextGateZ;

        public TrackPlanner(int seed, int laneCount = 3, float firstGateZ = 40f, float gateSpacing = 45f)
        {
            if (laneCount < 2) throw new ArgumentOutOfRangeException(nameof(laneCount));
            if (gateSpacing < 20f) throw new ArgumentOutOfRangeException(nameof(gateSpacing));
            _random = new Random(seed);
            _laneCount = laneCount;
            _gateSpacing = gateSpacing;
            _nextGateZ = firstGateZ;
        }

        public float NextGateZ => _nextGateZ;

        /// <summary>Items for the stretch ending at the next gate, then the gate row itself.</summary>
        public List<SpawnItem> PlanNextSection()
        {
            var items = new List<SpawnItem>();
            var gateZ = _nextGateZ;
            var sectionStart = gateZ - _gateSpacing;

            // Obstacles sit in the middle of the stretch, never on top of the gate.
            var obstacleCount = _random.Next(1, 3);
            for (var i = 0; i < obstacleCount; i++)
            {
                var z = sectionStart + _gateSpacing * (0.25f + 0.3f * i) + (float)_random.NextDouble() * 4f;
                var kind = (ObstacleKind)_random.Next(3);
                // Overhead and low obstacles block the full width, since they are handled by slide or jump.
                var lane = kind == ObstacleKind.Block ? _random.Next(_laneCount) : _laneCount / 2;
                items.Add(new SpawnItem(SpawnKind.Obstacle, z, lane, kind));
            }

            // One coin line in a random lane, near the start of the stretch.
            items.Add(new SpawnItem(SpawnKind.Coins, sectionStart + 6f, _random.Next(_laneCount),
                count: 5 + _random.Next(4)));

            // The gate row: one board per lane.
            for (var lane = 0; lane < _laneCount; lane++)
                items.Add(new SpawnItem(SpawnKind.Gate, gateZ, lane));

            items.Sort((a, b) => a.Z.CompareTo(b.Z));
            _nextGateZ += _gateSpacing;
            return items;
        }
    }
}
