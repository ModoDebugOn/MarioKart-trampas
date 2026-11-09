using System.Numerics;
using KartRace.Domain.Driving;
using NUnit.Framework;

namespace KartRace.Tests
{
    public class KartMotionTests
    {
        static KartMotion Kart() => new KartMotion(KartStats.Standard, Vector2.Zero, 0f);

        static void Drive(KartMotion kart, float throttle, float steer, float seconds)
        {
            for (float t = 0f; t < seconds; t += 0.01f) kart.Step(throttle, steer, 0.01f);
        }

        [Test]
        public void Throttle_ReachesTopSpeedAndStaysThere()
        {
            var kart = Kart();
            Drive(kart, 1f, 0f, 5f);
            Assert.AreEqual(KartStats.Standard.TopSpeed, kart.Speed, 1e-3f);
        }

        [Test]
        public void HeadingZero_GoesAlongZ()
        {
            var kart = Kart();
            Drive(kart, 1f, 0f, 1f);
            Assert.AreEqual(0f, kart.Position.X, 1e-3f);
            Assert.Greater(kart.Position.Y, 0f);
        }

        [Test]
        public void SteeringRight_TurnsTowardsX()
        {
            var kart = Kart();
            Drive(kart, 1f, 1f, 1f);
            Assert.Greater(kart.Heading, 0f);
            Assert.Greater(kart.Position.X, 0f);
        }

        [Test]
        public void Stopped_DoesNotTurnOnTheSpot()
        {
            var kart = Kart();
            Drive(kart, 0f, 1f, 1f);
            Assert.AreEqual(0f, kart.Heading, 1e-4f);
        }

        [Test]
        public void Releasing_RollsToAStop()
        {
            var kart = Kart();
            Drive(kart, 1f, 0f, 2f);
            Drive(kart, 0f, 0f, 10f);
            Assert.AreEqual(0f, kart.Speed, 1e-4f);
        }

        [Test]
        public void Brake_StopsFirstAndThenReverses()
        {
            var kart = Kart();
            Drive(kart, 1f, 0f, 2f);
            Drive(kart, -1f, 0f, 0.5f);   // 30 m/s² brakes ~24 m/s in under a second
            Assert.LessOrEqual(kart.Speed, 10f);
            Drive(kart, -1f, 0f, 3f);
            Assert.AreEqual(-KartStats.Standard.ReverseSpeed, kart.Speed, 1e-3f);
        }
    }
}
