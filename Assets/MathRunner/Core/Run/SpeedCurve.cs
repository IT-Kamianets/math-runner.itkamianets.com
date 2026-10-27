using System;

namespace MathRunner.Core.Run
{
    /// <summary>Forward speed that grows with distance travelled, up to a cap.</summary>
    public sealed class SpeedCurve
    {
        public float StartSpeed { get; }
        public float MaxSpeed { get; }
        /// <summary>Speed gained per 100 meters.</summary>
        public float GainPer100m { get; }

        public SpeedCurve(float startSpeed = 8f, float maxSpeed = 20f, float gainPer100m = 1.5f)
        {
            if (startSpeed <= 0f || maxSpeed < startSpeed) throw new ArgumentException("Invalid speeds");
            StartSpeed = startSpeed;
            MaxSpeed = maxSpeed;
            GainPer100m = gainPer100m;
        }

        public float SpeedAt(float distance)
        {
            var speed = StartSpeed + distance / 100f * GainPer100m;
            return speed > MaxSpeed ? MaxSpeed : speed;
        }
    }
}
