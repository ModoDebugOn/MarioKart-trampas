using System;
using System.Collections.Generic;
using System.Numerics;

namespace KartRace.Domain.Driving
{
    /// <summary>
    /// Where every kart is, measured on the AI's route: how far along it, and how far to its side.
    /// The AI uses it to see who is in front of it in its lane, and whether there is a gap beside.
    /// Updated once a frame, after the karts have moved.
    /// </summary>
    public sealed class Traffic
    {
        public sealed class Car
        {
            public KartMotion Kart { get; }
            /// <summary>Metres along the route.</summary>
            public float Along { get; internal set; }
            /// <summary>Metres to the right of the route (negative: to the left).</summary>
            public float Lateral { get; internal set; }

            public Car(KartMotion kart) => Kart = kart;
        }

        readonly AiRoute route;
        readonly List<Car> cars = new List<Car>();
        readonly Dictionary<KartMotion, Car> byKart = new Dictionary<KartMotion, Car>();

        public IReadOnlyList<Car> Cars => cars;

        public Traffic(AiRoute route) => this.route = route;

        public void Add(KartMotion kart)
        {
            var car = new Car(kart);
            cars.Add(car);
            byKart[kart] = car;
            Measure(car);
        }

        public Car Of(KartMotion kart) => byKart[kart];

        public void Update()
        {
            foreach (Car car in cars) Measure(car);
        }

        /// <summary>
        /// Metres the other car is ahead of this one along the route (negative: behind), the
        /// short way round the lap.
        /// </summary>
        public float Gap(Car from, Car to)
        {
            float length = route.Line.Length;
            float gap = (to.Along - from.Along) % length;
            if (gap > length * 0.5f) gap -= length;
            if (gap < -length * 0.5f) gap += length;
            return gap;
        }

        /// <summary>
        /// The nearest car in front of `self`, up to `distance` metres ahead, whose side position
        /// is less than `width` from `lateral` (so it is in the way). Null if the lane is clear.
        /// </summary>
        public Car Ahead(KartMotion self, float lateral, float distance, float width)
        {
            Car me = byKart[self], nearest = null;
            float best = distance;
            foreach (Car car in cars)
            {
                if (car == me || MathF.Abs(car.Lateral - lateral) >= width) continue;
                float gap = Gap(me, car);
                if (gap > 0f && gap < best)
                {
                    best = gap;
                    nearest = car;
                }
            }
            return nearest;
        }

        /// <summary>
        /// True if no car is less than `width` from `lateral` between `behind` metres behind
        /// `self` and `ahead` metres in front of it: room to move there.
        /// </summary>
        public bool Clear(KartMotion self, float lateral, float behind, float ahead, float width)
        {
            Car me = byKart[self];
            foreach (Car car in cars)
            {
                if (car == me || MathF.Abs(car.Lateral - lateral) >= width) continue;
                float gap = Gap(me, car);
                if (gap > -behind && gap < ahead) return false;
            }
            return true;
        }

        /// <summary>
        /// True if moving across the route from `from` to `to` would take `self` closer to a kart
        /// that is beside it (less than `alongside` metres ahead or behind) and less than `width`
        /// from where it would end up: it would squeeze into it.
        /// </summary>
        public bool Squeezes(KartMotion self, float from, float to, float alongside, float width)
        {
            Car me = byKart[self];
            foreach (Car car in cars)
            {
                if (car == me || MathF.Abs(Gap(me, car)) >= alongside) continue;
                float after = MathF.Abs(car.Lateral - to);
                if (after < width && after < MathF.Abs(car.Lateral - from)) return true;
            }
            return false;
        }

        void Measure(Car car)
        {
            Vector2 position = car.Kart.Position;
            car.Along = route.Line.DistanceAlong(position);
            car.Lateral = Vector2.Dot(position - route.Line.PointAt(car.Along), route.Line.RightAt(car.Along));
        }
    }
}
