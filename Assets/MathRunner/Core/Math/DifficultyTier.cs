namespace MathRunner.Core.Math
{
    /// <summary>Rules for generating questions at one difficulty level.</summary>
    public sealed class DifficultyTier
    {
        public string Name { get; }
        public MathOperation Operations { get; }
        public int MinOperand { get; }
        public int MaxOperand { get; }
        /// <summary>Largest allowed factor for multiplication and the quotient for division.</summary>
        public int MaxFactor { get; }
        public bool AllowNegativeResults { get; }

        public DifficultyTier(string name, MathOperation operations, int minOperand, int maxOperand,
            int maxFactor, bool allowNegativeResults)
        {
            Name = name;
            Operations = operations;
            MinOperand = minOperand;
            MaxOperand = maxOperand;
            MaxFactor = maxFactor;
            AllowNegativeResults = allowNegativeResults;
        }

        public static readonly DifficultyTier Easy =
            new DifficultyTier("Easy", MathOperation.Add | MathOperation.Subtract, 1, 20, 1, false);

        public static readonly DifficultyTier Medium =
            new DifficultyTier("Medium", MathOperation.Add | MathOperation.Subtract, 5, 50, 1, true);

        public static readonly DifficultyTier Hard =
            new DifficultyTier("Hard",
                MathOperation.Add | MathOperation.Subtract | MathOperation.Multiply, 2, 50, 10, true);

        public static readonly DifficultyTier Expert =
            new DifficultyTier("Expert",
                MathOperation.Add | MathOperation.Subtract | MathOperation.Multiply | MathOperation.Divide,
                5, 99, 12, true);
    }
}
