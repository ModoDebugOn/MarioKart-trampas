using KartRace.Domain.Race;
using NUnit.Framework;

namespace KartRace.Tests
{
    public class RaceProgressTests
    {
        // A 40 m lap, like the square of TrackLineTests.
        const float Lap = 40f;

        [Test]
        public void OnTheGrid_IsBehindTheStartLine()
        {
            var progress = new RaceProgress(Lap, -3f);
            Assert.AreEqual(-3f, progress.Total, 1e-4f);
            Assert.AreEqual(0, progress.Lap);
        }

        [Test]
        public void OnGrid_BehindTheLineIsNegative()
        {
            Assert.AreEqual(-3f, RaceProgress.OnGrid(Lap, 37f).Total, 1e-4f);
            Assert.AreEqual(2f, RaceProgress.OnGrid(Lap, 2f).Total, 1e-4f);
        }

        [Test]
        public void CrossingTheStartLine_StartsLapOne()
        {
            var progress = new RaceProgress(Lap, -3f);   // the track line says 37 m
            progress.Update(39f);
            progress.Update(1f);
            Assert.AreEqual(1f, progress.Total, 1e-4f);
            Assert.AreEqual(1, progress.Lap);
            Assert.AreEqual(1f, progress.DistanceInLap, 1e-4f);
        }

        [Test]
        public void EveryLap_AddsUp()
        {
            var progress = new RaceProgress(Lap, 0f);
            for (int lap = 0; lap < 3; lap++)
                foreach (float along in new[] { 10f, 20f, 30f, 0f })
                    progress.Update(along);
            Assert.AreEqual(120f, progress.Total, 1e-3f);
            Assert.AreEqual(4, progress.Lap);
        }

        [Test]
        public void ReversingOverTheLine_TakesTheLapBack()
        {
            var progress = new RaceProgress(Lap, 0f);
            progress.Update(2f);
            progress.Update(38f);   // 4 m backwards, through the line
            Assert.AreEqual(-2f, progress.Total, 1e-4f);
            Assert.AreEqual(0, progress.Lap);
        }
    }

    public class StandingsTests
    {
        static Racer At(Character character, float total) => new Racer(character, new RaceProgress(40f, total));

        [Test]
        public void Order_IsFurthestFirst()
        {
            Racer mario = At(Character.Mario, 10f), luigi = At(Character.Luigi, 50f), peach = At(Character.Peach, 30f);
            var standings = new Standings(new[] { mario, luigi, peach });
            CollectionAssert.AreEqual(new[] { luigi, peach, mario }, standings.Order);
            Assert.AreEqual(luigi, standings.Leader);
            Assert.AreEqual(3, standings.PositionOf(mario));
        }

        [Test]
        public void ALapDown_IsBehindEvenIfFurtherInTheLap()
        {
            Racer leader = At(Character.Bowser, 45f);   // lap 2, 5 m in
            Racer lapped = At(Character.Toad, 35f);     // lap 1, 35 m in
            var standings = new Standings(new[] { lapped, leader });
            Assert.AreEqual(leader, standings.Leader);
            Assert.AreEqual(10f, standings.DistanceToLeader(lapped), 1e-4f);
            Assert.AreEqual(0f, standings.DistanceToLeader(leader), 1e-4f);
        }

        [Test]
        public void Update_FollowsOvertakes()
        {
            Racer mario = At(Character.Mario, 0f), luigi = At(Character.Luigi, 0f);
            var standings = new Standings(new[] { mario, luigi });
            luigi.Progress.Update(5f);
            standings.Update();
            Assert.AreEqual(luigi, standings.Leader);
        }

        [Test]
        public void OnATie_NobodySwaps()
        {
            Racer mario = At(Character.Mario, 20f), luigi = At(Character.Luigi, 20f);
            var standings = new Standings(new[] { mario, luigi });
            standings.Update();
            CollectionAssert.AreEqual(new[] { mario, luigi }, standings.Order);
        }

        [Test]
        public void GapTo_IsPositiveWhenTheOtherIsAhead()
        {
            Racer mario = At(Character.Mario, 10f), luigi = At(Character.Luigi, 25f);
            Assert.AreEqual(15f, mario.GapTo(luigi), 1e-4f);
            Assert.AreEqual(-15f, luigi.GapTo(mario), 1e-4f);
        }
    }
}
