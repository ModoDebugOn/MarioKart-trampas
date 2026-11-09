using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using KartRace.Infrastructure;
using static KartRace.Editor.BuildTools;
using Character = KartRace.Domain.Race.Character;

namespace KartRace.Editor
{
    /// <summary>
    /// Generates the racers out of Unity primitives, so nothing on screen comes from a real game and
    /// everything can be regenerated after tweaking a number here:
    /// - Vehicles: three kart bodies (standard, pipe frame, sport) and two bikes (bike, scooter),
    ///   painted with each driver's colours.
    /// - Racers: one prefab per driver = its vehicle + the kit's capsule (Prefabs/Characters) nested
    ///   in the seat. Being nested, re-baking an outfit in the kit updates the racer too.
    /// The circuit builder puts them on the starting grid, in the order of the roster.
    ///
    /// Vehicles are 1 unit = 1 m with the front towards +Z and the root on the ground. The capsule
    /// is 1 wide and 2 tall, so the karts are about twice as wide as the driver, like in the games.
    /// </summary>
    static class KartBuilder
    {
        const string Root = "Assets/_Project";
        const string VehicleMaterials = Root + "/Materials/Vehicles";
        public const string RacerPrefabs = Root + "/Prefabs/Racers";
        const string CharacterPrefabs = Root + "/Prefabs/Characters";

        enum Vehicle { StandardKart, PipeFrame, SportKart, Bike, Scooter }

        /// <summary>One driver: kit outfit, vehicle, its two paint colours and the capsule's scale
        /// (heavyweights are bigger, lightweights smaller; Waluigi tall and thin, Wario wide).</summary>
        readonly struct Racer
        {
            public readonly Character Character;
            public string Name => Character.ToString();   // the outfit and prefab of that character
            public readonly Vehicle Vehicle;
            public readonly Color Body, Accent;
            public readonly Vector3 Scale;

            public Racer(Character character, Vehicle vehicle, Color body, Color accent, float sx, float sy)
            {
                Character = character; Vehicle = vehicle; Body = body; Accent = accent;
                Scale = new Vector3(sx, sy, sx);
            }
        }

        static readonly Color Red = new Color(0.86f, 0.1f, 0.1f);
        static readonly Color Green = new Color(0.1f, 0.62f, 0.2f);
        static readonly Color Blue = new Color(0.15f, 0.3f, 0.8f);
        static readonly Color Yellow = new Color(0.98f, 0.84f, 0.15f);
        static readonly Color Pink = new Color(1f, 0.52f, 0.72f);
        static readonly Color Purple = new Color(0.42f, 0.16f, 0.62f);
        static readonly Color White = new Color(0.96f, 0.96f, 0.94f);
        static readonly Color Black = new Color(0.1f, 0.1f, 0.11f);

        // The 20 drivers of the race: 14 karts and 6 bikes.
        static readonly Racer[] Roster =
        {
            new Racer(Character.Mario,       Vehicle.StandardKart, Red,                          White,  1f,    1f),
            new Racer(Character.Luigi,       Vehicle.StandardKart, Green,                        White,  1f,    1.1f),
            new Racer(Character.Peach,       Vehicle.Scooter,      Pink,                         White,  0.95f, 1.05f),
            new Racer(Character.Daisy,       Vehicle.Bike,         new Color(1f, 0.6f, 0.1f),    Green,  0.95f, 1.05f),
            new Racer(Character.Rosalina,    Vehicle.Bike,         new Color(0.4f, 0.82f, 0.9f), White,  0.95f, 1.12f),
            new Racer(Character.Yoshi,       Vehicle.PipeFrame,    Green,                        White,  1f,    1f),
            new Racer(Character.Birdo,       Vehicle.PipeFrame,    Pink,                         Red,    1f,    1.05f),
            new Racer(Character.Toad,        Vehicle.StandardKart, Blue,                         White,  0.85f, 0.85f),
            new Racer(Character.Toadette,    Vehicle.Scooter,      new Color(0.9f, 0.3f, 0.6f),  White,  0.85f, 0.85f),
            new Racer(Character.KoopaTroopa, Vehicle.PipeFrame,    Green,                        Yellow, 0.85f, 0.85f),
            new Racer(Character.Lakitu,      Vehicle.SportKart,    White,                        Green,  0.9f,  0.9f),
            new Racer(Character.DryBones,    Vehicle.Bike,         new Color(0.3f, 0.3f, 0.33f), White,  0.85f, 0.85f),
            new Racer(Character.ShyGuy,      Vehicle.StandardKart, Red,                          Black,  0.85f, 0.85f),
            new Racer(Character.Bowser,      Vehicle.SportKart,    Black,                        new Color(1f, 0.45f, 0.1f), 1.3f, 1.25f),
            new Racer(Character.BowserJr,    Vehicle.PipeFrame,    Green,                        new Color(1f, 0.55f, 0.15f), 0.9f, 0.9f),
            new Racer(Character.KingBoo,     Vehicle.SportKart,    Purple,                       Yellow, 1.25f, 1.2f),
            new Racer(Character.Wario,       Vehicle.StandardKart, Yellow,                       Purple, 1.15f, 1f),
            new Racer(Character.Waluigi,     Vehicle.Bike,         Purple,                       Black,  0.9f,  1.15f),
            new Racer(Character.DonkeyKong,  Vehicle.SportKart,    new Color(0.5f, 0.3f, 0.14f), Red,    1.25f, 1.2f),
            new Racer(Character.DiddyKong,   Vehicle.PipeFrame,    Red,                          Yellow, 0.85f, 0.85f),
        };

        /// <summary>The drivers in roster order, which is also their place on the starting grid.</summary>
        public static IReadOnlyList<string> RacerNames => Roster.Select(r => r.Name).ToArray();

        // ------------------------------------------------------------------ menu

        [MenuItem("Tools/Kart Race/Build Racers")]
        public static void BuildRacers()
        {
            foreach (var folder in new[] { VehicleMaterials, RacerPrefabs })
                EnsureFolder(folder);

            var parts = new Parts();
            foreach (Racer racer in Roster) BuildRacer(racer, parts);

            AssetDatabase.SaveAssets();
            Debug.Log($"[KartBuilder] {Roster.Length} racers generated in {RacerPrefabs}.");
        }

        // ------------------------------------------------------------------ racers

        /// <summary>The materials every vehicle shares (the paint is per driver).</summary>
        sealed class Parts
        {
            public readonly Material Tyre = Mat(VehicleMaterials, "Tyre", new Color(0.07f, 0.07f, 0.08f), 0.2f);
            public readonly Material Chassis = Mat(VehicleMaterials, "Chassis", new Color(0.18f, 0.19f, 0.21f), 0.3f);
            public readonly Material Metal = Mat(VehicleMaterials, "Metal", new Color(0.78f, 0.8f, 0.83f), 0.85f);
            public readonly Material Seat = Mat(VehicleMaterials, "Seat", new Color(0.12f, 0.11f, 0.11f), 0.25f);
            public readonly Material Glass = Mat(VehicleMaterials, "Glass", new Color(0.12f, 0.2f, 0.32f), 0.95f);
            public readonly Material Light = Mat(VehicleMaterials, "Headlight", new Color(1f, 0.95f, 0.7f), 0.9f);
        }

        /// <summary>Paint shared by every piece of that colour: Paint_RRGGBB.mat, glossy like a kart.</summary>
        static Material Paint(Color colour) =>
            Mat(VehicleMaterials, "Paint_" + ColorUtility.ToHtmlStringRGB(colour), colour, 0.6f);

        static void BuildRacer(Racer racer, Parts parts)
        {
            var character = AssetDatabase.LoadAssetAtPath<GameObject>($"{CharacterPrefabs}/{racer.Name}.prefab");
            if (character == null) { Debug.LogError($"[KartBuilder] Missing {racer.Name}.prefab: bake it with the character kit."); return; }

            var root = new GameObject(racer.Name);
            var vehicle = new GameObject("Vehicle").transform;
            vehicle.SetParent(root.transform, false);
            Material body = Paint(racer.Body), accent = Paint(racer.Accent);

            // Where the bottom of the capsule sits (sunk into the seat) and how much the driver leans.
            Vector3 seat;
            float lean;
            switch (racer.Vehicle)
            {
                case Vehicle.PipeFrame: PipeFrame(vehicle, body, accent, parts); seat = new Vector3(0f, 0.08f, -0.3f); lean = 0f; break;
                case Vehicle.SportKart: SportKart(vehicle, body, accent, parts); seat = new Vector3(0f, 0.08f, -0.3f); lean = -5f; break;
                case Vehicle.Bike:      Bike(vehicle, body, accent, parts);      seat = new Vector3(0f, 0.8f, -0.3f);  lean = 12f; break;
                case Vehicle.Scooter:   Scooter(vehicle, body, accent, parts);   seat = new Vector3(0f, 0.75f, -0.4f); lean = 0f; break;
                default:                StandardKart(vehicle, body, accent, parts); seat = new Vector3(0f, 0.1f, -0.25f); lean = 0f; break;
            }

            // The capsule's pivot is in its middle, half its height (= scale.y) above the bottom.
            // Bigger drivers sit further back so their belly doesn't go through the steering wheel.
            var driver = (GameObject)PrefabUtility.InstantiatePrefab(character, root.transform);
            driver.name = "Driver";
            driver.transform.localPosition = seat + new Vector3(0f, racer.Scale.y, -(racer.Scale.z - 1f) * 0.5f);
            driver.transform.localRotation = Quaternion.Euler(lean, 0f, 0f);
            driver.transform.localScale = racer.Scale;

            root.AddComponent<KartBody>().Assign(racer.Character);
            SavePrefab(root, $"{RacerPrefabs}/{racer.Name}.prefab");
        }

        // ------------------------------------------------------------------ karts

        /// <summary>The classic kart: rounded nose, side pods, seat back and two exhausts.</summary>
        static void StandardKart(Transform v, Material body, Material accent, Parts p)
        {
            Box("Floor", v, new Vector3(0f, 0.3f, 0f), new Vector3(1.5f, 0.16f, 2.5f), p.Chassis);
            Box("Hood", v, new Vector3(0f, 0.55f, 0.75f), new Vector3(1.15f, 0.36f, 1.0f), body);
            Piece(PrimitiveType.Sphere, "Nose", v, new Vector3(0f, 0.52f, 1.25f), Vector3.zero, new Vector3(1.15f, 0.42f, 0.7f), body);
            Box("Stripe", v, new Vector3(0f, 0.735f, 0.8f), new Vector3(0.28f, 0.02f, 0.95f), accent);
            Box("SidePod", v, new Vector3(0.66f, 0.5f, -0.15f), new Vector3(0.3f, 0.34f, 1.5f), body, mirror: true);
            Box("SeatBack", v, new Vector3(0f, 0.85f, -0.95f), new Vector3(1.0f, 0.85f, 0.16f), accent, pitch: -10f);
            Cylinder("Bumper", v, new Vector3(0f, 0.32f, 1.55f), 0.1f, 1.35f, accent, Vector3.right);
            Cylinder("Exhaust", v, new Vector3(0.32f, 0.55f, -1.3f), 0.09f, 0.5f, p.Metal, Vector3.forward, mirror: true);
            SteeringWheel(v, new Vector3(0f, 0.95f, 0.5f), p);
            Wheel(v, new Vector3(0.85f, 0.32f, 0.95f), 0.32f, 0.3f, accent, p, mirror: true);
            Wheel(v, new Vector3(0.88f, 0.4f, -0.9f), 0.4f, 0.42f, accent, p, mirror: true);
        }

        /// <summary>The bare go-kart: thin tubes, a small nose and the engine at the back.</summary>
        static void PipeFrame(Transform v, Material body, Material accent, Parts p)
        {
            Box("Floor", v, new Vector3(0f, 0.25f, 0f), new Vector3(1.1f, 0.08f, 2.2f), body);
            Cylinder("Rail", v, new Vector3(0.6f, 0.32f, 0f), 0.05f, 2.3f, body, Vector3.forward, mirror: true);
            Cylinder("FrontBar", v, new Vector3(0f, 0.32f, 1.15f), 0.05f, 1.25f, body, Vector3.right);
            Box("Nose", v, new Vector3(0f, 0.42f, 1.0f), new Vector3(0.75f, 0.26f, 0.55f), body);
            Box("Number", v, new Vector3(0f, 0.42f, 1.28f), new Vector3(0.4f, 0.2f, 0.02f), accent);
            Box("Seat", v, new Vector3(0f, 0.65f, -0.85f), new Vector3(0.9f, 0.7f, 0.14f), accent, pitch: -12f);
            Cylinder("RollBar", v, new Vector3(0.5f, 0.75f, -1.0f), 0.045f, 1.0f, p.Metal, Vector3.up, mirror: true);
            Cylinder("RollBarTop", v, new Vector3(0f, 1.25f, -1.0f), 0.045f, 1.05f, p.Metal, Vector3.right);
            Box("Engine", v, new Vector3(0f, 0.45f, -1.2f), new Vector3(0.6f, 0.35f, 0.35f), p.Chassis);
            Cylinder("Exhaust", v, new Vector3(0.22f, 0.7f, -1.4f), 0.07f, 0.4f, p.Metal, Vector3.forward);
            SteeringWheel(v, new Vector3(0f, 0.9f, 0.5f), p);
            Wheel(v, new Vector3(0.75f, 0.3f, 0.9f), 0.3f, 0.3f, accent, p, mirror: true);
            Wheel(v, new Vector3(0.78f, 0.33f, -0.9f), 0.33f, 0.36f, accent, p, mirror: true);
        }

        /// <summary>The fast one: long and low, wedge nose, front wing and rear spoiler.</summary>
        static void SportKart(Transform v, Material body, Material accent, Parts p)
        {
            Box("Body", v, new Vector3(0f, 0.4f, 0.05f), new Vector3(1.1f, 0.3f, 2.9f), body);
            Box("NoseWedge", v, new Vector3(0f, 0.46f, 1.3f), new Vector3(0.95f, 0.18f, 1.0f), body, pitch: 10f);
            Piece(PrimitiveType.Capsule, "SidePod", v, new Vector3(0.68f, 0.42f, -0.25f), new Vector3(90f, 0f, 0f), new Vector3(0.42f, 0.75f, 0.42f), body, mirror: true);
            Box("Windscreen", v, new Vector3(0f, 0.7f, 0.5f), new Vector3(0.8f, 0.3f, 0.04f), p.Glass, pitch: -35f);
            Box("FrontWing", v, new Vector3(0f, 0.22f, 1.75f), new Vector3(1.7f, 0.05f, 0.3f), accent);
            Box("SpoilerStrut", v, new Vector3(0.4f, 0.75f, -1.3f), new Vector3(0.06f, 0.5f, 0.15f), p.Chassis, mirror: true);
            Box("Spoiler", v, new Vector3(0f, 1.02f, -1.35f), new Vector3(1.7f, 0.06f, 0.42f), accent, pitch: -8f);
            Box("SeatBack", v, new Vector3(0f, 0.75f, -0.95f), new Vector3(0.95f, 0.7f, 0.14f), p.Seat, pitch: -15f);
            Cylinder("Exhaust", v, new Vector3(0.25f, 0.45f, -1.45f), 0.1f, 0.35f, p.Metal, Vector3.forward, mirror: true);
            SteeringWheel(v, new Vector3(0f, 0.9f, 0.55f), p);
            Wheel(v, new Vector3(0.9f, 0.3f, 1.05f), 0.3f, 0.28f, accent, p, mirror: true);
            Wheel(v, new Vector3(0.92f, 0.4f, -0.95f), 0.4f, 0.48f, accent, p, mirror: true);
        }

        // ------------------------------------------------------------------ bikes

        /// <summary>The standard bike: big wheels, tank, fork and handlebar. The driver leans forward.</summary>
        static void Bike(Transform v, Material body, Material accent, Parts p)
        {
            Box("Frame", v, new Vector3(0f, 0.75f, 0.05f), new Vector3(0.45f, 0.45f, 1.3f), body);
            Piece(PrimitiveType.Sphere, "Tank", v, new Vector3(0f, 1.0f, 0.45f), Vector3.zero, new Vector3(0.55f, 0.42f, 0.85f), body);
            Box("Fairing", v, new Vector3(0f, 1.1f, 0.9f), new Vector3(0.5f, 0.45f, 0.2f), accent, pitch: -25f);
            Box("Seat", v, new Vector3(0f, 0.98f, -0.4f), new Vector3(0.42f, 0.14f, 0.8f), p.Seat);
            Box("Tail", v, new Vector3(0f, 1.0f, -0.9f), new Vector3(0.4f, 0.26f, 0.45f), body, pitch: -12f);
            Cylinder("Fork", v, new Vector3(0.14f, 0.8f, 0.9f), 0.035f, 0.9f, p.Metal, Quaternion.Euler(-25f, 0f, 0f) * Vector3.up, mirror: true);
            Cylinder("Handlebar", v, new Vector3(0f, 1.25f, 0.72f), 0.035f, 0.95f, p.Chassis, Vector3.right);
            Piece(PrimitiveType.Sphere, "Headlight", v, new Vector3(0f, 1.1f, 1.02f), Vector3.zero, Vector3.one * 0.22f, p.Light);
            Piece(PrimitiveType.Sphere, "Fender", v, new Vector3(0f, 0.82f, 1.05f), Vector3.zero, new Vector3(0.28f, 0.12f, 0.8f), body);
            Cylinder("Exhaust", v, new Vector3(0.28f, 0.5f, -0.6f), 0.08f, 0.9f, p.Metal, Vector3.forward);
            Wheel(v, new Vector3(0f, 0.42f, 1.05f), 0.42f, 0.2f, accent, p);
            Wheel(v, new Vector3(0f, 0.42f, -0.95f), 0.42f, 0.24f, accent, p);
        }

        /// <summary>The scooter: small wheels, front shield, round back and an upright driver.</summary>
        static void Scooter(Transform v, Material body, Material accent, Parts p)
        {
            Box("Floorboard", v, new Vector3(0f, 0.4f, 0.1f), new Vector3(0.55f, 0.14f, 1.1f), body);
            Box("Shield", v, new Vector3(0f, 0.85f, 0.75f), new Vector3(0.65f, 0.95f, 0.14f), body, pitch: -15f);
            Piece(PrimitiveType.Sphere, "Back", v, new Vector3(0f, 0.62f, -0.6f), Vector3.zero, new Vector3(0.75f, 0.62f, 1.05f), body);
            Box("Seat", v, new Vector3(0f, 0.95f, -0.55f), new Vector3(0.48f, 0.14f, 0.75f), p.Seat);
            Cylinder("Column", v, new Vector3(0f, 1.15f, 0.82f), 0.05f, 0.6f, p.Metal, Quaternion.Euler(-15f, 0f, 0f) * Vector3.up);
            Cylinder("Handlebar", v, new Vector3(0f, 1.45f, 0.75f), 0.04f, 0.8f, accent, Vector3.right);
            Piece(PrimitiveType.Sphere, "Headlight", v, new Vector3(0f, 1.32f, 0.86f), Vector3.zero, Vector3.one * 0.2f, p.Light);
            Piece(PrimitiveType.Sphere, "Fender", v, new Vector3(0f, 0.52f, 0.88f), Vector3.zero, new Vector3(0.32f, 0.22f, 0.6f), body);
            Wheel(v, new Vector3(0f, 0.3f, 0.88f), 0.3f, 0.22f, accent, p);
            Wheel(v, new Vector3(0f, 0.3f, -0.8f), 0.3f, 0.26f, accent, p);
        }

        // ------------------------------------------------------------------ shared pieces

        /// <summary>Black tyre with a painted hub sticking out a little on the outer side.</summary>
        static void Wheel(Transform v, Vector3 position, float radius, float width, Material hub, Parts p, bool mirror = false)
        {
            Cylinder("Tyre", v, position, radius, width, p.Tyre, Vector3.right, mirror);
            float side = position.x == 0f ? 0f : Mathf.Sign(position.x) * 0.02f;
            Cylinder("Hub", v, position + new Vector3(side, 0f, 0f), radius * 0.5f, width + 0.04f, hub, Vector3.right, mirror);
        }

        static void SteeringWheel(Transform v, Vector3 position, Parts p)
        {
            Cylinder("SteeringWheel", v, position, 0.22f, 0.05f, p.Chassis, Quaternion.Euler(-55f, 0f, 0f) * Vector3.up);
            Cylinder("SteeringColumn", v, position + new Vector3(0f, -0.2f, 0.2f), 0.035f, 0.5f, p.Metal, Quaternion.Euler(-55f, 0f, 0f) * Vector3.up);
        }

        // ------------------------------------------------------------------ helpers

        static void Box(string name, Transform parent, Vector3 position, Vector3 size, Material material,
                        float pitch = 0f, bool mirror = false) =>
            Piece(PrimitiveType.Cube, name, parent, position, new Vector3(pitch, 0f, 0f), size, material, mirror);

        /// <summary>Unity's cylinder is 2 units tall along its Y; this one points along `axis`.</summary>
        static void Cylinder(string name, Transform parent, Vector3 position, float radius, float length,
                             Material material, Vector3 axis, bool mirror = false)
        {
            Vector3 euler = Quaternion.FromToRotation(Vector3.up, axis).eulerAngles;
            Piece(PrimitiveType.Cylinder, name, parent, position, euler, new Vector3(radius * 2f, length / 2f, radius * 2f), material, mirror);
        }
    }
}
