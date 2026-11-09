using System;
using System.IO;
using KartRace.Domain.Items;
using UnityEditor;
using UnityEngine;
using static KartRace.Editor.BuildTools;

namespace KartRace.Editor
{
    /// <summary>
    /// Draws the icon of every item (Textures/Items/{Item}.png, imported as sprites) out of plain
    /// shapes, so they are our own and can be regenerated after tweaking a number here.
    ///
    /// Each shape is a signed distance function: negative inside, positive outside, in units where
    /// the icon goes from -1 to 1 (y up). That gives smooth edges, a dark outline around every part
    /// (sticker style) and a white rim around the whole icon, so it reads on any background.
    /// </summary>
    static class ItemIconBuilder
    {
        const string Folder = "Assets/_Project/Textures/Items";
        const int Size = 256;
        const float Stroke = 0.045f;    // dark outline of each part
        const int RimPixels = 7;        // white rim around the whole icon

        static readonly Color Line = new Color(0.1f, 0.1f, 0.16f);
        static readonly Color Shine = new Color(1f, 1f, 1f, 0.5f);

        [MenuItem("Tools/Kart Race/Build Item Icons")]
        public static void BuildIcons()
        {
            EnsureFolder(Folder);
            foreach (Item item in Enum.GetValues(typeof(Item)))
            {
                var canvas = new Canvas(Size);
                Draw(item, canvas);
                canvas.Rim(Color.white, RimPixels);

                string path = $"{Folder}/{item}.png";
                File.WriteAllBytes(path, canvas.EncodeToPNG());
                AssetDatabase.ImportAsset(path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            Debug.Log($"[ItemIconBuilder] {Enum.GetValues(typeof(Item)).Length} item icons generated in {Folder}.");
        }

        public const string BoxFacePath = Folder + "/ItemBoxFace.png";

        /// <summary>
        /// The face of an item box: a see-through white square with a white rim and a big "?" in
        /// the middle. White, so the box can tint it with any colour of the rainbow.
        /// </summary>
        public static void BuildBoxFace()
        {
            EnsureFolder(Folder);
            var c = new Canvas(Size);
            c.At(Vector2.zero);
            c.Fill(p => Sdf.Box(p, Vector2.zero, new Vector2(0.94f, 0.94f), 0.2f), new Color(1f, 1f, 1f, 0.3f));
            c.Fill(p => Mathf.Abs(Sdf.Box(p, Vector2.zero, new Vector2(0.9f, 0.9f), 0.17f)) - 0.04f, Color.white);
            c.Sticker(Question, Color.white);

            File.WriteAllBytes(BoxFacePath, c.EncodeToPNG());
            AssetDatabase.ImportAsset(BoxFacePath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(BoxFacePath);
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
        }

        /// <summary>A question mark: a hook (most of a ring), a stem down to the middle and a dot.</summary>
        static float Question(Vector2 p)
        {
            var centre = new Vector2(0f, 0.24f);
            const float radius = 0.27f, thick = 0.1f, from = -60f, to = 200f;
            Vector2 d = p - centre;
            float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            if (angle < -90f) angle += 360f;   // so the hook's range, -60 … 200, is in one piece
            Vector2 end = centre + radius * Sdf.Dir(from);
            float hook = angle >= from && angle <= to
                ? Mathf.Abs(d.magnitude - radius) - thick
                : Mathf.Min((p - end).magnitude, (p - (centre + radius * Sdf.Dir(to))).magnitude) - thick;
            float stem = Sdf.Segment(p, end, new Vector2(0f, -0.2f), thick);
            float dot = Sdf.Circle(p, new Vector2(0f, -0.5f), 0.12f);
            return Mathf.Min(Mathf.Min(hook, stem), dot);
        }

        static void Draw(Item item, Canvas c)
        {
            switch (item)
            {
                case Item.Banana:          c.At(new Vector2(0f, 0.05f), 0.85f, -15f); Banana(c); break;
                case Item.TripleBanana:    Triple(c, -15f, () => Banana(c), 0.47f); break;
                case Item.GreenPuck:       c.At(Vector2.zero); GreenPuck(c); break;
                case Item.TripleGreenPuck: Triple(c, 0f, () => GreenPuck(c)); break;
                case Item.RedPuck:         c.At(Vector2.zero); RedPuck(c); break;
                case Item.TripleRedPuck:   Triple(c, 0f, () => RedPuck(c)); break;
                case Item.BlueBomb:        c.At(new Vector2(0f, -0.06f)); BlueBomb(c); break;
                case Item.Turbo:           c.At(Vector2.zero); Turbo(c, false); break;
                case Item.TripleTurbo:     Triple(c, 0f, () => Turbo(c, false)); break;
                case Item.GoldenTurbo:     c.At(Vector2.zero); Turbo(c, true); break;
                case Item.Star:            c.At(Vector2.zero); Star(c); break;
                case Item.Bullet:          c.At(new Vector2(0.06f, 0f), 0.88f, 20f); Bullet(c); break;
                case Item.Lightning:       c.At(Vector2.zero); Lightning(c); break;
                case Item.Bomb:            c.At(Vector2.zero); Bomb(c); break;
                case Item.Ink:             c.At(Vector2.zero); Ink(c); break;
                case Item.Coin:            c.At(Vector2.zero); Coin(c); break;
                case Item.Horn:            c.At(Vector2.zero); Horn(c); break;
            }
        }

        /// <summary>Three small copies: one at the back, two in front.</summary>
        static void Triple(Canvas c, float degrees, Action draw, float scale = 0.55f)
        {
            foreach (var at in new[] { new Vector2(0f, 0.34f), new Vector2(-0.4f, -0.3f), new Vector2(0.4f, -0.3f) })
            {
                c.At(at, scale, degrees);
                draw();
            }
        }

        // ------------------------------------------------------------------ items

        /// <summary>A curved fruit: an arc that is thick in the middle and thin at the ends.</summary>
        static void Banana(Canvas c)
        {
            var centre = new Vector2(0f, 0.85f);
            const float radius = 1.05f, from = 225f, to = 315f;
            Vector2 End(float degrees) => centre + radius * Sdf.Dir(degrees);
            float Thickness(float t) => 0.05f + 0.25f * Mathf.Pow(Mathf.Sin(Mathf.PI * t), 0.7f);
            float Body(Vector2 p)
            {
                Vector2 d = p - centre;
                float angle = Mathf.Repeat(Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg, 360f);
                float t = (angle - from) / (to - from);
                if (t >= 0f && t <= 1f) return Mathf.Abs(d.magnitude - radius) - Thickness(t);
                return Mathf.Min((p - End(from)).magnitude, (p - End(to)).magnitude) - 0.05f;
            }

            Vector2 stemBase = End(to);
            c.Sticker(p => Sdf.Segment(p, stemBase, stemBase + Sdf.Dir(45f) * 0.2f, 0.055f), new Color(0.48f, 0.33f, 0.14f));
            c.Sticker(Body, Sdf.Vertical(new Color(1f, 0.93f, 0.4f), new Color(0.95f, 0.66f, 0.1f), -0.25f, 0.15f));
            c.Fill(p => Sdf.Circle(p, End(from), 0.07f), new Color(0.32f, 0.2f, 0.1f));
            // Shine along the outer curve (only the bottom of that circle: the top is not the banana).
            c.Fill(p => Mathf.Max(Mathf.Max(Mathf.Abs((p - centre).magnitude - (radius + 0.1f)) - 0.03f, Mathf.Abs(p.x) - 0.35f),
                                  p.y - centre.y + 0.5f), Shine);
        }

        /// <summary>A hockey puck seen from above at an angle: dark side, light top with a symbol.</summary>
        static void Puck(Canvas c, Color side, Color top)
        {
            float Top(Vector2 p) => Sdf.Ellipse(p, new Vector2(0f, 0.18f), 0.78f, 0.36f);
            float Whole(Vector2 p) => Mathf.Min(Top(p), Mathf.Min(Sdf.Ellipse(p, new Vector2(0f, -0.18f), 0.78f, 0.36f),
                                                                   Sdf.Box(p, Vector2.zero, new Vector2(0.78f, 0.18f))));
            c.Sticker(Whole, Sdf.Vertical(side, Color.Lerp(side, Color.black, 0.3f), -0.55f, 0f));
            c.Sticker(Top, Sdf.Vertical(top, Color.Lerp(top, side, 0.4f), -0.2f, 0.55f));
            c.Fill(p => Sdf.Ellipse(p, new Vector2(-0.3f, 0.3f), 0.2f, 0.07f, -10f), Shine);
        }

        static void GreenPuck(Canvas c)
        {
            Puck(c, new Color(0.1f, 0.5f, 0.2f), new Color(0.35f, 0.85f, 0.4f));
            // Arrow: it goes straight.
            c.Fill(p => Mathf.Min(Sdf.Polygon(p, ArrowHead), Sdf.Box(p, new Vector2(-0.12f, 0.17f), new Vector2(0.2f, 0.05f))), Color.white);
        }

        static readonly Vector2[] ArrowHead = { new(0.42f, 0.17f), new(0.06f, 0.33f), new(0.06f, 0.01f) };

        static void RedPuck(Canvas c)
        {
            Puck(c, new Color(0.62f, 0.08f, 0.08f), new Color(1f, 0.3f, 0.25f));
            // Target: it homes in on someone.
            c.Fill(p => Mathf.Abs(Sdf.Ellipse(p, new Vector2(0f, 0.18f), 0.46f, 0.2f)) - 0.035f, Color.white);
            c.Fill(p => Sdf.Ellipse(p, new Vector2(0f, 0.18f), 0.13f, 0.06f), Color.white);
        }

        /// <summary>A winged blue bomb: it flies over everyone to the leader.</summary>
        static void BlueBomb(Canvas c)
        {
            var wing = Sdf.Vertical(Color.white, new Color(0.75f, 0.8f, 0.9f), 0f, 0.5f);
            foreach (float side in new[] { -1f, 1f })
            {
                c.Sticker(p => Sdf.Ellipse(p, new Vector2(0.52f * side, 0.3f), 0.32f, 0.17f, -25f * side), wing);
                c.Sticker(p => Sdf.Ellipse(p, new Vector2(0.55f * side, 0.12f), 0.26f, 0.12f, -8f * side), wing);
            }
            float Body(Vector2 p) => Sdf.Circle(p, new Vector2(0f, -0.08f), 0.52f);
            c.Sticker(Body, Sdf.Vertical(new Color(0.4f, 0.65f, 1f), new Color(0.08f, 0.22f, 0.7f), -0.6f, 0.45f));
            c.Fill(p => Mathf.Max(Body(p), Mathf.Abs(p.y + 0.12f) - 0.07f), new Color(0.06f, 0.15f, 0.5f));
            c.Fill(p => Sdf.Ellipse(p, new Vector2(-0.2f, 0.17f), 0.15f, 0.09f, 35f), Shine);
            Fuse(c, new Vector2(0f, 0.48f), 0f);
        }

        /// <summary>A canister of nitro: one boost (or a golden one that lasts).</summary>
        static void Turbo(Canvas c, bool golden)
        {
            Color light = golden ? new Color(1f, 0.92f, 0.45f) : new Color(1f, 0.62f, 0.2f);
            Color dark = golden ? new Color(0.85f, 0.58f, 0.08f) : new Color(0.9f, 0.3f, 0.08f);
            float Body(Vector2 p) => Sdf.Box(p, new Vector2(0f, -0.14f), new Vector2(0.36f, 0.56f), 0.14f);

            c.Sticker(p => Sdf.Box(p, new Vector2(0f, 0.5f), new Vector2(0.14f, 0.1f), 0.03f), new Color(0.7f, 0.72f, 0.76f));
            c.Sticker(p => Sdf.Box(p, new Vector2(0f, 0.63f), new Vector2(0.21f, 0.06f), 0.03f), golden ? dark : new Color(0.85f, 0.12f, 0.1f));
            c.Sticker(Body, Sdf.Vertical(light, dark, -0.7f, 0.4f));
            // White label with two chevrons pointing up: "faster".
            c.Fill(p => Mathf.Max(Body(p), Mathf.Abs(p.y + 0.14f) - 0.24f), Color.white);
            foreach (float y in new[] { -0.3f, -0.12f })
                c.Fill(p => Mathf.Min(Sdf.Segment(p, new Vector2(-0.17f, y), new Vector2(0f, y + 0.14f), 0.05f),
                                      Sdf.Segment(p, new Vector2(0f, y + 0.14f), new Vector2(0.17f, y), 0.05f)), dark);
            c.Fill(p => Sdf.Box(p, new Vector2(-0.22f, -0.14f), new Vector2(0.045f, 0.42f), 0.04f), Shine);
            if (golden)
                foreach (var (at, size) in new[] { (new Vector2(0.55f, 0.5f), 0.16f), (new Vector2(-0.55f, 0.15f), 0.11f), (new Vector2(0.58f, -0.45f), 0.12f) })
                    c.Sticker(Sparkle(at, size), new Color(1f, 0.97f, 0.7f));
        }

        static void Star(Canvas c)
        {
            Vector2[] points = Sdf.StarPoints(new Vector2(0f, -0.04f), 0.8f, 0.37f, 5);
            // Rainbow from top to bottom.
            c.Sticker(p => Sdf.Polygon(p, points) - 0.05f,
                      p => Color.HSVToRGB(Mathf.InverseLerp(0.85f, -0.85f, p.y) * 0.78f, 0.7f, 1f));
            c.Fill(p => Sdf.Ellipse(p, new Vector2(-0.18f, 0.3f), 0.12f, 0.2f, -30f), Shine);
            c.Sticker(Sparkle(new Vector2(0.68f, 0.62f), 0.14f), Color.white);
            c.Sticker(Sparkle(new Vector2(-0.7f, -0.58f), 0.11f), Color.white);
        }

        /// <summary>A big silver bullet with speed lines: it drives itself to the front.</summary>
        static void Bullet(Canvas c)
        {
            foreach (float y in new[] { -0.18f, 0.02f, 0.22f })
                c.Sticker(p => Sdf.Segment(p, new Vector2(-0.95f, y), new Vector2(-0.78f + Mathf.Abs(y) * 0.4f, y), 0.035f), Color.white);
            float Body(Vector2 p) => Mathf.Min(Sdf.Ellipse(p, new Vector2(0.3f, 0f), 0.42f, 0.3f),
                                               Sdf.Box(p, new Vector2(-0.12f, 0f), new Vector2(0.42f, 0.3f)));
            c.Sticker(Body, Sdf.Vertical(new Color(0.85f, 0.88f, 0.95f), new Color(0.38f, 0.42f, 0.52f), -0.3f, 0.3f));
            c.Fill(p => Mathf.Max(Body(p), Mathf.Abs(p.x - 0.05f) - 0.06f), new Color(0.85f, 0.15f, 0.1f));
            c.Sticker(p => Sdf.Box(p, new Vector2(-0.58f, 0f), new Vector2(0.1f, 0.34f), 0.03f),
                      Sdf.Vertical(new Color(1f, 0.82f, 0.35f), new Color(0.75f, 0.5f, 0.12f), -0.34f, 0.34f));
            c.Fill(p => Sdf.Box(p, new Vector2(0.02f, 0.17f), new Vector2(0.4f, 0.04f), 0.04f), Shine);
        }

        static void Lightning(Canvas c)
        {
            c.Sticker(p => Sdf.Polygon(p, Bolt), Sdf.Vertical(new Color(1f, 0.97f, 0.55f), new Color(1f, 0.62f, 0.08f), -0.9f, 0.9f));
            c.Fill(p => Sdf.Segment(p, new Vector2(0f, 0.8f), new Vector2(-0.22f, 0.05f), 0.04f), Shine);
        }

        static readonly Vector2[] Bolt =
        {
            new(-0.1f, 0.88f), new(0.42f, 0.88f), new(0.14f, 0.24f), new(0.46f, 0.24f),
            new(-0.25f, -0.9f), new(-0.03f, -0.06f), new(-0.38f, -0.06f),
        };

        static void Bomb(Canvas c)
        {
            c.Sticker(p => Sdf.Circle(p, new Vector2(0f, -0.12f), 0.58f),
                      Sdf.Vertical(new Color(0.38f, 0.38f, 0.46f), new Color(0.07f, 0.07f, 0.1f), -0.7f, 0.45f));
            c.Fill(p => Sdf.Ellipse(p, new Vector2(-0.22f, 0.14f), 0.16f, 0.1f, 35f), Shine);
            Fuse(c, new Vector2(0.27f, 0.42f), -30f);
        }

        /// <summary>A splat of ink with drips.</summary>
        static void Ink(Canvas c)
        {
            float Splat(Vector2 p)
            {
                float d = Sdf.Circle(p, new Vector2(0f, 0.08f), 0.46f);
                for (int i = 0; i < 8; i++)
                {
                    float angle = i * 45f + (i % 2) * 12f;
                    float reach = i % 2 == 0 ? 0.66f : 0.56f;
                    d = Sdf.SmoothUnion(d, Sdf.Circle(p, new Vector2(0f, 0.08f) + Sdf.Dir(angle) * reach, i % 2 == 0 ? 0.17f : 0.12f), 0.12f);
                }
                d = Sdf.SmoothUnion(d, Sdf.Segment(p, new Vector2(-0.22f, -0.3f), new Vector2(-0.22f, -0.72f), 0.07f), 0.1f);
                d = Sdf.SmoothUnion(d, Sdf.Circle(p, new Vector2(-0.22f, -0.76f), 0.1f), 0.05f);
                d = Sdf.SmoothUnion(d, Sdf.Segment(p, new Vector2(0.3f, -0.3f), new Vector2(0.3f, -0.6f), 0.06f), 0.1f);
                return d;
            }
            c.Sticker(Splat, Sdf.Vertical(new Color(0.42f, 0.28f, 0.55f), new Color(0.1f, 0.06f, 0.16f), -0.6f, 0.6f));
            c.Fill(p => Sdf.Ellipse(p, new Vector2(-0.18f, 0.3f), 0.14f, 0.08f, 30f), Shine);
            c.Fill(p => Sdf.Circle(p, new Vector2(0.12f, 0.38f), 0.05f), Shine);
        }

        static void Coin(Canvas c)
        {
            Color gold = new Color(1f, 0.85f, 0.25f), dark = new Color(0.82f, 0.55f, 0.08f);
            c.Sticker(p => Sdf.Ellipse(p, new Vector2(0.08f, 0f), 0.6f, 0.8f), dark);   // the edge, seen at an angle
            c.Sticker(p => Sdf.Ellipse(p, Vector2.zero, 0.6f, 0.8f), Sdf.Vertical(gold, Color.Lerp(gold, dark, 0.5f), -0.8f, 0.8f));
            c.Fill(p => Mathf.Abs(Sdf.Ellipse(p, Vector2.zero, 0.44f, 0.62f)) - 0.035f, dark);
            c.Fill(p => Sdf.Polygon(p, Diamond), dark);
            c.Fill(p => Sdf.Box(p, new Vector2(-0.3f, 0.2f), new Vector2(0.04f, 0.28f), 0.04f, -12f), Shine);
        }

        static readonly Vector2[] Diamond = { new(0f, 0.32f), new(0.2f, 0f), new(0f, -0.32f), new(-0.2f, 0f) };

        /// <summary>A brass horn with a rubber bulb, blasting sound waves.</summary>
        static void Horn(Canvas c)
        {
            var brass = Sdf.Vertical(new Color(1f, 0.86f, 0.4f), new Color(0.78f, 0.52f, 0.12f), -0.45f, 0.45f);
            c.At(new Vector2(-0.05f, 0f), 0.85f);
            foreach (float radius in new[] { 0.62f, 0.82f })
                c.Sticker(p => Mathf.Max(Mathf.Abs(Sdf.Circle(p, new Vector2(0.3f, 0f), radius)) - 0.035f,
                                         Mathf.Abs(p.y) - (p.x - 0.3f) * 0.85f), Color.white);
            c.Sticker(p => Sdf.Circle(p, new Vector2(-0.6f, 0f), 0.22f),
                      Sdf.Vertical(new Color(1f, 0.35f, 0.3f), new Color(0.6f, 0.06f, 0.06f), -0.22f, 0.22f));
            c.Sticker(p => Sdf.Polygon(p, Cone), brass);
            c.Sticker(p => Sdf.Ellipse(p, new Vector2(0.32f, 0f), 0.12f, 0.46f), brass);
            c.Fill(p => Sdf.Ellipse(p, new Vector2(0.35f, 0f), 0.07f, 0.36f), new Color(0.35f, 0.2f, 0.05f));
            c.Fill(p => Sdf.Segment(p, new Vector2(-0.3f, 0.1f), new Vector2(0.15f, 0.27f), 0.03f), Shine);
        }

        static readonly Vector2[] Cone = { new(-0.45f, 0.1f), new(0.3f, 0.44f), new(0.3f, -0.44f), new(-0.45f, -0.1f) };

        // ------------------------------------------------------------------ shared parts

        /// <summary>Metal cap, fuse and spark on top of a bomb.</summary>
        static void Fuse(Canvas c, Vector2 cap, float degrees)
        {
            Vector2 up = Sdf.Dir(90f + degrees);
            Vector2 tip = cap + up * 0.08f + Sdf.Dir(60f + degrees) * 0.2f;
            c.Sticker(p => Sdf.Segment(p, cap + up * 0.08f, tip, 0.035f), new Color(0.55f, 0.38f, 0.18f));
            c.Sticker(p => Sdf.Box(p, cap, new Vector2(0.15f, 0.08f), 0.03f, degrees), new Color(0.7f, 0.72f, 0.76f));
            c.Sticker(Sparkle(tip + Sdf.Dir(60f + degrees) * 0.06f, 0.15f), new Color(1f, 0.75f, 0.15f));
        }

        static Func<Vector2, float> Sparkle(Vector2 centre, float size)
        {
            Vector2[] points = Sdf.StarPoints(centre, size, size * 0.4f, 4);
            return p => Sdf.Polygon(p, points);
        }

        // ------------------------------------------------------------------ drawing

        /// <summary>The icon being drawn. Shapes are placed with At (offset, scale, turn): the same
        /// drawing code makes the single item and the three small copies of the triple.</summary>
        sealed class Canvas
        {
            readonly int size;
            readonly Color[] pixels;
            Vector2 offset;
            float scale = 1f, degrees;

            public Canvas(int size)
            {
                this.size = size;
                pixels = new Color[size * size];   // transparent
            }

            public void At(Vector2 offset, float scale = 1f, float degrees = 0f)
            {
                this.offset = offset;
                this.scale = scale;
                this.degrees = degrees;
            }

            public void Fill(Func<Vector2, float> shape, Color colour) => Fill(shape, _ => colour);

            /// <summary>Paints the inside of a shape, with smooth edges one pixel wide.</summary>
            public void Fill(Func<Vector2, float> shape, Func<Vector2, Color> paint)
            {
                float pixel = 2f / size;
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    var world = new Vector2((x + 0.5f) / size * 2f - 1f, (y + 0.5f) / size * 2f - 1f);
                    Vector2 local = Sdf.Rotate(world - offset, -degrees) / scale;
                    float coverage = Mathf.Clamp01(0.5f - shape(local) * scale / pixel);
                    if (coverage <= 0f) continue;
                    Color colour = paint(local);
                    colour.a *= coverage;
                    int i = y * size + x;
                    pixels[i] = Over(colour, pixels[i]);
                }
            }

            /// <summary>A part with a dark outline: the outline is the same shape a bit bigger.</summary>
            public void Sticker(Func<Vector2, float> shape, Color colour) => Sticker(shape, _ => colour);

            public void Sticker(Func<Vector2, float> shape, Func<Vector2, Color> paint)
            {
                float stroke = Stroke / scale;
                Fill(p => shape(p) - stroke, Line);
                Fill(shape, paint);
            }

            /// <summary>A rim of `colour` around everything drawn, `radius` pixels thick, underneath it.</summary>
            public void Rim(Color colour, int radius)
            {
                var alpha = new float[pixels.Length];
                for (int i = 0; i < pixels.Length; i++) alpha[i] = pixels[i].a;
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float near = 0f;
                    for (int dy = -radius; dy <= radius && near < 1f; dy++)
                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        int sx = x + dx, sy = y + dy;
                        if (sx < 0 || sy < 0 || sx >= size || sy >= size) continue;
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        near = Mathf.Max(near, alpha[sy * size + sx] * Mathf.Clamp01(radius + 0.5f - d));
                    }
                    var rim = new Color(colour.r, colour.g, colour.b, near);
                    int i = y * size + x;
                    pixels[i] = Over(pixels[i], rim);
                }
            }

            public byte[] EncodeToPNG()
            {
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                texture.SetPixels(pixels);
                texture.Apply();
                byte[] png = texture.EncodeToPNG();
                UnityEngine.Object.DestroyImmediate(texture);
                return png;
            }

            /// <summary>`top` painted over `bottom` (both with straight alpha).</summary>
            static Color Over(Color top, Color bottom)
            {
                float a = top.a + bottom.a * (1f - top.a);
                if (a <= 0f) return new Color(0f, 0f, 0f, 0f);
                Color rgb = (top * top.a + bottom * bottom.a * (1f - top.a)) / a;
                return new Color(rgb.r, rgb.g, rgb.b, a);
            }
        }

        /// <summary>Signed distance functions (negative inside) and paints.</summary>
        static class Sdf
        {
            public static Vector2 Dir(float degrees) =>
                new Vector2(Mathf.Cos(degrees * Mathf.Deg2Rad), Mathf.Sin(degrees * Mathf.Deg2Rad));

            public static Vector2 Rotate(Vector2 v, float degrees)
            {
                float s = Mathf.Sin(degrees * Mathf.Deg2Rad), c = Mathf.Cos(degrees * Mathf.Deg2Rad);
                return new Vector2(c * v.x - s * v.y, s * v.x + c * v.y);
            }

            public static float Circle(Vector2 p, Vector2 centre, float radius) => (p - centre).magnitude - radius;

            /// <summary>Approximate (exact on the short axis), plenty for smooth edges.</summary>
            public static float Ellipse(Vector2 p, Vector2 centre, float rx, float ry, float degrees = 0f)
            {
                Vector2 q = Rotate(p - centre, -degrees);
                return (new Vector2(q.x / rx, q.y / ry).magnitude - 1f) * Mathf.Min(rx, ry);
            }

            public static float Box(Vector2 p, Vector2 centre, Vector2 half, float round = 0f, float degrees = 0f)
            {
                Vector2 q = Rotate(p - centre, -degrees);
                q = new Vector2(Mathf.Abs(q.x), Mathf.Abs(q.y)) - half + Vector2.one * round;
                return Vector2.Max(q, Vector2.zero).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - round;
            }

            /// <summary>A capsule: the segment a-b, `radius` thick.</summary>
            public static float Segment(Vector2 p, Vector2 a, Vector2 b, float radius)
            {
                Vector2 ab = b - a;
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Vector2.Dot(ab, ab));
                return (p - (a + ab * t)).magnitude - radius;
            }

            /// <summary>Any closed polygon, convex or not (Inigo Quilez's polygon SDF).</summary>
            public static float Polygon(Vector2 p, Vector2[] v)
            {
                float d = Vector2.Dot(p - v[0], p - v[0]);
                float sign = 1f;
                for (int i = 0, j = v.Length - 1; i < v.Length; j = i, i++)
                {
                    Vector2 e = v[j] - v[i], w = p - v[i];
                    Vector2 b = w - e * Mathf.Clamp01(Vector2.Dot(w, e) / Vector2.Dot(e, e));
                    d = Mathf.Min(d, Vector2.Dot(b, b));
                    bool c1 = p.y >= v[i].y, c2 = p.y < v[j].y, c3 = e.x * w.y > e.y * w.x;
                    if ((c1 && c2 && c3) || (!c1 && !c2 && !c3)) sign = -sign;
                }
                return sign * Mathf.Sqrt(d);
            }

            /// <summary>Corners of a star with `tips` points, the first one pointing up.</summary>
            public static Vector2[] StarPoints(Vector2 centre, float outer, float inner, int tips)
            {
                var points = new Vector2[tips * 2];
                for (int k = 0; k < points.Length; k++)
                    points[k] = centre + Dir(90f + k * 180f / tips) * (k % 2 == 0 ? outer : inner);
                return points;
            }

            /// <summary>Union that melts the two shapes together over a distance k.</summary>
            public static float SmoothUnion(float a, float b, float k)
            {
                float h = Mathf.Clamp01(0.5f + 0.5f * (b - a) / k);
                return Mathf.Lerp(b, a, h) - k * h * (1f - h);
            }

            /// <summary>Gradient from `top` (at y = to) to `bottom` (at y = from), in the shape's own space.</summary>
            public static Func<Vector2, Color> Vertical(Color top, Color bottom, float from, float to) =>
                p => Color.Lerp(bottom, top, Mathf.InverseLerp(from, to, p.y));
        }
    }
}
