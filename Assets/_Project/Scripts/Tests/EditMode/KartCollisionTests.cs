using System.Numerics;
using KartRace.Domain.Driving;
using NUnit.Framework;

namespace KartRace.Tests
{
    public class KartCollisionTests
    {
        const float Radius = 1.1f;

        // Facing +Z at (x, z), going at that speed.
        static KartMotion Kart(float x, float z, float speed)
        {
            var kart = new KartMotion(KartStats.Standard, new Vector2(x, z), 0f);
            kart.Bump(Vector2.Zero, speed);
            return kart;
        }

        [Test]
        public void Apart_NothingHappens()
        {
            KartMotion a = Kart(0f, 0f, 20f), b = Kart(0f, 5f, 10f);
            Assert.AreEqual(0, KartCollisions.Resolve(new[] { a, b }, Radius));
            Assert.AreEqual(20f, a.Speed);
            Assert.AreEqual(10f, b.Speed);
        }

        [Test]
        public void Overlapping_ArePushedApart()
        {
            KartMotion a = Kart(0f, 0f, 0f), b = Kart(0f, 1f, 0f);
            KartCollisions.Resolve(new[] { a, b }, Radius);
            Assert.AreEqual(2f * Radius, Vector2.Distance(a.Position, b.Position), 1e-4f);
        }

        [Test]
        public void RearEnd_TheOneBehindSlows_TheOneInFrontSpeedsUp()
        {
            KartMotion behind = Kart(0f, 0f, 20f), front = Kart(0f, 2f, 10f);
            KartCollisions.Resolve(new[] { behind, front }, Radius, bounce: 0f);
            // No bounce, same weight: they leave at the average speed.
            Assert.AreEqual(15f, behind.Speed, 1e-3f);
            Assert.AreEqual(15f, front.Speed, 1e-3f);
        }

        [Test]
        public void SideBySide_PushesSideways_WithoutChangingSpeed()
        {
            KartMotion left = Kart(0f, 0f, 20f), right = Kart(2f, 0f, 20f);
            KartCollisions.Resolve(new[] { left, right }, Radius);
            Assert.AreEqual(20f, left.Speed, 1e-3f);
            Assert.AreEqual(20f, right.Speed, 1e-3f);
            Assert.Less(left.Position.X, 0f);
            Assert.Greater(right.Position.X, 2f);
        }
    }
}
