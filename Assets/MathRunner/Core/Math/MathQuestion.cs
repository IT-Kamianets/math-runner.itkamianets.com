using System;

namespace MathRunner.Core.Math
{
    /// <summary>One question with one answer per lane. Immutable.</summary>
    public sealed class MathQuestion
    {
        public string Text { get; }
        public int[] Answers { get; }
        public int CorrectIndex { get; }

        public int CorrectAnswer => Answers[CorrectIndex];

        public MathQuestion(string text, int[] answers, int correctIndex)
        {
            if (answers == null || answers.Length == 0)
                throw new ArgumentException("Answers required", nameof(answers));
            if (correctIndex < 0 || correctIndex >= answers.Length)
                throw new ArgumentOutOfRangeException(nameof(correctIndex));

            Text = text;
            Answers = (int[])answers.Clone();
            CorrectIndex = correctIndex;
        }

        public bool IsCorrect(int index) => index == CorrectIndex;
    }
}
