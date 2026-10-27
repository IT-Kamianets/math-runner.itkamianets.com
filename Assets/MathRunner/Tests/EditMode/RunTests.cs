using System.Linq;
using MathRunner.Core.Run;
using MathRunner.Core.Track;
using NUnit.Framework;

namespace MathRunner.Tests
{
    public class RunSessionTests
    {
        [Test]
        public void CorrectAnswers_BuildStreak_AndScore()
        {
            var s = new RunSession();
            Assert.AreEqual(10, s.ResolveAnswer(true));
            Assert.AreEqual(1, s.Streak);
            Assert.AreEqual(10, s.Score);
        }

        [Test]
        public void StreakBonus_AppearsAtFive()
        {
            var s = new RunSession();
            for (var i = 0; i < 4; i++) s.ResolveAnswer(true);
            Assert.AreEqual(11, s.ResolveAnswer(true));
        }

        [Test]
        public void WrongAnswer_ResetsStreak_AndLosesLife()
        {
            var s = new RunSession(3);
            s.ResolveAnswer(true);
            s.ResolveAnswer(false);
            Assert.AreEqual(0, s.Streak);
            Assert.AreEqual(1, s.BestStreak);
            Assert.AreEqual(2, s.Lives);
        }

        [Test]
        public void LosingAllLives_EndsRun_AndRaisesDiedOnce()
        {
            var s = new RunSession(2);
            var died = 0;
            s.Died += () => died++;
            s.ResolveAnswer(false);
            s.HitObstacle();
            s.HitObstacle();
            Assert.IsTrue(s.IsOver);
            Assert.AreEqual(1, died);
        }

        [Test]
        public void AfterGameOver_NothingChanges()
        {
            var s = new RunSession(1);
            s.HitObstacle();
            s.ResolveAnswer(true);
            s.CollectCoin();
            Assert.AreEqual(0, s.Score);
            Assert.AreEqual(0, s.Coins);
        }

        [Test]
        public void GainLife_IsCappedAtMax()
        {
            var s = new RunSession(3);
            s.GainLife();
            Assert.AreEqual(3, s.Lives);
            s.HitObstacle();
            s.GainLife();
            Assert.AreEqual(3, s.Lives);
        }
    }

    public class LaneModelTests
    {
        [Test]
        public void StartsInMiddleLane()
        {
            Assert.AreEqual(1, new LaneModel(3).Current);
        }

        [Test]
        public void Shift_StopsAtEdges()
        {
            var lane = new LaneModel(3);
            Assert.IsTrue(lane.Shift(-1));
            Assert.IsFalse(lane.Shift(-1));
            Assert.AreEqual(0, lane.Current);
            lane.Shift(1);
            lane.Shift(1);
            Assert.IsFalse(lane.Shift(1));
            Assert.AreEqual(2, lane.Current);
        }

        [Test]
        public void LaneToX_IsCentered()
        {
            var lane = new LaneModel(3);
            Assert.AreEqual(-2f, lane.LaneToX(0, 2f), 0.001f);
            Assert.AreEqual(0f, lane.LaneToX(1, 2f), 0.001f);
            Assert.AreEqual(2f, lane.LaneToX(2, 2f), 0.001f);
        }
    }

    public class SpeedCurveTests
    {
        [Test]
        public void Speed_GrowsThenCaps()
        {
            var c = new SpeedCurve(8f, 12f, 2f);
            Assert.AreEqual(8f, c.SpeedAt(0f), 0.001f);
            Assert.AreEqual(10f, c.SpeedAt(100f), 0.001f);
            Assert.AreEqual(12f, c.SpeedAt(10000f), 0.001f);
        }
    }

    public class GameStateMachineTests
    {
        [Test]
        public void NormalFlow_IsAllowed()
        {
            var m = new GameStateMachine();
            Assert.IsTrue(m.TryMoveTo(GameState.Countdown));
            Assert.IsTrue(m.TryMoveTo(GameState.Running));
            Assert.IsTrue(m.TryMoveTo(GameState.Paused));
            Assert.IsTrue(m.TryMoveTo(GameState.Running));
            Assert.IsTrue(m.TryMoveTo(GameState.GameOver));
            Assert.IsTrue(m.TryMoveTo(GameState.Countdown));
        }

        [Test]
        public void InvalidJump_IsRejected()
        {
            var m = new GameStateMachine();
            Assert.IsFalse(m.TryMoveTo(GameState.Running));
            Assert.AreEqual(GameState.Menu, m.State);
        }
    }

    public class TrackPlannerTests
    {
        [Test]
        public void EverySection_HasOneGatePerLane()
        {
            var planner = new TrackPlanner(1, 3);
            for (var i = 0; i < 20; i++)
            {
                var gates = planner.PlanNextSection().Where(x => x.Kind == SpawnKind.Gate).ToList();
                Assert.AreEqual(3, gates.Count);
                CollectionAssert.AreEquivalent(new[] { 0, 1, 2 }, gates.Select(g => g.Lane).ToArray());
                Assert.AreEqual(1, gates.Select(g => g.Z).Distinct().Count());
            }
        }

        [Test]
        public void Items_AreOrdered_AndGatesAdvance()
        {
            var planner = new TrackPlanner(2, 3, 40f, 45f);
            var lastGateZ = 0f;
            for (var i = 0; i < 10; i++)
            {
                var section = planner.PlanNextSection();
                var zs = section.Select(x => x.Z).ToList();
                CollectionAssert.AreEqual(zs.OrderBy(z => z).ToList(), zs);
                var gateZ = section.First(x => x.Kind == SpawnKind.Gate).Z;
                Assert.Greater(gateZ, lastGateZ);
                lastGateZ = gateZ;
            }
        }

        [Test]
        public void Obstacles_NeverOverlapGateRow()
        {
            var planner = new TrackPlanner(3);
            for (var i = 0; i < 50; i++)
            {
                var section = planner.PlanNextSection();
                var gateZ = section.First(x => x.Kind == SpawnKind.Gate).Z;
                foreach (var o in section.Where(x => x.Kind == SpawnKind.Obstacle))
                    Assert.Less(o.Z, gateZ - 8f);
            }
        }

        [Test]
        public void SameSeed_IsDeterministic()
        {
            var a = new TrackPlanner(7).PlanNextSection();
            var b = new TrackPlanner(7).PlanNextSection();
            Assert.AreEqual(a.Count, b.Count);
            for (var i = 0; i < a.Count; i++)
            {
                Assert.AreEqual(a[i].Kind, b[i].Kind);
                Assert.AreEqual(a[i].Z, b[i].Z, 0.0001f);
            }
        }
    }
}
