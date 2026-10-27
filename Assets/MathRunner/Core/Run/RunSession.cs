using System;

namespace MathRunner.Core.Run
{
    public enum AnswerResult
    {
        Correct,
        Wrong,
    }

    /// <summary>Score, streak and lives for one run. No Unity types.</summary>
    public sealed class RunSession
    {
        public const int PointsPerCorrect = 10;
        public const int StreakBonusStep = 5;
        public const int CoinPoints = 1;

        public int MaxLives { get; }
        public int Lives { get; private set; }
        public int Score { get; private set; }
        public int Streak { get; private set; }
        public int BestStreak { get; private set; }
        public int CorrectCount { get; private set; }
        public int WrongCount { get; private set; }
        public int Coins { get; private set; }
        public float Distance { get; private set; }
        public bool IsOver => Lives <= 0;

        public event Action<AnswerResult> AnswerResolved;
        public event Action Died;

        public RunSession(int maxLives = 3)
        {
            if (maxLives < 1) throw new ArgumentOutOfRangeException(nameof(maxLives));
            MaxLives = maxLives;
            Lives = maxLives;
        }

        public void AddDistance(float meters)
        {
            if (IsOver || meters <= 0f) return;
            Distance += meters;
        }

        /// <summary>Scores the chosen gate. Returns the points gained (0 on a wrong answer).</summary>
        public int ResolveAnswer(bool correct)
        {
            if (IsOver) return 0;

            if (correct)
            {
                Streak++;
                if (Streak > BestStreak) BestStreak = Streak;
                CorrectCount++;
                // +1 point per full 5 streak, so a streak of 5 earns 11, 10 earns 12.
                var gained = PointsPerCorrect + Streak / StreakBonusStep;
                Score += gained;
                AnswerResolved?.Invoke(AnswerResult.Correct);
                return gained;
            }

            Streak = 0;
            WrongCount++;
            LoseLife();
            AnswerResolved?.Invoke(AnswerResult.Wrong);
            return 0;
        }

        public void HitObstacle()
        {
            if (IsOver) return;
            Streak = 0;
            LoseLife();
        }

        public void CollectCoin()
        {
            if (IsOver) return;
            Coins++;
            Score += CoinPoints;
        }

        public void GainLife()
        {
            if (IsOver || Lives >= MaxLives) return;
            Lives++;
        }

        private void LoseLife()
        {
            Lives--;
            if (Lives <= 0) Died?.Invoke();
        }
    }
}
