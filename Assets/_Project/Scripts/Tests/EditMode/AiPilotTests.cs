using System;
using System.Numerics;
using KartRace.Domain.Driving;
using KartRace.Domain.Track;
using NUnit.Framework;

namespace KartRace.Tests
{
    public class AiPilotTests
    {
        const float Frame = 1f / 60f;

        // A circle of 60 m radius, clockwise seen from above (so the AI keeps turning right).
        static TrackLine Circle(float radius = 60f)
        {
            var points = new Vector2[72];
            for (int i = 0; i < points.Length; i++)
            {
                float a = i * 2f * MathF.PI / points.Length;
                points[i] = new Vector2(-radius * MathF.Cos(a), radius * MathF.Sin(a));
            }
            return new TrackLine(points);
        }

        // A long straight towards +Z, closed far away (2 km there and back, 40 m apart).
        static TrackLine Straight() => new TrackLine(new[]
        {
            new Vector2(0f, 0f), new Vector2(0f, 1000f), new Vector2(40f, 1000f), new Vector2(40f, 0f),
        });

        // On the line at that distance, facing along it.
        static KartMotion KartOn(TrackLine route, float along = 0f, float lateral = 0f)
        {
            Vector2 d = route.DirectionAt(along);
            return new KartMotion(KartStats.Standard, route.PointAt(along) + route.RightAt(along) * lateral,
                                  MathF.Atan2(d.X, d.Y) * 180f / MathF.PI);
        }

        [Test]
        public void FromStill_Accelerates()
        {
            var route = Circle();
            new AiPilot(route, KartStats.Standard).Drive(KartOn(route), Frame, out float throttle, out _);
            Assert.AreEqual(1f, throttle);
        }

        [Test]
        public void OnAClockwiseCircle_SteersRight()
        {
            var route = Circle();
            new AiPilot(route, KartStats.Standard).Drive(KartOn(route), Frame, out _, out float steer);
            Assert.Greater(steer, 0f);
        }

        [Test]
        public void TighterCurves_AreSlower()
        {
            float wide = new AiPilot(Circle(120f), KartStats.Standard).CurveSpeed(0f);
            float tight = new AiPilot(Circle(30f), KartStats.Standard).CurveSpeed(0f);
            Assert.Less(tight, wide);
        }

        [Test]
        public void DrivingALap_StaysOnItsLateral()
        {
            var line = Circle();
            var kart = KartOn(line, 0f, 2f);
            var pilot = new AiPilot(AiRoute.Plain(line, 4f), KartStats.Standard) { Lateral = 2f };
            float worst = 0f;
            for (int frame = 0; frame < 60 * 20; frame++)   // 20 s at 60 fps
            {
                pilot.Drive(kart, Frame, out float throttle, out float steer);
                kart.Step(throttle, steer, Frame);
                float along = line.DistanceAlong(kart.Position);
                Vector2 onLane = line.PointAt(along) + line.RightAt(along) * pilot.Lateral;
                if (frame > 120) worst = MathF.Max(worst, Vector2.Distance(kart.Position, onLane));
            }
            Assert.Less(worst, 2f, "Drifted off its lane.");
            Assert.Greater(kart.Speed, 10f, "Crawled instead of racing.");
        }

        [Test]
        public void Wander_DriftsAcrossTheRoute_WithinItsWidth()
        {
            var line = Straight();
            var pilot = new AiPilot(AiRoute.Plain(line, 5f), KartStats.Standard) { Wander = 1f, Phase = 1f };
            var kart = KartOn(line);
            float most = 0f;
            for (int frame = 0; frame < 60 * 30; frame++)
            {
                pilot.Drive(kart, Frame, out _, out _);
                most = MathF.Max(most, MathF.Abs(pilot.Lateral));
                Assert.LessOrEqual(MathF.Abs(pilot.Lateral), 5f + 1e-4f);
            }
            Assert.Greater(most, 2f, "Never left the line.");
        }

        /// <summary>
        /// You (fast) 10 m behind a slow kart in your lane: the AI sees it and goes for a side;
        /// with karts on both sides, it slows to the slow kart's speed instead.
        /// </summary>
        [Test]
        public void SlowKartAhead_GoesBeside_OrWaitsIfBoxedIn()
        {
            var line = Straight();
            var route = AiRoute.Plain(line, 5f);

            KartMotion fast = KartOn(line, 100f), slow = KartOn(line, 110f);
            fast.Bump(Vector2.Zero, 20f);
            slow.Bump(Vector2.Zero, 8f);
            var traffic = new Traffic(route);
            traffic.Add(fast);
            traffic.Add(slow);
            var pilot = new AiPilot(route, KartStats.Standard) { Traffic = traffic };
            pilot.Drive(fast, Frame, out _, out _);
            Assert.IsTrue(pilot.Overtaking);

            // The same, but with a kart on each side of the slow one.
            KartMotion boxed = KartOn(line, 100f);
            boxed.Bump(Vector2.Zero, 20f);
            var crowded = new Traffic(route);
            crowded.Add(boxed);
            crowded.Add(slow);
            crowded.Add(KartOn(line, 108f, -2.8f));
            crowded.Add(KartOn(line, 108f, 2.8f));
            var waiting = new AiPilot(route, KartStats.Standard) { Traffic = crowded };
            waiting.Drive(boxed, Frame, out float throttle, out _);
            Assert.IsFalse(waiting.Overtaking);
            Assert.IsTrue(waiting.Blocked);
            Assert.Less(throttle, 0f, "Should ease off to the slow kart's speed.");
        }

        [Test]
        public void RacingLine_CutsTheInsideOfACorner_AndKeepsToTheRoad()
        {
            // On a right-hand circle the inside is to the right: the line moves right of the centre.
            var centre = Circle(50f);
            AiRoute route = AiRoute.RacingLine(centre, 16f);
            Vector2 p = route.Line.PointAt(0f);
            float along = centre.DistanceAlong(p);
            float offset = Vector2.Dot(p - centre.PointAt(along), centre.RightAt(along));
            Assert.Greater(offset, 1f);

            var (left, right) = route.RoomAt(0f);
            Assert.AreEqual(16f - 2f * 1.5f, left + right, 0.2f, "The room is the road minus a margin each side.");
            Assert.Less(right, left, "Less room on the inside, where the line already is.");
        }

        // Two 1 km straights joined by half circles of 20 m, a point every 2 m (as a real track).
        static TrackLine Stadium()
        {
            var points = new System.Collections.Generic.List<Vector2>();
            for (float z = 0f; z < 1000f; z += 2f) points.Add(new Vector2(0f, z));
            for (int i = 0; i < 31; i++)
            {
                float a = MathF.PI * i / 31f;   // round the top, turning right
                points.Add(new Vector2(20f - 20f * MathF.Cos(a), 1000f + 20f * MathF.Sin(a)));
            }
            for (float z = 1000f; z > 0f; z -= 2f) points.Add(new Vector2(40f, z));
            for (int i = 0; i < 31; i++)
            {
                float a = MathF.PI * i / 31f;   // round the bottom
                points.Add(new Vector2(20f + 20f * MathF.Cos(a), -20f * MathF.Sin(a)));
            }
            return new TrackLine(points);
        }

        [Test]
        public void RacingLine_OnAStraight_StaysInTheMiddle()
        {
            AiRoute route = AiRoute.RacingLine(Stadium(), 16f);
            var (left, right) = route.RoomAt(500f);
            Assert.AreEqual(left, right, 0.3f);
        }

        [Test]
        public void Traffic_SeesWhoIsAheadInTheLane_AndWhereThereIsRoom()
        {
            var line = Straight();
            var traffic = new Traffic(AiRoute.Plain(line, 5f));
            KartMotion me = KartOn(line, 100f), front = KartOn(line, 108f), beside = KartOn(line, 104f, 3f);
            traffic.Add(me);
            traffic.Add(front);
            traffic.Add(beside);

            Assert.AreSame(front, traffic.Ahead(me, 0f, 20f, 2f).Kart);
            Assert.IsNull(traffic.Ahead(me, 0f, 5f, 2f), "Too far ahead to matter.");
            Assert.AreEqual(8f, traffic.Gap(traffic.Of(me), traffic.Of(front)), 0.01f);
            Assert.IsFalse(traffic.Clear(me, 3f, 3f, 10f, 2f), "Someone is on the right.");
            Assert.IsTrue(traffic.Clear(me, -3f, 3f, 10f, 2f), "Nobody on the left.");
        }
    }
}
