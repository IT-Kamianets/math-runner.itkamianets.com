using System.Linq;
using MathRunner.Core.Math;
using NUnit.Framework;

namespace MathRunner.Tests
{
    public class QuestionGeneratorTests
    {
        private static readonly DifficultyTier[] Tiers =
        {
            DifficultyTier.Easy, DifficultyTier.Medium, DifficultyTier.Hard, DifficultyTier.Expert,
        };

        [Test]
        public void Answers_AreDistinct_AndCorrectIsInRange()
        {
            var generator = new QuestionGenerator(1);
            foreach (var tier in Tiers)
            {
                for (var i = 0; i < 500; i++)
                {
                    var q = generator.Next(tier);
                    Assert.AreEqual(3, q.Answers.Length);
                    Assert.AreEqual(3, q.Answers.Distinct().Count(), q.Text);
                    Assert.That(q.CorrectIndex, Is.InRange(0, 2));
                }
            }
        }

        [Test]
        public void CorrectAnswer_MatchesQuestionText()
        {
            var generator = new QuestionGenerator(2);
            foreach (var tier in Tiers)
            {
                for (var i = 0; i < 500; i++)
                {
                    var q = generator.Next(tier);
                    Assert.AreEqual(Evaluate(q.Text), q.CorrectAnswer, q.Text);
                }
            }
        }

        [Test]
        public void Easy_NeverProducesNegativeAnswers()
        {
            var generator = new QuestionGenerator(3);
            for (var i = 0; i < 1000; i++)
            {
                var q = generator.Next(DifficultyTier.Easy);
                Assert.That(q.Answers, Has.All.GreaterThanOrEqualTo(0), q.Text);
            }
        }

        [Test]
        public void Division_HasWholeResult()
        {
            var generator = new QuestionGenerator(4);
            for (var i = 0; i < 1000; i++)
            {
                var q = generator.Next(DifficultyTier.Expert);
                if (!q.Text.Contains("/")) continue;
                var parts = q.Text.Split('/');
                Assert.AreEqual(0, int.Parse(parts[0]) % int.Parse(parts[1]), q.Text);
            }
        }

        [Test]
        public void SameSeed_GivesSameQuestions()
        {
            var a = new QuestionGenerator(42);
            var b = new QuestionGenerator(42);
            for (var i = 0; i < 50; i++)
            {
                var qa = a.Next(DifficultyTier.Hard);
                var qb = b.Next(DifficultyTier.Hard);
                Assert.AreEqual(qa.Text, qb.Text);
                Assert.AreEqual(qa.CorrectIndex, qb.CorrectIndex);
            }
        }

        [Test]
        public void CorrectLane_IsSpreadAcrossAllLanes()
        {
            var generator = new QuestionGenerator(5);
            var seen = new bool[3];
            for (var i = 0; i < 200; i++) seen[generator.Next(DifficultyTier.Easy).CorrectIndex] = true;
            Assert.That(seen, Has.All.True);
        }

        private static int Evaluate(string text)
        {
            var p = text.Split(' ');
            var a = int.Parse(p[0]);
            var b = int.Parse(p[2]);
            switch (p[1])
            {
                case "+": return a + b;
                case "-": return a - b;
                case "x": return a * b;
                case "/": return a / b;
                default: throw new System.InvalidOperationException(text);
            }
        }
    }
}
