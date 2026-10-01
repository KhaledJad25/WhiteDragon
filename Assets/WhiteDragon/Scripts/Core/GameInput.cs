using UnityEngine;
using UnityEngine.InputSystem;

namespace WhiteDragon
{
    /// <summary>All keyboard and mouse actions, created in code on first use.</summary>
    public static class GameInput
    {
        static InputAction move, look, jump, fire, interact, toggleCursor, toggleDebug, restart;

        public static InputAction Move { get { Ensure(); return move; } }
        public static InputAction Look { get { Ensure(); return look; } }
        public static InputAction Jump { get { Ensure(); return jump; } }
        public static InputAction Fire { get { Ensure(); return fire; } }
        public static InputAction Interact { get { Ensure(); return interact; } }
        public static InputAction ToggleCursor { get { Ensure(); return toggleCursor; } }
        public static InputAction ToggleDebug { get { Ensure(); return toggleDebug; } }
        public static InputAction Restart { get { Ensure(); return restart; } }

        static void Ensure()
        {
            if (move != null) return;

            move = new InputAction("Move", InputActionType.Value);
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            look = new InputAction("Look", InputActionType.Value, "<Mouse>/delta");
            jump = new InputAction("Jump", InputActionType.Button, "<Keyboard>/space");
            fire = new InputAction("Fire", InputActionType.Button, "<Mouse>/leftButton");
            interact = new InputAction("Interact", InputActionType.Button, "<Keyboard>/e");
            toggleCursor = new InputAction("ToggleCursor", InputActionType.Button, "<Keyboard>/escape");
            toggleDebug = new InputAction("ToggleDebug", InputActionType.Button, "<Keyboard>/f1");
            restart = new InputAction("Restart", InputActionType.Button, "<Keyboard>/r");

            foreach (var a in All()) a.Enable();
        }

        static InputAction[] All() => new[] { move, look, jump, fire, interact, toggleCursor, toggleDebug, restart };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            if (move != null)
                foreach (var a in All()) a.Dispose();
            move = look = jump = fire = interact = toggleCursor = toggleDebug = restart = null;
        }
    }
}
