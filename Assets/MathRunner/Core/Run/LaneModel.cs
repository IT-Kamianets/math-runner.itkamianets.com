using System;

namespace MathRunner.Core.Run
{
    /// <summary>The runner's lane. Lane 0 is the leftmost.</summary>
    public sealed class LaneModel
    {
        public int LaneCount { get; }
        public int Current { get; private set; }

        public LaneModel(int laneCount = 3)
        {
            if (laneCount < 2) throw new ArgumentOutOfRangeException(nameof(laneCount));
            LaneCount = laneCount;
            Current = laneCount / 2;
        }

        /// <summary>Moves one lane left (-1) or right (+1). Returns false at the edge.</summary>
        public bool Shift(int direction)
        {
            var target = Current + System.Math.Sign(direction);
            if (target < 0 || target >= LaneCount || target == Current) return false;
            Current = target;
            return true;
        }

        public void Reset() => Current = LaneCount / 2;

        /// <summary>World X offset of a lane, centered on 0.</summary>
        public float LaneToX(int lane, float laneWidth)
        {
            return (lane - (LaneCount - 1) * 0.5f) * laneWidth;
        }
    }
}
