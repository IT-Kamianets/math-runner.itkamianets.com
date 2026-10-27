using System;
using System.Collections.Generic;

namespace MathRunner.Core.Math
{
    /// <summary>Builds questions with one correct and several distinct wrong answers.</summary>
    public sealed class QuestionGenerator
    {
        private readonly Random _random;

        public QuestionGenerator(int seed) : this(new Random(seed)) { }

        public QuestionGenerator(Random random)
        {
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public MathQuestion Next(DifficultyTier tier, int laneCount = 3)
        {
            if (tier == null) throw new ArgumentNullException(nameof(tier));
            if (laneCount < 2) throw new ArgumentOutOfRangeException(nameof(laneCount));

            var op = PickOperation(tier.Operations);
            BuildQuestion(tier, op, out var text, out var result);

            var answers = BuildAnswers(result, laneCount, tier);
            var correctIndex = _random.Next(laneCount);
            // Swap the correct answer (placed first) into its random lane.
            (answers[0], answers[correctIndex]) = (answers[correctIndex], answers[0]);
            return new MathQuestion(text, answers, correctIndex);
        }

        private MathOperation PickOperation(MathOperation allowed)
        {
            var options = new List<MathOperation>(4);
            foreach (var candidate in new[]
                     { MathOperation.Add, MathOperation.Subtract, MathOperation.Multiply, MathOperation.Divide })
            {
                if ((allowed & candidate) != 0) options.Add(candidate);
            }

            if (options.Count == 0) throw new ArgumentException("Tier has no operations");
            return options[_random.Next(options.Count)];
        }

        private void BuildQuestion(DifficultyTier tier, MathOperation op, out string text, out int result)
        {
            switch (op)
            {
                case MathOperation.Add:
                {
                    var a = Operand(tier);
                    var b = Operand(tier);
                    text = a + " + " + b;
                    result = a + b;
                    break;
                }
                case MathOperation.Subtract:
                {
                    var a = Operand(tier);
                    var b = Operand(tier);
                    if (!tier.AllowNegativeResults && b > a) (a, b) = (b, a);
                    text = a + " - " + b;
                    result = a - b;
                    break;
                }
                case MathOperation.Multiply:
                {
                    var a = _random.Next(2, tier.MaxFactor + 1);
                    var b = _random.Next(2, tier.MaxFactor + 1);
                    text = a + " x " + b;
                    result = a * b;
                    break;
                }
                case MathOperation.Divide:
                {
                    var divisor = _random.Next(2, tier.MaxFactor + 1);
                    var quotient = _random.Next(2, tier.MaxFactor + 1);
                    text = divisor * quotient + " / " + divisor;
                    result = quotient;
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(op));
            }
        }

        private int Operand(DifficultyTier tier) => _random.Next(tier.MinOperand, tier.MaxOperand + 1);

        /// <summary>Index 0 is the correct answer. All values are distinct.</summary>
        private int[] BuildAnswers(int correct, int count, DifficultyTier tier)
        {
            var answers = new int[count];
            var used = new HashSet<int> { correct };
            answers[0] = correct;

            // Wrong answers stay close to the right one, so they look plausible.
            var spread = System.Math.Max(3, System.Math.Abs(correct) / 4 + 2);
            for (var i = 1; i < count; i++)
            {
                int candidate;
                var attempts = 0;
                do
                {
                    var offset = _random.Next(1, spread + 1) * (_random.Next(2) == 0 ? -1 : 1);
                    candidate = correct + offset;
                    attempts++;
                    // Widen the search if the area around the answer is crowded.
                    if (attempts > 20) spread++;
                } while (used.Contains(candidate) || (!tier.AllowNegativeResults && candidate < 0));

                used.Add(candidate);
                answers[i] = candidate;
            }

            return answers;
        }
    }
}
