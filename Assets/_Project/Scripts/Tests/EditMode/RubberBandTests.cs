using KartRace.Application;
using KartRace.Domain.Driving;
using KartRace.Domain.Race;
using KartRace.Domain.Cheats;
using KartRace.Domain.Track;
using NUnit.Framework;

namespace KartRace.Tests
{
    public class RubberBandTests
    {
        static readonly RubberBand Band = RubberBand.Normal;

        [Test]
        public void Close_RacesNormally()
        {
            Assert.AreEqual(1f, Band.Pace(0f));
            Assert.AreEqual(1f, Band.Pace(Band.DeadZone));
            Assert.AreEqual(1f, Band.Pace(-Band.DeadZone));
        }

        [Test]
        public void FarBehind_GetsTheFullBoost()
        {
            Assert.AreEqual(1f + Band.MaxBoost, Band.Pace(1000f), 1e-5f);
        }

        [Test]
        public void FarAhead_HoldsBackTheMost()
        {
            Assert.AreEqual(1f - Band.MaxHoldBack, Band.Pace(-1000f), 1e-5f);
        }

        [Test]
        public void TheMoreItStretches_TheHarderItPulls()
        {
            float behind = Band.DeadZone + Band.CatchUpDistance;
            float ahead = Band.DeadZone + Band.HoldBackDistance;
            Assert.Less(Band.Pace(behind * 0.5f), Band.Pace(behind));
            Assert.Greater(Band.Pace(-ahead * 0.5f), Band.Pace(-ahead));
        }

        [Test]
        public void Lead_MovesTheSpotTheRivalWants()
        {
            var ahead = new RubberBand(5f, 0.3f, 50f, 0.2f, 30f, lead: 20f);
            Assert.AreEqual(1f, ahead.Pace(-20f));       // 20 m ahead of you: right where it wants
            Assert.Greater(ahead.Pace(0f), 1f);          // level with you: still pushes to get ahead
            var behind = new RubberBand(5f, 0.3f, 50f, 0.2f, 30f, lead: -15f);
            Assert.AreEqual(1f, behind.Pace(15f));       // 15 m behind you: happy there
            Assert.Less(behind.Pace(0f), 1f);            // level with you: lets you go
        }

        [Test]
        public void Slack_EachRivalHasItsOwnEdges()
        {
            // 60 m of slack behind, 20 ahead (Lead 0, dead zone 5).
            Assert.AreEqual(1f, Band.Pace(50f, slackBehind: 60f, slackAhead: 20f));    // 50 m behind you: slack
            Assert.Greater(Band.Pace(80f, slackBehind: 60f, slackAhead: 20f), 1f);     // 80 m behind: hurries
            Assert.AreEqual(1f, Band.Pace(-20f, slackBehind: 60f, slackAhead: 20f));   // 20 m ahead: slack
            Assert.Less(Band.Pace(-40f, slackBehind: 60f, slackAhead: 20f), 1f);       // 40 m ahead: waits
        }

        static readonly TrackLine Square = new TrackLine(new[]
        {
            new System.Numerics.Vector2(0f, 0f), new System.Numerics.Vector2(1000f, 0f),
            new System.Numerics.Vector2(1000f, 1000f), new System.Numerics.Vector2(0f, 1000f),
        });

        static Racer At(Character who, float total) => new Racer(who, new RaceProgress(Square.Length, total));

        // Two humans 600 m apart, and a rival 100 m behind the one in front.
        static readonly Racer First = At(Character.Mario, 1000f), Last = At(Character.Luigi, 400f);
        static readonly Racer[] Humans = { Last, First };   // not in race order on purpose
        static readonly Racer Between = At(Character.Peach, 900f);

        [Test]
        public void ChaseLeader_ChasesTheHumanInFront()
        {
            Assert.AreEqual(100f, new ChaseLeader().Gap(Between, 0, Humans), 1e-3f);
        }

        [Test]
        public void ChaseNearest_ChasesWhoeverIsClosest_AheadOrBehind()
        {
            var rule = new ChaseNearest();
            Assert.AreEqual(100f, rule.Gap(Between, 0, Humans), 1e-3f);                     // 100 ahead vs 500 behind
            Assert.AreEqual(-100f, rule.Gap(At(Character.Toad, 500f), 0, Humans), 1e-3f);  // just past the last one
        }

        [Test]
        public void ChaseAverage_ChasesASpotHalfwayBetween()
        {
            // Halfway is at 700 m: 200 m behind this rival, so it should slow down.
            Assert.AreEqual(-200f, new ChaseAverage().Gap(Between, 0, Humans), 1e-3f);
        }

        [Test]
        public void SplitPack_TakesTurnsDownTheListOfHumans()
        {
            var rule = new SplitPack();
            Assert.AreEqual(Between.GapTo(Humans[0]), rule.Gap(Between, 0, Humans), 1e-3f);
            Assert.AreEqual(Between.GapTo(Humans[1]), rule.Gap(Between, 1, Humans), 1e-3f);
            Assert.AreEqual(Between.GapTo(Humans[0]), rule.Gap(Between, 2, Humans), 1e-3f);
        }

        [Test]
        public void OneHuman_EveryRuleGivesTheSameGap()
        {
            Racer[] alone = { First };
            foreach (IChaseRule rule in new IChaseRule[] { new ChaseLeader(), new ChaseNearest(), new ChaseAverage(), new SplitPack() })
                Assert.AreEqual(100f, rule.Gap(Between, 3, alone), 1e-3f);
        }

        [Test]
        public void RubberBanding_UsesTheRuleItIsGiven()
        {
            var pilot = new AiPilot(Square, KartStats.Standard);
            var banding = new RubberBanding(Humans, new ChaseLeader());
            RubberBanding.Rival rival = banding.AddRival(Between, pilot);
            banding.Update();
            Assert.AreEqual(100f, rival.Gap, 1e-3f);
            Assert.Greater(pilot.Pace, 1f);      // 100 m behind the leader: hurries

            banding.Rule = new ChaseAverage();
            banding.Update();
            Assert.AreEqual(-200f, rival.Gap, 1e-3f);
            Assert.Less(pilot.Pace, 1f);         // 200 m past the halfway spot: waits
        }

        [Test]
        public void RubberBanding_SameGapDifferentRankDifferentPace()
        {
            Racer you = At(Character.Mario, 500f), a = At(Character.Luigi, 460f), b = At(Character.Peach, 460f);
            AiPilot first = new AiPilot(Square, KartStats.Standard), second = new AiPilot(Square, KartStats.Standard);

            // Lead 0 so the gap is all that counts.
            var banding = new RubberBanding(you)
            {
                Level = new Difficulty(new RubberBand(5f, 0.3f, 50f, 0.2f, 30f, lead: 0f), 1f),
                PackLength = 60f,
            };
            RubberBanding.Rival worst = banding.AddRival(a, first), best = banding.AddRival(b, second);
            banding.Update();
            Assert.AreEqual(0f, worst.Rank);
            Assert.AreEqual(1f, best.Rank);
            Assert.AreEqual(1f, worst.Pilot.Pace);    // 40 m behind with 60 of slack: fine there
            Assert.Greater(best.Pilot.Pace, 1f);      // 40 m behind with no slack: hurries
        }

        [Test]
        public void RubberBanding_RanksSpreadTheSkillAndTheSlack()
        {
            var banding = new RubberBanding(At(Character.Mario, 0f))
            {
                Level = Difficulty.Hard, SkillSpread = 0.1f, PackLength = 100f, PackAhead = 20f,
            };
            Character[] who = { Character.Luigi, Character.Peach, Character.Daisy, Character.Yoshi };
            foreach (Character c in who) banding.AddRival(At(c, 0f), new AiPilot(Square, KartStats.Standard));
            banding.Update();

            // Every rank from 0 to 1 used once, in another order than they joined.
            var ranks = new System.Collections.Generic.List<float>();
            foreach (RubberBanding.Rival rival in banding.Rivals) ranks.Add(rival.Rank);
            float[] inOrder = { 0f, 1f / 3f, 2f / 3f, 1f };
            CollectionAssert.AreEquivalent(inOrder, ranks);
            CollectionAssert.AreNotEqual(inOrder, ranks);

            foreach (RubberBanding.Rival rival in banding.Rivals)
            {
                if (rival.Rank == 1f)
                {
                    Assert.AreEqual(Difficulty.Hard.Skill, rival.Pilot.Skill, 1e-5f);   // the best: the level's skill
                    Assert.AreEqual(0f, rival.SlackBehind, 1e-5f);
                    Assert.AreEqual(20f, rival.SlackAhead, 1e-5f);
                }
                if (rival.Rank == 0f)
                {
                    Assert.AreEqual(Difficulty.Hard.Skill * 0.9f, rival.Pilot.Skill, 1e-5f);   // the worst: 10 % less
                    Assert.AreEqual(100f, rival.SlackBehind, 1e-5f);
                    Assert.AreEqual(0f, rival.SlackAhead, 1e-5f);
                }
            }
        }

        [Test]
        public void RubberBanding_SwitchedOffStillHasItsSkill()
        {
            var banding = new RubberBanding(At(Character.Mario, 500f)) { Level = Difficulty.Easy, Enabled = false };
            var pilot = new AiPilot(Square, KartStats.Standard);
            banding.AddRival(At(Character.Luigi, 0f), pilot);
            banding.Update();
            Assert.AreEqual(1f, pilot.Pace);
            Assert.Less(pilot.Skill, 1f);   // without the band an easy rival is still slower than you
        }

        [Test]
        public void Difficulty_HarderLevelsWaitLessAndWantToBeFurtherAhead()
        {
            Difficulty easy = Difficulty.Easy, normal = Difficulty.Normal, hard = Difficulty.Hard;
            Assert.Greater(easy.Band.MaxHoldBack, normal.Band.MaxHoldBack);
            Assert.Greater(normal.Band.MaxHoldBack, hard.Band.MaxHoldBack);
            Assert.Less(easy.Band.Lead, normal.Band.Lead);
            Assert.Less(normal.Band.Lead, hard.Band.Lead);
            Assert.Less(easy.Skill, hard.Skill);
        }

        [Test]
        public void RubberBanding_SpeedsUpTheOneBehindAndSlowsTheOneAhead()
        {
            Racer you = At(Character.Mario, 500f), behind = At(Character.Luigi, 100f), ahead = At(Character.Peach, 900f);
            AiPilot slow = new AiPilot(Square, KartStats.Standard), fast = new AiPilot(Square, KartStats.Standard);

            var banding = new RubberBanding(you);
            banding.AddRival(behind, slow);
            banding.AddRival(ahead, fast);
            banding.Update();
            Assert.Greater(slow.Pace, 1f);
            Assert.Less(fast.Pace, 1f);

            banding.Enabled = false;
            banding.Update();
            Assert.AreEqual(1f, slow.Pace);
            Assert.AreEqual(1f, fast.Pace);
        }
    }
}
