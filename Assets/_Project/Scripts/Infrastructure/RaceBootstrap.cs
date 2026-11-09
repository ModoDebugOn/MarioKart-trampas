using System.Collections.Generic;
using KartRace.Application;
using KartRace.Application.UseCases;
using KartRace.Domain.Cheats;
using KartRace.Domain.Driving;
using KartRace.Domain.Items;
using KartRace.Domain.Race;
using KartRace.Infrastructure.Profiles;
using KartRace.Presentation;
using UnityEngine;

namespace KartRace.Infrastructure
{
    /// <summary>
    /// Composition root of the race: puts every kart in the standings, gives each player theirs
    /// (one, or two on a split screen) and the AI the rest, ties the rivals to the players with the
    /// rubber band (who chases whom, the chase rule it injects), lays the item boxes on the track,
    /// and points a camera and a HUD at each player.
    /// Keys: G switches the rubber band on or off; 1, 2, 3 choose easy, normal, hard; C the chase rule;
    /// B, R, Z switch the layers of its drawing (bands, pace, zone); O the item odds; T the AI's route;
    /// K the bumps between karts; V the cameras look back at the pack;
    /// E uses player 1's item, Enter player 2's.
    /// </summary>
    public sealed class RaceBootstrap : MonoBehaviour
    {
        [SerializeField] Character player = Character.Mario;
        [Tooltip("A second player on the same screen, split in two (keys: arrows and Enter, or the second gamepad).")]
        [SerializeField] bool twoPlayers;
        [SerializeField] Character secondPlayer = Character.Luigi;
        [SerializeField] TrackPath track;
        [SerializeField] ChaseCamera chaseCamera;
        [Tooltip("Player 1's HUD: race, item roulette and item odds on one object of its own, which is copied for player 2.")]
        [SerializeField] RaceHud hud;
        [SerializeField] RubberBandDrawing rubberBandDrawing;
        [SerializeField] AiRouteDrawing routeDrawing;
        [Tooltip("Karts bump into each other (key K).")]
        [SerializeField] bool collisions = true;
        [Tooltip("The best rival has the difficulty's skill; the worst is this much slower: 0.06 = 6 %.")]
        [SerializeField, Range(0f, 0.2f)] float skillSpread = 0.06f;
        [Tooltip("Metres of track the pack strings out over with the band on: each rival has its own slack " +
                 "before the band pulls, so they line up behind you (or ahead) instead of in a clump.")]
        [SerializeField, Range(0f, 400f)] float packLength = 160f;
        [Tooltip("Metres past the band's Lead the best rival may get before it waits for you (the rest, less). " +
                 "Small, so a rival that overtakes you does not run away.")]
        [SerializeField, Range(0f, 200f)] float packAhead = 30f;

        [Header("Rubber band")]
        [Tooltip("The rivals speed up when far behind you and hold back when far ahead (key G).")]
        [SerializeField] bool rubberBand = true;
        [Tooltip("Easy, normal and hard, in that order (keys 1, 2, 3).")]
        [SerializeField] DifficultyProfile[] difficulties;
        [SerializeField] int difficulty = 1;
        [Tooltip("With two players, who the rivals chase (key C). With one, they all chase you.")]
        [SerializeField] Chase chase = Chase.Leader;

        /// <summary>The chase rules of the Domain, as a choice for the inspector.</summary>
        public enum Chase { Leader, Nearest, Average, Split }

        // In the order of the enum: the rule, and how the HUD says it ("Persiguen …").
        static readonly (IChaseRule Rule, string Name)[] ChaseRules =
        {
            (new ChaseLeader(), "al primero"),
            (new ChaseNearest(), "al más cercano"),
            (new ChaseAverage(), "a la media"),
            (new SplitPack(), "mitad y mitad"),
        };

        [Header("Players")]
        [Tooltip("The players' karts drive themselves (the cameras still follow them).")]
        [SerializeField] bool autopilot = true;
        [Tooltip("How fast player 1's autopilot drives compared with a normal kart: below 1 to see the rivals wait.")]
        [SerializeField, Range(0.5f, 1.2f)] float autopilotPace = 1f;
        [Tooltip("The same for player 2: lower, so one player leads and the other trails (0.93: about 100 m lost per lap).")]
        [SerializeField, Range(0.5f, 1.2f)] float secondAutopilotPace = 0.93f;
        [Tooltip("With the autopilot on, seconds a player holds their item before using it.")]
        [SerializeField, Range(0.5f, 10f)] float autopilotItemDelay = 2f;

        [Header("Items")]
        [SerializeField] ItemBoxView itemBoxPrefab;
        [Tooltip("Metres along the lap of each row of boxes (on the straights).")]
        [SerializeField] float[] itemRows = { 120f, 380f, 740f };
        [SerializeField, Range(1, 7)] int boxesPerRow = 5;
        [Tooltip("Metres between the boxes of a row.")]
        [SerializeField, Range(1f, 5f)] float boxSpacing = 3f;
        [Tooltip("Metres from a box at which a kart takes it.")]
        [SerializeField, Range(0.5f, 3f)] float boxRadius = 1.6f;
        [Tooltip("Seconds a box takes to come back.")]
        [SerializeField, Range(0.5f, 10f)] float boxRespawn = 2f;
        [Tooltip("Seed of the roulette: the same seed gives the same items every take.")]
        [SerializeField] int itemSeed = 1;

        /// <summary>A person at the wheel, with what goes with them: their kart, camera and HUD.</summary>
        sealed class Human
        {
            public int Index;
            public KartBody Kart;
            public Racer Racer;
            public PlayerDriver Driver;
            public Camera Camera;
            public RaceHud Hud;
            public ItemRouletteHud Roulette;
            public ItemOddsChart Odds;
            public float HeldFor;   // seconds holding an item (for the autopilot)
        }

        readonly List<Human> humans = new List<Human>();
        AiRoute route;
        Traffic traffic;
        AdvanceKarts advanceKarts;
        RubberBanding rubberBanding;
        AdvanceItems advanceItems;
        UseItem useItem;
        UseRivalItems useRivalItems;
        ItemBoxSensor itemSensor;
        DifficultyProfile applied;
        int appliedVersion = -1;

        DifficultyProfile Current =>
            difficulties != null && difficulties.Length > 0 ? difficulties[Mathf.Clamp(difficulty, 0, difficulties.Length - 1)] : null;

        void Awake()
        {
            KartBody[] karts = FindObjectsByType<KartBody>();
            var race = gameObject.AddComponent<RaceTracker>();
            race.Begin(track, karts);

            // The AI's own route (a racing line with its width), where everyone is on it, and what
            // moves all the karts at once so they can bump. The circuit sits at the origin, so the
            // route and the karts share the same X, Z.
            route = AiRoute.RacingLine(track.Line, track.RoadWidth);
            traffic = new Traffic(route);
            var motions = new List<KartMotion>();
            foreach (KartBody kart in karts)
            {
                traffic.Add(kart.Motion);
                motions.Add(kart.Motion);
            }
            advanceKarts = new AdvanceKarts(motions, traffic);
            gameObject.AddComponent<KartSimulation>().Begin(advanceKarts, karts);
            if (routeDrawing != null) routeDrawing.Show(route, track.transform);

            BeginPlayers(race, karts);

            // The rivals chase the humans; who among them, the rule chosen in the inspector.
            rubberBanding = new RubberBanding(humans.ConvertAll(human => human.Racer), ChaseRules[(int)chase].Rule);
            if (rubberBandDrawing != null)
                rubberBandDrawing.Begin(rubberBanding, humans.ConvertAll(human => human.Kart.transform),
                                        track.Line, track.transform, track.RoadWidth);
            int rival = 0;
            foreach (KartBody kart in karts)
            {
                if (humans.Exists(human => human.Kart == kart)) continue;
                // Each rival drifts across the route at its own rhythm (golden angle: phases never line up);
                // how far it drifts is the difficulty's.
                AiPilot pilot = Pilot(kart, 0f, rival * 2.39996f);
                RubberBanding.Rival banded = rubberBanding.AddRival(race.RacerOf(kart), pilot);
                rival++;
                kart.gameObject.AddComponent<AiDriver>().Begin(pilot);
                if (rubberBandDrawing != null) rubberBandDrawing.AddRival(kart.transform, banded);
            }
            ApplyDifficulty();
            BeginItems(race, karts);
        }

        /// <summary>
        /// Player 1, and player 2 if there is one: their kart (driven by them, not by the AI), their
        /// camera and their HUD. With two players the camera and the HUD are copied and the screen
        /// is split; each HUD then lives in its player's half.
        /// </summary>
        void BeginPlayers(RaceTracker race, KartBody[] karts)
        {
            DemoInput.Players = twoPlayers ? 2 : 1;
            var characters = twoPlayers ? new[] { player, secondPlayer } : new[] { player };
            for (int i = 0; i < characters.Length; i++)
            {
                Character who = characters[i];
                KartBody kart = System.Array.Find(karts, k => k.Character == who);
                var human = new Human { Index = i, Kart = kart, Racer = race.RacerOf(kart) };
                human.Driver = kart.gameObject.AddComponent<PlayerDriver>();
                human.Driver.Player = i;
                human.Driver.Autopilot = Pilot(kart, 0f, 0f);   // on the racing line, but it overtakes too

                ChaseCamera chase = i == 0 ? chaseCamera : Copy(chaseCamera);
                if (chase != null)
                {
                    chase.Target = kart.transform;
                    human.Camera = chase.GetComponent<Camera>();
                }
                RaceHud view = i == 0 ? hud : CopyHud();
                if (view != null)
                {
                    human.Hud = view;
                    human.Roulette = view.GetComponent<ItemRouletteHud>();
                    human.Odds = view.GetComponent<ItemOddsChart>();
                    view.Show(race.Standings, human.Racer);
                }
                humans.Add(human);
            }

            // Each HUD is drawn by its player's camera, also with one player: the recording kit only
            // records what the cameras draw, and an overlay canvas is not drawn by any.
            foreach (Human human in humans)
            {
                if (human.Hud != null) human.Hud.ShowIn(human.Camera);
                if (human.Roulette != null) human.Roulette.ShowIn(human.Camera);
                if (human.Odds != null) human.Odds.ShowIn(human.Camera);
            }
            if (twoPlayers) gameObject.AddComponent<SplitScreen>().Show(humans[0].Camera, humans[1].Camera);
        }

        /// <summary>Player 2's HUD: a copy of player 1's object (race, roulette and odds).</summary>
        RaceHud CopyHud()
        {
            if (hud == null) return null;
            if (hud.gameObject == gameObject)
            {
                Debug.LogWarning("[RaceBootstrap] The HUD shares the race's object, so it cannot be copied for player 2. " +
                                 "Rebuild the scene (Tools/Kart Race/Build Race Scene).");
                return null;
            }
            GameObject copy = Instantiate(hud.gameObject, hud.transform.parent);
            copy.name = "Player 2 HUD";
            return copy.GetComponent<RaceHud>();
        }

        /// <summary>Player 2's camera: a copy of player 1's, without its ears or its "main" tag.</summary>
        static ChaseCamera Copy(ChaseCamera camera)
        {
            if (camera == null) return null;
            ChaseCamera copy = Instantiate(camera);
            copy.name = "Player 2 Camera";
            copy.tag = "Untagged";
            AudioListener ears = copy.GetComponent<AudioListener>();
            if (ears != null) Destroy(ears);
            return copy;
        }

        /// <summary>
        /// The roulettes (the players' table for the humans, the AI's own for the rest), the rows of
        /// boxes across the road, and what watches the karts drive through them.
        /// </summary>
        void BeginItems(RaceTracker race, KartBody[] karts)
        {
            float lap = track.Line.Length;
            var openItemBox = new OpenItemBox(race.Standings,
                new ItemRoulette(ItemTable.Mk8Players, lap), new ItemRoulette(ItemTable.Mk8Cpu, lap),
                humans.ConvertAll(human => human.Racer), new System.Random(itemSeed));

            var boxes = new List<ItemBox>();
            var spots = new List<(Vector3, ItemBox)>();
            var parent = new GameObject("Item Boxes").transform;
            foreach (float row in itemRows)
                for (int i = 0; i < boxesPerRow; i++)
                {
                    Vector3 at = track.WorldAt(row, (i - (boxesPerRow - 1) * 0.5f) * boxSpacing);
                    var box = new ItemBox(boxRespawn);
                    boxes.Add(box);
                    spots.Add((at, box));
                    if (itemBoxPrefab != null) Instantiate(itemBoxPrefab, at, Quaternion.identity, parent).Show(box);
                }

            var drivers = new List<(Transform, Racer)>();
            foreach (KartBody kart in karts) drivers.Add((kart.transform, race.RacerOf(kart)));
            itemSensor = new ItemBoxSensor(openItemBox, spots, drivers, boxRadius);
            advanceItems = new AdvanceItems(race.Standings, boxes);
            useItem = new UseItem();
            useRivalItems = new UseRivalItems(race.Standings, rubberBanding, useItem);
            foreach (Human human in humans)
            {
                if (human.Roulette != null) human.Roulette.Show(human.Racer);
                if (human.Odds != null) human.Odds.Show(openItemBox, race.Standings, human.Racer);
            }
        }

        // After the RaceTracker has updated the standings (it runs earlier).
        void LateUpdate()
        {
            if (DemoInput.ToggleRubberBand) rubberBand = !rubberBand;
            if (DemoInput.NextChaseRule && twoPlayers) chase = (Chase)(((int)chase + 1) % ChaseRules.Length);
            if (DemoInput.ToggleCollisions) collisions = !collisions;
            if (DemoInput.ToggleRoute && routeDrawing != null) routeDrawing.Visible = !routeDrawing.Visible;
            if (DemoInput.ToggleView)
                foreach (Human human in humans)
                    if (human.Camera != null && human.Camera.TryGetComponent(out ChaseCamera view)) view.LookBack = !view.LookBack;
            advanceKarts.Collisions = collisions;
            int chosen = DemoInput.Difficulty;
            if (chosen >= 0 && difficulties != null && chosen < difficulties.Length) difficulty = chosen;
            // A new level, or its numbers changed in the inspector.
            if (Current != applied || (Current != null && Current.Version != appliedVersion)) ApplyDifficulty();
            if (rubberBandDrawing != null)
            {
                if (DemoInput.ToggleBands) rubberBandDrawing.ShowBands = !rubberBandDrawing.ShowBands;
                if (DemoInput.TogglePace) rubberBandDrawing.ShowPace = !rubberBandDrawing.ShowPace;
                if (DemoInput.ToggleZone) rubberBandDrawing.ShowZone = !rubberBandDrawing.ShowZone;
            }

            // Every frame, so the sliders work in Play mode.
            foreach (Human human in humans)
            {
                human.Driver.UseAutopilot = autopilot;
                human.Driver.Autopilot.Pace = human.Index == 0 ? autopilotPace : secondAutopilotPace;
                if (human.Hud != null && Current != null) human.Hud.ShowRubberBand(rubberBand, Current.DisplayName, Current.Color);
                if (human.Hud != null) human.Hud.ShowChase(twoPlayers && rubberBand ? ChaseRules[(int)chase].Name : null);
            }
            rubberBanding.Rule = ChaseRules[(int)chase].Rule;
            rubberBanding.Enabled = rubberBand;
            rubberBanding.SkillSpread = skillSpread;
            rubberBanding.PackLength = packLength;
            rubberBanding.PackAhead = packAhead;
            rubberBanding.Update();
            UpdateItems();
        }

        void UpdateItems()
        {
            advanceItems.Execute(Time.deltaTime);
            itemSensor.Detect();
            bool toggleOdds = DemoInput.ToggleOdds;

            foreach (Human human in humans)
            {
                if (toggleOdds && human.Odds != null) human.Odds.Visible = !human.Odds.Visible;
                // Each player uses theirs with their key; with the autopilot on, on their own after a moment.
                human.HeldFor = human.Racer.Slot.Held != null ? human.HeldFor + Time.deltaTime : 0f;
                if (DemoInput.UseItem(human.Index) || (autopilot && human.HeldFor >= autopilotItemDelay))
                    useItem.Execute(human.Racer);
            }
            // The AI uses its item when the difficulty's habits say so (what each item does comes later).
            useRivalItems.Execute(Time.deltaTime);
        }

        /// <summary>The same band with the numbers of the chosen level, and the rivals as good as it says.</summary>
        void ApplyDifficulty()
        {
            applied = Current;
            if (applied == null) return;
            appliedVersion = applied.Version;
            rubberBanding.Level = applied.CreateDifficulty();
        }

        /// <summary>
        /// An AI at the wheel of that kart: on the AI's route, watching the traffic, starting at
        /// the side of the route where the kart stands on the grid.
        /// </summary>
        AiPilot Pilot(KartBody kart, float wander, float phase) => new AiPilot(route, KartStats.Standard)
        {
            Traffic = traffic,
            Wander = wander,
            Phase = phase,
            Lateral = traffic.Of(kart.Motion).Lateral,
        };
    }
}
