namespace KartRace.Infrastructure
{
    /// <summary>
    /// Another source of input that can take over from the keyboard: the recording kit, a test...
    /// While it is Active, <see cref="DemoInput"/> ignores the real devices.
    /// Nothing in the demo uses it; it is there so other tools can drive the demo.
    /// Driving and items are per player (0 the first, 1 the second); the rest is for the whole demo.
    /// </summary>
    public interface IInputOverride
    {
        bool Active { get; }

        /// <summary>1 accelerate, 0 nothing, -1 brake / reverse.</summary>
        float Throttle(int player);

        /// <summary>-1 left … 1 right.</summary>
        float Steer(int player);

        /// <summary>True on the frame that player uses their item.</summary>
        bool UseItem(int player);

        /// <summary>True on the frame the rubber band is switched on or off.</summary>
        bool ToggleRubberBand { get; }

        /// <summary>Difficulty chosen this frame (0 easy, 1 normal, 2 hard), or -1 for none.</summary>
        int Difficulty { get; }

        /// <summary>True on the frame the bands layer is switched on or off.</summary>
        bool ToggleBands { get; }

        /// <summary>True on the frame the pace labels layer is switched on or off.</summary>
        bool TogglePace { get; }

        /// <summary>True on the frame the zone on the road is switched on or off.</summary>
        bool ToggleZone { get; }

        /// <summary>True on the frame the item odds chart is switched on or off.</summary>
        bool ToggleOdds { get; }

        /// <summary>True on the frame the next chase rule is chosen (who the rivals chase with two players).</summary>
        bool NextChaseRule { get; }

        /// <summary>True on the frame the AI's route on the road is switched on or off.</summary>
        bool ToggleRoute { get; }

        /// <summary>True on the frame the bumps between karts are switched on or off.</summary>
        bool ToggleCollisions { get; }

        /// <summary>True on the frame the cameras switch between looking ahead and looking back at the pack.</summary>
        bool ToggleView { get; }
    }
}
