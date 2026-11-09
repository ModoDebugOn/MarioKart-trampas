using UnityEngine;
using UnityEngine.InputSystem;

namespace KartRace.Infrastructure
{
    /// <summary>
    /// Reads the keyboard (or the gamepads) for the demo controls. Driving is per player:
    /// - Player 1: W / S accelerate and brake, A / D steer, E uses the item; or the first gamepad.
    ///   Playing alone, the arrows and Enter work for player 1 too.
    /// - Player 2: arrows, Enter uses the item; or the second gamepad.
    /// - Gamepad: right trigger accelerates, left trigger brakes, left stick steers, A uses the item.
    /// For the whole demo:
    /// - G: rubber band on / off. 1, 2, 3: easy, normal, hard. C: who the rivals chase (two players).
    /// - Layers of the drawing: B bands, R pace, Z zone on the road, O item odds, T the AI's route.
    /// - K: bumps between karts on / off.
    /// - V: the cameras look ahead or back at the pack.
    /// </summary>
    public static class DemoInput
    {
        /// <summary>Optional input that replaces the keyboard while it is active.</summary>
        public static IInputOverride Override { get; set; }

        /// <summary>How many people play. Alone, player 1 also gets player 2's keys.</summary>
        public static int Players { get; set; } = 1;

        static bool Overridden => Override != null && Override.Active;

        static Keyboard Keys => Keyboard.current;

        /// <summary>That player's gamepad: alone, whichever was used last; two players, one each.</summary>
        static Gamepad Pad(int player) =>
            Players <= 1 ? Gamepad.current : player < Gamepad.all.Count ? Gamepad.all[player] : null;

        /// <summary>Whether that player uses the WASD side or the arrows side of the keyboard.</summary>
        static bool Wasd(int player) => player == 0;
        static bool Arrows(int player) => player == 1 || Players <= 1;

        /// <summary>1 accelerating, -1 braking, 0 hands off (or both at once).</summary>
        public static float Throttle(int player)
        {
            if (Overridden) return Override.Throttle(player);
            float throttle = 0f;
            if (Keys != null)
            {
                if ((Wasd(player) && Keys.wKey.isPressed) || (Arrows(player) && Keys.upArrowKey.isPressed)) throttle += 1f;
                if ((Wasd(player) && Keys.sKey.isPressed) || (Arrows(player) && Keys.downArrowKey.isPressed)) throttle -= 1f;
            }
            Gamepad pad = Pad(player);
            if (pad != null) throttle += pad.rightTrigger.ReadValue() - pad.leftTrigger.ReadValue();
            return Mathf.Clamp(throttle, -1f, 1f);
        }

        /// <summary>-1 left … 1 right.</summary>
        public static float Steer(int player)
        {
            if (Overridden) return Override.Steer(player);
            float steer = 0f;
            if (Keys != null)
            {
                if ((Wasd(player) && Keys.dKey.isPressed) || (Arrows(player) && Keys.rightArrowKey.isPressed)) steer += 1f;
                if ((Wasd(player) && Keys.aKey.isPressed) || (Arrows(player) && Keys.leftArrowKey.isPressed)) steer -= 1f;
            }
            Gamepad pad = Pad(player);
            if (pad != null) steer += pad.leftStick.x.ReadValue();
            return Mathf.Clamp(steer, -1f, 1f);
        }

        /// <summary>E (player 1) / Enter (player 2) / gamepad A: use your item.</summary>
        public static bool UseItem(int player)
        {
            if (Overridden) return Override.UseItem(player);
            Gamepad pad = Pad(player);
            return (Keys != null && ((Wasd(player) && Keys.eKey.wasPressedThisFrame) ||
                                     (Arrows(player) && (Keys.enterKey.wasPressedThisFrame || Keys.numpadEnterKey.wasPressedThisFrame))))
                || (pad != null && pad.buttonSouth.wasPressedThisFrame);
        }

        /// <summary>G: switch the rubber band on or off.</summary>
        public static bool ToggleRubberBand =>
            Overridden ? Override.ToggleRubberBand : Keys != null && Keys.gKey.wasPressedThisFrame;

        /// <summary>1 / 2 / 3: easy, normal, hard (returned as 0, 1, 2); -1 if none was pressed.</summary>
        public static int Difficulty
        {
            get
            {
                if (Overridden) return Override.Difficulty;
                if (Keys == null) return -1;
                if (Keys.digit1Key.wasPressedThisFrame) return 0;
                if (Keys.digit2Key.wasPressedThisFrame) return 1;
                if (Keys.digit3Key.wasPressedThisFrame) return 2;
                return -1;
            }
        }

        /// <summary>B: the bands from you to every rival, on or off.</summary>
        public static bool ToggleBands =>
            Overridden ? Override.ToggleBands : Keys != null && Keys.bKey.wasPressedThisFrame;

        /// <summary>R: the pace (ritmo) over each rival, on or off.</summary>
        public static bool TogglePace =>
            Overridden ? Override.TogglePace : Keys != null && Keys.rKey.wasPressedThisFrame;

        /// <summary>Z: the zone on the road where the rivals want to be, on or off.</summary>
        public static bool ToggleZone =>
            Overridden ? Override.ToggleZone : Keys != null && Keys.zKey.wasPressedThisFrame;

        /// <summary>O: the chart with the odds of every item, on or off.</summary>
        public static bool ToggleOdds =>
            Overridden ? Override.ToggleOdds : Keys != null && Keys.oKey.wasPressedThisFrame;

        /// <summary>C: the next rule of who the rivals chase with two players.</summary>
        public static bool NextChaseRule =>
            Overridden ? Override.NextChaseRule : Keys != null && Keys.cKey.wasPressedThisFrame;

        /// <summary>T: the AI's route (trazada) on the road, on or off.</summary>
        public static bool ToggleRoute =>
            Overridden ? Override.ToggleRoute : Keys != null && Keys.tKey.wasPressedThisFrame;

        /// <summary>K: the bumps between karts, on or off.</summary>
        public static bool ToggleCollisions =>
            Overridden ? Override.ToggleCollisions : Keys != null && Keys.kKey.wasPressedThisFrame;

        /// <summary>V: the cameras look ahead (behind your kart) or back at the pack (high in front of it).</summary>
        public static bool ToggleView =>
            Overridden ? Override.ToggleView : Keys != null && Keys.vKey.wasPressedThisFrame;
    }
}
