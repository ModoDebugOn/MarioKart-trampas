using System.Numerics;
using KartRace.Domain.Track;
using NUnit.Framework;

namespace KartRace.Tests
{
    public class TrackLineTests
    {
        // A 10 x 10 square, anticlockwise seen from above: 40 m per lap.
        static TrackLine Square() => new TrackLine(new[]
        {
            new Vector2(0f, 0f), new Vector2(10f, 0f), new Vector2(10f, 10f), new Vector2(0f, 10f),
        });

        [Test]
        public void Length_IsTheWholeLap()
        {
            Assert.AreEqual(40f, Square().Length, 1e-4f);
        }

        [Test]
        public void PointAt_WalksAlongTheLine()
        {
            Vector2 p = Square().PointAt(15f);
            Assert.AreEqual(10f, p.X, 1e-4f);
            Assert.AreEqual(5f, p.Y, 1e-4f);
        }

        [Test]
        public void PointAt_WrapsAroundEveryLap()
        {
            var track = Square();
            Assert.AreEqual(track.PointAt(5f), track.PointAt(45f));
            Assert.AreEqual(track.PointAt(35f), track.PointAt(-5f));
        }

        [Test]
        public void DistanceAlong_ProjectsOffTheLineOntoIt()
        {
            // 1 m outside the first side, halfway along it.
            Assert.AreEqual(5f, Square().DistanceAlong(new Vector2(5f, -1f)), 1e-4f);
        }

        [Test]
        public void DistanceAlong_IsTheInverseOfPointAt()
        {
            var track = Square();
            foreach (float d in new[] { 0f, 7.5f, 21f, 39f })
                Assert.AreEqual(d, track.DistanceAlong(track.PointAt(d)), 1e-3f);
        }

        [Test]
        public void RightAt_PointsToTheRightOfTravel()
        {
            // Going +X along the first side, the right is -Z (seen from above, Z forward).
            Vector2 right = Square().RightAt(5f);
            Assert.AreEqual(0f, right.X, 1e-4f);
            Assert.AreEqual(-1f, right.Y, 1e-4f);
        }

        [Test]
        public void Smooth_GoesThroughTheControlPointsAndStartsAtTheFirst()
        {
            var controls = new[] { new Vector2(0f, 0f), new Vector2(50f, 0f), new Vector2(50f, 50f), new Vector2(0f, 50f) };
            var track = TrackLine.Smooth(controls, 1f);
            Assert.AreEqual(controls[0], track[0]);
            foreach (Vector2 c in controls)
                Assert.Less(Vector2.Distance(c, track.PointAt(track.DistanceAlong(c))), 1e-3f);
        }
    }
}
