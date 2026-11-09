using System.Collections.Generic;
using System.IO;
using KartRace.Domain.Track;
using KartRace.Infrastructure;
using KartRace.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static KartRace.Editor.BuildTools;
using Num = System.Numerics;

namespace KartRace.Editor
{
    /// <summary>
    /// Generates the circuit of the demo, all of it our own:
    /// - The centre line: a smooth closed spline through the control points below (Domain's TrackLine).
    /// - The road: one mesh built along that line (asphalt, white edge lines, red and white kerbs on
    ///   the corners, the chequered start line and the grid marks), plus grass and trees.
    /// - Prefab Circuit with a TrackPath that keeps the line for the race.
    /// - Scene 00_Circuito: the circuit and the 20 racers on the starting grid, one player.
    /// - Scene 01_DosJugadores: the same with two players on a split screen (Mario and Luigi).
    ///
    /// Flat on purpose: the video is about who is ahead and by how much, so the whole lap has to be
    /// readable from above. Change the control points and rebuild to get another layout.
    /// </summary>
    static class CircuitBuilder
    {
        const string Root = "Assets/_Project";
        const string EnvMaterials = Root + "/Materials/Environment";
        const string MeshFolder = Root + "/Meshes";
        const string EnvPrefabs = Root + "/Prefabs/Environment";
        const string CircuitPrefab = EnvPrefabs + "/Circuit.prefab";
        const string ScenePath = Root + "/Scenes/00_Circuito.unity";
        const string TwoPlayersScenePath = Root + "/Scenes/01_DosJugadores.unity";

        const float RoadWidth = 16f;      // 20 racers: room for two columns and overtaking
        const float Spacing = 2f;         // metres between samples of the centre line
        const float KerbWidth = 1.5f;
        const float KerbRadius = 55f;     // corners tighter than this get kerbs
        const int TreeCount = 220;

        // The layout seen from above (X, Z), in order of travel. The first point is the start line,
        // on the main straight, which goes towards +Z. About 1.5 km per lap: long enough for the gaps
        // to open, and three long straights where you can watch the rubber band pull them shut.
        static readonly Vector2[] ControlPoints =
        {
            new(0, 0), new(0, 60), new(0, 120), new(0, 180),                  // main straight
            new(12, 218), new(45, 240), new(90, 240),                         // turn 1, wide right-hander
            new(140, 236), new(200, 234), new(260, 232),                      // top straight
            new(300, 220), new(318, 190),                                     // turn 2
            new(310, 150), new(285, 120), new(290, 85), new(320, 55),         // the esses
            new(330, 10), new(325, -50), new(320, -110),                      // east straight
            new(300, -150), new(265, -160), new(240, -140),                   // hairpin
            new(230, -100), new(205, -70), new(170, -75),                     // back up and left
            new(140, -100), new(120, -140), new(100, -175), new(60, -185),    // the long left-right
            new(20, -175), new(-5, -150), new(-3, -110), new(0, -60),         // last corner, onto the straight
        };

        // Submeshes of the road, one material each.
        const int Asphalt = 0, White = 1, Red = 2, Black = 3;

        // ------------------------------------------------------------------ menu

        [MenuItem("Tools/Kart Race/Build Circuit")]
        static void BuildCircuit()
        {
            foreach (var folder in new[] { EnvMaterials, MeshFolder, EnvPrefabs }) EnsureFolder(folder);

            var controls = new Num.Vector2[ControlPoints.Length];
            for (int i = 0; i < controls.Length; i++) controls[i] = new Num.Vector2(ControlPoints[i].x, ControlPoints[i].y);
            TrackLine line = TrackLine.Smooth(controls, Spacing);

            var root = new GameObject("Circuit");
            root.AddComponent<TrackPath>().Store(line, RoadWidth);

            var road = new GameObject("Road");
            road.transform.SetParent(root.transform, false);
            Mesh mesh = SaveMesh(BuildRoadMesh(line), MeshFolder + "/CircuitRoad.asset");
            road.AddComponent<MeshFilter>().sharedMesh = mesh;
            road.AddComponent<MeshRenderer>().sharedMaterials = new[]
            {
                Mat(EnvMaterials, "Asphalt", new Color(0.32f, 0.33f, 0.36f), 0.1f),
                Mat(EnvMaterials, "LineWhite", new Color(0.96f, 0.96f, 0.94f), 0.1f),
                Mat(EnvMaterials, "KerbRed", new Color(0.85f, 0.12f, 0.1f), 0.1f),
                Mat(EnvMaterials, "LineBlack", new Color(0.1f, 0.1f, 0.11f), 0.1f),
            };
            road.AddComponent<MeshCollider>().sharedMesh = mesh;

            BuildGrass(root.transform, line);
            BuildTrees(root.transform, line);

            SavePrefab(root, CircuitPrefab);
            AssetDatabase.SaveAssets();
            Debug.Log($"[CircuitBuilder] Circuit generated: {line.Length:0} m per lap, {line.Count} samples.");
        }

        [MenuItem("Tools/Kart Race/Build Race Scene")]
        static void BuildRaceScene()
        {
            if ((File.Exists(ScenePath) || File.Exists(TwoPlayersScenePath)) &&
                !EditorUtility.DisplayDialog("Build Race Scene",
                    $"{ScenePath} and {TwoPlayersScenePath} will be overwritten. Go ahead?", "Overwrite", "Cancel"))
                return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            BuildRaceScenes();
        }

        /// <summary>The circuit, the racers, and the two race scenes: one player and two players.</summary>
        public static void BuildRaceScenes()
        {
            BuildCircuit();
            KartBuilder.BuildRacers();
            BuildScene(TwoPlayersScenePath, twoPlayers: true);
            BuildScene(ScenePath, twoPlayers: false);   // last: the one left open
        }

        static void BuildScene(string scenePath, bool twoPlayers)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var light = Object.FindAnyObjectByType<Light>();
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            light.intensity = 1.2f;
            light.shadows = LightShadows.Soft;

            // Distance haze so the grass melts into the sky instead of ending in a hard line.
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.78f, 0.86f, 0.93f);
            RenderSettings.fogStartDistance = 150f;
            RenderSettings.fogEndDistance = 450f;

            var circuit = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(CircuitPrefab));
            TrackPath path = circuit.GetComponent<TrackPath>();

            var racers = new GameObject("Racers").transform;
            IReadOnlyList<string> names = KartBuilder.RacerNames;
            for (int place = 0; place < names.Count; place++)
            {
                StartingGrid.Slot(place, out float distance, out float lateral);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{KartBuilder.RacerPrefabs}/{names[place]}.prefab");
                if (prefab == null) continue;
                var racer = (GameObject)PrefabUtility.InstantiatePrefab(prefab, racers);
                racer.transform.SetPositionAndRotation(OnTrack(path, distance, lateral), Facing(path, distance));
            }

            // Overview of the grid from behind the pack, with the first corner in the distance.
            var camera = Camera.main;
            camera.transform.position = OnTrack(path, -85f, 14f) + Vector3.up * 16f;
            camera.transform.LookAt(OnTrack(path, -15f, 0f));
            camera.fieldOfView = 50f;
            camera.farClipPlane = 600f;

            // In Play the camera chases your kart; in the editor it keeps the overview above.
            var chase = camera.gameObject.AddComponent<ChaseCamera>();
            var raceObject = new GameObject("Race");
            var race = new SerializedObject(raceObject.AddComponent<RaceBootstrap>());
            race.FindProperty("track").objectReferenceValue = path;
            race.FindProperty("chaseCamera").objectReferenceValue = chase;
            race.FindProperty("twoPlayers").boolValue = twoPlayers;
            // The HUD on an object of its own: with two players, player 2 gets a copy of it.
            var hud = new GameObject("HUD");
            hud.transform.SetParent(raceObject.transform, false);
            race.FindProperty("hud").objectReferenceValue = hud.AddComponent<RaceHud>();

            // The rubber band drawn over the race (unlit lines that take the colour we give them).
            var drawing = new SerializedObject(raceObject.AddComponent<RubberBandDrawing>());
            drawing.FindProperty("lineMaterial").objectReferenceValue = LineMaterial();
            drawing.ApplyModifiedPropertiesWithoutUndo();
            race.FindProperty("rubberBandDrawing").objectReferenceValue = drawing.targetObject;
            race.FindProperty("routeDrawing").objectReferenceValue = AddRouteDrawing(raceObject);
            race.ApplyModifiedPropertiesWithoutUndo();
            DifficultyBuilder.Assign((RaceBootstrap)race.targetObject, DifficultyBuilder.Build());
            ItemBoxBuilder.Assign((RaceBootstrap)race.targetObject);

            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log("[CircuitBuilder] Scene saved at " + scenePath);
        }

        /// <summary>The layer that draws the AI's route on the road (key T), with the debug line material.</summary>
        public static AiRouteDrawing AddRouteDrawing(GameObject raceObject)
        {
            var routeDrawing = new SerializedObject(raceObject.AddComponent<AiRouteDrawing>());
            routeDrawing.FindProperty("lineMaterial").objectReferenceValue = LineMaterial();
            routeDrawing.ApplyModifiedPropertiesWithoutUndo();
            return (AiRouteDrawing)routeDrawing.targetObject;
        }

        /// <summary>Material for the debug lines: unlit, coloured by each LineRenderer (Sprites/Default).</summary>
        static Material LineMaterial()
        {
            const string folder = Root + "/Materials/Debug", path = folder + "/Line.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            EnsureFolder(folder);
            material = new Material(Shader.Find("Sprites/Default"));
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static Vector3 OnTrack(TrackPath path, float distance, float lateral)
        {
            Num.Vector2 p = path.Line.PointAt(distance) + path.Line.RightAt(distance) * lateral;
            return path.WorldPoint(p);
        }

        static Quaternion Facing(TrackPath path, float distance)
        {
            Num.Vector2 d = path.Line.DirectionAt(distance);
            return Quaternion.LookRotation(path.transform.TransformDirection(new Vector3(d.X, 0f, d.Y)));
        }

        // ------------------------------------------------------------------ road mesh

        /// <summary>Vertices plus one triangle list per submesh. Every piece is a flat quad lying on
        /// the ground; a little height keeps lines and kerbs above the asphalt.</summary>
        sealed class MeshData
        {
            public readonly List<Vector3> Vertices = new();
            public readonly List<int>[] Triangles = { new(), new(), new(), new() };

            /// <summary>Quad between two slices of the track (a, d at the first; b, c at the next),
            /// from `inner` to `outer` metres to the right of the line. Clockwise seen from above.</summary>
            public void Quad(int submesh, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
            {
                int i = Vertices.Count;
                Vertices.Add(a); Vertices.Add(b); Vertices.Add(c); Vertices.Add(d);
                Triangles[submesh].AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
            }
        }

        static Mesh BuildRoadMesh(TrackLine line)
        {
            var m = new MeshData();
            int n = line.Count;
            float half = RoadWidth / 2f;

            // At every sample: where it is and which way is right (average of both segments, so the
            // edges of consecutive quads meet without gaps).
            var position = new Vector3[n];
            var right = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                position[i] = ToWorld(line[i]);
                Num.Vector2 dir = Num.Vector2.Normalize(line[(i + 1) % n] - line[(i - 1 + n) % n]);
                right[i] = new Vector3(dir.Y, 0f, -dir.X);
            }
            bool[] corner = Corners(position, n);

            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                void Strip(int submesh, float inner, float outer, float y) => m.Quad(submesh,
                    position[i] + right[i] * inner + Vector3.up * y, position[j] + right[j] * inner + Vector3.up * y,
                    position[j] + right[j] * outer + Vector3.up * y, position[i] + right[i] * outer + Vector3.up * y);

                Strip(Asphalt, -half, half, 0f);
                Strip(White, -half + 0.4f, -half + 0.75f, 0.01f);
                Strip(White, half - 0.75f, half - 0.4f, 0.01f);
                if (corner[i])
                {
                    // Red and white blocks, two samples (4 m) each.
                    int colour = (i / 2) % 2 == 0 ? Red : White;
                    Strip(colour, -half - KerbWidth, -half, 0.02f);
                    Strip(colour, half, half + KerbWidth, 0.02f);
                }
            }

            StartLine(m, line, half);
            GridMarks(m, line);

            var mesh = new Mesh { name = "CircuitRoad", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(m.Vertices);
            mesh.subMeshCount = m.Triangles.Length;
            for (int s = 0; s < m.Triangles.Length; s++) mesh.SetTriangles(m.Triangles[s], s);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Samples on a corner tighter than KerbRadius, widened a few samples each way so
        /// the kerbs start before the corner and end after it.</summary>
        static bool[] Corners(Vector3[] position, int n)
        {
            const int window = 3;   // compare the direction 3 samples before and after (12 m)
            var tight = new bool[n];
            for (int i = 0; i < n; i++)
            {
                Vector3 before = position[i] - position[(i - window + n) % n];
                Vector3 after = position[(i + window) % n] - position[i];
                float turn = Vector3.Angle(before, after) * Mathf.Deg2Rad;
                float length = (before.magnitude + after.magnitude) / 2f;
                tight[i] = turn > 0f && length / turn < KerbRadius;   // radius = arc / angle
            }
            var corner = new bool[n];
            for (int i = 0; i < n; i++)
                for (int k = -window; k <= window; k++)
                    corner[i] |= tight[(i + k + n) % n];
            return corner;
        }

        /// <summary>The chequered start line across the road at distance 0: two rows of 1 m squares.</summary>
        static void StartLine(MeshData m, TrackLine line, float half)
        {
            int cells = Mathf.RoundToInt(RoadWidth);
            for (int row = 0; row < 2; row++)
            for (int c = 0; c < cells; c++)
                Cell(m, (row + c) % 2 == 0 ? White : Black, line, row - 1f, row, -half + c, -half + c + 1f, 0.015f);
        }

        /// <summary>A short white bar in front of each place of the grid.</summary>
        static void GridMarks(MeshData m, TrackLine line)
        {
            for (int place = 0; place < KartBuilder.RacerNames.Count; place++)
            {
                StartingGrid.Slot(place, out float distance, out float lateral);
                float front = distance + 2f;   // just ahead of the kart's nose
                Cell(m, White, line, front, front + 0.35f, lateral - 1.2f, lateral + 1.2f, 0.015f);
            }
        }

        /// <summary>Quad between two distances along the line and two lateral offsets.</summary>
        static void Cell(MeshData m, int submesh, TrackLine line, float from, float to, float inner, float outer, float y)
        {
            Vector3 P(float distance, float lateral) =>
                ToWorld(line.PointAt(distance) + line.RightAt(distance) * lateral) + Vector3.up * y;
            m.Quad(submesh, P(from, inner), P(to, inner), P(to, outer), P(from, outer));
        }

        static Vector3 ToWorld(Num.Vector2 p) => new Vector3(p.X, 0f, p.Y);

        static Mesh SaveMesh(Mesh mesh, string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(mesh, path);
                return mesh;
            }
            // Keep the asset (and its GUID): copy the new data into it.
            existing.Clear();
            existing.indexFormat = mesh.indexFormat;
            existing.SetVertices(mesh.vertices);
            existing.subMeshCount = mesh.subMeshCount;
            for (int s = 0; s < mesh.subMeshCount; s++) existing.SetTriangles(mesh.GetTriangles(s), s);
            existing.RecalculateNormals();
            existing.RecalculateBounds();
            EditorUtility.SetDirty(existing);
            Object.DestroyImmediate(mesh);
            return existing;
        }

        // ------------------------------------------------------------------ surroundings

        /// <summary>A grass square under the whole circuit with some margin, just below the road.</summary>
        static void BuildGrass(Transform parent, TrackLine line)
        {
            Bounds bounds = new Bounds(ToWorld(line[0]), Vector3.zero);
            for (int i = 0; i < line.Count; i++) bounds.Encapsulate(ToWorld(line[i]));
            // Far beyond the fog: the skybox is not fogged, so the grass has to reach the horizon or
            // the sky's grey ground shows up behind it.
            const float margin = 3000f;
            var grass = GameObject.CreatePrimitive(PrimitiveType.Cube);
            grass.name = "Grass";
            grass.transform.SetParent(parent, false);
            grass.transform.localPosition = new Vector3(bounds.center.x, -0.06f, bounds.center.z);
            grass.transform.localScale = new Vector3(bounds.size.x + margin * 2f, 0.1f, bounds.size.z + margin * 2f);
            grass.GetComponent<Renderer>().sharedMaterial = Mat(EnvMaterials, "Grass", new Color(0.36f, 0.66f, 0.3f), 0.05f);
        }

        /// <summary>Trees scattered off the track (fixed seed: the same ones every build). On video
        /// they are what tells you how fast a kart goes.</summary>
        static void BuildTrees(Transform parent, TrackLine line)
        {
            var trunk = Mat(EnvMaterials, "Trunk", new Color(0.45f, 0.3f, 0.18f), 0.1f);
            var leaves = Mat(EnvMaterials, "Leaves", new Color(0.2f, 0.52f, 0.22f), 0.1f);
            var trees = new GameObject("Trees").transform;
            trees.SetParent(parent, false);

            // Anywhere around the circuit (its bounds plus a margin), but never on the road.
            Bounds area = new Bounds(ToWorld(line[0]), Vector3.zero);
            for (int i = 0; i < line.Count; i++) area.Encapsulate(ToWorld(line[i]));
            area.Expand(120f);

            var random = new System.Random(5);
            float clearance = RoadWidth / 2f + KerbWidth + 8f;
            int placed = 0;
            for (int attempt = 0; placed < TreeCount && attempt < TreeCount * 20; attempt++)
            {
                var p = new Num.Vector2(area.min.x + (float)random.NextDouble() * area.size.x,
                                        area.min.z + (float)random.NextDouble() * area.size.z);
                if (Num.Vector2.Distance(p, line.PointAt(line.DistanceAlong(p))) < clearance) continue;

                float size = 0.8f + (float)random.NextDouble() * 0.6f;
                var tree = new GameObject("Tree").transform;
                tree.SetParent(trees, false);
                tree.localPosition = ToWorld(p);
                Piece(PrimitiveType.Cylinder, "Trunk", tree, new Vector3(0f, 1.5f * size, 0f), Vector3.zero, new Vector3(0.6f, 1.5f, 0.6f) * size, trunk);
                Piece(PrimitiveType.Sphere, "Crown", tree, new Vector3(0f, 4f * size, 0f), Vector3.zero, new Vector3(3.6f, 3.2f, 3.6f) * size, leaves);
                placed++;
            }
        }
    }
}
