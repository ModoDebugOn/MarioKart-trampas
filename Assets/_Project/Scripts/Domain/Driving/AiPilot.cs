using System;
using System.Numerics;
using KartRace.Domain.Track;

namespace KartRace.Domain.Driving
{
    /// <summary>
    /// The AI at the wheel. It does not look at the road: it follows its route (a racing line
    /// with a width, like the CPU routes of the real games) and turns it into the same two numbers
    /// you give with the pad, throttle and steering.
    /// - Where across the route: each rival drifts slowly from side to side within the route's
    ///   width (its own rhythm, Phase), so they do not all drive on the same line.
    /// - Overtaking: if a slower kart is in its way, it moves beside it on the side with room and
    ///   stays there until it is past; with no room on either side, it matches its speed. Then it
    ///   drifts back. How Mario Kart's CPU overtakes is not documented: this is how it is usually done.
    /// - Steering: aims at a point of the route a few metres ahead ("pure pursuit"), at its Lateral.
    /// - Throttle: goes for its top speed times Pace, but slows down before a curve it could not
    ///   take that fast.
    /// Pace is the number the rubber band touches; Skill is the rival's own, so they do not all go
    /// equally fast (with the band on, a better rival settles further ahead of you).
    /// </summary>
    public sealed class AiPilot
    {
        /// <summary>Metres ahead of the kart it aims at, plus a bit more the faster it goes.</summary>
        const float LookAhead = 6f, LookAheadPerSpeed = 0.3f;
        /// <summary>Degrees off the target that already means full steering.</summary>
        const float FullSteerAngle = 25f;
        /// <summary>Stretch of route it reads ahead to see how much it bends, and where it starts.</summary>
        const float CurveFrom = 5f, CurveLength = 30f;
        /// <summary>Share of the kart's turn rate it dares to use in a curve (the rest is margin).</summary>
        const float CurveMargin = 0.8f;
        /// <summary>Metres per second it moves across the route.</summary>
        const float SideSpeed = 2.5f;
        /// <summary>Two karts closer than this across the route are in each other's way.</summary>
        const float KartWidth = 2.2f;
        /// <summary>Metres ahead it watches for traffic, plus a bit more the faster it goes.</summary>
        const float TrafficAhead = 6f, TrafficAheadPerSpeed = 0.5f;
        /// <summary>Metres the kart it overtakes must be behind it before it drifts back.</summary>
        const float PassedBy = 4f;
        /// <summary>m/s slower a kart must be to be worth overtaking (same speed: just follow it).</summary>
        const float WorthPassing = 1.5f;
        /// <summary>Seconds it tries to get past before giving up.</summary>
        const float PassPatience = 4f;
        /// <summary>Metres it keeps from the kart in front, and how hard it brakes to keep them (m/s per metre short).</summary>
        const float Following = 5f, FollowingBrake = 1.5f;
        /// <summary>Metres behind and ahead along the route that count as "beside" it.</summary>
        const float Alongside = 3f;

        readonly AiRoute route;
        readonly KartStats stats;
        float clock;
        Traffic.Car passing;   // the kart it is overtaking, if any
        float passLateral, passTime;

        /// <summary>
        /// Metres to the right of the route it drives on now (negative: to the left). It moves
        /// there little by little; set it to start where the kart is.
        /// </summary>
        public float Lateral { get; set; }
        /// <summary>Share of the route's width it drifts over: 0 always on the line, 1 edge to edge.</summary>
        public float Wander { get; set; }
        /// <summary>Its own rhythm of drifting (any number), so two rivals do not drift together.</summary>
        public float Phase { get; set; }
        /// <summary>Where the other karts are; without it, it drives as if alone.</summary>
        public Traffic Traffic { get; set; }
        /// <summary>
        /// How fast it goes compared with a normal kart: 1 normal, 1.3 a 30 % faster (its kart's top
        /// speed goes up too), 0.8 holding back. Set by the rubber band.
        /// </summary>
        public float Pace { get; set; } = 1f;
        /// <summary>How good this rival is: 1.04 is 4 % faster than a normal kart, 0.96 4 % slower.</summary>
        public float Skill { get; set; } = 1f;

        /// <summary>True while it is overtaking someone.</summary>
        public bool Overtaking => passing != null;
        /// <summary>True when the way is blocked and it is waiting behind someone.</summary>
        public bool Blocked { get; private set; }

        /// <summary>m/s it wants to go on a straight.</summary>
        public float TargetSpeed => stats.TopSpeed * Pace * Skill;

        public AiRoute Route => route;

        public AiPilot(AiRoute route, KartStats stats)
        {
            this.route = route;
            this.stats = stats;
        }

        /// <summary>A route with no width (it never drifts): its line, as is.</summary>
        public AiPilot(TrackLine line, KartStats stats) : this(AiRoute.Plain(line, 0f), stats) { }

        public void Drive(KartMotion kart, float deltaTime, out float throttle, out float steer)
        {
            clock += deltaTime;
            kart.TopSpeedScale = MathF.Max(Pace * Skill, 1f);   // only ever lifts the limit; holding back is easing off
            TrackLine line = route.Line;
            float along = line.DistanceAlong(kart.Position);
            float speed = MathF.Max(kart.Speed, 0f);
            float wantedSpeed = MathF.Min(TargetSpeed, CurveSpeed(along));

            // Across the route: its drift, or beside the kart it is overtaking.
            var (left, right) = route.RoomAt(along);
            float side = Drift(left, right);
            Blocked = false;
            if (Traffic != null) side = Overtake(kart, side, speed, left, right, deltaTime, ref wantedSpeed);
            float next = MoveTowards(Lateral, Math.Clamp(side, -left, right), SideSpeed * deltaTime);
            // Never slide sideways into a kart that is beside it.
            if (Traffic != null && Traffic.Squeezes(kart, Lateral, next, Alongside, KartWidth)) next = Lateral;
            Lateral = next;

            // Steering: the angle between where it faces and where the target point is.
            float ahead = along + LookAhead + speed * LookAheadPerSpeed;
            Vector2 target = line.PointAt(ahead) + line.RightAt(ahead) * Lateral;
            Vector2 toTarget = target - kart.Position;
            float wanted = MathF.Atan2(toTarget.X, toTarget.Y) * 180f / MathF.PI;
            steer = Math.Clamp(DeltaAngle(kart.Heading, wanted) / FullSteerAngle, -1f, 1f);

            // Throttle: towards the speed it wants.
            throttle = Math.Clamp((wantedSpeed - kart.Speed) / 2f, -1f, 1f);
        }

        /// <summary>
        /// Where it would like to be across the route: two slow waves mixed (so it never quite
        /// repeats), scaled to the room on each side.
        /// </summary>
        float Drift(float left, float right)
        {
            float wave = 0.6f * MathF.Sin(0.23f * clock + Phase) + 0.4f * MathF.Sin(0.61f * clock + 2.1f * Phase);
            return wave * Wander * (wave > 0f ? right : left);
        }

        /// <summary>
        /// Keeps overtaking the kart it is passing until it is behind (or it gives up); otherwise
        /// looks at the kart in its lane. Clearly slower: it picks the side with room (the one
        /// nearer where it wanted to be first). Whatever it decides, while a kart is still in its
        /// lane it follows it, keeping its distance: it only speeds up once it has moved out.
        /// Returns where to be across the route.
        /// </summary>
        float Overtake(KartMotion kart, float side, float speed, float left, float right, float deltaTime,
                       ref float wantedSpeed)
        {
            Traffic.Car me = Traffic.Of(kart);
            if (passing != null)
            {
                passTime += deltaTime;
                float gap = Traffic.Gap(me, passing);
                bool past = gap < -PassedBy, gone = gap > TrafficAhead * 3f, tired = passTime > PassPatience;
                if (past || gone || tired) passing = null;
                else side = passLateral;
            }

            Traffic.Car blocker = Traffic.Ahead(kart, Lateral, TrafficAhead + speed * TrafficAheadPerSpeed, KartWidth);
            if (blocker == null) return side;
            float gapAhead = Traffic.Gap(me, blocker);

            if (passing == null && blocker.Kart.Speed < wantedSpeed - WorthPassing)
            {
                float toLeft = blocker.Lateral - KartWidth * 1.2f, toRight = blocker.Lateral + KartWidth * 1.2f;
                bool leftFirst = MathF.Abs(toLeft - side) < MathF.Abs(toRight - side);
                Blocked = true;
                foreach (float option in leftFirst ? new[] { toLeft, toRight } : new[] { toRight, toLeft })
                {
                    if (option < -left || option > right) continue;   // off the route's width
                    if (!Traffic.Clear(kart, option, Alongside, gapAhead + 4f, KartWidth)) continue;
                    passing = blocker;
                    passLateral = side = option;
                    passTime = 0f;
                    Blocked = false;
                    break;
                }
            }
            // Follow it while it is in the way: its speed, a bit less if closer than it should be.
            float follow = blocker.Kart.Speed - MathF.Max(0f, Following - gapAhead) * FollowingBrake;
            wantedSpeed = MathF.Min(wantedSpeed, MathF.Max(follow, 0f));
            return side;
        }

        /// <summary>
        /// Fastest speed for the curve ahead. Turning an angle in some metres at speed v takes a
        /// turn rate of v * angle / metres; the kart has only so much, so v can be at most
        /// turnRate * metres / angle.
        /// </summary>
        public float CurveSpeed(float along)
        {
            Vector2 from = route.Line.DirectionAt(along + CurveFrom);
            Vector2 to = route.Line.DirectionAt(along + CurveFrom + CurveLength);
            float angle = MathF.Abs(DeltaAngle(Heading(from), Heading(to)));
            if (angle < 1f) return float.MaxValue;
            return CurveMargin * stats.TurnRate * CurveLength / angle;
        }

        static float MoveTowards(float from, float to, float step) =>
            MathF.Abs(to - from) <= step ? to : from + MathF.Sign(to - from) * step;

        static float Heading(Vector2 direction) => MathF.Atan2(direction.X, direction.Y) * 180f / MathF.PI;

        /// <summary>From one heading to another the short way round, in (-180, 180].</summary>
        static float DeltaAngle(float from, float to)
        {
            float delta = (to - from) % 360f;
            if (delta > 180f) delta -= 360f;
            if (delta <= -180f) delta += 360f;
            return delta;
        }
    }
}
